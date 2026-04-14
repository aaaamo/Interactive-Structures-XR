using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gradient-based structural optimizer for truss geometry.
/// Port of Python optimizer.py + metrics.py (_analytical_compliance_gradient).
///
/// Computation strategy: incremental/reactive.
///   ComputeGradient         — 1 FEM solve → gradient per free node (main thread, fast)
///   ComputeGhostPositions   — N gradient steps on a positions snapshot (main-thread coroutine)
///
/// Ghost computation uses an internal FEM solver (SolveWithPositions) that operates on
/// plain Vector3[] arrays instead of reading transform.position, so it never touches
/// the live scene during iteration.
/// </summary>
public static class TrussOptimizer
{
    // ------------------------------------------------------------------ //
    // Public API
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Compute the compliance gradient for each free, non-load node.
    /// Returns gradient Vector3 per node (move node in -gradient direction to reduce compliance).
    /// Requires a valid TrussAnalysisResult (errorMessage == null).
    ///
    /// Port of Python optimizer.py _analytical_compliance_gradient (lines 60-120).
    /// </summary>
    public static Dictionary<NodeBehaviour, Vector3> ComputeGradient(
        StructureData data,
        TrussAnalysisResult result,
        float E, float A)
    {
        var grad = new Dictionary<NodeBehaviour, Vector3>();
        if (result == null || result.errorMessage != null) return grad;

        int n = data.nodes.Count;

        // Build displacement array indexed by nodeIndexMap position
        var uGlobal = new Vector3[n];
        if (result.displacements != null)
        {
            foreach (var kvp in data.nodeIndexMap)
            {
                if (result.displacements.TryGetValue(kvp.Value, out Vector3 d))
                {
                    uGlobal[kvp.Value] = d;
                }
            }
        }

        var gradArr = new Vector3[n];

        // Load-bearing nodes are frozen (not moved by the optimizer)
        var loadNodeSet = BuildLoadNodeSet(data);

        // Accumulate gradient contributions from each member
        // Port of optimizer.py lines 77-116
        foreach (var edge in data.edges)
        {
            if (edge == null || edge.nodeA == null || edge.nodeB == null) continue;

            int p = data.nodeIndexMap[edge.nodeA];
            int q = data.nodeIndexMap[edge.nodeB];

            Vector3 posA = edge.nodeA.transform.position;
            Vector3 posB = edge.nodeB.transform.position;

            AccumulateGradient(ref gradArr[p], ref gradArr[q],
                posA, posB, uGlobal[p], uGlobal[q], E, A);
        }

        // Return only FREE, non-load nodes
        for (int i = 0; i < n; i++)
        {
            NodeBehaviour node = data.nodes[i];
            if (!node.isSupport && !loadNodeSet.Contains(node))
            {
                grad[node] = gradArr[i];
            }
        }

        return grad;
    }

