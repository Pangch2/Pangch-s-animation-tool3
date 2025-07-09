using GameSystem;
using UnityEngine;
using BDObjectSystem.Display;
using BDObjectSystem.Utility;
using System;

namespace BDObjectSystem
{
    public class BdObjectContainer : MonoBehaviour
    {
        public string BdObjectID => BdObject.ID;
#if UNITY_EDITOR
        public string bdObjectID;
        void Update()
        {
            bdObjectID = BdObject.ID;
        }
#endif

        public BdObject BdObject;
        public DisplayObject displayObj;

        public BdObjectContainer[] children;
        public BdObjectContainer parent;

        public Matrix4x4 transformation;
        public Matrix4x4 parentMatrix;

        public bool IsParentNull = false;

        public void Init(BdObject bdObject, BdObjectManager manager)
        {
            // 기본 정보 설정
            BdObject = bdObject;
            gameObject.name = bdObject.Data.name;
            // bdObjectID = bdObject.ID;

            // 그룹과 디스플레이 구분 
            if (!bdObject.Data.isBlockDisplay && !bdObject.Data.isItemDisplay && !bdObject.Data.isTextDisplay) return;

            // 블록 디스플레이
            if (bdObject.Data.isBlockDisplay)
            {
                var obj = Instantiate(manager.blockDisplay, transform);
                obj.LoadDisplayModel(bdObject.ParsedName, bdObject.ParsedState);
                displayObj = obj;

                // blockDisplay�� ��ġ�� ���� �ϴܿ� ����
                obj.transform.localPosition = -obj.AABBBound.min / 2;
            }
            // 아이템 디스플레이
            else if (bdObject.Data.isItemDisplay)
            {
                var obj = Instantiate(manager.itemDisplay, transform);
                obj.LoadDisplayModel(bdObject.ParsedName, bdObject.ParsedState);
                displayObj = obj;
            }
            // 텍스트 디스플레이 
            else
            {
                var obj = Instantiate(manager.textDisplay, transform);
                obj.Init(bdObject);
                displayObj = obj;

            }
        }

        // 마지막에 호출되는 PostProcess
        public void PostProcess(BdObjectContainer[] childArray)
        {
            // 좌표 설정
            SetTransformation(BdObject.Data.transforms);
            children = childArray;

            //if (displayObj == null)
            //{
            //    transform.position = new Vector3(transform.position.x, transform.position.y, -transform.position.z);
            //}
        }

        public void SetTransformation(float[] transform) => SetTransformation(MatrixHelper.GetMatrix(transform));

        public void SetTransformation(in Matrix4x4 mat)
        {
            transformation = mat;
            MatrixHelper.ApplyMatrixToTransform(transform, transformation);
        }

        public void ChangeBDObject(BdObject bdObject)
        {
            // 1. 새로운 BdObject 정보로 교체합니다.
            this.BdObject = bdObject;
            gameObject.name = bdObject.Data.name;

            // 2. 기존에 있던 디스플레이 모델(블록, 아이템 등)을 파괴합니다.
            if (displayObj != null)
            {
                Destroy(displayObj.gameObject);
                displayObj = null;
            }

            // 3. Init 메서드의 로직을 재활용하여 새로운 디스플레이 모델을 생성합니다.
            // 그룹 객체는 디스플레이가 없으므로 바로 종료합니다.
            if (!bdObject.Data.isBlockDisplay && !bdObject.Data.isItemDisplay && !bdObject.Data.isTextDisplay) return;

            // 디스플레이 공통 처리
            var typeStart = bdObject.Data.name.IndexOf('[');
            if (typeStart == -1)
            {
                typeStart = bdObject.Data.name.Length;
            }
            var modelName = bdObject.Data.name[..typeStart];
            var state = bdObject.Data.name[typeStart..].Replace("[", "").Replace("]", "");

            // BdObjectManager 인스턴스를 가져옵니다.
            var manager = GameManager.GetManager<BdObjectManager>();

            // 블록 디스플레이
            if (bdObject.Data.isBlockDisplay)
            {
                var obj = Instantiate(manager.blockDisplay, transform);
                obj.LoadDisplayModel(modelName, state);
                displayObj = obj;
                obj.transform.localPosition = -obj.AABBBound.min / 2;
            }
            // 아이템 디스플레이
            else if (bdObject.Data.isItemDisplay)
            {
                var obj = Instantiate(manager.itemDisplay, transform);
                obj.LoadDisplayModel(modelName, state);
                displayObj = obj;
            }
            // 텍스트 디스플레이
            else
            {
                var obj = Instantiate(manager.textDisplay, transform);
                obj.Init(bdObject);
                displayObj = obj;
            }
        }
    }
}
