using System.Collections.Generic;
using System.Linq;
using BDObjectSystem.Utility;

namespace BDObjectSystem
{
    /// <summary>
    /// 런타임에서 사용되는 객체로, 데이터(BdObjectData)를 기반으로 추가 로직과 캐싱된 상태를 가집니다.
    /// </summary>
    public class BdObject
    {
        public BdObjectData Data { get; } // 원본 데이터

        // --- 런타임 속성 및 관계 ---
        public BdObject Parent { get; set; }
        public BdObject[] Children { get; private set; }

        // --- 캐싱된 속성 ---
        private string _id;
        public string ID => GetID();

        public bool IsDisplay => Data.isBlockDisplay || Data.isItemDisplay || Data.isTextDisplay;
        public bool IsHeadDisplay { get; private set; }

        private bool _isNameParsed = false;
        private string _parsedName;
        private string _parsedState;

        public string ParsedName { get { ParseNameIfNeeded(); return _parsedName; } }
        public string ParsedState { get { ParseNameIfNeeded(); return _parsedState; } }

        /// <summary>
        /// 데이터 모델(BdObjectData)을 기반으로 런타임 객체(BdObject)를 생성합니다.
        /// </summary>
        public BdObject(BdObjectData data, BdObject parent = null)
        {
            Data = data;
            Parent = parent;

            // 자식 객체들도 재귀적으로 생성
            if (data.children != null)
            {
                Children = data.children.Select(childData => new BdObject(childData, this)).ToArray();
            }

            // 역직렬화 시점에 수행하던 초기화 로직
            Initialize();
        }

        private void Initialize()
        {
            // IsHeadDisplay 값 계산
            IsHeadDisplay = Data.isItemDisplay && (Data.name?.Contains("player_head") ?? false);

            // ID 값 초기화 (UUID 또는 Tag 우선)
            var uuid = BdObjectHelper.GetUuid(Data.nbt);
            if (!string.IsNullOrEmpty(uuid))
            {
                _id = uuid;
                return;
            }
            var tag = BdObjectHelper.GetTags(Data.nbt);
            if (!string.IsNullOrEmpty(tag))
            {
                _id = tag;
            }
        }

        private void ParseNameIfNeeded()
        {
            if (_isNameParsed) return;

            if (string.IsNullOrEmpty(Data.name))
            {
                _parsedName = null;
                _parsedState = null;
            }
            else
            {
                var typeStart = Data.name.IndexOf('[');
                if (typeStart == -1)
                {
                    _parsedName = Data.name;
                    _parsedState = string.Empty;
                }
                else
                {
                    _parsedName = Data.name[..typeStart];
                    _parsedState = Data.name[typeStart..].Replace("[", "").Replace("]", "");
                }
            }
            _isNameParsed = true;
        }

        public string GetHeadTexture() => Data.ExtraData.GetValueOrDefault("defaultTextureValue", string.Empty) as string;

        private string GetID()
        {
            if (!string.IsNullOrEmpty(_id)) return _id;

            if (Children == null || Children.Length == 0)
            {
                _id = string.Empty;
            }
            else
            {
                var childIds = Children.Select(child => child.GetID()).ToList();
                childIds.Sort();
                _id = $"[{string.Join(",", childIds)}]";
            }
            return _id;
        }

        public string GetEntityType()
        {
            return Data.isBlockDisplay ? "block_display" :
                   Data.isItemDisplay ? "item_display" :
                   Data.isTextDisplay ? "text_display" : null;
        }
        
        // Clone 메서드는 필요에 따라 수정해야 합니다.
        // 이제 BdObjectData를 복제하고 새 BdObject를 생성하는 방식이 될 것입니다.
    }
}

