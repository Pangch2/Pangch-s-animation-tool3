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

    public int MaxTick => frames[frames.Count-1].Tick;

    AnimObjList manager;

    public void Init(BDObject first, AnimObjList list, string FileName)
    {
        title.text = FileName;
        manager = list;
        fileName = FileName;

        firstFrame.Init(0, first, this);
        frames[0] = firstFrame;

        AnimManager.TickChanged += OnTickChanged;
    }

    void OnTickChanged(int tick)
    {

    }

    // tick �� ���� ����� �� ������ (tick���� ����(�����鼭 ���� ū ��), tick���� ������(ũ�鼭 ���� ���� ��)) ���ؼ� ��ȯ�ϱ�
    (Frame, Frame) GetNearestFrame(int tick)
    {
        int idx = frames.IndexOfKey(tick);

        Frame left = null;
        Frame right = null;

        if (idx >= 0)
        {
            left = frames[idx];
            if (idx < frames.Count - 1)
                right = frames[idx+1];
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
            CustomLog.LogError("�� BDObject�� ������Ʈ�� �ٸ� �����Դϴ�!");
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
        //Debug.Log(a.ToString() + " " + b.ToString());
        
        // 1) �� �� null�̸� "����"�� ����
        if (a == null && b == null)
            return true;

        // 2) �� �ʸ� null�̸� �ٸ�
        if (a == null || b == null)
            return false;

        // 3) name�� �ٸ��� �ٷ� false
        if (a.name != b.name && !IsFirst)
            return false;

        // 4) children ��
        //    a.children�� b.children�� ��� null�̸� ���� ����
        //    �ϳ��� null�̾ false
        if (a.children == null && b.children == null)
            return true;  // �ڽ��� �� �� ������ name�� ���� �����ε� ���

        if (a.children == null || b.children == null)
            return false;

        // ���̰� �ٸ��� false
        if (a.children.Length != b.children.Length)
            return false;

        // ������ �ڽ��� ��������� ��
        for (int i = 0; i < a.children.Length; i++)
        {
            if (!CheckBDObject(a.children[i], b.children[i]))
                return false;
        }

        // ���� ��� �˻縦 ����ϸ� true
        return true;
    }

}
