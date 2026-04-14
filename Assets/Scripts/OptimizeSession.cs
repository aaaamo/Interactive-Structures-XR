using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Tracks the history of structural states during an optimize session.
/// Supports rollback to any previous snapshot and provides a compliance sparkline
/// for display in the HUD.
///
/// History persists across Optimize mode re-entry so the full session is visible.
/// Wire graphManager and worldCoordinateManager in the Inspector.
/// </summary>
public class OptimizeSession : MonoBehaviour
{
    [Header("References")]
    public GraphManager graphManager;
    public WorldCoordinateManager worldCoordinateManager;
    public OptimizeVisualizer optimizeVisualizer;

    [Header("Settings")]
    public int maxHistory = 20;

    // ------------------------------------------------------------------ //
    // State
    // ------------------------------------------------------------------ //

    private readonly List<StructureSnapshot> _history = new List<StructureSnapshot>();
    private int _bestIndex = -1;
    private int _cursorIndex = -1;

    // Sparkline characters ordered from low to high bar height
    private static readonly char[] SparkChars = { '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█' };
    private const int SparklineWidth = 15;

    // ------------------------------------------------------------------ //
    // Public read-only accessors
    // ------------------------------------------------------------------ //

    public IReadOnlyList<StructureSnapshot> History => _history;
    public int BestIndex => _bestIndex;
    public int CursorIndex => _cursorIndex;
    public bool HasHistory => _history.Count > 0;

    // ------------------------------------------------------------------ //
    // Recording
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Capture the current node positions and the given compliance value.
    /// Call this after each drag-end in Optimize mode (before OnStructureChanged).
    /// </summary>
    public void TakeSnapshot(NodeBehaviour[] nodes, float compliance)
    {
        var snap = new StructureSnapshot
        {
            nodeNetworkIds = new ulong[nodes.Length],
            nodePositions  = new Vector3[nodes.Length],
            nodeIsSupport  = new bool[nodes.Length],
            compliance     = compliance,
            timestamp      = Time.time,
        };

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null) { continue; }
            snap.nodeNetworkIds[i] = nodes[i].NetworkObjectId;
            snap.nodePositions[i]  = nodes[i].transform.position;
            snap.nodeIsSupport[i]  = nodes[i].isSupport;
        }

        // If the cursor is not at the end (user rolled back, then edited),
        // truncate the "future" history before adding the new snapshot.
        if (_cursorIndex >= 0 && _cursorIndex < _history.Count - 1)
        {
            _history.RemoveRange(_cursorIndex + 1, _history.Count - _cursorIndex - 1);
            _bestIndex = FindBestIndex();
        }

        // Trim oldest entry if at capacity
        if (_history.Count >= maxHistory)
        {
            _history.RemoveAt(0);
            if (_bestIndex > 0) { _bestIndex--; }
            else if (_bestIndex == 0) { _bestIndex = FindBestIndex(); }
        }

        _history.Add(snap);
        _cursorIndex = _history.Count - 1;

        // Update best
        if (_bestIndex < 0 || compliance < _history[_bestIndex].compliance)
        {
            _bestIndex = _history.Count - 1;
        }
    }

    /// <summary>Returns true if the given compliance would set a new session best.</summary>
    public bool IsNewBest(float compliance)
    {
        if (_bestIndex < 0) { return true; }
        return compliance < _history[_bestIndex].compliance;
    }

    // ------------------------------------------------------------------ //
    // Cursor navigation (Left thumbstick Y in Optimize mode)
    // ------------------------------------------------------------------ //

    /// <summary>Returns the snapshot at the current cursor position, or null if no history.</summary>
    public StructureSnapshot GetCursorSnapshot()
    {
        if (_cursorIndex < 0 || _cursorIndex >= _history.Count) { return null; }
        return _history[_cursorIndex];
    }

    /// <summary>Move the history cursor by delta steps (+1 = newer, -1 = older).</summary>
    public void MoveCursor(int delta)
    {
        if (_history.Count == 0) { return; }
        _cursorIndex = Mathf.Clamp(_cursorIndex + delta, 0, _history.Count - 1);
    }

    /// <summary>Reset cursor to the newest (latest) entry.</summary>
    public void ResetCursorToLatest()
    {
        _cursorIndex = _history.Count > 0 ? _history.Count - 1 : -1;
    }

    // ------------------------------------------------------------------ //
    // Rollback
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Restore node positions to the snapshot at CursorIndex.
    /// Calls optimizeVisualizer.OnStructureChanged() after all moves.
    /// </summary>
    public void RollbackToCursor()
    {
        if (_cursorIndex < 0 || _cursorIndex >= _history.Count) { return; }

        StructureSnapshot snap = _history[_cursorIndex];

        for (int i = 0; i < snap.nodeNetworkIds.Length; i++)
        {
            ulong id = snap.nodeNetworkIds[i];
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out var netObj))
            {
                continue;
            }

            NodeBehaviour node = netObj.GetComponent<NodeBehaviour>();
            if (node == null) { continue; }

            // Convert world position → ParentWorld local space for network sync
            Vector3 localPos = worldCoordinateManager != null
                ? worldCoordinateManager.WorldToLocal(snap.nodePositions[i])
                : snap.nodePositions[i];

            if (NetworkManager.Singleton.IsServer)
            {
                node.transform.localPosition = localPos;
            }
            else
            {
                node.MoveServerRpc(localPos);
            }
        }

        // Future history is intentionally kept intact after rollback.
        // It will be truncated only if the user makes a new edit (TakeSnapshot).
        optimizeVisualizer?.OnStructureChanged();
    }

    // ------------------------------------------------------------------ //
    // Sparkline
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Returns a string like "▁▂▄█▇▅▃|▂▁" where | marks the cursor position.
    /// Shows up to SparklineWidth entries (oldest truncated).
    /// </summary>
    public string GetSparkline()
    {
        if (_history.Count == 0) { return string.Empty; }

        // Take the most recent SparklineWidth entries
        int start = Mathf.Max(0, _history.Count - SparklineWidth);
        int count = _history.Count - start;

        float min = float.MaxValue;
        float max = float.MinValue;
        for (int i = start; i < _history.Count; i++)
        {
            float c = _history[i].compliance;
            if (c < min) { min = c; }
            if (c > max) { max = c; }
        }

        float range = max - min;
        var sb = new System.Text.StringBuilder(count + 2);

        for (int i = start; i < _history.Count; i++)
        {
            float normalized = range > 0 ? (_history[i].compliance - min) / range : 0.5f;
            int charIdx = Mathf.Clamp(Mathf.RoundToInt(normalized * (SparkChars.Length - 1)), 0, SparkChars.Length - 1);
            sb.Append(SparkChars[charIdx]);

            // Insert cursor marker after the cursor entry
            int adjustedCursor = _cursorIndex - start;
            if (i - start == adjustedCursor) { sb.Append('|'); }
        }

        return sb.ToString();
    }

    // ------------------------------------------------------------------ //
    // Helpers
    // ------------------------------------------------------------------ //

    /// <summary>Wipe all history — call when the structure is reset or cleared.</summary>
    public void ResetHistory()
    {
        _history.Clear();
        _bestIndex   = -1;
        _cursorIndex = -1;
    }

    private int FindBestIndex()
    {
        if (_history.Count == 0) { return -1; }
        int best = 0;
        for (int i = 1; i < _history.Count; i++)
        {
            if (_history[i].compliance < _history[best].compliance) { best = i; }
        }
        return best;
    }
}
