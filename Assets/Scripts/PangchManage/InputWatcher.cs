using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InputWatcher : MonoBehaviour
{
    public TMP_InputField inputField;
    public GameObject[] objectsToControl;

    public Color emptyTextColor = Color.gray;

    private Dictionary<TMP_Text, Color> originalTextColors = new Dictionary<TMP_Text, Color>();

    private void Start()
    {
        if (inputField == null || inputField.textComponent == null)
        {
            return;
        }

        foreach (var obj in objectsToControl)
        {
            if (obj == null) continue;

            var texts = obj.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                if (!originalTextColors.ContainsKey(text))
                    originalTextColors[text] = text.color;
            }
        }

        if (!originalTextColors.ContainsKey(inputField.textComponent))
            originalTextColors[inputField.textComponent] = inputField.textComponent.color;

        inputField.onValueChanged.AddListener(OnInputValueChanged);
        inputField.onEndEdit.AddListener(OnInputEndEdit);

        OnInputValueChanged(inputField.text); // �ʱ� ���� ����
    }

    private void OnEnable()
    {
        // ������Ʈ�� �ٽ� Ȱ��ȭ�Ǹ� �Է� ���� ó��ó�� ����
        if (inputField != null)
        {
            OnInputEndEdit(inputField.text);
        }
    }

    private void OnInputValueChanged(string input)
    {
        bool isEmpty = string.IsNullOrWhiteSpace(input);

        if (isEmpty && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == inputField.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // �Է� �߿��� ���� �������� ����
        // UpdateUITextColors(isEmpty);
    }

    private void OnInputEndEdit(string input)
    {
        bool isEmpty = string.IsNullOrWhiteSpace(input);

        foreach (var obj in objectsToControl)
        {
            if (obj == null) continue;

            var selectables = obj.GetComponentsInChildren<Selectable>(true);
            var graphics = obj.GetComponentsInChildren<Graphic>(true);

            foreach (var sel in selectables)
                sel.interactable = !isEmpty;

            foreach (var g in graphics)
                g.raycastTarget = !isEmpty;
        }

        UpdateUITextColors(isEmpty);
    }

    private void UpdateUITextColors(bool isEmpty)
    {
        foreach (var obj in objectsToControl)
        {
            if (obj == null) continue;

            var texts = obj.GetComponentsInChildren<TMP_Text>(true);

            foreach (var text in texts)
            {
                if (originalTextColors.TryGetValue(text, out Color originalColor))
                {
                    text.color = isEmpty ? emptyTextColor : originalColor;
                }
            }
        }

        if (originalTextColors.TryGetValue(inputField.textComponent, out Color inputOriginalColor))
        {
            inputField.textComponent.color = isEmpty ? emptyTextColor : inputOriginalColor;
        }
    }
}
