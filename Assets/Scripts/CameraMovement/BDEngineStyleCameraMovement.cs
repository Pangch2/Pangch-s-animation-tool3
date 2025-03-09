using UnityEngine;
using UnityEngine.InputSystem;

public class BDEngineStyleCameraMovement : MonoBehaviour
{
    public static bool CanMoveCamera { get; set; } = true;

    [Header("References")]
    public Transform pivot; // ī�޶� �ٶ� �ǹ�

    [Header("Camera Movement Settings")]
    public float rotationSpeed = 90f; // ȸ�� �ӵ� (��/��)
    public float panSpeed = 5f;       // �� �ӵ� (����/��)
    public float zoomSpeed = 10f;     // �� �ӵ�
    public float minDistance = 2f;    // ī�޶�~�ǹ� �ּ� �Ÿ�
    public float maxDistance = 50f;   // ī�޶�~�ǹ� �ִ� �Ÿ�

    private float currentDistance;    // ���� ī�޶�~�ǹ� �Ÿ�
    private Vector3 pivotInitPos;     // �ǹ� �ʱ� ��ġ
    private float initDistance;       // ī�޶� �ʱ� �Ÿ�

    [Header("Input Actions")]
    // MyCameraActions.inputactions (Asset) ����
    public InputActionAsset inputActions;

    // ���ο��� ã�Ƽ� �� Action Map �� Action ����
    private InputActionMap cameraMap;
    private InputAction rotateAction;
    private InputAction panAction;
    private InputAction lookDeltaAction;
    private InputAction zoomAction;

    private void OnEnable()
    {
        // �ǹ��� ������ ��ũ��Ʈ �ߴ�
        if (pivot == null)
        {
            Debug.LogError("Pivot is not assigned.");
            enabled = false;
            return;
        }

        // �ʱ� �� ����
        currentDistance = Vector3.Distance(transform.position, pivot.position);
        initDistance = currentDistance;
        pivotInitPos = pivot.position;
        transform.LookAt(pivot);

        // --- 1) Action Map �������� ---
        //    (InputActionAsset �ȿ� "Camera" ���� �����ؾ� ��)
        cameraMap = inputActions.FindActionMap("Camera", throwIfNotFound: true);

        // --- 2) Action ���� ã�� ---
        rotateAction = cameraMap.FindAction("Rotate", throwIfNotFound: true);      // Button
        panAction = cameraMap.FindAction("Pan", throwIfNotFound: true);           // Button
        lookDeltaAction = cameraMap.FindAction("LookDelta", throwIfNotFound: true); // Vector2
        zoomAction = cameraMap.FindAction("Zoom", throwIfNotFound: true);         // float

        // --- 3) Enable ---
        cameraMap.Enable(); // or rotateAction.Enable(); panAction.Enable(); ...

        // ����: cameraMap.Enable() �� ȣ���ϸ� 
        //       cameraMap �ȿ� �ִ� ��� �׼��� �� ���� Enable �˴ϴ�.
    }

    private void OnDisable()
    {
        // Disable
        cameraMap?.Disable();
        // �Ǵ� ���� �׼ǵ� rotateAction?.Disable(); ��
    }

    void Update()
    {
        if (!CanMoveCamera) return;

        // Action�� ���� �� �б�
        bool rotatePressed = rotateAction.ReadValue<float>() > 0.5f;   // ���콺 ���� ��ư
        bool panPressed = panAction.ReadValue<float>() > 0.5f;         // ���콺 ������ ��ư
        Vector2 lookDelta = lookDeltaAction.ReadValue<Vector2>();     // ���콺 �̵�
        float zoomValue = zoomAction.ReadValue<float>();              // ���콺 ��

        // --- 1) ȸ�� ---
        if (rotatePressed && lookDelta.sqrMagnitude > 0.0001f)
        {
            RotateAroundPivot(lookDelta, Time.deltaTime);
        }

        // --- 2) �� ---
        if (panPressed && lookDelta.sqrMagnitude > 0.0001f)
        {
            PanCamera(lookDelta, Time.deltaTime);
        }

        // --- 3) �� ---
        if (Mathf.Abs(zoomValue) > 0.0001f)
        {
            ZoomCamera(zoomValue, Time.deltaTime);
        }
    }

    private void RotateAroundPivot(Vector2 delta, float dt)
    {
        float yaw = delta.x * rotationSpeed * dt;
        float pitch = -delta.y * rotationSpeed * dt; // ���� �̵��� ��(-)

        // 1) yaw : pivot ���� ���� Up
        transform.RotateAround(pivot.position, Vector3.up, yaw);

        // 2) pitch : ī�޶� ���� Right
        transform.RotateAround(pivot.position, transform.right, pitch);

        // 3) �Ÿ� ���� & pivot �ٶ󺸱�
        Vector3 direction = (transform.position - pivot.position).normalized;
        transform.position = pivot.position + direction * currentDistance;
        transform.LookAt(pivot);
    }

    private void PanCamera(Vector2 delta, float dt)
    {
        Vector3 rightMovement = transform.right * (delta.x * panSpeed * dt);
        Vector3 upMovement = transform.up * (delta.y * panSpeed * dt);
        Vector3 panMovement = rightMovement + upMovement;

        transform.position += panMovement;
        pivot.position += panMovement;
    }

    private void ZoomCamera(float zoomValue, float dt)
    {
        currentDistance -= zoomValue * zoomSpeed * dt;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        Vector3 direction = (transform.position - pivot.position).normalized;
        transform.position = pivot.position + direction * currentDistance;
    }

    public void ResetCamera()
    {
        pivot.position = pivotInitPos;
        currentDistance = initDistance;

        transform.position = pivotInitPos + new Vector3(0, 0, -currentDistance);
        transform.LookAt(pivotInitPos);
    }
}
