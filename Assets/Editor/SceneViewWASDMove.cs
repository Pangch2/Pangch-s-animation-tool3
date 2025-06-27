using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public class SceneViewWASDMove
{
    private static bool isRightMouseDown = false; // ��Ŭ�� ���� ����
    private static HashSet<KeyCode> pressedKeys = new HashSet<KeyCode>(); // �����ִ� Ű ���� ����
    private static float lastUpdateTime = 0f; // ������ ������Ʈ �ð�
    private static float moveSpeed = 2f; // �⺻ �̵� �ӵ� (��ũ�ѷ� ���� ����)

    static SceneViewWASDMove()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        lastUpdateTime = Time.realtimeSinceStartup; // �ʱ� �ð� ����
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // ��Ŭ�� ���� ó��
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            isRightMouseDown = true;
        }
        else if (e.type == EventType.MouseUp && e.button == 1)
        {
            isRightMouseDown = false;
            lastUpdateTime = Time.realtimeSinceStartup;
            pressedKeys.Clear();
        }
        if (isRightMouseDown)
        {
            if (e.type == EventType.KeyDown)
            {
                pressedKeys.Add(e.keyCode);
                e.Use();
            }
            else if (e.type == EventType.KeyUp)
            {
                pressedKeys.Remove(e.keyCode);
                e.Use();
            }

            if (e.type == EventType.ScrollWheel)
            {
                float scrollVelocity = e.delta.y * 0.1f;
                moveSpeed = Mathf.Clamp(moveSpeed - scrollVelocity, 0.1f, 100f);
                e.Use();
            }

            HandleCameraMovement(sceneView);
        }
    }

    private static void HandleCameraMovement(SceneView sceneView)
    {
        float currentTime = Time.realtimeSinceStartup;
        float deltaTime = currentTime - lastUpdateTime;
        lastUpdateTime = currentTime;

        Vector3 movement = Vector3.zero;

        // ���� �����ִ� Ű�� ������� �̵� ���
        foreach (var key in pressedKeys)
        {
            if (key == KeyCode.W) movement += Vector3.forward;
            if (key == KeyCode.S) movement += Vector3.back;
            if (key == KeyCode.A) movement += Vector3.left;
            if (key == KeyCode.D) movement += Vector3.right;
            if (key == KeyCode.LeftShift) movement += Vector3.down;
            if (key == KeyCode.Space) movement += Vector3.up;
            if (key == KeyCode.LeftControl) movement *= 1.25f;
        }

        // ī�޶��� ���� Y�� ���⿡ �°� ��� �̵� ����
        Quaternion cameraRotation = Quaternion.Euler(0, sceneView.camera.transform.eulerAngles.y, 0);
        Vector3 adjustedMovement = cameraRotation * movement;

        // �̵� �ӵ��� ����
        sceneView.pivot += adjustedMovement.normalized * moveSpeed * deltaTime * 10;

        // ���� ������Ʈ
        sceneView.Repaint();
    }
}