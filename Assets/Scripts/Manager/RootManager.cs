using UnityEngine;

public abstract class RootManager : MonoBehaviour
{
    // ��� ���� �Ŵ����� GameManager�� ����ϱ�
    protected virtual void Awake()
    {
        GameManager.Instance.RegisterManager(this);
    }
}
