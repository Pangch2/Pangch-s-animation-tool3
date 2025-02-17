using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using Minecraft;

public class BlockModelGenerator : MonoBehaviour
{
    public MinecraftModelData modelData;
    public string modelName;
    public Color color = Color.white;

    public void SetModelByBlockState(JToken modelInfo)
    {
        // ��� ���� ����
        string modelLocation;
        JObject modelObject;

        if (modelInfo.Type == JTokenType.Array)
        {
            modelObject = modelInfo[0] as JObject;
            modelLocation = modelInfo[0]["model"].ToString();
            //CustomLog.Log("Model : " + modelLocation);
        }
        else
        {
            modelObject = modelInfo as JObject;
            modelLocation = modelInfo["model"].ToString();
        }

        int xRot = modelObject.TryGetValue("x", out JToken xToken) ? xToken.Value<int>() : 0;
        int yRot = modelObject.TryGetValue("y", out JToken yToken) ? yToken.Value<int>() : 0;
        bool uvlock = modelObject.TryGetValue("uvlock", out JToken uvlockToken) ? uvlockToken.Value<bool>() : false;

        SetModel(modelLocation);

        // X��, Y�� ȸ���� ��Ȯ�� ����
        Quaternion modelXRot = Quaternion.Euler(-xRot, 0, 0);
        Quaternion modelYRot = Quaternion.Euler(0, -yRot, 0);

        transform.localRotation = modelYRot * modelXRot;
    }

    public void SetModel(string modelLocation)
    {
        // �ҷ��� ���� �������� �����ϱ�
        modelLocation = MinecraftFileManager.RemoveNamespace(modelLocation);
        modelData = MinecraftFileManager.GetModelData("models/" + modelLocation + ".json").UnpackParent();
        BDObjectManager bdManager = GameManager.GetManager<BDObjectManager>();

        //Debug.Log("Model Data: " + modelData);

        // �� �����͸� �̿��ؼ� ����� ����
        int count = modelData.elements.Count;
        for (int i = 0; i < count; i++)
        {
            JObject element = modelData.elements[i];

            MeshRenderer cubeObject = Instantiate(bdManager.cubePrefab, transform);
            //cubeObject.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);

            // ť���� ��ġ�� ũ�⸦ ����
            Vector3 from = new Vector3(
                element["from"][0].Value<float>(),
                element["from"][1].Value<float>(),
                element["from"][2].Value<float>());
            Vector3 to = new Vector3(
                element["to"][0].Value<float>(),
                element["to"][1].Value<float>(),
                element["to"][2].Value<float>());

            Vector3 size = (to - from) / 32.0f;
            Vector3 center = (from + to) / 32.0f;

            // ť���� ũ�� ����
            cubeObject.transform.localScale = size;

            // ť���� ȸ���� ����
            SetRotation(element, cubeObject, size);
            // ���� ��ġ ����
            cubeObject.transform.localPosition = center - new Vector3(0.5f, 0.5f, 0.5f);

            // ť���� �ؽ��ĸ� ����
            SetFaces(modelData, element, cubeObject);
        }
    }

    protected void SetRotation(JObject element, MeshRenderer cubeObject, Vector3 size)
    {
        if (!element.ContainsKey("rotation"))
        {
            return;
        }

        JObject rotation = element["rotation"] as JObject;

        // origin �� Ȯ�� �� ���� ��ǥ ��ȯ
        Vector3 origin = new Vector3(
            rotation["origin"][0].Value<float>(),
            rotation["origin"][1].Value<float>(),
            rotation["origin"][2].Value<float>()
        ) / 16.0f;
        Vector3 worldOrigin = cubeObject.transform.parent.position + origin;

        // ȸ���� �� ���� ����
        Vector3 axis = rotation["axis"].ToString() switch
        {
            "x" => Vector3.right,
            "y" => Vector3.up,
            "z" => Vector3.forward,
            _ => Vector3.zero
        };
        float angle = rotation["angle"].Value<float>();

        // ȸ�� ����
        cubeObject.transform.RotateAround(worldOrigin, axis, angle);

        // ������ ������ (rescale �ɼ� ����)
        if (rotation.TryGetValue("rescale", out JToken rescaleToken) && rescaleToken.Value<bool>())
        {
            float scaleFactor = Mathf.Sqrt(2.0f); // �밢�� ���� ����
            cubeObject.transform.localScale = size * scaleFactor;
        }
    }

