using UnityEngine;

public class BDEngineStyleCameraMovement : MonoBehaviour
{
    public Transform pivot;               // ī�޶� ȸ���� �߽���
    public float rotationSpeed = 5f;      // ȸ�� �ӵ�
    public float panSpeed = 5f;           // �����¿� �̵� �ӵ�
    public float zoomSpeed = 10f;         // �� �ӵ�
    public float minDistance = 2f;        // �߽������� �ּ� �Ÿ�
    public float maxDistance = 50f;       // �߽������� �ִ� �Ÿ�

    private float currentDistance;        // ���� �߽������� �Ÿ�
    private Vector3 lastMousePosition;    // ���콺 ������ ��ġ

    private Vector3 pivotInitPos;
    private float InitDistance;

    void Start()
    {
        // �ʱ� �Ÿ� ����
        currentDistance = Vector3.Distance(transform.position, pivot.position);

        pivotInitPos = pivot.position;
        InitDistance = currentDistance;
    }

    void Update()
    {
        // ���콺 �Է� ó��
        HandleMouseInput();

        if (Input.GetKeyDown(KeyCode.F))
        {
            // ī�޶�� pivot �ʱ� ��ġ�� �̵�
            pivot.position = pivotInitPos;
            currentDistance = InitDistance;
            transform.position = pivotInitPos + new Vector3(0, 0, -currentDistance);
            transform.LookAt(pivotInitPos);
        }
    }

    void HandleMouseInput()
    {
        // ���콺 ���� Ŭ��: ȸ��
        if (Input.GetMouseButton(0))
        {
            RotateAroundPivot();
        }

        // ���콺 ������ Ŭ��: �����¿� �̵�
        if (Input.GetMouseButton(1))
        {
            PanCamera();
        }

        // ���콺 ��: ����/����
        ZoomCamera();
    }

    void RotateAroundPivot()
    {
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

        // ���콺 �̵����� ���� ȸ�� ���� ���
        float yaw = mouseDelta.x * rotationSpeed * Time.deltaTime;   // Y�� ȸ��
        float pitch = -mouseDelta.y * rotationSpeed * Time.deltaTime; // X�� ȸ��

        // �߽����� �������� ȸ��
        transform.RotateAround(pivot.position, Vector3.up, yaw);        // Y�� ȸ��
        transform.RotateAround(pivot.position, transform.right, pitch); // X�� ȸ��

        // ī�޶�� �߽����� �Ÿ� ����
        Vector3 direction = (transform.position - pivot.position).normalized;
        transform.position = pivot.position + direction * currentDistance;

        // ī�޶� �׻� �߽����� �ٶ󺸵��� ����
        transform.LookAt(pivot);
    }

    void PanCamera()
    {
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

        // ���콺 �̵����� ���� �̵��� ���
        Vector3 right = transform.right * mouseDelta.x * panSpeed * Time.deltaTime;
        Vector3 up = transform.up * mouseDelta.y * panSpeed * Time.deltaTime;

        // ī�޶� ��ġ�� �̵�
        transform.position += right + up;

        // �߽����� �Բ� �̵�
        pivot.position += right + up;
    }

    void ZoomCamera()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");

        // �Ÿ� ����
        currentDistance -= scrollDelta * zoomSpeed * Time.deltaTime;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // ī�޶� ��ġ ������Ʈ
        Vector3 direction = (transform.position - pivot.position).normalized;
        transform.position = pivot.position + direction * currentDistance;
    }

    void LateUpdate()
    {
        // ���콺 ������ ��ġ ������Ʈ
        lastMousePosition = Input.mousePosition;
    }
}
