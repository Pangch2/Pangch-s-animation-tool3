

using System.Collections.Generic;
using BDObjectSystem;
using UnityEngine;

namespace Animation.AnimFrame
{
    /// <summary>
    /// ExportFrame is a struct that represents a frame of animation data to be exported.
    /// </summary>
    public readonly struct ExportFrame
    {
        public readonly int Tick;
        public readonly int Interpolation;
        public readonly Dictionary<string, (BdObject, Matrix4x4)> NodeDict;

        public ExportFrame(Frame frame)
        {
            Tick = frame.tick;
            Interpolation = frame.interpolation;
            NodeDict = new();
            foreach (var obj in frame.leafObjects)
            {
                if (obj.Value != null)
                {
                    NodeDict.Add(obj.Key, (obj.Value, frame.worldMatrixDict[obj.Key]));
                }
            }
        }
        public void Merge(Frame frame)
        {
            foreach (var obj in frame.leafObjects)
            {
                if (obj.Value != null && !NodeDict.ContainsKey(obj.Key))
                {
                    NodeDict.Add(obj.Key, (obj.Value, frame.worldMatrixDict[obj.Key]));
                }
            }
        }
    }
}