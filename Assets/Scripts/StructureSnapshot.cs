using UnityEngine;

/// <summary>
/// Immutable snapshot of node positions at a point in time, with the compliance value.
/// Used by OptimizeSession to record history and support rollback.
///
/// Only node positions are captured — connectivity and loads are not expected to change
/// during an optimize session (add/delete requires switching to Build mode).
/// </summary>
[System.Serializable]
public class StructureSnapshot
{
    /// <summary>NetworkObject IDs of the nodes, in the order they were captured.</summary>
    public ulong[] nodeNetworkIds;

    /// <summary>World-space positions corresponding to nodeNetworkIds.</summary>
    public Vector3[] nodePositions;

    /// <summary>Support state for each node.</summary>
    public bool[] nodeIsSupport;

    /// <summary>Structural compliance C = Σ (F · u) at the time of capture.</summary>
    public float compliance;

    /// <summary>Time.time at capture (used for display only).</summary>
    public float timestamp;
}
