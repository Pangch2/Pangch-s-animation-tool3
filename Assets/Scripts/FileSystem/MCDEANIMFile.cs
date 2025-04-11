using System;
using System.Collections.Generic;
using UnityEngine;
using Animation.AnimFrame;
using BDObjectSystem;
using BDObjectSystem.Utility;

namespace FileSystem
{
    #region MCDEANIMFile
    [Serializable]
    public class MCDEANIMFile
    {
        public string name = string.Empty;
        public string version;
        public List<AnimObjectFile> animObjects = new List<AnimObjectFile>();

        public void UpdateAnimObject(List<AnimObject> AnimObjects)
        {
            int cnt = AnimObjects.Count;
            for (int i = 0; i < cnt; i++)
            {
                if (i < animObjects.Count)
                {
                    animObjects[i].SetInformation(AnimObjects[i]);
                }
                else
                {
                    animObjects.Add(new AnimObjectFile(AnimObjects[i]));
                }
            }
        }
        #endregion

        #region AnimObject
        [Serializable]
        public class AnimObjectFile
        {
            public string name;
            public List<FrameFile> frameFiles = new List<FrameFile>();

            public AnimObjectFile(){}

            public AnimObjectFile(AnimObject animObject)
            {
                SetInformation(animObject);
            }

            public void SetInformation(AnimObject animObject)
            {
                name = animObject.bdFileName;

                var frameValues = animObject.frames.Values;
                int i = 0;
                foreach (var frame in frameValues)
                {
                    if (i < frameFiles.Count)
                    {
                        frameFiles[i].SetInformation(frame);
                    }
                    else
                    {
                        frameFiles.Add(new FrameFile(frame));
                    }
                    i++;
                }
            }

        }
        #endregion

        #region FrameFile
        [Serializable]
        public class FrameFile
        {
            public string name;
            public int tick;
            public int interpolation;

            public BdObject bdObject;

            public FrameFile(){}

            public FrameFile(Frame frame)
            {
                SetInformation(frame);
            }

            public void SetInformation(Frame frame)
            {
                name = frame.fileName;
                tick = frame.tick;
                interpolation = frame.interpolation;
                bdObject = frame.Info;
            }
        }
        #endregion
    }
}