using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnimObject : MonoBehaviour
{
    public RectTransform rect;
    public TextMeshProUGUI title;
    public Frame firstFrame;
    public SortedList<int, Frame> frames = new SortedList<int, Frame>();
    public string fileName;

    public int MaxTick => frames.Values[frames.Count-1].Tick;

    AnimObjList manager;

    BDObjectContainer root;
    Dictionary<string, BDObjectContainer> idDict;

    public void Init(string FileName, AnimObjList list)
    {
        title.text = FileName;
        manager = list;
        fileName = FileName;

        var bdObject = GameManager.GetManager<BDObjectManager>().BDObjects[FileName];
        root = bdObject.Item1;
        idDict = bdObject.Item2;

        firstFrame.Init(fileName, 0, GameManager.Instance.Setting.DefaultInterpolation, root.BDObject, this);
        frames[0] = firstFrame;

        AnimManager.TickChanged += OnTickChanged;

    }

    #region Transform
    void OnTickChanged(int tick)
    {
        // ƽ�� �´� �������� ã��
        int left = GetLeftFrame(tick);
        if (left < 0) return;
        Frame leftFrame = frames.Values[left];

        if (leftFrame.interpolation == 0 || leftFrame.Tick + leftFrame.interpolation <= tick) 
        {

            // ���� ���� ����
            SetObjectTransformation(root.BDObject.ID, leftFrame.info);
        }
        else
        {
            // ���� On
            float t = (float)(tick - leftFrame.Tick) / leftFrame.interpolation;

            // ù��° �������� inter���� �׻� 0�̶� ������ ���� ����
            Frame before = frames.Values[left - 1];
            SetObjectTransformationInter(t, before, leftFrame);
        }

    }

    // ���� ���� �״�� ����
    public void SetObjectTransformation(string id, BDObject obj)
    {
        if (!idDict.TryGetValue(id, out BDObjectContainer target))
        {
            Debug.Log("Target not found, name : " + id);
            return;
        }

        target.SetTransformation(obj.transforms);

        if (obj.children != null)
        {
            foreach (var child in obj.children)
            {
                SetObjectTransformation(child.ID, child);
            }
        }
    }

    public void SetObjectTransformationInter(float t, Frame a, Frame b)
        => SetObjectTransformationInter(
            root.BDObject.ID, t, 
            a.info, b.info, 
            a.IDDataDict, b.IDDataDict,
            new HashSet<string>());
    // target�� a, b�� t ������ �����Ͽ� ����
    void SetObjectTransformationInter(
        string targetName, float t, 
        BDObject a, BDObject b, 
        Dictionary<string, BDObject> aDict, Dictionary<string, BDObject> bDict,
        HashSet<string> visitedNodes
        )
    {
        if (visitedNodes.Contains(targetName)) return;
        visitedNodes.Add(targetName);

        if (!idDict.TryGetValue(targetName, out BDObjectContainer target))
        {
            Debug.Log("Target not found, name : " + targetName);
            return;
        }

        // 1. transforms(4x4 ���, float[16])�� t ������ ����
        float[] result = new float[16];
        for (int i = 0; i < 16; i++)
        {
            result[i] = a.transforms[i] * (1f - t) + b.transforms[i] * t;
        }

        // 2. ������ ����� target�� ����
        target.SetTransformation(result);

        // �ڽ��� ������ ����
        if (a.children == null && b.children == null) return;

        HashSet<string> processedKeys = new HashSet<string>();

        foreach (var key in aDict.Keys)
        {
            if (bDict.TryGetValue(key, out BDObject bChild) && aDict[key] != null)
            {
                BDObject aChild = aDict[key];
                processedKeys.Add(key);

                if (idDict.TryGetValue(key, out BDObjectContainer targetChild))
                {
                    SetObjectTransformationInter(key, t, aChild, bChild, aDict, bDict, visitedNodes);
                }
            }
        }

        foreach (var key in bDict.Keys)
        {
            if (!processedKeys.Contains(key) && aDict.TryGetValue(key, out BDObject aChild))
            {
                BDObject bChild = bDict[key];

                if (idDict.TryGetValue(key, out BDObjectContainer targetChild))
                {
                    SetObjectTransformationInter(key, t, aChild, bChild, aDict, bDict, visitedNodes);
                }
            }
        }
    }


    int GetLeftFrame(int tick)
    {

        // 1. ���� ���� �����Ӻ��� tick�� ������ null ��ȯ
        if (frames.Values[0].Tick > tick)
            return -1;

        int left = 0;
        int right = frames.Count - 1;
        var keys = frames.Keys;
        int idx = -1; // �ʱ갪�� -1�� ���� (��ȿ�� �ε����� ���� ��� ���)

        // 2. ���� Ž������ left ������ ã��
        while (left <= right)
        {
            int mid = (left + right) / 2;
            if (keys[mid] <= tick) // "<" ��� "<=" ����Ͽ� ��Ȯ�� tick�� ��� mid�� idx�� ����
            {
                idx = mid; // ���� mid�� left �ĺ�
                left = mid + 1; // �� ū �� Ž��
            }
            else
            {
                right = mid - 1; // �� ���� �� Ž��
            }
        }

        // 3. leftIdx ���� (idx�� -1�� ��쵵 ���)
        if (idx >= 0)
        {
            return idx;
        }

        return -1;
    }

    #endregion

    #region EditFrame

    // Ŭ������ �� 
    public void OnEventTriggerClick(BaseEventData eventData)
    {
        PointerEventData pointerData = eventData as PointerEventData;

        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            //Debug.Log("Right Click");
            var line = manager.Timeline.GetTickLine(pointerData.position);
            GameManager.GetManager<ContextMenuManager>().ShowContextMenu(this, line.Tick);
        }
    }

    // tick ��ġ�� ������ �߰��ϱ�. ���� tick�� �̹� �������� �ִٸ� tick�� �׸�ŭ �ڷ� �̷�
    // ���� �Է����� ���� BDObject�� firstFrame�� �ٸ� ���¶�� �ź�
    public void AddFrame(string fileName, BDObject frameInfo, int tick, int inter = -1)
    {
        //Debug.Log("fileName : " + fileName + ", tick : " + tick + ", inter : " + inter);

        var frame = Instantiate(manager.framePrefab, transform.GetChild(0));

        while (frames.ContainsKey(tick))
        {
            tick++;
        }

        frames.Add(tick, frame);
        frame.Init(fileName, tick, inter < 0 ? GameManager.Instance.Setting.DefaultInterpolation : inter, frameInfo, this);
    }

    // �̸����� s, i ���� �����Ͽ� ������ �߰��ϱ�
    public void AddFrame(BDObject frameInfo, string fileName)
    {
        int tick = MaxTick;

        if (GameManager.Instance.Setting.UseNameInfoExtract)
        {
            int sValue = BDObjectHelper.ExtractNumber(fileName, "s", 0);
            int iValue = BDObjectHelper.ExtractNumber(fileName, "i", -1);

            if (sValue > 0)
                tick += sValue;
            else
                tick += GameManager.Instance.Setting.DefaultTickInterval;

            AddFrame(fileName, frameInfo, tick, iValue);
        }
        else
        {
            AddFrame(fileName, frameInfo, tick + GameManager.Instance.Setting.DefaultTickInterval);
        }
    }

    // ������ �����ϱ�
    public void RemoveFrame(Frame frame)
    {
        frames.Remove(frame.Tick);
        Destroy(frame.gameObject);

        if (frames.Count == 0)
        {
            RemoveAnimObj();
        }
        else if (frame.Tick == 0)
        {
            frames.Values[0].SetTick(0);
        }

    }

    // �ִϸ��̼� ������Ʈ �����ϱ�
    public void RemoveAnimObj()
    {
        AnimManager.TickChanged -= OnTickChanged;
        manager.RemoveAnimObject(this);
    }

    // ��ġ�� ���� �������� Ȯ���ϰ� ���� �����ϸ� frames �����ϰ� true ��ȯ
    public bool ChangePos(Frame frame, int firstTick, int changedTick)
    {
        //Debug.Log("firstTick : " + firstTick + ", changedTick : " +  changedTick);
        if (firstTick == changedTick) return true;
        if (frames.ContainsKey(changedTick)) return false;

        frames.Remove(firstTick);
        frames.Add(changedTick, frame);

        OnTickChanged(GameManager.GetManager<AnimManager>().Tick);
        return true;
    }

    //bool CheckBDObject(BDObject a, BDObject b, bool IsFirst = false)
    //{
    //    //Debug.Log(a?.ToString() + " vs " + b?.ToString());

    //    // 1) �� �� null�̸� "����"�� ����
    //    if (a == null && b == null)
    //    {
    //        //CustomLog.Log("Both objects are null �� Considered equal.");
    //        return true;
    //    }

    //    // 2) �� �ʸ� null�̸� �ٸ�
    //    if (a == null || b == null)
    //    {
    //        CustomLog.LogError($"One object is null -> a: {(a == null ? "null" : a.name)}, b: {(b == null ? "null" : b.name)}");
    //        return false;
    //    }

    //    // 3) name�� �ٸ��� �ٷ� false
    //    if (a.name != b.name && !IsFirst)
    //    {
    //        CustomLog.LogError($"Different Name -> a: {a.name}, b: {b.name}");
    //        return false;
    //    }

    //    // 4) children ��
    //    if (a.children == null && b.children == null)
    //    {
    //        //CustomLog.Log($"Both '{a.name}' and '{b.name}' have no children �� Considered equal.");
    //        return true;
    //    }

    //    if (a.children == null || b.children == null)
    //    {
    //        CustomLog.LogError($"Children mismatch -> a: {(a.children == null ? "null" : "exists")}, b: {(b.children == null ? "null" : "exists")}");
    //        return false;
    //    }

    //    // ���̰� �ٸ��� false
    //    if (a.children.Length != b.children.Length)
    //    {
    //        CustomLog.LogError($"Children count mismatch -> a: {a.children.Length}, b: {b.children.Length}");
    //        CustomLog.LogError($"a: {string.Join(", ", a.children.Select(c => c.name))}");
    //        CustomLog.LogError($"b: {string.Join(", ", b.children.Select(c => c.name))}");
    //        return false;
    //    }

    //    // ������ �ڽ��� ��������� ��
    //    for (int i = 0; i < a.children.Length; i++)
    //    {
    //        if (!CheckBDObject(a.children[i], b.children[i]))
    //        {
    //            CustomLog.LogError($"Child mismatch at index {i} �� a: {a.children[i]?.name}, b: {b.children[i]?.name}");
    //            return false;
    //        }
    //    }

    //    // ���� ��� �˻縦 ����ϸ� true
    //    return true;
    //}
    #endregion
}