    protected void SetFaces(MinecraftModelData model, JObject element, MeshRenderer cubeObject)
    {
        if (!element.TryGetValue("faces", out JToken facesToken)) return;
        JObject faces = facesToken as JObject;

        Texture texture = null;
        bool isTextureAnimated = false;

        bool IsTransparented = false;

        ReadOnlySpan<string> NoTransparent = new[] { "bed", "fire", "banner" };
        for (int i = 0; i < NoTransparent.Length; i++)
        {
            if (modelName.Contains(NoTransparent[i]))
                IsTransparented = true;
        }

        // �� ���� ä���
        foreach (var face in faces)
        {
            JObject faceData = face.Value as JObject;
            // �� face�� �ؽ�ó �ε� �� ����
            var faceTexture = faceData["texture"];

            Enum.TryParse(face.Key, true, out MinecraftModelData.FaceDirection dir);
            int idx = (int)dir;

            string texturePath = DisplayObject.GetTexturePath(faceTexture.ToString(), model.textures);
            Texture2D blockTexture = CreateTexture(texturePath);
            bool IsAnimated = MinecraftFileManager.IsTextureAnimated(texturePath);

            // ���� üũ
            if (!IsTransparented)
            {
                if (CheckForTransparency(blockTexture))
                {
                    IsTransparented = true;

                    // ��� ���� �����ϱ�
                    var cubeMaterials = cubeObject.materials;
                    int cnt = cubeObject.materials.Length;
                    Material tshader = GameManager.GetManager<BDObjectManager>().BDObjTransportMaterial;

                    for (int i = 0; i < cnt; i++)
                    {
                        cubeMaterials[i] = tshader;
                    }
                    cubeObject.materials = cubeMaterials;
                }
            }

            Material mat = cubeObject.materials[idx];

            if (IsAnimated)
            {
                // �ִϸ��̼��� ��� ù��° ĭ ����
                float uvY = 16.0f * (16.0f / blockTexture.height);
                Vector4 uv = new Vector4(0, 0, 16, uvY);
                mat.SetVector("_UVFace", uv);
            }
            else if (faceData.ContainsKey("uv"))
            {
                // UV ����: [xMin, yMin, xMax, yMax] (Minecraft ���� 16x16)
                JArray uvArray = faceData["uv"] as JArray;
                Vector4 uv = new Vector4(
                    uvArray[0].Value<float>(),
                    uvArray[1].Value<float>(),
                    uvArray[2].Value<float>(),
                    uvArray[3].Value<float>()
                );

                mat.SetVector("_UVFace", uv);
            }

            // rotation ����: faceData�� uv ���� ������ ȸ���� ���� (0, 90, 180, 270)
            if (faceData.ContainsKey("rotation"))
            {
                int rotation = faceData["rotation"].Value<int>() % 360;
                if (rotation < 0)
                    rotation += 360;
                // Ŀ���� ���̴����� uv ������ ȸ���ϵ��� _Rotation ������Ƽ�� ����մϴ�.
                mat.SetFloat("_Rotation", -rotation);
            }
            else
            {
                mat.SetFloat("_Rotation", 0);
            }

            // ���� �ؽ�ó ����
            mat.mainTexture = blockTexture;

            if (texture == null)
            {
                texture = mat.mainTexture;
                isTextureAnimated = IsAnimated;
            }
        }

        // face�� ��õ��� ���� ���� �⺻ �ؽ�ó�� ä��
        const int faceCount = 6;
        for (int i = 0; i < faceCount; i++)
        {
            if (cubeObject.materials[i].mainTexture == null)
            {
                cubeObject.materials[i].mainTexture = texture;

                if (isTextureAnimated)
                {
                    float uvY = 16.0f * (16.0f / texture.height);
                    Vector4 uv = new Vector4(0, 0, 16, uvY);
                    cubeObject.materials[i].SetVector("_UVFace", uv);
                }
            }
        }
        /*
        foreach (MinecraftModelData.FaceDirection direction in Enum.GetValues(typeof(MinecraftModelData.FaceDirection)))
        {
            string key = direction.ToString();
            if (!faces.ContainsKey(key))
            {
                cubeObject.materials[(int)direction].mainTexture = texture;
            }
        }
        */

        // ���彺�� ���̾� Ư�� ó��
        if (modelName.Contains("redstone_wire"))
        {
            //CustomLog.Log("Redstone wire");
            int cnt = cubeObject.materials.Length;
            for (int i = 0; i < cnt; i++)
            {
                cubeObject.materials[i].color = Color.red;
            }
        }
        else if (modelName.Contains("banner") && element.ContainsKey("color"))
        {
            
            int cnt = cubeObject.materials.Length;
            for (int i = 0; i < cnt; i++)
            {
                cubeObject.materials[i].color = color;
            }
        }
    }

    protected virtual Texture2D CreateTexture(string path)
    {
        return MinecraftFileManager.GetTextureFile(path);
    }

    // ������ �κ��� �ִ��� Ȯ��
    protected virtual bool CheckForTransparency(Texture2D texture)
    {
        if (texture == null)
        {
            return false;
        }


        Color[] pixels = texture.GetPixels();

        foreach (Color pixel in pixels)
        {
            if (pixel.a < 1.0f)
            {
                return true; // ���� �Ǵ� ������ �ȼ� ����
            }
        }
        return false; // ������ ������
    }
}
