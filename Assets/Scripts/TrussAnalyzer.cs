
// ============================================================================
// FILE 3: TrussAnalyzer.cs
// Direct Stiffness Method implementation
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

public static class TrussAnalyzer
{
    public static TrussAnalysisResult AnalyzeTruss(StructureData data, float E, float A)
    {
        TrussAnalysisResult result = new TrussAnalysisResult();

        if (data.supportNodes.Count == 0)
        {
            result.errorMessage = "No support nodes! Structure is unstable.";
            return result;
        }

        int numNodes = data.nodes.Count;
        int numMembers = data.edges.Count;
        int numDOF = numNodes * 3;

        Debug.Log($"=== Starting Direct Stiffness Analysis ===");
        Debug.Log($"Nodes: {numNodes}, Members: {numMembers}, Total DOF: {numDOF}");

        // STEP 1: Compute member properties
        MemberProperty[] members = new MemberProperty[numMembers];
        for (int i = 0; i < numMembers; i++)
        {
            members[i] = ComputeMemberProperties(data.edges[i], data.nodeIndexMap);
        }

        // STEP 2: Build global stiffness matrix
        MatrixMxN K_global = new MatrixMxN(numDOF, numDOF);
        K_global.Zero();

        for (int i = 0; i < numMembers; i++)
        {
            MatrixMxN K_member = ComputeGlobalStiffness(members[i], E, A);
            AssembleGlobalStiffness(K_global, K_member, members[i]);
        }

        Debug.Log($"Global stiffness matrix assembled: {numDOF}x{numDOF}");

        // STEP 3: Build load vector
        float[] F = new float[numDOF];
        foreach (var kvp in data.nodeLoads)
        {
            int nodeIdx = kvp.Key;
            Vector3 load = kvp.Value;
            F[nodeIdx * 3 + 0] = load.x;
            F[nodeIdx * 3 + 1] = load.y;
            F[nodeIdx * 3 + 2] = load.z;
        }

        // STEP 4: Apply boundary conditions
        bool[] isFixed = new bool[numDOF];
        foreach (int supportIdx in data.supportNodes)
        {
            isFixed[supportIdx * 3 + 0] = true;
            isFixed[supportIdx * 3 + 1] = true;
            isFixed[supportIdx * 3 + 2] = true;
        }

        // STEP 5: Partition system
        List<int> freeDOFs = new List<int>();
        for (int i = 0; i < numDOF; i++)
        {
            if (!isFixed[i])
                freeDOFs.Add(i);
        }

        if (freeDOFs.Count == 0)
        {
            result.errorMessage = "All DOFs are constrained!";
            return result;
        }

        Debug.Log($"Free DOFs: {freeDOFs.Count}");

        int nFree = freeDOFs.Count;
        MatrixMxN K_ff = new MatrixMxN(nFree, nFree);
        float[] F_free = new float[nFree];

        for (int i = 0; i < nFree; i++)
        {
            F_free[i] = F[freeDOFs[i]];
            for (int j = 0; j < nFree; j++)
            {
                K_ff.data[i, j] = K_global.data[freeDOFs[i], freeDOFs[j]];
            }
        }

        // STEP 6: Solve for displacements
        float[] u_free = MatrixSolver.SolveLinearSystem(K_ff, F_free);

        if (u_free == null)
        {
            result.errorMessage = "Failed to solve system. Structure may be singular/unstable.";
            return result;
        }

        Debug.Log("Displacements computed successfully");

        float[] u_global = new float[numDOF];
        for (int i = 0; i < nFree; i++)
        {
            u_global[freeDOFs[i]] = u_free[i];
        }

        // STEP 7: Compute member forces
        result.memberForces = new float[numMembers];
        for (int i = 0; i < numMembers; i++)
        {
            result.memberForces[i] = ComputeMemberForce(members[i], u_global, E, A);
        }

        // STEP 8: Compute reaction forces
        result.reactions = new Dictionary<int, Vector3>();
        float[] F_reaction = MatrixMxN.Multiply(K_global, u_global);

        foreach (int supportIdx in data.supportNodes)
        {
            Vector3 reaction = new Vector3(
                F_reaction[supportIdx * 3 + 0] - F[supportIdx * 3 + 0],
                F_reaction[supportIdx * 3 + 1] - F[supportIdx * 3 + 1],
                F_reaction[supportIdx * 3 + 2] - F[supportIdx * 3 + 2]
            );
            result.reactions[supportIdx] = reaction;
        }

        Debug.Log("=== Analysis Complete ===");
        return result;
    }

