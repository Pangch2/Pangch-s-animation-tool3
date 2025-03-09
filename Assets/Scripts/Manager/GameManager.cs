using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // �̱��� Static ����
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<GameManager>();
            return instance;
        }
    }

    // ��� �Ŵ����� ����Ǵ� ��ųʸ�
    private Dictionary<Type, BaseManager> managers = new Dictionary<Type, BaseManager>();

    public SettingManager Setting => GetManager<SettingManager>();

    // �Ŵ����� �������� �Լ�
    public static T GetManager<T>() where T : BaseManager
    {
        // ��ųʸ����� �ش� Ÿ���� �Ŵ����� ã�� ��ȯ
        if (instance.managers.TryGetValue(typeof(T), out var manager))
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
            Debug.LogError("WARNING! FIND TWO MANAGER : " + manager.name);
        }
    }

    void Awake()
    {
        // �ڱ� �ڽ��� ��� ��� (�˻� ����)
        if (Instance == null)
        {
            instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
}

