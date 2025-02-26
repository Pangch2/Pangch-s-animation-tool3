using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : BaseManager
{
    // �̱��� Static ����
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameManager>();
            }
            return instance;
        }
    }

    // ��� �Ŵ����� ����Ǵ� ��ųʸ�
    private static Dictionary<Type, BaseManager> managers = new Dictionary<Type, BaseManager>();

    // �Ŵ����� �������� �Լ�
    public static T GetManager<T>() where T : BaseManager
    {
        // ��ųʸ����� �ش� Ÿ���� �Ŵ����� ã�� ��ȯ
        if (managers.TryGetValue(typeof(T), out var manager))
        {
            if (manager is T)
                return manager as T;
        }

        CustomLog.LogError($"Manager of type {typeof(T)} not found!");
        return null;
    }

    // �Ŵ����� ����ϴ� �Լ�
    public void RegisterManager(BaseManager manager)
    {
        var type = manager.GetType();
        if (!managers.ContainsKey(type))
        {
            managers[type] = manager;
        }
        else
        {
            //CustomLog.LogError($"Manager of type {type} is already registered.");
            Destroy(manager.gameObject);
        }
    }

    protected override void Awake()
    {
        if (instance == null || instance == this)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

