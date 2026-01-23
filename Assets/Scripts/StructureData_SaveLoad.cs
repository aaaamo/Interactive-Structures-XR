using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal save file structure containing only necessary geometric data
/// </summary>
[System.Serializable]
public class StructureSaveData
{
    public string structureName;
    public string dateCreated;
    public int version = 1; // For future compatibility

    public List<NodeData> nodes = new List<NodeData>();
    public List<EdgeData> edges = new List<EdgeData>();
    public List<LoadData> loads = new List<LoadData>();

    // Optional: ParentWorld transform data (for future use)
    public ParentWorldData parentWorldData;

    [System.Serializable]
    public class NodeData
    {
        public int id;
        public float x, y, z; // Position
        public bool isSupport; // Free or fixed

        public NodeData(int id, Vector3 position, bool isSupport)
        {
            this.id = id;
            this.x = position.x;
            this.y = position.y;
            this.z = position.z;
            this.isSupport = isSupport;
        }

        public Vector3 GetPosition()
        {
            return new Vector3(x, y, z);
        }
    }

    [System.Serializable]
    public class EdgeData
    {
        public int nodeAId;
        public int nodeBId;

        public EdgeData(int nodeAId, int nodeBId)
        {
            this.nodeAId = nodeAId;
            this.nodeBId = nodeBId;
        }
    }

    [System.Serializable]
    public class LoadData
    {
        public int nodeId;
        public float dirX, dirY, dirZ; // Direction (normalized)
        public float magnitude;

        public LoadData(int nodeId, Vector3 direction, float magnitude)
        {
            this.nodeId = nodeId;
            this.dirX = direction.x;
            this.dirY = direction.y;
            this.dirZ = direction.z;
            this.magnitude = magnitude;
        }

        public Vector3 GetDirection()
        {
            return new Vector3(dirX, dirY, dirZ);
        }
    }

    /// <summary>
    /// ParentWorld transform data for coordinate system reference
    /// </summary>
    [System.Serializable]
    public class ParentWorldData
    {
        public float originX, originY, originZ;
        public float dirX, dirY, dirZ;  // X-axis direction

        public ParentWorldData(Vector3 origin, Vector3 direction)
        {
            originX = origin.x;
            originY = origin.y;
            originZ = origin.z;
            dirX = direction.x;
            dirY = direction.y;
            dirZ = direction.z;
        }

        public Vector3 GetOrigin()
        {
            return new Vector3(originX, originY, originZ);
        }

        public Vector3 GetDirection()
        {
            return new Vector3(dirX, dirY, dirZ);
        }
    }
}
