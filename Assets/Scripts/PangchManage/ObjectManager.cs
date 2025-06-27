using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    // 비활성화할 오브젝트들
    public GameObject[] objectsToDeactivate;  // 여러 개의 오브젝트를 비활성화

    // 활성화할 오브젝트들
    public GameObject[] objectsToActivate;  // 여러 개의 오브젝트를 활성화

    void Start()
    {
        // 비활성화할 오브젝트들 비활성화
        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);  // 비활성화
            }
        }

        // 활성화할 오브젝트들 활성화
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);  // 활성화
            }
        }
    }
}
