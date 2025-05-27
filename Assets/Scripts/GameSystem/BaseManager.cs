using UnityEngine;

namespace GameSystem
{
    public abstract class BaseManager : MonoBehaviour
    {
        protected virtual void Awake()
        {
            GameManager.RegisterManager(this);
        }
    }
}
