using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

public class BDObjectManager : RootManager
{
    public Material BDObjTransportMaterial;
    public Transform BDObjectParent;
    public BDObejctContainer BDObjectPrefab;

    public int BDObjectCount = 0;
    //public List<BDObejctContainer> BDObjectList = new List<BDObejctContainer>();

    [Header("Prefabs")]
    public BlockDisplay blockDisplay;
    public ItemDisplay itemDisplay;

    public MeshRenderer cubePrefab;
    public ItemModelGenerator itemPrefab;
    public BlockModelGenerator blockPrefab;
    public HeadGenerator headPrefab;

    public void AddObjectByCo(BDObject[] bdObjects) => StartCoroutine(AddObejctUsingCo(bdObjects, BDObjectParent));

    //public void AddObjects(List<BDObject> bdObjects) => StartCoroutine(DebugCo(bdObjects));

    IEnumerator DebugCo(BDObject[] bdObjects)
    {
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        yield return StartCoroutine(AddObejctUsingCo(bdObjects, BDObjectParent));

        stopwatch.Stop();
        CustomLog.Log($"AddObjects Time: {stopwatch.ElapsedMilliseconds}ms");

    }

    // Transform�� �⺻������ �����ϱ�
    public void AddObjects(BDObject[] bdObjects) => AddObjects(bdObjects, BDObjectParent);

    void AddObjects(BDObject[] bdObjects, Transform parent)
    {
        // �迭�� ��ȸ�ϸ�
        int count = bdObjects.Length;
        for (int i = 0; i < count; i++)
        {
            // ������Ʈ ����
            var newObj = Instantiate(BDObjectPrefab, parent);
            newObj.Init(bdObjects[i], this);
            //BDObjectList.Add(newObj);
            BDObjectCount++;

            // �ڽ� ������Ʈ�� �߰�
            if (bdObjects[i].children != null)
            {
                AddObjects(bdObjects[i].children, newObj.transform);
            }

            // �ڽ� ���� ���� �� ��ó��.
            newObj.PostProcess();
        }
    }

    IEnumerator AddObejctUsingCo(BDObject[] bdObjects, Transform parent)
    {
        int count = bdObjects.Length;
        for (int i = 0; i < count; i++)
        {
            var bdObject = bdObjects[i];

            var newObj = Instantiate(BDObjectPrefab, parent);
            newObj.Init(bdObject, this);
            //BDObjectList.Add(newObj);

            // �ڽ� ������Ʈ�� �߰�
            if (bdObject.children != null)
            {
                yield return StartCoroutine(AddObejctUsingCo(bdObject.children, newObj.transform));
            }

            newObj.PostProcess();

            yield return null;
        }
    }

    public void ClearAllObject()
    {
        Destroy(BDObjectParent.gameObject);
        BDObjectParent = new GameObject("BDObjectParent").transform;
    }
}
