using System;
using System.Collections.Generic;
using UnityEngine;

public class Timeline : MonoBehaviour
{
    public TickLine gridPrefab;
    public List<TickLine> grid;
    public RectTransform TimeBar;
    int tick = 0;

    public int GridCount = 100;
    public event Action OnGridChanged;

    private void Start()
    {
        SetTickTexts(0);
        for (int i = 0; i < GridCount; i++)
        {
            grid[i].index = i;
        }
        AnimManager.TickChanged += OnAnimManagerTickChanged;

        OnAnimManagerTickChanged(GameManager.GetManager<AnimManager>().Tick);
    }

    private void OnAnimManagerTickChanged(int Tick)
    {
        tick = Tick;
        TickLine line = GetTickLine(tick, true);

        if (line != null)
        {
            TimeBar.anchoredPosition = new Vector2(line.rect.anchoredPosition.x, TimeBar.anchoredPosition.y);
        }
    }

    /// <summary>
    /// ƽ�� �ش��ϴ� �׸��带 �����´�.
    /// </summary>
    /// <param name="tick"></param>
    /// <param name="ChangeGrid"> true�� ��� tick�� �ش��ϵ��� �׸��带 �����Ѵ�.</param>
    /// <returns>ã�� ��� : TickLine, ��ã�� ��� : null</returns>
    public TickLine GetTickLine(int tick, bool ChangeGrid)
    {
        if (tick < 0)
        {
            return null;
        }
        // ���� ���̸� �׸��� ����
        TickLine line = grid[0];
        if (tick < line.Tick)
        {
            if (!ChangeGrid) return null;
            SetTickTexts(tick);
            return GetTickLine(tick, ChangeGrid);
        }

        line = grid[GridCount-1];
        if (tick > line.Tick)
        {
            if (!ChangeGrid) return null;
            SetTickTexts(line.Tick + 1);
            return GetTickLine(tick, ChangeGrid);
        }

        // ���� Ž������ ã��
        int index = grid.BinarySearch(null, Comparer<TickLine>.Create((a, b) => a.Tick.CompareTo(tick)));
        if (index >= 0)
        {
            return grid[index];
        }
        return null;
    }

    // ���� RectTransform�� ���� ����� TickLine�� ��ȯ�Ѵ�.
    public TickLine GetTickLine(Vector2 pos)
    {
        int max_index = 0;
        float max = Vector2.Distance(pos, grid[max_index].rect.position);
        for (int i = 1; i < grid.Count; i++)
        {
            float distance = Vector2.Distance(pos, grid[i].rect.position);
            if (distance < max)
            {
                max = distance;
                max_index = i;
            }
        }
        return grid[max_index];
    }

    public void ChangeGrid(int move)
    {
        GridCount += move;
        SetTickTexts(grid[0].Tick);

        OnAnimManagerTickChanged(tick);
        OnGridChanged?.Invoke();
    }

    // �׸��� �����ϱ�
    public void SetTickTexts(int start)
    {
        for (int i = 0; i < GridCount; i++)
        {
            if (grid.Count <= i)
            {
                TickLine newGrid = Instantiate(gridPrefab, transform);
                grid.Add(newGrid);
                newGrid.index = i;
            }
            else
            {
                grid[i].gameObject.SetActive(true);
            }

            grid[i].SetTick(start + i, i == 0);
        }
        for (int i = GridCount; i < grid.Count; i++)
        {
            grid[i].gameObject.SetActive(false);
        }
    }
}
