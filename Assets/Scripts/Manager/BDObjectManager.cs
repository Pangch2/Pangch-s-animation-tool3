using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

public class BDObjectManager : BaseManager
{
    public Material BDObjTransportMaterial;
    public Material BDObjHeadMaterial;

    public Transform BDObjectParent;
    public BDObejctContainer BDObjectPrefab;

    public int BDObjectCount = 0;
    //public List<BDObejctContainer> BDObjectList = new List<BDObejctContainer>();

    [Header("Prefabs")]
    public BlockDisplay blockDisplay;
    public ItemDisplay itemDisplay;
    public TextDisplay textDisplay;

    public MeshRenderer cubePrefab;
    public ItemModelGenerator itemPrefab;
    public BlockModelGenerator blockPrefab;
    public HeadGenerator headPrefab;

    public event Action<BDObject> EndAddObject;

    // Transform�� �⺻������ �����ϱ�
    public async Task AddObjects(BDObject[] bdObjects)
    {
        await AddObjectsAsync(bdObjects, BDObjectParent);

        EndAddObject?.Invoke(bdObjects[0]);
    }

    async Task AddObjectsAsync(BDObject[] bdObjects, Transform parent)
    {
        int count = bdObjects.Length;

        for (int i = 0; i < count; i++)
        {
            // ������Ʈ ����
            var newObj = Instantiate(BDObjectPrefab, parent);
            newObj.Init(bdObjects[i], this);
            BDObjectCount++;

            // �ڽ� ������Ʈ �񵿱� ����
            if (bdObjects[i].children != null)
            {
                await AddObjectsAsync(bdObjects[i].children, newObj.transform);
            }

            // ��ó�� ����
            newObj.PostProcess();

            // �� 10������ �� ������ ����
            if (i % 10 == 0)
            {
                await Task.Yield(); // �� ������ ��� (�ڷ�ƾ�� yield return null�� ����)
            }
        }
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
