using Newtonsoft.Json.Linq;
using UnityEngine;

public class BDObejctContainer : MonoBehaviour
{
    public BDObject BDObject;

    public Matrix4x4 transformation;

    public BDObejctContainer Init(BDObject bdObject)
    {
        BDObject = bdObject;
        gameObject.name = bdObject.name;

        // ���÷��̶��
        if (bdObject.isBlockDisplay || bdObject.isItemDisplay)
        {
            //var block = Resources.Load<GameObject>("Prefab/Block");
            //var blockObj = Instantiate(block, transform);
            //blockObj.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);

            // �̸��� ���� �и�
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
                GameObject blockDisplay = new GameObject("BlockDisplay");
                blockDisplay.transform.SetParent(transform);
                blockDisplay.transform.localPosition = Vector3.zero;
                blockDisplay.AddComponent<BlockDisplay>().LoadBlockModel(name, state);
                //LoadBlockModel(name, state);
            }
            // ������ ���÷����� ��
            else
            {
                //LoadItemModel(name, state);
            }
        }

        // �ڽ� ������Ʈ�� �߰�
        var manager = GameManager.GetManager<BDObjectManager>();
        if (bdObject.children != null)
        {
            foreach (var child in BDObject.children)
            {
                manager.AddObject(transform, child);
            }
        }

        // ��ȯ ����� ����
        transformation = AffineTransformation.GetMatrix(BDObject.transforms);
        AffineTransformation.ApplyMatrixToTransform(transform, transformation);

        return this;
    }

    
}