    static MemberProperty ComputeMemberProperties(EdgeBehaviour edge, Dictionary<NodeBehaviour, int> nodeMap)
    {
        MemberProperty prop = new MemberProperty();
        prop.nodeA_idx = nodeMap[edge.nodeA];
        prop.nodeB_idx = nodeMap[edge.nodeB];
        Vector3 posA = edge.nodeA.transform.position;
        Vector3 posB = edge.nodeB.transform.position;
        Vector3 delta = posB - posA;
        prop.length = delta.magnitude;

        if (prop.length < 1e-6f)
        {
            Debug.LogWarning("Zero-length member detected!");
            prop.length = 1e-6f;
        }

        prop.cx = delta.x / prop.length;
        prop.cy = delta.y / prop.length;
        prop.cz = delta.z / prop.length;
        return prop;
    }

    static MatrixMxN ComputeGlobalStiffness(MemberProperty member, float E, float A)
    {
        float k = (E * A) / member.length;
        float cx = member.cx;
        float cy = member.cy;
        float cz = member.cz;

        MatrixMxN K = new MatrixMxN(6, 6);

        float cx2 = cx * cx;
        float cy2 = cy * cy;
        float cz2 = cz * cz;
        float cxcy = cx * cy;
        float cxcz = cx * cz;
        float cycz = cy * cz;

        K.data[0, 0] = k * cx2; K.data[0, 1] = k * cxcy; K.data[0, 2] = k * cxcz;
        K.data[1, 0] = k * cxcy; K.data[1, 1] = k * cy2; K.data[1, 2] = k * cycz;
        K.data[2, 0] = k * cxcz; K.data[2, 1] = k * cycz; K.data[2, 2] = k * cz2;

        K.data[0, 3] = -k * cx2; K.data[0, 4] = -k * cxcy; K.data[0, 5] = -k * cxcz;
        K.data[1, 3] = -k * cxcy; K.data[1, 4] = -k * cy2; K.data[1, 5] = -k * cycz;
        K.data[2, 3] = -k * cxcz; K.data[2, 4] = -k * cycz; K.data[2, 5] = -k * cz2;

        K.data[3, 0] = -k * cx2; K.data[3, 1] = -k * cxcy; K.data[3, 2] = -k * cxcz;
        K.data[4, 0] = -k * cxcy; K.data[4, 1] = -k * cy2; K.data[4, 2] = -k * cycz;
        K.data[5, 0] = -k * cxcz; K.data[5, 1] = -k * cycz; K.data[5, 2] = -k * cz2;

        K.data[3, 3] = k * cx2; K.data[3, 4] = k * cxcy; K.data[3, 5] = k * cxcz;
        K.data[4, 3] = k * cxcy; K.data[4, 4] = k * cy2; K.data[4, 5] = k * cycz;
        K.data[5, 3] = k * cxcz; K.data[5, 4] = k * cycz; K.data[5, 5] = k * cz2;

        return K;
    }

    static void AssembleGlobalStiffness(MatrixMxN K_global, MatrixMxN K_member, MemberProperty member)
    {
        int[] dofs = new int[6];
        dofs[0] = member.nodeA_idx * 3 + 0;
        dofs[1] = member.nodeA_idx * 3 + 1;
        dofs[2] = member.nodeA_idx * 3 + 2;
        dofs[3] = member.nodeB_idx * 3 + 0;
        dofs[4] = member.nodeB_idx * 3 + 1;
        dofs[5] = member.nodeB_idx * 3 + 2;

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                K_global.data[dofs[i], dofs[j]] += K_member.data[i, j];
            }
        }
    }

    static float ComputeMemberForce(MemberProperty member, float[] u_global, float E, float A)
    {
        float k = (E * A) / member.length;

        Vector3 u_A = new Vector3(
            u_global[member.nodeA_idx * 3 + 0],
            u_global[member.nodeA_idx * 3 + 1],
            u_global[member.nodeA_idx * 3 + 2]
        );

        Vector3 u_B = new Vector3(
            u_global[member.nodeB_idx * 3 + 0],
            u_global[member.nodeB_idx * 3 + 1],
            u_global[member.nodeB_idx * 3 + 2]
        );

        Vector3 lambda = new Vector3(member.cx, member.cy, member.cz);
        Vector3 delta_u = u_B - u_A;
        float axial_displacement = Vector3.Dot(lambda, delta_u);
        float force = k * axial_displacement;

        return force;
    }
}

public struct MemberProperty
{
    public int nodeA_idx;
    public int nodeB_idx;
    public float length;
    public float cx, cy, cz;
}
