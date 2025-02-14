using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ItemModelGenerator : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    Mesh mesh;

    public Texture2D layer0Textures;
    public Texture2D layer1Textures = null;

    #region Voxel Data
    // ������ü�� 8�� �������� �����ǥ (������ -0.5 ~ 0.5)
    static readonly float3[] verticePositions =
    {
        new float3(-0.5f,  0.5f, -0.5f), new float3(-0.5f,  0.5f,  0.5f), // 0, 1
        new float3( 0.5f,  0.5f,  0.5f), new float3( 0.5f,  0.5f, -0.5f), // 2, 3
        new float3(-0.5f, -0.5f, -0.5f), new float3(-0.5f, -0.5f,  0.5f), // 4, 5
        new float3( 0.5f, -0.5f,  0.5f), new float3( 0.5f, -0.5f, -0.5f), // 6, 7
    };

    // �� ���� ������ ������ �ε��� (verticePositions �迭 ����)
    static readonly int4[] faceVertices =
    {
        new int4(1, 2, 3, 0),   // up
        new int4(6, 5, 4, 7),   // down
        new int4(2, 1, 5, 6),   // front
        new int4(0, 3, 7, 4),   // back
        new int4(3, 2, 6, 7),   // right
        new int4(1, 0, 4, 5)    // left
    };

    // �ﰢ�� �ε��� (���簢�� ���� �� ���� �ﰢ������ ����)
    static readonly int[] triangleVertices = { 0, 1, 2, 0, 2, 3 };

    // UV ��ǥ (���� ��, ������ ��, ������ �Ʒ�, ���� �Ʒ�)
    static readonly int2[] dUV =
    {
        new int2(0, 1),
        new int2(1, 1),
        new int2(1, 0),
        new int2(0, 0)
    };
    #endregion

    List<float3> vertices = new List<float3>();
    List<int> triangles = new List<int>();
    List<float2> uvs = new List<float2>();
    List<Color> colors = new List<Color>();

    public void Init(Texture2D layer0, Texture2D layer1 = null)
    {
        mesh = new Mesh();
        meshFilter.sharedMesh = mesh;

        layer0Textures = layer0;
        layer1Textures = layer1;

        Generate();
    }

    Color GetPixel(int x, int y)
    {
        if (layer1Textures == null) return layer0Textures.GetPixel(x, y);

        Color color = layer1Textures.GetPixel(x, y);
        if (color.a == 0)
        {
            color = layer0Textures.GetPixel(x, y);
        }
        return color;
    }

    public void Generate()
    {
        int width = layer0Textures.width;
        int height = layer0Textures.height;

        // �ؽ�ó�� �� �ȼ��� ��ȸ�Ͽ� ���İ� 0�� �ƴ� �ȼ��� ���� ���� ��� ����
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = GetPixel(x, y);
                if (pixelColor.a == 0) continue;

                // ��/�ڸ��� ������ �߰�
                AddFace(new int3(x, y, 0), 2, pixelColor);
                AddFace(new int3(x, y, 0), 3, pixelColor);

                // �ֺ��� �ȼ��� ������ �ش� ������ ���� �߰�
                if (x == 0 || GetPixel(x - 1, y).a == 0)
                    AddFace(new int3(x, y, 0), 5, pixelColor);

                if (x == width - 1 || GetPixel(x + 1, y).a == 0)
                    AddFace(new int3(x, y, 0), 4, pixelColor);

                if (y == 0 || GetPixel(x, y - 1).a == 0)
                    AddFace(new int3(x, y, 0), 1, pixelColor);

                if (y == height - 1 || GetPixel(x, y + 1).a == 0)
                    AddFace(new int3(x, y, 0), 0, pixelColor);
            }
        }

        // === �޽��� ������ ��� �߾����� ������ ===
        if (vertices.Count > 0)
        {
            // �ּ�, �ִ� ��ǥ ���
            float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var v in vertices)
            {
                min = math.min(min, v);
                max = math.max(max, v);
            }
            // �߾Ӱ� ���
            float3 center = (min + max) / 2f;
            // ��� ������ �߾� �������� �̵�
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] -= center;
            }
        }
        // ====================================

        meshFilter.sharedMesh.Clear();
        meshFilter.sharedMesh.SetVertices(vertices.ConvertAll(v => (Vector3)v));
        meshFilter.sharedMesh.SetTriangles(triangles, 0);
        meshFilter.sharedMesh.SetUVs(0, uvs.ConvertAll(v => new Vector2(v.x, v.y)));
        meshFilter.sharedMesh.SetColors(colors);

        meshFilter.sharedMesh.RecalculateNormals();
        meshFilter.sharedMesh.RecalculateTangents();

        // ���� �� ����Ʈ �ʱ�ȭ
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        colors.Clear();
    }

    void AddFace(int3 p, int dir, Color color)
    {
        int vc = vertices.Count;

        for (int i = 0; i < 4; i++)
        {
            // ������ ��� ũ�� ��� voxelScale�� ���� ũ�⸦ ����.
            float3 dp = verticePositions[faceVertices[dir][i]];// * voxelScale;
            vertices.Add(p + dp);
            // ���İ��� 1�� ����� �������� �ʰ� ����
            colors.Add(color);
        }

        for (int i = 0; i < 6; i++)
        {
            triangles.Add(vc + triangleVertices[i]);
        }

        for (int i = 0; i < 4; i++)
        {
            uvs.Add(new float2(dUV[i].x, dUV[i].y));
        }
    }
}
