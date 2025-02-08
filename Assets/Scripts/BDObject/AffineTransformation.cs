using UnityEngine;
using System.Collections.Generic;

public class AffineTransformation
{
    // ��� ��ȯ �Լ�
    public static Matrix4x4 GetMatrix(float[] transforms)
    {
        if (transforms.Length != 16)
        {
            Debug.LogError("Invalid transform data");
            return Matrix4x4.identity;
        }
        return new Matrix4x4(
            new Vector4(transforms[0], transforms[1], transforms[2], transforms[3]),
            new Vector4(transforms[4], transforms[5], transforms[6], transforms[7]),
            new Vector4(transforms[8], transforms[9], transforms[10], transforms[11]),
            new Vector4(transforms[12], transforms[13], transforms[14], transforms[15])
        );
    }

    public static void ApplyMatrixToTransform(Transform target, Matrix4x4 matrix)
    {
        // 1. Translation (localPosition)
        Vector3 translation = matrix.GetRow(3); // ������ ������ ����

        // 2. Scale (localScale)
        Vector3 scale = new Vector3(
            matrix.GetRow(0).magnitude, // X �� ������
            matrix.GetRow(1).magnitude, // Y �� ������
            matrix.GetRow(2).magnitude  // Z �� ������
        );

        // 3. Rotation (localRotation)
        // ������ ���Ÿ� ���� ����ȭ
        Vector3 normalizedX = matrix.GetRow(0).normalized;
        Vector3 normalizedY = matrix.GetRow(1).normalized;
        Vector3 normalizedZ = matrix.GetRow(2).normalized;

        Quaternion rotation = Quaternion.LookRotation(normalizedZ, normalizedY);

        // 4. Transform�� ����
        target.localPosition = translation;
        target.localScale = scale;
        target.localRotation = rotation;
    }
}
