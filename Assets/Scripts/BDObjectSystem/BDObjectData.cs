using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BDObjectSystem
{
    /// <summary>
    /// JSON 파일의 구조를 그대로 반영하는 순수한 데이터 모델 클래스입니다.
    /// </summary>
    [Serializable]
    public class BdObjectData
    {
        public string name;
        public string nbt;
        public bool isBlockDisplay;
        public bool isItemDisplay;
        public bool isTextDisplay;
        public float[] transforms;
        public JObject options;
        public BdObjectData[] children;

        [JsonExtensionData]
        public Dictionary<string, object> ExtraData;

        /// <summary>
        /// 이 데이터 객체의 깊은 복사본을 생성합니다.
        /// </summary>
        /// <returns>모든 하위 데이터를 포함하여 완전히 복제된 새 BdObjectData 인스턴스입니다.</returns>
        public BdObjectData Clone()
        {
            var clone = new BdObjectData
            {
                name = this.name,
                nbt = this.nbt,
                isBlockDisplay = this.isBlockDisplay,
                isItemDisplay = this.isItemDisplay,
                isTextDisplay = this.isTextDisplay,
                
                // 참조 타입들은 각각 깊은 복사를 수행합니다.
                transforms = this.transforms != null ? (float[])this.transforms.Clone() : null,
                options = this.options?.DeepClone() as JObject,
                ExtraData = this.ExtraData != null ? new Dictionary<string, object>(this.ExtraData) : null,
                
                // 자식들도 재귀적으로 복제합니다.
                children = this.children?.Select(c => c.Clone()).ToArray()
            };
            return clone;
        }
    }
}