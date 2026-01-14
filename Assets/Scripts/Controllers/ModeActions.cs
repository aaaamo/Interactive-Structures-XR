using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InteractiveStructures.Controllers
{
    /// <summary>
    /// Handles mode-specific actions (AddNode, AddEdge, Delete, etc.)
    /// </summary>
    public class ModeActions : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GraphManager graphManager;
        [SerializeField] private Transform markerTransform;
        [SerializeField] private GridPointRenderer gridRenderer;
        [SerializeField] private StructuralAnalyzer structuralAnalyzer;
        [SerializeField] private RaycastSurfaceFinder surfaceFinder;
        [SerializeField] private ModeDisplayUI modeDisplayUI;
        [SerializeField] private ContextualTutorialPanel contextualTutorial;

        // Temporary objects for multi-step actions
        private NodeBehaviour firstSelectedNode;
        private EdgeBehaviour tempEdge;
        private NodeBehaviour firstLoadNode;
        private LoadBehaviour tempLoad;

        public void Initialize(GraphManager gm, Transform marker, GridPointRenderer grid,
            StructuralAnalyzer analyzer, RaycastSurfaceFinder surface, ModeDisplayUI ui, ContextualTutorialPanel tutorial)
        {
            graphManager = gm;
            markerTransform = marker;
            gridRenderer = grid;
            structuralAnalyzer = analyzer;
            surfaceFinder = surface;
            modeDisplayUI = ui;
            contextualTutorial = tutorial;
        }

        #region Public Action Methods

        public void AddNode()
        {
            Vector3 snapPos = GetGridPoint(markerTransform.position);
            graphManager.CreateNode(snapPos);
            HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
            contextualTutorial?.OnActionPerformed(OVRGraphController.Mode.AddNode);
        }

        public void AddEdge()
        {
            NodeBehaviour node = GeometryQuery.FindNodeAt(markerTransform.position);
            if (node == null) return;

            if (firstSelectedNode == null)
            {
                // First node selection
                firstSelectedNode = node;
                tempEdge = graphManager.CreateEdge(firstSelectedNode);
                HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
                VisualFeedbackManager.Instance?.SetSelected(node.gameObject, new Color(0.2f, 1f, 0.2f, 1f));
            }
            else if (node != firstSelectedNode)
            {
                // Second node selection
                if (!GeometryQuery.EdgeExists(firstSelectedNode, node))
                {
                    CompleteEdge(node);
                }
                else
                {
                    CancelEdgeCreation();
                    modeDisplayUI?.ShowMessage("Edge already exists!", new Color(1f, 0.3f, 0.3f));
                    StartCoroutine(ClearMessageAfterDelay(2f));
                }

                VisualFeedbackManager.Instance?.ClearSelection();
                firstSelectedNode = null;
                tempEdge = null;
            }
        }

        public void AddLoad()
        {
            if (firstLoadNode == null)
            {
                // First step: select node
                NodeBehaviour node = GeometryQuery.FindNodeAt(markerTransform.position);
                if (node == null) return;

                firstLoadNode = node;
                tempLoad = graphManager.CreateLoad(firstLoadNode, Vector3.down, 1f);
                HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
                VisualFeedbackManager.Instance?.SetSelected(node.gameObject, new Color(1f, 0.6f, 0.2f, 1f));
            }
            else
            {
                // Second step: set direction and magnitude
                Vector3 dir = markerTransform.position - firstLoadNode.transform.position;
                float mag = dir.magnitude;
                tempLoad.SetDirection(dir.normalized);
                tempLoad.SetMagnitude(mag);
                firstLoadNode.loads.Add(tempLoad);
                HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
                VisualFeedbackManager.Instance?.ClearSelection();
                firstLoadNode = null;
                tempLoad = null;
            }
        }

        public void ToggleSupport()
        {
            NodeBehaviour node = GeometryQuery.FindNodeAt(markerTransform.position);
            if (node != null)
            {
                node.ToggleSupport();
                HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
                contextualTutorial?.OnActionPerformed(OVRGraphController.Mode.ToggleSupport);
            }
        }

        public void StartMove()
        {
            // Try to move node first
            NodeBehaviour node = GeometryQuery.FindNodeAt(markerTransform.position);
            if (node != null)
            {
                StartCoroutine(MoveNodeCoroutine(node));
                return;
            }

            // Try to move edge
            EdgeBehaviour edge = GeometryQuery.FindEdgeAt(markerTransform.position);
            if (edge != null)
            {
                StartCoroutine(MoveEdgeCoroutine(edge));
                return;
            }

            // Try to move load
            LoadBehaviour load = GeometryQuery.FindLoadAt(markerTransform.position);
            if (load != null)
            {
                StartCoroutine(MoveLoadCoroutine(load));
            }
        }

        public void Delete()
        {
            // Try to delete node
            NodeBehaviour node = GeometryQuery.FindNodeAt(markerTransform.position);
            if (node != null)
            {
                DeleteNode(node);
                HapticFeedback.Trigger(HapticFeedback.HapticType.Strong);
                return;
            }

            // Try to delete edge
            EdgeBehaviour edge = GeometryQuery.FindEdgeAt(markerTransform.position);
            if (edge != null)
            {
                DeleteEdge(edge);
                HapticFeedback.Trigger(HapticFeedback.HapticType.Strong);
                return;
            }

            // Try to delete load
            LoadBehaviour load = GeometryQuery.FindLoadAt(markerTransform.position);
            if (load != null)
            {
                DeleteLoad(load);
                HapticFeedback.Trigger(HapticFeedback.HapticType.Strong);
            }
        }

        public void StartGrab()
        {
            NodeBehaviour startNode = GeometryQuery.FindNodeAt(markerTransform.position);

            if (startNode == null)
            {
                EdgeBehaviour edge = GeometryQuery.FindEdgeAt(markerTransform.position);
                if (edge != null)
                    startNode = edge.nodeA ?? edge.nodeB;
            }

            if (startNode != null)
            {
                StartCoroutine(GrabStructureCoroutine(startNode));
            }
        }

        public void ConfirmAnalysis()
        {
            structuralAnalyzer?.PerformAnalysis();
        }

        public void CancelAnalysis()
        {
            Debug.Log("Analysis cancelled");
        }

        public void SetGridAnchor()
        {
            surfaceFinder?.SetAnchor();
        }

        #endregion

        #region Temporary Object Updates

        public void UpdateTempEdge()
        {
            if (tempEdge != null && firstSelectedNode != null && markerTransform != null)
            {
                tempEdge.UpdateEdgePosition(markerTransform.position);
            }
        }

        public void UpdateTempLoad()
        {
            if (firstLoadNode == null || tempLoad == null) return;

            Vector3 dir = markerTransform.position - firstLoadNode.transform.position;
            float mag = dir.magnitude;
            tempLoad.SetDirection(dir.normalized);
            tempLoad.SetMagnitude(mag);
        }

        public void CleanupTemp()
        {
            if (tempEdge != null)
            {
                graphManager.RemoveEdge(tempEdge);
                tempEdge = null;
                firstSelectedNode = null;
            }

            if (tempLoad != null)
            {
                Destroy(tempLoad.gameObject);
                tempLoad = null;
                firstLoadNode = null;
            }

            VisualFeedbackManager.Instance?.ClearHover();
            VisualFeedbackManager.Instance?.ClearSelection();
        }

        #endregion

        #region Helper Methods

        private Vector3 GetGridPoint(Vector3 pos)
        {
            if (gridRenderer == null) return pos;
            return gridRenderer.GetClosestGridPoint(pos);
        }

        private void CompleteEdge(NodeBehaviour secondNode)
        {
            tempEdge.nodeB = secondNode;

            if (firstSelectedNode.connectedEdges == null)
                firstSelectedNode.connectedEdges = new List<EdgeBehaviour>();
            firstSelectedNode.connectedEdges.Add(tempEdge);

            if (secondNode.connectedEdges == null)
                secondNode.connectedEdges = new List<EdgeBehaviour>();
            secondNode.connectedEdges.Add(tempEdge);

            tempEdge.UpdateEdgePosition();
            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
        }

        private void CancelEdgeCreation()
        {
            graphManager.RemoveEdge(tempEdge);
            HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
        }

        private void DeleteNode(NodeBehaviour node)
        {
            if (node == null) return;

            // Delete all loads on this node
            if (node.loads != null)
            {
                foreach (var load in node.loads)
                {
                    if (load != null)
                        Destroy(load.gameObject);
                }
            }

            // Delete all connected edges
            if (node.connectedEdges != null)
            {
                foreach (var edge in node.connectedEdges)
                {
                    if (edge != null)
                    {
                        if (edge.nodeA != null && edge.nodeA != node)
                            edge.nodeA.connectedEdges?.Remove(edge);
                        if (edge.nodeB != null && edge.nodeB != node)
                            edge.nodeB.connectedEdges?.Remove(edge);
                        graphManager.RemoveEdge(edge);
                    }
                }
            }

            Destroy(node.gameObject);
        }

        private void DeleteEdge(EdgeBehaviour edge)
        {
            if (edge == null) return;

            if (edge.nodeA != null)
                edge.nodeA.connectedEdges?.Remove(edge);
            if (edge.nodeB != null)
                edge.nodeB.connectedEdges?.Remove(edge);

            graphManager.RemoveEdge(edge);
        }

        private void DeleteLoad(LoadBehaviour load)
        {
            if (load == null) return;

            if (load.node != null)
                load.node.loads.Remove(load);

            Destroy(load.gameObject);
        }

        #endregion

        #region Coroutines

        private IEnumerator MoveNodeCoroutine(NodeBehaviour node)
        {
            if (node == null) yield break;

            Vector3 offset = node.transform.position - markerTransform.position;

            while (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                Vector3 newPos = GetGridPoint(markerTransform.position + offset);
                node.transform.position = newPos;

                if (node.connectedEdges != null)
                {
                    foreach (var edge in node.connectedEdges)
                    {
                        edge?.UpdateEdgePosition();
                    }
                }

                yield return null;
            }
        }

        private IEnumerator MoveEdgeCoroutine(EdgeBehaviour edge)
        {
            if (edge == null || edge.nodeA == null || edge.nodeB == null) yield break;

            Vector3 offsetA = edge.nodeA.transform.position - markerTransform.position;
            Vector3 offsetB = edge.nodeB.transform.position - markerTransform.position;

            while (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                edge.nodeA.transform.position = markerTransform.position + offsetA;
                edge.nodeB.transform.position = markerTransform.position + offsetB;

                foreach (var node in new[] { edge.nodeA, edge.nodeB })
                {
                    if (node.connectedEdges != null)
                    {
                        foreach (var e in node.connectedEdges)
                        {
                            e?.UpdateEdgePosition();
                        }
                    }
                }

                yield return null;
            }
        }

        private IEnumerator MoveLoadCoroutine(LoadBehaviour load)
        {
            if (load == null || load.node == null) yield break;

            Vector3 offset = load.EndPoint() - markerTransform.position;

            while (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                Vector3 newEndPoint = markerTransform.position + offset;
                Vector3 dir = newEndPoint - load.node.transform.position;
                float mag = dir.magnitude;
                load.SetDirection(dir.normalized);
                load.SetMagnitude(mag);

                yield return null;
            }
        }

        private IEnumerator GrabStructureCoroutine(NodeBehaviour startNode)
        {
            if (startNode == null) yield break;

            // Find all connected nodes using BFS
            HashSet<NodeBehaviour> connectedNodes = FindConnectedNodes(startNode);

            // Store offsets from marker
            Dictionary<NodeBehaviour, Vector3> offsets = new Dictionary<NodeBehaviour, Vector3>();
            foreach (var node in connectedNodes)
            {
                offsets[node] = node.transform.position - markerTransform.position;
            }

            // Move all nodes while trigger is held
            while (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                foreach (var node in connectedNodes)
                {
                    node.transform.position = markerTransform.position + offsets[node];
                }

                foreach (var node in connectedNodes)
                {
                    if (node.connectedEdges != null)
                    {
                        foreach (var edge in node.connectedEdges)
                        {
                            edge?.UpdateEdgePosition();
                        }
                    }
                }

                yield return null;
            }
        }

        private HashSet<NodeBehaviour> FindConnectedNodes(NodeBehaviour startNode)
        {
            HashSet<NodeBehaviour> connectedNodes = new HashSet<NodeBehaviour>();
            Queue<NodeBehaviour> queue = new Queue<NodeBehaviour>();

            queue.Enqueue(startNode);
            connectedNodes.Add(startNode);

            while (queue.Count > 0)
            {
                NodeBehaviour node = queue.Dequeue();

                if (node.connectedEdges != null)
                {
                    foreach (EdgeBehaviour edge in node.connectedEdges)
                    {
                        NodeBehaviour other = edge.nodeA == node ? edge.nodeB : edge.nodeA;
                        if (other != null && !connectedNodes.Contains(other))
                        {
                            connectedNodes.Add(other);
                            queue.Enqueue(other);
                        }
                    }
                }
            }

            return connectedNodes;
        }

        private IEnumerator ClearMessageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            modeDisplayUI?.ClearMessage(OVRGraphController.Mode.AddEdge);
        }

        #endregion

        #region Public Getters

        public bool IsAddingEdge => firstSelectedNode != null;
        public bool IsAddingLoad => firstLoadNode != null;
        public NodeBehaviour GetFirstSelectedNode() => firstSelectedNode;
        public EdgeBehaviour GetTempEdge() => tempEdge;
        public LoadBehaviour GetTempLoad() => tempLoad;

        #endregion
    }
}
