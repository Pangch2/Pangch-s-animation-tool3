using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

public class BDObjectManager : BaseManager
{
    // ���÷��� ������
    public Material BDObjTransportMaterial;
    public Material BDObjHeadMaterial;

    public Transform BDObjectParent;
    public BDObejctContainer BDObjectPrefab;

    public int BDObjectCount = 0;
    //public List<BDObejctContainer> BDObjectList = new List<BDObejctContainer>();
    public Dictionary<string, BDObejctContainer> BDObjects = new Dictionary<string, BDObejctContainer>();

    [Header("Prefabs")]
    public BlockDisplay blockDisplay;
    public ItemDisplay itemDisplay;
    public TextDisplay textDisplay;

    public MeshRenderer cubePrefab;
    public ItemModelGenerator itemPrefab;
    public BlockModelGenerator blockPrefab;
    public HeadGenerator headPrefab;

    // �ֻ��� BDObject�� �ϳ� �޾Ƽ� ��ü ���� ������ �����ϰ�,
    // �ش� �ֻ��� ������Ʈ�� Dictionary�� ����Ѵ�.
    public async Task AddObject(BDObject bdObject, string fileName)
    {
        // �ֻ��� BDObject�� Ʈ�� ������ ����
        var rootObj = await CreateObjectHierarchyAsync(bdObject, BDObjectParent);

        // �ֻ��� ������Ʈ�� Dictionary�� ���
        BDObjects[fileName] = rootObj;
    }

    // bdObject �ϳ��� �޾� �ڽ��� GameObject�� �����ϰ�, �� �ڽĵ鵵 ��������� �����Ѵ�.
    private async Task<BDObejctContainer> CreateObjectHierarchyAsync(BDObject bdObject, Transform parent, int batchSize = 10)
    {
        // BDObjectPrefab ������� �ν��Ͻ� ����
        var newObj = Instantiate(BDObjectPrefab, parent);

        // �ʱ�ȭ
        newObj.Init(bdObject, this);
        BDObjectCount++;

        // �ڽ� Ʈ�� ����
        if (bdObject.children != null && bdObject.children.Length > 0)
        {
            for (int i = 0; i < bdObject.children.Length; i++)
            {
                await CreateObjectHierarchyAsync(bdObject.children[i], newObj.transform, batchSize);

                // ���� �������� �� �����Ӿ� ���
                if (i % batchSize == 0)
                {
                    await Task.Yield();
                }
            }
        }

        // ���� ��ó��
        newObj.PostProcess();

        return newObj;
    }

    //void AddObjects(BDObject[] bdObjects, Transform parent)
    //{
    //    // �迭�� ��ȸ�ϸ�
    //    int count = bdObjects.Length;
    //    for (int i = 0; i < count; i++)
    //    {
    //        // ������Ʈ ����
    //        var newObj = Instantiate(BDObjectPrefab, parent);
    //        newObj.Init(bdObjects[i], this);
    //        //BDObjectList.Add(newObj);
    //        BDObjectCount++;

    //        // �ڽ� ������Ʈ�� �߰�
    //        if (bdObjects[i].children != null)
    //        {
    //            AddObjects(bdObjects[i].children, newObj.transform);
    //        }

    //        // �ڽ� ���� ���� �� ��ó��.
    //        newObj.PostProcess();
    //    }
    //}

    public void ClearAllObject()
    {
        Destroy(BDObjectParent.gameObject);
        BDObjectParent = new GameObject("BDObjectParent").transform;
        BDObjectParent.localScale = new Vector3(1, 1, -1);
    }
}
