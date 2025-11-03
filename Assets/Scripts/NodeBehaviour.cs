using UnityEngine;
using System.Collections.Generic;

public class NodeBehaviour : MonoBehaviour
{
    public List<EdgeBehaviour> connectedEdges = new List<EdgeBehaviour>();
    public List<LoadBehaviour> loads = new List<LoadBehaviour>();
    public bool isSupport = false;

    public GameObject supportVisual;
    public GameObject freeVisual;


    void Awake()
    {
        if (connectedEdges == null)
            connectedEdges = new List<EdgeBehaviour>();

        ApplyVisualState();
    }

    public void ToggleSupport()
    {
        isSupport = !isSupport;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (freeVisual != null) freeVisual.SetActive(!isSupport);
        if (supportVisual != null) supportVisual.SetActive(isSupport);
    }
}
