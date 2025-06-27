using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public class SceneViewWASDMove
{
    private static bool isRightMouseDown = false; // 우클릭 상태 추적
    private static HashSet<KeyCode> pressedKeys = new HashSet<KeyCode>(); // 눌려있는 키 상태 추적
    private static float lastUpdateTime = 0f; // 마지막 업데이트 시간
    private static float moveSpeed = 2f; // 기본 이동 속도 (스크롤로 조정 가능)

    static SceneViewWASDMove()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        lastUpdateTime = Time.realtimeSinceStartup; // 초기 시간 설정
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // 우클릭 상태 처리
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

        // 현재 눌려있는 키를 기반으로 이동 계산
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

        // 카메라의 현재 Y축 방향에 맞게 평면 이동 적용
        Quaternion cameraRotation = Quaternion.Euler(0, sceneView.camera.transform.eulerAngles.y, 0);
        Vector3 adjustedMovement = cameraRotation * movement;

        // 이동 속도와 적용
        sceneView.pivot += adjustedMovement.normalized * moveSpeed * deltaTime * 10;

        // 강제 업데이트
        sceneView.Repaint();
    }
}