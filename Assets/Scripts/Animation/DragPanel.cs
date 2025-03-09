using UnityEngine;
using UnityEngine.EventSystems;

public class DragPanel : MonoBehaviour, 
    IPointerDownHandler//, IPointerMoveHandler//, IPointerUpHandler
{
    bool isDragging = false;
    public RectTransform AnimPanel;
    public RectTransform rect;

    public RectTransform canvasRectTransform;

    float lastHeight;
    float lastPanelSize;

    private void Start()
    {
        lastHeight = canvasRectTransform.rect.height;
    }

    private void Update()
    {
        if (lastHeight != canvasRectTransform.rect.height)
        {
            Debug.Log("Canvas Height Changed");
            SetPanelSize(lastPanelSize);
            lastHeight = canvasRectTransform.rect.height;
        }

        if (isDragging)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,  // ĵ������ RectTransform
                Input.mousePosition,                   // ���콺 ��ġ (Screen Space)
                null,                    // ���� ����ϴ� ī�޶�
                out var localPoint                         // ��ȯ�� UI ��ǥ
            );
            Debug.Log(localPoint);
            Debug.Log(canvasRectTransform.rect.height);

            SetPanelSize((canvasRectTransform.rect.height / 2) + localPoint.y);

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                BDEngineStyleCameraMovement.CanMoveCamera = true;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        BDEngineStyleCameraMovement.CanMoveCamera = false;
    }

    public void SetPanelSize(float y)
    {
        float height = -(canvasRectTransform.rect.height - y);
        AnimPanel.offsetMax = new Vector2(AnimPanel.offsetMax.x, height);
        lastPanelSize = y;

        //rect.position = new Vector3(rect.position.x, y, rect.position.z);
    }

    //public void OnPointerUp(PointerEventData eventData)
    //{
    //    isDragging = false;
    //    BDEngineStyleCameraMovement.CanMoveCamera = true;
    //}
}
