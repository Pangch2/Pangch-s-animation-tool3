using System;
using System.Collections.Generic;
using GameSystem;
using UnityEngine;
using Animation.UI;
using BDObjectSystem;
using FileSystem;
using System.Linq;
using Unity.VisualScripting;

namespace Animation.AnimFrame
{
    public class AnimObjList : BaseManager
    {
        public RectTransform importButton;
        public float jump;

        public AnimObject animObjectPrefab;
        public Frame framePrefab;
        public List<AnimObject> animObjects = new();

        public Timeline timeline;
        public Transform frameParent;
        

        private void Start()
        {
            GameManager.GetManager<FileLoadManager>().animObjList = this;
            timeline = GameManager.GetManager<AnimManager>().timeline;
            jump = importButton.sizeDelta.y * 1.5f;
        }

        public AnimObject AddAnimObject(string fileName)
        {
            //Debug.Log("EndAddObject: " + obj.name);

            var animObject = Instantiate(animObjectPrefab, frameParent);
            animObject.Init(fileName, this);
            animObject.rect.anchoredPosition = new Vector2(animObject.rect.anchoredPosition.x, importButton.anchoredPosition.y - 60f);

            importButton.anchoredPosition = new Vector2(importButton.anchoredPosition.x, importButton.anchoredPosition.y - jump);

            var animMan = GameManager.GetManager<AnimManager>();
            animMan.Tick = 0;
            animMan.timeline.SetTickTexts(0);

            var SaveMan = GameManager.GetManager<SaveManager>();

            // 최초 삽입 시 세이브 파일 생성
            if (SaveMan.IsNoneSaved)
            {
                SaveMan.MakeNewMDEFile(fileName);
            }
            animObjects.Add(animObject);
            
            return animObject;
        }

        public void ResetAnimObject()
        {
            var objs = animObjects.ToArray();
            foreach (var obj in objs)
            {
                RemoveAnimObject(obj);
            }
            
        }

        public void RemoveAnimObject(AnimObject obj)
        {
            var idx = animObjects.IndexOf(obj);
            animObjects.RemoveAt(idx);

            GameManager.GetManager<BdObjectManager>().RemoveBdObject(obj.bdFileName);

            Destroy(obj.gameObject);

            for (var i = idx; i < animObjects.Count; i++)
            {
                animObjects[i].rect.anchoredPosition = new Vector2(animObjects[i].rect.anchoredPosition.x, animObjects[i].rect.anchoredPosition.y + jump);
            }
            importButton.anchoredPosition = new Vector2(importButton.anchoredPosition.x, importButton.anchoredPosition.y + jump);

            CustomLog.Log("Line Removed: " + obj.bdFileName);
        }

        public List<SortedList<int, ExportFrame>> GetAllFrames()
        {
            var frames = new List<SortedList<int, ExportFrame>>();
            
            foreach (var animObject in animObjects)
            {
                var animFrames = new SortedList<int, ExportFrame>();
                foreach (var frame in animObject.frames.Values)
                {
                    animFrames.Add(frame.tick, new ExportFrame(frame));
                }
                frames.Add(animFrames);
            }

            return frames;
        }
    }
}
