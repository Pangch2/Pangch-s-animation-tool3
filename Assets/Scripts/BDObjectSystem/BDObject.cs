using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BDObjectSystem.Utility;
using UnityEngine;
using System;


namespace BDObjectSystem
{
    //[System.Serializable]
    public class BdObject
    {
        // JSON Property
        public string name;
        public string nbt;
        public bool isBlockDisplay;
        public bool isItemDisplay;
        public bool isTextDisplay;
        public float[] transforms;

        public JObject options;
        public BdObject[] children;

        [JsonExtensionData]
        public Dictionary<string, object> ExtraData;

        // Additional Property
        [SerializeField]
        [JsonIgnore] 
        private string _id;
        [JsonIgnore]
        public string ID => GetID();
        // [field: JsonIgnore] public string ID { get; set; }

        [JsonIgnore]
        public BdObject Parent;

        [JsonIgnore]
        public bool IsDisplay => isBlockDisplay || isItemDisplay || isTextDisplay;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            var uuid = BdObjectHelper.GetUuid(nbt);
            if (!string.IsNullOrEmpty(uuid))
            {
                _id = uuid;
                return;
            }

            var tag = BdObjectHelper.GetTags(nbt);
            if (!string.IsNullOrEmpty(tag))
            {
                _id = tag;
            }
        }

        private string GetID()
        {
            if (!string.IsNullOrEmpty(_id)) return _id;

            if (children == null || children.Length == 0)
            {
                _id = name;
            }
            else
            {
                List<string> childIds = new List<string>();
                foreach (var child in children)
                {
                    childIds.Add(child.GetID()); // 자식의 ID 재귀 호출
                }

                childIds.Sort(); // 순서 무시

                // 구조 포함된 식별자 생성
                string groupID = $"[{string.Join(",", childIds)}]";

                // // 자식이 하나일 경우 구분자 추가 (중간 그룹 존재 인식용)
                // if (childIds.Count == 1)
                //     groupID += "g";

                _id = groupID;
            }

            return _id;
        }

        public string GetEntityType()
        {
            return isBlockDisplay ? "block_display" :
                   isItemDisplay ? "item_display" :
                   isTextDisplay ? "text_display" : null;
        }
    }
}

