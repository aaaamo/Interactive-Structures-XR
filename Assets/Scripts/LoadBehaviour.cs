
//using UnityEngine;

//public class LoadBehaviour: MonoBehaviour
//{
//    public NodeBehaviour node; 
//    public Vector3 direction;
//    public float magnitude;
//}

using UnityEngine;

public class LoadBehaviour : MonoBehaviour
{
    public NodeBehaviour node;
    public Vector3 direction = Vector3.down; // default down
    public float magnitude = 1f;

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
        this.transform.rotation = Quaternion.LookRotation(direction);
        arrow.localScale = new Vector3(1, magnitude, 1);
        arrow.localPosition = new Vector3(0, -2.5f * magnitude, 0);

        //Vector3 dir = direction.normalized;
        //// Rotate child around parent (node)
        //arrow.rotation = Quaternion.LookRotation(dir);

        //// Keep offset in local space
        //arrow.localPosition = dir * magnitude * 0.5f; // place it halfway along vector

        //// Scale arrow along its local Z (or Y depending on model)
        //arrow.localScale = new Vector3(1, magnitude, 1);
    }
}
