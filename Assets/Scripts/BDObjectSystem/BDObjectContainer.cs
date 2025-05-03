using GameSystem;
using UnityEngine;
using BDObjectSystem.Display;
using BDObjectSystem.Utility;

namespace BDObjectSystem
{
    public class BdObjectContainer : MonoBehaviour
    {
        public string bdObjectID;

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
            gameObject.name = bdObject.name;
            bdObjectID = bdObject.ID;

            // 그룹과 디스플레이 구분 
            if (!bdObject.isBlockDisplay && !bdObject.isItemDisplay && !bdObject.isTextDisplay) return;

            // 디스플레이 공통부분
            var typeStart = bdObject.name.IndexOf('[');
            if (typeStart == -1)
            {
                typeStart = bdObject.name.Length;
            }
            var modelName = bdObject.name[..typeStart];
            var state = bdObject.name[typeStart..];
            state = state.Replace("[", "").Replace("]", "");

            // 블록 디스플레이
            if (bdObject.isBlockDisplay)
            {
                var obj = Instantiate(manager.blockDisplay, transform);
                obj.LoadDisplayModel(modelName, state);
                displayObj = obj;

                // blockDisplay�� ��ġ�� ���� �ϴܿ� ����
                obj.transform.localPosition = -obj.AABBBound.min / 2;
            }
            // 아이템 디스플레이
            else if (bdObject.isItemDisplay)
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

        // ��ó��
        public void PostProcess(BdObjectContainer[] childArray)
        {
            // ��ȯ ����� ����
            SetTransformation(BdObject.transforms);
            children = childArray;

            //if (displayObj == null)
            //{
            //    transform.position = new Vector3(transform.position.x, transform.position.y, -transform.position.z);
            //}
        }

        public void SetTransformation(float[] transform) => SetTransformation(AffineTransformation.GetMatrix(transform));

        public void SetTransformation(in Matrix4x4 mat)
        {
            transformation = mat;
            AffineTransformation.ApplyMatrixToTransform(transform, transformation);
        }

    }
}
