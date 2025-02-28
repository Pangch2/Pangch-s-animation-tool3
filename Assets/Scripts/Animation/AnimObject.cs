using System.Collections.Generic;
using System.Linq;
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

    public BDObejctContainer objectContainer;

    public int MaxTick => frames.Values[frames.Count-1].Tick;

    AnimObjList manager;

    public void Init(BDObejctContainer bdObejct, AnimObjList list, string FileName)
    {
        title.text = FileName;
        manager = list;
        fileName = FileName;

        objectContainer = bdObejct;

        firstFrame.Init(0, bdObejct.BDObject, this);
        frames[0] = firstFrame;

        AnimManager.TickChanged += OnTickChanged;
    }

    void OnTickChanged(int tick)
    {
        // ƽ�� �´� �������� ã��
        var (left, right) = GetNearestFrame(tick);

        if (right == null)
        {
            // Ÿ�Ӷ����� ���� ������ �������� �Ѿ�ų� ��Ȯ�� ��ġ�ϴ� �������� ���� ��
            SetObjectTransformation(objectContainer, left.info);
        }
        else if (left == null)
        {
            // Ÿ�� �ٰ� ���� ���� �������� �Ѿ
            SetObjectTransformation(objectContainer, right.info);
        }
        else
        {
            // �� ������ ���̿� ����
            float t = (float)(tick - left.Tick) / (right.Tick - left.Tick);

        }

    }

    // ���� ���� �״�� ����
    public void SetObjectTransformation(BDObejctContainer target, BDObject obj)
    {
        target.SetTransformation(obj.transforms);

        if (obj.children != null)
        {
            for (int i = 0; i < obj.children.Length; i++)
            {
                SetObjectTransformation(target.children[i], obj.children[i]);
            }
        }
    }

    // target�� a, b�� t ������ �����Ͽ� ����
    public void SetObjectTransformationInter(BDObejctContainer target, float t, BDObject a, BDObject b)
    {
        // 1. transforms(4x4 ���, float[16])�� t ������ ����
        float[] result = new float[16];
        for (int i = 0; i < 16; i++)
        {
            // ���� ����: (1 - t) * a + t * b
            result[i] = a.transforms[i] * (1f - t) + b.transforms[i] * t;
        }

        // 2. ������ ����� target�� ����
        target.SetTransformation(result);

        // 3. �ڽ��� �ִٸ�, �����ϰ� ��������� ó��
        //    a.children�� b.children�� ������ ���ٰ� ����
        if (a.children != null && b.children != null)
        {
            for (int i = 0; i < a.children.Length; i++)
            {
                // �ڽĵ� ���� ������� ����
                SetObjectTransformationInter(target.children[i], t, a.children[i], b.children[i]);
            }
        }
    }

    // tick �� ���� ����� �� ������ (tick���� ����(�����鼭 ���� ū ��), tick���� ������(ũ�鼭 ���� ���� ��)) ���ؼ� ��ȯ�ϱ�
    (Frame, Frame) GetNearestFrame(int tick)
    {
        if (frames.Values[0].Tick < tick)
            return (null, frames.Values[0]);
        else if (MaxTick > tick)
            return (frames.Values[frames.Count - 1], null);

        int idx = frames.IndexOfKey(tick);

        Frame left = null;
        Frame right = null;

        if (idx >= 0)
        {
            left = frames.Values[idx];
            right = null;
            //if (idx < frames.Count - 1)
            //    right = frames[idx+1];
        }
        else
        {
            // tick Ű�� ���ٸ�, ~idx�� "���� ��ġ"�� ��
            int insertionIndex = ~idx;

            // ���� �ε����� insertionIndex - 1 (�׷� frames.Keys[leftIndex] < tick)
            int leftIndex = insertionIndex - 1;
            if (leftIndex >= 0)
            {
                left = frames.Values[leftIndex];
            }

            // ������ �ε����� insertionIndex (�׷� frames.Keys[insertionIndex] > tick)
            if (insertionIndex < frames.Count)
            {
                right = frames.Values[insertionIndex];
            }
        }

        Debug.Log($"Tick: {tick}, Left: {left?.Tick}, Right: {right?.Tick}, idx: {~idx}");
        return (left, right);
    }

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
    public void AddFrame(BDObject frameInfo, int tick)
    {
        if (!CheckBDObject(firstFrame.info, frameInfo, true))
        {
            return;
        }

        var frame = Instantiate(manager.framePrefab, transform.GetChild(0));

        while (frames.ContainsKey(tick))
        {
            tick++;
        }

        frames.Add(tick, frame);
        frame.Init(tick, frameInfo, this);
    }

    public void AddFrame(BDObject frameInfo) => AddFrame(frameInfo, MaxTick + GameManager.Instance.Setting.DefaultTickInterval);

    // ��ġ�� ���� �������� Ȯ���ϰ� ���� �����ϸ� frames �����ϰ� true ��ȯ
    public bool ChangePos(Frame frame, int firstTick, int changedTick)
    {
        //Debug.Log("firstTick : " + firstTick + ", changedTick : " +  changedTick);
        if (firstTick == changedTick) return true;
        if (frames.ContainsKey(changedTick)) return false;

        frames.Remove(firstTick);
        frames.Add(changedTick, frame);
        return true;
    }

    // �� BDObject�� ���Ͽ� name�� �ٸ��� false ��ȯ, children���� Ȯ���Ѵ�.
    // ù��°�� ��� �̸� �� �н�
    bool CheckBDObject(BDObject a, BDObject b, bool IsFirst = false)
    {
        //Debug.Log(a?.ToString() + " vs " + b?.ToString());

        // 1) �� �� null�̸� "����"�� ����
        if (a == null && b == null)
        {
            //CustomLog.Log("Both objects are null �� Considered equal.");
            return true;
        }

        // 2) �� �ʸ� null�̸� �ٸ�
        if (a == null || b == null)
        {
            CustomLog.LogError($"One object is null -> a: {(a == null ? "null" : a.name)}, b: {(b == null ? "null" : b.name)}");
            return false;
        }

        // 3) name�� �ٸ��� �ٷ� false
        if (a.name != b.name && !IsFirst)
        {
            CustomLog.LogError($"Different Name -> a: {a.name}, b: {b.name}");
            return false;
        }

        // 4) children ��
        if (a.children == null && b.children == null)
        {
            //CustomLog.Log($"Both '{a.name}' and '{b.name}' have no children �� Considered equal.");
            return true;
        }

        if (a.children == null || b.children == null)
        {
            CustomLog.LogError($"Children mismatch -> a: {(a.children == null ? "null" : "exists")}, b: {(b.children == null ? "null" : "exists")}");
            return false;
        }

        // ���̰� �ٸ��� false
        if (a.children.Length != b.children.Length)
        {
            CustomLog.LogError($"Children count mismatch -> a: {a.children.Length}, b: {b.children.Length}");
            CustomLog.LogError($"a: {string.Join(", ", a.children.Select(c => c.name))}");
            CustomLog.LogError($"b: {string.Join(", ", b.children.Select(c => c.name))}");
            return false;
        }

        // ������ �ڽ��� ��������� ��
        for (int i = 0; i < a.children.Length; i++)
        {
            if (!CheckBDObject(a.children[i], b.children[i]))
            {
                CustomLog.LogError($"Child mismatch at index {i} �� a: {a.children[i]?.name}, b: {b.children[i]?.name}");
                return false;
            }
        }

        // ���� ��� �˻縦 ����ϸ� true
        return true;
    }


}
