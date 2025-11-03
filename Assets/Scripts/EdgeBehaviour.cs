using UnityEngine;

public class EdgeBehaviour : MonoBehaviour
{
    public NodeBehaviour nodeA;
    public NodeBehaviour nodeB;

    private Transform edgeTransform;

    void Awake()
    {
        edgeTransform = transform;
        if (edgeTransform == null)
            Debug.LogError("Edge transform missing!");
    }

    public void UpdateEdgePosition(Vector3? tempEnd = null)
    {
        if (nodeA == null)
        {
            Debug.LogWarning("EdgeBehaviour nodeA is null!");
            return;
        }

        Vector3 start = nodeA.transform.position;
        Vector3 end;

        if (nodeB != null)
            end = nodeB.transform.position;
        else if (tempEnd.HasValue)
            end = tempEnd.Value;
        else
            return; // nothing to update

        Vector3 middle = (start + end) / 2f;
        edgeTransform.position = middle;

        // scale along Y-axis (cylinder pivot at center, height = 2)
        Vector3 scale = edgeTransform.localScale;
        scale.y = Vector3.Distance(start, end) / 2f;
        edgeTransform.localScale = scale;

        // rotate cylinder to align
        edgeTransform.up = (end - start).normalized;
    }
}
