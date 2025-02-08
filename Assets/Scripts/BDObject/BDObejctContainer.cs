using Newtonsoft.Json.Linq;
using UnityEngine;

public class BDObejctContainer : MonoBehaviour
{
    public BDObject BDObject;
    public DisplayObject displayObj;

    public Matrix4x4 transformation;

    public BDObejctContainer Init(BDObject bdObject)
    {
        // ���� ����
        BDObject = bdObject;
        gameObject.name = bdObject.name;

        // ���÷��̶��
        if (bdObject.isBlockDisplay || bdObject.isItemDisplay)
        {
            // �̸��� ���� ����
            int typeStart = bdObject.name.IndexOf('[');
            if (typeStart == -1)
            {
                typeStart = bdObject.name.Length;
            }
            string name = bdObject.name.Substring(0, typeStart);
            string state = bdObject.name.Substring(typeStart);
            state = state.Replace("[", "").Replace("]", "");

            // ��� ���÷����� ��
            if (bdObject.isBlockDisplay)
            {
                // ��� ���÷��� �ڽ����� ���� �� �� �ε�
                GameObject blockDisplay = new GameObject("BlockDisplay");
                blockDisplay.transform.SetParent(transform);
                blockDisplay.transform.localPosition = Vector3.zero;
                displayObj = blockDisplay.AddComponent<BlockDisplay>().LoadDisplayModel(name, state);

                // blockDisplay�� ��ġ�� ���� �ϴܿ� ����
                blockDisplay.transform.localPosition = -displayObj.AABBBound.min;
            }
            // ������ ���÷����� ��
            else
            {
                // ������ ���÷��� �ڽ����� ���� �� �� �ε�
                GameObject itemDisplay = new GameObject("ItemDisplay");
                itemDisplay.transform.SetParent(transform);
                itemDisplay.transform.localPosition = Vector3.zero;
                displayObj = itemDisplay.AddComponent<ItemDisplay>().LoadDisplayModel(name, state);
            }
        }

        return this;
    }

    // ��ó��
    public void PostProcess()
    {
        // ��ȯ ����� ����
        transformation = AffineTransformation.GetMatrix(BDObject.transforms);
        AffineTransformation.ApplyMatrixToTransform(transform, transformation);
    }

    
}
