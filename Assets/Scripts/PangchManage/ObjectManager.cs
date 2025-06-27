using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    // ��Ȱ��ȭ�� ������Ʈ��
    public GameObject[] objectsToDeactivate;  // ���� ���� ������Ʈ�� ��Ȱ��ȭ

    // Ȱ��ȭ�� ������Ʈ��
    public GameObject[] objectsToActivate;  // ���� ���� ������Ʈ�� Ȱ��ȭ

    void Start()
    {
        // ��Ȱ��ȭ�� ������Ʈ�� ��Ȱ��ȭ
        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);  // ��Ȱ��ȭ
            }
        }

        // Ȱ��ȭ�� ������Ʈ�� Ȱ��ȭ
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);  // Ȱ��ȭ
            }
        }
    }
}
