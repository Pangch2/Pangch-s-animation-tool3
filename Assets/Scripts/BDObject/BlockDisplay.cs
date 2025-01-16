using Newtonsoft.Json.Linq;
using UnityEngine;

public class BlockDisplay : MonoBehaviour
{
    public MinecraftModelData modelData;
    public GameObject modelElementParent;

    public void LoadBlockModel(string name, string state)
    {
        //Debug.Log(name + ", " + state);

        // ��� ������Ʈ�� �ҷ��ͼ�
        JObject blockState = MinecraftFileManager.GetJSONData("blockstates/" + name + ".json");

        // variants ������ ���
        if (blockState.ContainsKey("variants"))
        {
            JObject variants = blockState["variants"] as JObject;
            //Debug.Log("Variants : " + variants.ToString());
            // ��� ������Ʈ�� �ش��ϴ� ���� �ҷ���
            if (variants.ContainsKey(state))
            {

                var modelInfo = variants[state];
                string modelLocation;

                JObject modelObject;

                if (modelInfo.Type == JTokenType.Array)
                {
                    modelObject = modelInfo[0] as JObject;
                    modelLocation = modelInfo[0]["model"].ToString();
                }
                else
                {
                    modelObject = modelInfo as JObject;
                    modelLocation = modelInfo["model"].ToString();
                }

                modelLocation = MinecraftFileManager.RemoveNamespace(modelLocation);

                int xRot = modelObject.TryGetValue("x", out JToken xToken) ? xToken.Value<int>() : 0;
                int yRot = modelObject.TryGetValue("y", out JToken yToken) ? yToken.Value<int>() : 0;
                bool uvlock = modelObject.TryGetValue("uvlock", out JToken uvlockToken) ? uvlockToken.Value<bool>() : false;

                // �ҷ��� ���� �������� �����ϱ�
                //Debug.Log("model location : " + modelLocation);
                modelData = MinecraftFileManager.GetModelData("models/" + modelLocation + ".json").UnpackParent();

                SetModelByMinecraftModel(modelData);

                //Quaternion modelXRot = Quaternion.Euler(xRot, 0, 0);
                //Quaternion modelYRot = Quaternion.Euler(0, yRot, 0);

                modelElementParent.transform.Rotate(new Vector3(xRot, yRot, 0));

                // ȸ�� �� Pivot�� ���������� �̵�
                AlignBlockDisplayToAABBCorner();

            }
            else
            {
                Debug.LogError("State not found: " + state);
            }
        }
    }

    void SetModelByMinecraftModel(MinecraftModelData model)
    {
        //Debug.Log(model.ToString());

        if (model.elements == null)
        {
            // ��Ŀ����, ħ�� ���

        }

        modelElementParent = new GameObject("Model");
        modelElementParent.transform.SetParent(transform);
        modelElementParent.transform.localPosition = Vector3.zero;
        // Model -> Block��
        // Block Element ���� ���� -> Model�� pivot�� ���� �ϴ����� �̵�
        // defaultSize : 0.5


        // ȸ�� : xRot, yRot, rotaiton

        // �� �����͸� �̿��ؼ� ����� ����
        foreach (var element in model.elements)
        {
            MeshRenderer cubeObject = Instantiate(Resources.Load<MeshRenderer>("minecraft/block"), modelElementParent.transform);
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

            Vector3 size = (to - from) / 16.0f * cubeObject.transform.localScale.x;
            Vector3 center = from / 16.0f + size / 2.0f;

            //cubeObject.transform.localPosition = center;

            cubeObject.transform.localScale = size;
            cubeObject.transform.localPosition = center;

            // ť���� �ؽ��ĸ� ����
            if (element.ContainsKey("faces"))
            {
                JObject faces = element["faces"] as JObject;
                foreach (var face in faces)
                {

                    // �� �ؽ�ó �ε� �� ����
                    var faceTexture = face.Value["texture"];
                    int idx = MinecraftModelData.faceToTextureName[face.Key];
                    //cubeObject.materials[idx].shader = BDObjManager.BDObjShader;
                    cubeObject.materials[idx].mainTexture = CreateTexture(faceTexture.ToString(), model.textures);
                }
            }

            //ť���� ȸ���� ����
            if (element.ContainsKey("rotation"))
            {
                JObject rotation = element["rotation"] as JObject;
                Vector3 origin = new Vector3(
                    rotation["origin"][0].Value<float>(),
                    rotation["origin"][1].Value<float>(),
                    rotation["origin"][2].Value<float>()
                    ) / 16.0f;
                Vector3 axis = rotation["axis"].ToString() switch
                {
                    "x" => Vector3.right,
                    "y" => Vector3.up,
                    "z" => Vector3.forward,
                    _ => Vector3.zero
                };

                float angle = rotation["angle"].Value<float>();

                // ť�긦 �߽ɿ��� ȸ��
                cubeObject.transform.RotateAround(cubeObject.transform.position + origin, axis, angle);
            }
        }
    }

    void AlignBlockDisplayToAABBCorner()
    {
        // 1. AABB ���
        Bounds aabb = new Bounds();
        bool isInitialized = false;

        foreach (Transform child in modelElementParent.transform)
        {
            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (!isInitialized)
                {
                    aabb = renderer.bounds;
                    isInitialized = true;
                }
                else
                {
                    aabb.Encapsulate(renderer.bounds);
                }
            }
        }

        // 2. AABB�� ���ϴ� ���� ������ ���
        Vector3 corner = aabb.min;

        // 3. Model �̵� (BlockDisplay�� Pivot���� ����)
        Vector3 offset = transform.position - corner;
        modelElementParent.transform.position += offset;
    }

    Texture2D CreateTexture(string path, JObject textures)
    {
        if (path[0] == '#')
        {
            return CreateTexture(textures[path.Substring(1)].ToString(), textures);
        }

        string texturePath = "textures/" + MinecraftFileManager.RemoveNamespace(path) + ".png";
        //Debug.Log(texturePath);
        Texture2D texture = MinecraftFileManager.GetTextureFile(texturePath);
        if (texture == null)
        {
            Debug.LogError("Texture not found: " + texturePath);
            return null;
        }
        return texture;
    }
}