    /// <summary>
    /// Run N gradient descent steps on a snapshot of positions (no scene writes).
    /// Returns final Vector3[] (indexed same as nodes[]).
    ///
    /// Call this on the main thread (or from a coroutine). It uses an internal FEM solver
    /// that reads only from the positions[] array, never from transform.position.
    /// </summary>
    public static Vector3[] ComputeGhostPositions(
        NodeBehaviour[] nodes,
        EdgeBehaviour[] edges,
        Vector3[] snapshot,
        float E, float A,
        int steps = 20,
        float stepSize = 0.01f)
    {
        var positions = (Vector3[])snapshot.Clone();

        // Pre-build fixed index set (supports + load nodes)
        var fixedIdx = new HashSet<int>();
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null) continue;
            if (nodes[i].isSupport) fixedIdx.Add(i);
            if (nodes[i].loads != null && nodes[i].loads.Count > 0) fixedIdx.Add(i);
        }

        // Connectivity map (node index → list of (other index, edge index)) — built once
        var connectivity = BuildConnectivity(nodes, edges);

        // Support and load vectors — built once
        var supportIndices = new List<int>();
        var nodeLoads = new Dictionary<int, Vector3>();
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null) continue;
            if (nodes[i].isSupport) supportIndices.Add(i);

            Vector3 totalLoad = Vector3.zero;
            if (nodes[i].loads != null)
            {
                foreach (var load in nodes[i].loads)
                {
                    if (load != null) totalLoad += load.direction.normalized * load.magnitude;
                }
            }
            if (totalLoad.magnitude > 0.001f)
            {
                nodeLoads[i] = totalLoad;
            }
        }

        if (supportIndices.Count == 0 || nodeLoads.Count == 0) return positions;

        for (int step = 0; step < steps; step++)
        {
            // FEM solve with current positions snapshot
            var (uGlobal, success) = SolveWithPositions(
                nodes, edges, positions, connectivity,
                supportIndices, nodeLoads, E, A);

            if (!success) break;

            // Accumulate full gradient per node
            var gradients = new Vector3[nodes.Length];
            for (int mi = 0; mi < edges.Length; mi++)
            {
                var edge = edges[mi];
                if (edge == null || edge.nodeA == null || edge.nodeB == null) { continue; }
                if (!TryGetIndex(nodes, edge.nodeA, out int p)) { continue; }
                if (!TryGetIndex(nodes, edge.nodeB, out int q)) { continue; }

                var gpDelta = Vector3.zero;
                var gqDelta = Vector3.zero;
                AccumulateGradientVec(ref gpDelta, ref gqDelta,
                    positions[p], positions[q], uGlobal[p], uGlobal[q], E, A);

                if (!fixedIdx.Contains(p)) { gradients[p] += gpDelta; }
                if (!fixedIdx.Contains(q)) { gradients[q] += gqDelta; }
            }

            // Normalize by max magnitude so stepSize is always in world-space units
            float maxMag = 0f;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!fixedIdx.Contains(i) && gradients[i].magnitude > maxMag)
                {
                    maxMag = gradients[i].magnitude;
                }
            }

            if (maxMag < 1e-20f) { break; }

            bool anyGrad = false;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (fixedIdx.Contains(i)) { continue; }
                positions[i] -= (stepSize / maxMag) * gradients[i];
                anyGrad = true;
            }

            if (!anyGrad) break;
        }

        return positions;
    }

    // ------------------------------------------------------------------ //
    // Internal FEM solver (positions-array variant — no transform reads)
    // ------------------------------------------------------------------ //

    private static (Vector3[] uGlobal, bool success) SolveWithPositions(
        NodeBehaviour[] nodes,
        EdgeBehaviour[] edges,
        Vector3[] positions,
        List<(int other, int edgeIdx)>[] connectivity,
        List<int> supportIndices,
        Dictionary<int, Vector3> nodeLoads,
        float E, float A)
    {
        int n = nodes.Length;
        int numDOF = n * 3;

        // Assemble global stiffness matrix
        var K = new float[numDOF, numDOF];
        for (int mi = 0; mi < edges.Length; mi++)
        {
            var edge = edges[mi];
            if (edge == null || edge.nodeA == null || edge.nodeB == null) continue;
            if (!TryGetIndex(nodes, edge.nodeA, out int p)) continue;
            if (!TryGetIndex(nodes, edge.nodeB, out int q)) continue;

            Vector3 delta = positions[q] - positions[p];
            float L = delta.magnitude;
            if (L < 1e-6f) continue;

            float k = E * A / L;
            float cx = delta.x / L, cy = delta.y / L, cz = delta.z / L;
            float[] c = { cx, cy, cz };

            int[] dofs = {
                p * 3, p * 3 + 1, p * 3 + 2,
                q * 3, q * 3 + 1, q * 3 + 2
            };
            float[] cv = { cx, cy, cz, -cx, -cy, -cz };

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    K[dofs[i], dofs[j]] += k * cv[i] * cv[j];
                }
            }
        }

        // Assemble force vector
        var F = new float[numDOF];
        foreach (var kvp in nodeLoads)
        {
            int idx = kvp.Key;
            F[idx * 3]     = kvp.Value.x;
            F[idx * 3 + 1] = kvp.Value.y;
            F[idx * 3 + 2] = kvp.Value.z;
        }

        // Partition into free/fixed DOFs
        var isFixed = new bool[numDOF];
        foreach (int si in supportIndices)
        {
            isFixed[si * 3]     = true;
            isFixed[si * 3 + 1] = true;
            isFixed[si * 3 + 2] = true;
        }

        var freeDOFs = new List<int>();
        for (int i = 0; i < numDOF; i++)
        {
            if (!isFixed[i]) freeDOFs.Add(i);
        }

        if (freeDOFs.Count == 0)
        {
            return (new Vector3[n], false);
        }

        int nFree = freeDOFs.Count;
        var Kff = new MatrixMxN(nFree, nFree);
        var Ff  = new float[nFree];
        for (int i = 0; i < nFree; i++)
        {
            Ff[i] = F[freeDOFs[i]];
            for (int j = 0; j < nFree; j++)
            {
                Kff.data[i, j] = K[freeDOFs[i], freeDOFs[j]];
            }
        }

        float[] uFree = MatrixSolver.SolveLinearSystem(Kff, Ff);
        if (uFree == null) return (new Vector3[n], false);

        var uFlat = new float[numDOF];
        for (int i = 0; i < nFree; i++)
        {
            uFlat[freeDOFs[i]] = uFree[i];
        }

        var uGlobal = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            uGlobal[i] = new Vector3(uFlat[i * 3], uFlat[i * 3 + 1], uFlat[i * 3 + 2]);
        }

        return (uGlobal, true);
    }

    // ------------------------------------------------------------------ //
    // Gradient math — shared between ComputeGradient and ghost loop
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Accumulate gradient contributions from one member onto gradArr[p] and gradArr[q].
    /// Port of optimizer.py lines 83-116.
    /// </summary>
    private static void AccumulateGradient(
        ref Vector3 gradP, ref Vector3 gradQ,
        Vector3 posA, Vector3 posB,
        Vector3 uA, Vector3 uB,
        float E, float A)
    {
        Vector3 delta = posB - posA;
        float L = delta.magnitude;
        if (L < 1e-6f) return;

        float cx = delta.x / L, cy = delta.y / L, cz = delta.z / L;
        float L2 = L * L;
        float EA = E * A;

        Vector3 Du = uB - uA;
        float[] DuArr = { Du.x, Du.y, Du.z };
        float[] cArr  = { cx,   cy,   cz   };

        float elongation = cx * Du.x + cy * Du.y + cz * Du.z;

        float gPx = 0f, gPy = 0f, gPz = 0f;
        for (int d = 0; d < 3; d++)
        {
            float dDeltaRaw = 0f;
            for (int e = 0; e < 3; e++)
            {
                dDeltaRaw += (cArr[d] * cArr[e] - (d == e ? 1f : 0f)) * DuArr[e];
            }

            float contrib = EA / L2 * (2f * elongation * dDeltaRaw + cArr[d] * elongation * elongation);

            if (d == 0) { gPx -= contrib; }
            else if (d == 1) { gPy -= contrib; }
            else             { gPz -= contrib; }
        }

        var gP = new Vector3(gPx, gPy, gPz);
        gradP += gP;
        gradQ -= gP; // grad[q] += contrib  ↔  -(grad[p] -= contrib)
    }

    // Variant that writes into separate out vars (used by ghost loop with positions[])
    private static void AccumulateGradientVec(
        ref Vector3 gradP, ref Vector3 gradQ,
        Vector3 posA, Vector3 posB,
        Vector3 uA, Vector3 uB,
        float E, float A)
    {
        AccumulateGradient(ref gradP, ref gradQ, posA, posB, uA, uB, E, A);
    }

    // ------------------------------------------------------------------ //
    // Utility helpers
    // ------------------------------------------------------------------ //

    private static HashSet<NodeBehaviour> BuildLoadNodeSet(StructureData data)
    {
        var set = new HashSet<NodeBehaviour>();
        for (int i = 0; i < data.nodes.Count; i++)
        {
            var node = data.nodes[i];
            if (node.loads != null && node.loads.Count > 0)
            {
                set.Add(node);
            }
        }
        return set;
    }

    /// <summary>
    /// Build per-node adjacency list: index → list of (neighbour index, edge array index).
    /// </summary>
    private static List<(int other, int edgeIdx)>[] BuildConnectivity(
        NodeBehaviour[] nodes, EdgeBehaviour[] edges)
    {
        var map = new Dictionary<NodeBehaviour, int>();
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null) map[nodes[i]] = i;
        }

        var conn = new List<(int, int)>[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            conn[i] = new List<(int, int)>();
        }

        for (int mi = 0; mi < edges.Length; mi++)
        {
            var edge = edges[mi];
            if (edge == null || edge.nodeA == null || edge.nodeB == null) continue;
            if (!map.TryGetValue(edge.nodeA, out int a)) continue;
            if (!map.TryGetValue(edge.nodeB, out int b)) continue;
            conn[a].Add((b, mi));
            conn[b].Add((a, mi));
        }
        return conn;
    }

    private static bool TryGetIndex(NodeBehaviour[] nodes, NodeBehaviour target, out int index)
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == target) { index = i; return true; }
        }
        index = -1;
        return false;
    }
}
