using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BDObjectSystem.Utility;
using UnityEngine;


namespace BDObjectSystem
{
    [System.Serializable]
    public class BdObject
    {
        // JSON Property
        public string Name;
        public string Nbt;
        public bool IsBlockDisplay;
        public bool IsItemDisplay;
        public bool IsTextDisplay;
        public float[] Transforms;

        public JObject Options;
        public BdObject[] Children;

        [JsonExtensionData]
        public Dictionary<string, object> ExtraData;

        // Additional Property
        [SerializeField]
        [JsonIgnore] private string _id;
        public string ID => GetID();
        // [field: JsonIgnore] public string ID { get; set; }

        [JsonIgnore]
        public BdObject Parent;

        public bool IsDisplay => IsBlockDisplay || IsItemDisplay || IsTextDisplay;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            var uuid = BdObjectHelper.GetUuid(Nbt);
            if (!string.IsNullOrEmpty(uuid))
            {
                _id = uuid;
                return;
            }

            var tag = BdObjectHelper.GetTags(Nbt);
            if (!string.IsNullOrEmpty(tag))
            {
                _id = tag;
            }
        }

        private string GetID()
        {
            if (!string.IsNullOrEmpty(_id)) return _id;

            if (Children == null || Children.Length == 0)
            {
                _id = Name;
            }
            else
            {
                List<string> childIds = new List<string>();
                foreach (var child in Children)
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



        // private string GetID()
        // {
        //     if (!string.IsNullOrEmpty(_id)) return _id;

        //     if (Children == null) _id = Name;
        //     else
        //     {
        //         var childSum = new StringBuilder();
        //         foreach (var child in Children)
        //         {
        //             childSum.Append(child.ID);
        //             // childSum += child.GetID();
        //         }

        //         if (Children.Length <= 1) childSum.Append("g");
        //         _id = childSum.ToString();
        //     }

        //     return _id;
        // }
    }
}

