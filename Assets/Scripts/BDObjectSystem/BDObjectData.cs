using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BDObjectSystem
{
    /// <summary>
    /// JSON 파일의 구조를 그대로 반영하는 순수한 데이터 모델 클래스입니다.
    /// </summary>
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
    }
}