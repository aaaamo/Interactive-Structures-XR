using UnityEngine;

public class LoadBehaviour : MonoBehaviour
{
    public NodeBehaviour node;
    public Vector3 direction = Vector3.down; // default down
    public float magnitude = 1f;
    private float offset = 0.01f;
    private float scale = 0.012f;

    [Header("Visual")]
    public Transform arrow; // assign arrow prefab child

    void Awake()
    {
        if (arrow == null && transform.childCount > 0)
            arrow = transform.GetChild(0);
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
        UpdateArrow();
    }

    public void SetMagnitude(float mag)
    {
        magnitude = mag;
        UpdateArrow();
    }

    public void UpdateArrow()
    {
        if (arrow == null) return;
        Vector3 localX = Vector3.Cross(direction, new Vector3(Random.value, Random.value, Random.value));

        this.transform.rotation = Quaternion.LookRotation(localX, direction);
        arrow.localScale = new Vector3(scale, magnitude - offset, scale);
        arrow.localPosition = new Vector3(0, 0.5f * magnitude + offset, 0);
    }

    public Vector3 GetForceVector()
    {
        return direction * magnitude;
    }

    public Vector3 EndPoint()
    {
        return node.transform.position + direction.normalized * magnitude;
    }
}
