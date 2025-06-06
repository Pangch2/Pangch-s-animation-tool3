using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using GameSystem;
using BDObjectSystem.Display;
using BDObjectSystem.Utility;
using Animation;
using Animation.AnimFrame;
using Cysharp.Threading.Tasks;

namespace BDObjectSystem
{
    public class BdObjectManager : BaseManager
    {
        #region Variables
        // BDObjects Property
        [Header("BDObject Materials")]
        [FormerlySerializedAs("BDObjTransportMaterial")] public Material bdObjTransportMaterial;
        [FormerlySerializedAs("BDObjHeadMaterial")] public Material bdObjHeadMaterial;

        [Header("Variables and Transforms")]
        public Transform bdObjectParent;
        public int bdObjectCount;
        public readonly Dictionary<string, BDObjectAnimator> BDObjectAnim = new();
        // public readonly Dictionary<string, (BdObjectContainer, Dictionary<string, BdObjectContainer>)> BdObjects = new();
        public BdObjectContainer currentBdObject;

        [Header("Prefabs")]
        [FormerlySerializedAs("BDObjectPrefab")] public BdObjectContainer bdObjectPrefab;
        public BlockDisplay blockDisplay;
        public ItemDisplay itemDisplay;
        public TextDisplay textDisplay;

        public MeshRenderer cubePrefab;
        public ItemModelGenerator itemPrefab;
        public BlockModelGenerator blockPrefab;
        public HeadGenerator headPrefab;

        #endregion

        #region Make BDObject


        public async UniTask AddObject(BdObject bdObject, string fileName)
        {
            bdObjectCount = 0;
            // BDObjectContainer 생성하기 
            currentBdObject = await CreateObjectHierarchyAsync(bdObject, bdObjectParent);

            BDObjectAnim[fileName] = new BDObjectAnimator(currentBdObject);
            Debug.Log($"AddObject: {fileName}");
        }

        // BDObject 따라가면서 BDObjectContainer 생성 
        private async UniTask<BdObjectContainer> CreateObjectHierarchyAsync(BdObject bdObject, Transform parent, BdObjectContainer parentBdobj = null, int batchSize = 10)
        {
            // BDObjectPrefab 으로 생성하기 
            var newObj = Instantiate(bdObjectPrefab, parent);
            newObj.parent = parentBdobj;

            // Set BDObjectContainer
            newObj.Init(bdObject, this);
            bdObjectCount++;

            BdObjectContainer[] children = null;

            // when children exist
            if (bdObject.children is { Length: > 0 })
            {
                children = new BdObjectContainer[bdObject.children.Length];
                for (var i = 0; i < bdObject.children.Length; i++)
                {
                    children[i] = await CreateObjectHierarchyAsync(bdObject.children[i], newObj.transform, newObj, batchSize);

                    // delay
                    if (i % batchSize == 0)
                    {
                        await UniTask.Yield();
                    }
                }
            }

            // 위치 처리 
            newObj.PostProcess(children);

            return newObj;
        }
        #endregion


        // 모든 BDObject 제거        
        public void ClearAllObject()
        {
            Destroy(bdObjectParent.gameObject);
            bdObjectParent = new GameObject("BDObjectParent").transform;
            bdObjectParent.localScale = new Vector3(1, 1, -1);
            BDObjectAnim.Clear();

        }

        // BDObject 제거
        public void RemoveBdObject(string bdName)
        {
            if (BDObjectAnim.Remove(bdName, out var obj))
            {
                Destroy(obj.RootObject.gameObject);
            }
        }
    }
}
