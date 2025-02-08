using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BDObjectManager : RootManager
{
    public Material BDObjTransportMaterial;
    public Transform BDObjectParent;
    public BDObejctContainer BDObjectPrefab;
    public List<BDObejctContainer> BDObjectList = new List<BDObejctContainer>();

    public void AddObjectByCo(BDObject[] bdObjects) => StartCoroutine(AddObejctUsingCo(bdObjects, BDObjectParent));

    //public void AddObjects(List<BDObject> bdObjects) => StartCoroutine(DebugCo(bdObjects));

    IEnumerator DebugCo(BDObject[] bdObjects)
    {
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        yield return StartCoroutine(AddObejctUsingCo(bdObjects, BDObjectParent));

        stopwatch.Stop();
        Debug.Log($"AddObjects Time: {stopwatch.ElapsedMilliseconds}ms");

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
            var newObj = Instantiate(BDObjectPrefab, parent).Init(bdObjects[i]);
            BDObjectList.Add(newObj);

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

            var newObj = Instantiate(BDObjectPrefab, parent).Init(bdObject);
            BDObjectList.Add(newObj);

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
        foreach (var obj in BDObjectList)
        {
            Destroy(obj.gameObject);
        }
        BDObjectList.Clear();
    }
}
