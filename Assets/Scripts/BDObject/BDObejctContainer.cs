using Newtonsoft.Json.Linq;
using UnityEngine;

public class BDObejctContainer : MonoBehaviour
{
    public BDObject BDObject;
    public DisplayObject displayObj;

    public Matrix4x4 transformation;

    public void Init(BDObject bdObject, BDObjectManager manager)
    {
        // ���� ����
        BDObject = bdObject;
        gameObject.name = bdObject.name;

        // ���÷��̶��
        if (bdObject.isBlockDisplay || bdObject.isItemDisplay || bdObject.isTextDisplay)
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
                //GameObject blockDisplay = new GameObject("BlockDisplay");
                //blockDisplay.transform.SetParent(transform);
                //blockDisplay.transform.localPosition = Vector3.zero;
                //displayObj = blockDisplay.AddComponent<BlockDisplay>().LoadDisplayModel(name, state);
                var obj = Instantiate(manager.blockDisplay, transform);
                obj.LoadDisplayModel(name, state);
                displayObj = obj;

                // blockDisplay�� ��ġ�� ���� �ϴܿ� ����
                obj.transform.localPosition = -obj.AABBBound.min/2;
            }
            // ������ ���÷����� ��
            else if (bdObject.isItemDisplay)
            {
                // ������ ���÷��� �ڽ����� ���� �� �� �ε�
                //GameObject itemDisplay = new GameObject("ItemDisplay");
                //itemDisplay.transform.SetParent(transform);
                //itemDisplay.transform.localPosition = Vector3.zero;
                //displayObj = itemDisplay.AddComponent<ItemDisplay>();
                var obj = Instantiate(manager.itemDisplay, transform);
                obj.LoadDisplayModel(name, state);
                displayObj = obj;
            }
            // �ؽ�Ʈ ���÷����� ��
            else
            {
                var obj = Instantiate(manager.textDisplay, transform);
                obj.Init(bdObject);
                displayObj = obj;

            }
        }
    }

    // ��ó��
    public void PostProcess()
    {
        // ��ȯ ����� ����
        transformation = AffineTransformation.GetMatrix(BDObject.transforms);
        AffineTransformation.ApplyMatrixToTransform(transform, transformation);

        //if (displayObj == null)
        //{
        //    transform.position = new Vector3(transform.position.x, transform.position.y, -transform.position.z);
        //}
    }

    
}
