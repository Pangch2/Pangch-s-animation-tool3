using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BDObjectSystem.Utility
{
    public static class BdObjectHelper
    {
        private const string FrameFormatString = @"\b{0}(\d+)\b";
        public static readonly Regex NBT_TagRegex = new Regex(@"Tags:\[([^\]]+)\]");
        public static readonly Regex NBT_UUIDRegex = new Regex(@"UUID:\[I;(-?\d+),(-?\d+),(-?\d+),(-?\d+)\]");

        // reading Tags:[] and return string
        public static string GetTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var match = NBT_TagRegex.Match(input);
            return match.Success ? match.Groups[1].Value : null;
        }

        // reading UUID:[] and return string
        public static string GetUuid(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var match = NBT_UUIDRegex.Match(input);
            return match.Success
                ? $"{match.Groups[1].Value},{match.Groups[2].Value},{match.Groups[3].Value},{match.Groups[4].Value}"
                : null;
        }

        // set bdobjects parent
        public static void SetParent(BdObject parent, BdObject target)
        {
            target.Parent = parent;

            if (target.children == null) return;
            foreach (var child in target.children)
            {
                SetParent(target, child);
            }
        }
        
        // get number in input ({key}{number})
        public static int ExtractNumber(string input, string key, int defaultValue = 0)
        {
            var match = Regex.Match(input, string.Format(FrameFormatString, key));
            return match.Success ? int.Parse(match.Groups[1].Value) : defaultValue;
        }

        // get string in input ({key}{any number})
        public static string ExtractFrame(string input, string key)
        {
            var match = Regex.Match(input, string.Format(FrameFormatString, key));
            return match.Success ? match.Groups[0].Value : null;
        }

        // making ID:obj dict
        public static Dictionary<string, BdObjectContainer> SetDisplayIDDictionary(BdObjectContainer root)
        {
            var idDataDict = new Dictionary<string, BdObjectContainer>();
            var queue = new Queue<BdObjectContainer>();
            queue.Enqueue(root);
        
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                
                if (obj.BdObject.IsDisplay)
                {
                    if (idDataDict.ContainsKey(obj.BdObjectID))
                    {
                        CustomLog.LogError($"{obj.BdObjectID}가 중복됨: 애니메이션 불가능!");
                        idDataDict.Clear();
                        return idDataDict;
                    }
                    idDataDict[obj.BdObjectID] = obj;
                }
                
                // BFS
                if (obj.children == null) continue;
                foreach (var child in obj.children)
                {
                    queue.Enqueue(child);
                }
            }
            return idDataDict;
        }

        /// <summary>
        /// SetDisplayList: BDObject의 자식중 모든 display 오브젝트를 BFS로 탐색하여 리스트에 저장
        /// 또한 입력으로 들어온 Dictionary에 모든 ID-Matrix를 저장합니다.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static Dictionary<string, BdObject> SetDisplayDict(BdObject root, Dictionary<string, Matrix4x4> ModelMatrix)
        {
            var resultList = new Dictionary<string, BdObject>();
            var queue = new Queue<BdObject>();
            queue.Enqueue(root);
            
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                ModelMatrix[obj.ID] = obj.transforms.GetMatrix();
            
                if (obj.IsDisplay)
                {
                    resultList.Add(obj.ID, obj);
                }
                
                // BFS
                if (obj.children == null) continue;
                foreach (var child in obj.children)
                {
                    queue.Enqueue(child);
                }
            }
            return resultList;
        }


        /// <summary>
        /// 해당 root의 자식들 중 Tag, UUID가 존재하지 않는 오브젝트가 존재한다면 false를 반환합니다.
        /// </summary>
        /// <param name="root"> 최상위 BDObject</param>
        /// <returns></returns>
        public static bool HasVaildID(BdObject root)
        {
            var queue = new Queue<BdObject>();
            queue.Enqueue(root);
        
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                
                if (obj.IsDisplay && string.IsNullOrEmpty(obj.ID))
                {
                    return false;
                }
                
                // BFS
                if (obj.children == null) continue;
                foreach (var child in obj.children)
                {
                    queue.Enqueue(child);
                }
            }
            return true;
        }


        public enum IDValidationResult 
        {
            Vaild, NoID, Mismatch
        }
        /// <summary>
        /// 해당 root의 자식들 중 기존 알고리즘과 주어진 tag, uuid가 일치하지 않는 오브젝트가 존재한다면 false를 반환합니다.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="tag"></param>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public static IDValidationResult HasVaildID(BdObject root, string tag, int uuid = -1)
        {
            var queue = new Queue<BdObject>();
            queue.Enqueue(root);

            int idx = 1;
        
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                
                if (obj.IsDisplay)
                {
                    if (string.IsNullOrEmpty(obj.ID))
                    {
                        return IDValidationResult.NoID;
                    }

                    if (uuid == -1)
                    {
                        string expectTag = $"{tag}0,{tag}{idx}";
                        if (obj.ID != expectTag)
                        {
                            return IDValidationResult.Mismatch;
                        }
                    }
                    else
                    {
                        string expectTag = $"{uuid},{idx},0,0";
                        if (obj.ID != expectTag)
                        {
                            return IDValidationResult.Mismatch;
                        }
                    }

                    idx++;
                }
                
                // BFS
                if (obj.children == null) continue;
                foreach (var child in obj.children)
                {
                    queue.Enqueue(child);
                }
            }
            return IDValidationResult.Vaild;
        }
    }
}
