using System;
using UnityEngine;

public class AnimManager : BaseManager
{
    [SerializeField]
    private int _tick = 0;
    public int Tick
    {
        get => _tick;
        set
        {
            if (value < 0)
            {
                value = 0;
            }
            _tick = value;
            TickChanged?.Invoke(_tick);
        }
    }

    [SerializeField]
    private float _tickSpeed = 20.0f;
    public float TickSpeed
    {
        get => _tickSpeed;
        set
        {
            _tickSpeed = value;
            tickInterval = 1.0f / _tickSpeed; // ��Ȯ�� �ð� ���� ������Ʈ
        }
    }

    public static event Action<int> TickChanged;

    public bool IsPlaying { get; set; } = false;

    public Timeline Timeline;

    private float lastTickTime = 0f;  // ������ Tick ������Ʈ �ð�
    private float tickInterval = 1.0f / 20.0f; // �ʱ� Tick ����

    private void Start()
    {
        tickInterval = 1.0f / _tickSpeed; // �ʱ� TickSpeed �ݿ�
        lastTickTime = Time.time; // ���� �ð� ���
    }

    private void Update()
    {
        if (IsPlaying)
        {
            if (Time.time - lastTickTime >= tickInterval)
            {
                lastTickTime = Time.time; // ���� �ð� ������Ʈ
                Tick++; // Tick ����
            }
        }
    }

    public void TickAdd(int value)
    {
        Tick += value;
    }
}
