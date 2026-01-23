using UnityEngine;

namespace InteractiveStructures.Controllers
{
    /// <summary>
    /// Helper class for finding nodes, edges, and loads near a position
    /// </summary>
    public static class GeometryQuery
    {
        private const float NODE_SEARCH_RADIUS = 0.03f;
        private const float EDGE_SEARCH_RADIUS = 0.02f;
        private const float LOAD_SEARCH_RADIUS = 0.03f;

        /// <summary>
        /// Find the closest node to a position
        /// </summary>
        public static NodeBehaviour FindNodeAt(Vector3 position, float searchRadius = NODE_SEARCH_RADIUS)
        {
            NodeBehaviour[] nodes = Object.FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
            NodeBehaviour closest = null;
            float minDist = searchRadius;

            foreach (var node in nodes)
            {
                if (node == null) continue;

                float dist = Vector3.Distance(node.transform.position, position);
                if (dist < minDist)
                {
                    closest = node;
                    minDist = dist;
                }
            }

            return closest;
        }

        /// <summary>
        /// Find the closest edge to a position
        /// </summary>
        public static EdgeBehaviour FindEdgeAt(Vector3 position, float searchRadius = EDGE_SEARCH_RADIUS)
        {
            EdgeBehaviour[] edges = Object.FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.None);
            EdgeBehaviour closest = null;
            float minDist = searchRadius;

            foreach (var edge in edges)
            {
                if (edge == null || edge.nodeA == null || edge.nodeB == null)
                    continue;

                Vector3 closestPoint = ClosestPointOnSegment(
                    edge.nodeA.transform.position,
                    edge.nodeB.transform.position,
                    position);

                float dist = Vector3.Distance(position, closestPoint);
                if (dist < minDist)
                {
                    closest = edge;
                    minDist = dist;
                }
            }

            return closest;
        }

        /// <summary>
        /// Find the closest load to a position
        /// </summary>
        public static LoadBehaviour FindLoadAt(Vector3 position, float searchRadius = LOAD_SEARCH_RADIUS)
        {
            LoadBehaviour[] loads = Object.FindObjectsByType<LoadBehaviour>(FindObjectsSortMode.None);
            LoadBehaviour closest = null;
            float minDist = searchRadius;

            foreach (var load in loads)
            {
                if (load == null || load.node == null) continue;

                float dist = Vector3.Distance(load.EndPoint(), position);

                if (dist < minDist)
                {
                    closest = load;
                    minDist = dist;
                }
            }

            return closest;
        }

        /// <summary>
        /// Get closest point on a line segment
        /// </summary>
        public static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
        {
            Vector3 ab = b - a;
            float t = Vector3.Dot(point - a, ab) / Vector3.Dot(ab, ab);
            t = Mathf.Clamp01(t);
            return a + t * ab;
        }

        /// <summary>
        /// Check if an edge already exists between two nodes
        /// </summary>
        public static bool EdgeExists(NodeBehaviour nodeA, NodeBehaviour nodeB)
        {
            if (nodeA == null || nodeB == null || nodeA.connectedEdges == null)
                return false;

            foreach (var edge in nodeA.connectedEdges)
            {
                if (edge == null) continue;

                if ((edge.nodeA == nodeA && edge.nodeB == nodeB) ||
                    (edge.nodeB == nodeA && edge.nodeA == nodeB))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
