using System.Collections.Generic;
using BDObjectSystem;
using UnityEngine;

namespace Animation.AnimFrame
{
    /// <summary>
    /// Represents the data for a single node (entity) within an animation frame.
    /// </summary>
    public readonly struct NodeData
    {
        public readonly BdObject Object;
        public readonly Matrix4x4 Transform;
        public readonly int Interpolation;

        public NodeData(BdObject obj, Matrix4x4 transform, int interpolation)
        {
            Object = obj;
            Transform = transform;
            Interpolation = interpolation;
        }
    }

    /// <summary>
    /// ExportFrame is a struct that represents a frame of animation data to be exported.
    /// </summary>
    public readonly struct ExportFrame
    {
        public readonly int Tick;
        public readonly Dictionary<string, NodeData> NodeDict;

        public ExportFrame(Frame frame)
        {
            Tick = frame.tick;
            NodeDict = new();
            foreach (var obj in frame.leafObjects)
            {
                if (obj.Value != null)
                {
                    var nodeData = new NodeData(obj.Value, frame.worldMatrixDict[obj.Key], frame.interpolation);
                    NodeDict.Add(obj.Key, nodeData);
                }
            }
        }
        public void Merge(Frame frame)
        {
            foreach (var obj in frame.leafObjects)
            {
                if (obj.Value != null && !NodeDict.ContainsKey(obj.Key))
                {
                    var nodeData = new NodeData(obj.Value, frame.worldMatrixDict[obj.Key], frame.interpolation);
                    NodeDict.Add(obj.Key, nodeData);
                }
            }
        }
    }
}