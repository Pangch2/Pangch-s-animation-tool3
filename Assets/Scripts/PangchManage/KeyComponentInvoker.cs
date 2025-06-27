using UnityEngine;
using System;
using System.Reflection;

public class KeyComponentInvokerAuto : MonoBehaviour
{
    public KeyCode triggerKey = KeyCode.Escape;
    public GameObject targetObject;
    public string componentTypeName;
    public string methodName = "DoAction";

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            Type compType = Type.GetType(componentTypeName);
            if (compType == null)
                compType = FindTypeByName(componentTypeName);

            Component comp = targetObject?.GetComponent(compType);
            MethodInfo method = compType?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            method?.Invoke(comp, null);
        }
    }

    private Type FindTypeByName(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName);
            if (type != null)
                return type;

            foreach (var t in assembly.GetTypes())
            {
                if (t.Name == typeName)
                    return t;
            }
        }
        return null;
    }
}
