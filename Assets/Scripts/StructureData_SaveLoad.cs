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
}
