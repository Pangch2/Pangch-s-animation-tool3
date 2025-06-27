using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BDObjectSystem.Utility;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;


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

        public string GetHeadTexture() => ExtraData.TryGetValue("defaultTextureValue", out var value) ? value as string : string.Empty;

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
                _id = string.Empty;
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

        public BdObject Clone()
        {
            BdObject newObj = new BdObject
            {
                name = this.name,
                nbt = this.nbt,
                isBlockDisplay = this.isBlockDisplay,
                isItemDisplay = this.isItemDisplay,
                isTextDisplay = this.isTextDisplay,
                _id = this._id, // 기존 _id 값을 복사합니다. GetID()를 통해 필요시 다시 계산될 수 있습니다.
                Parent = null  // 복제된 객체는 초기에 부모가 없습니다.
            };

            // transforms 배열 복제
            if (this.transforms != null)
            {
                newObj.transforms = new float[this.transforms.Length];
                Array.Copy(this.transforms, newObj.transforms, this.transforms.Length);
            }
            else
            {
                newObj.transforms = null;
            }

            // options JObject 복제 (Newtonsoft.Json.Linq.JObject 사용 시)
            if (this.options != null)
            {
                newObj.options = (JObject)this.options.DeepClone();
            }
            else
            {
                newObj.options = null;
            }

            // children 배열 복제 (재귀적으로 각 자식 객체도 복제)
            if (this.children != null)
            {
                newObj.children = new BdObject[this.children.Length];
                for (int i = 0; i < this.children.Length; i++)
                {
                    if (this.children[i] != null)
                    {
                        BdObject clonedChild = this.children[i].Clone();
                        clonedChild.Parent = newObj; // 복제된 자식의 부모를 현재 새로 생성된 객체로 설정
                        newObj.children[i] = clonedChild;
                    }
                    else
                    {
                        newObj.children[i] = null;
                    }
                }
            }
            else
            {
                newObj.children = null;
            }

            // ExtraData 딕셔너리 복제 (값은 얕은 복사 -> 수정할 일 없음)
            if (this.ExtraData != null)
            {
                newObj.ExtraData = new Dictionary<string, object>(this.ExtraData);
            }
            else
            {
                newObj.ExtraData = null;
            }

            return newObj;
        }
    }
}

