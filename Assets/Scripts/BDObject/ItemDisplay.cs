using Newtonsoft.Json.Linq;
using System.Linq;
using UnityEngine;
using Minecraft;
using Minecraft.MColor;
using System;

public class ItemDisplay : DisplayObject
{
    public ItemModelGenerator itemModel;
    JObject currentItemState;

    public override void LoadDisplayModel(string name, string state)
    {
        // items ������ ������ �����ͼ�
        modelName = name;
        JObject ItemState = MinecraftFileManager.GetJSONData("items/" + name + ".json");

        if (!ItemState.ContainsKey("model"))
        {
            CustomLog.LogError("Model not found: " + name);
            return;
        }

        // �� ������ ������
        currentItemState = ItemState.GetValue("model") as JObject;

        CheckModelType(currentItemState);
    }

    private void CheckModelType(JObject model)
    {
        //CustomLog.Log(model["type"] + " : " + model);
        switch (model["type"].ToString())
        {
            case "minecraft:model":
                TypeModel(model["model"].ToString());
                break;
            case "minecraft:condition":
                CheckModelType(model["on_false"] as JObject);
                break;
            case "minecraft:select":
                TypeSelect(model);
                break;
            case "minecraft:special":
                TypeSpecial(model);
                break;
            case "minecraft:composite":
                JArray parts = model["models"] as JArray;
                int cnt = parts.Count();
                for (int i = 0; i < cnt; i++)
                {
                    CheckModelType(parts[i] as JObject);
                }
                break;
            case "minecraft:range_dispatch":

                CheckModelType(model["entries"][0]["model"] as JObject);
                break;
            default:
                CustomLog.LogError("Unknown model type: " + model["type"]);
                break;
        }
    }

    void TypeModel(string model)
    {
        //Debug.Log("Model: " + model);
        //string model = itemState["model"].ToString();
        if (model.StartsWith("minecraft:block/"))
        {
            GenerateUsingBlockModel(model);
        }
        else
        {
            SetItemModel(model);
        }
    }

    private void GenerateUsingBlockModel(string model, Color co)
    {
        var bd = Instantiate(GameManager.GetManager<BDObjectManager>().blockPrefab, transform);
        bd.modelName = model;
        bd.color = co;
        bd.SetModel(model);
    }

    void GenerateUsingBlockModel(string model)
    {
        GenerateUsingBlockModel(model, Color.white);
    }

    void TypeSelect(JObject itemState)
    {
        // gui�� ���� ���� ã�Ƽ� ����
        JArray cases = itemState["cases"] as JArray;
        foreach (var item in cases)
        {
            JObject caseItem = item as JObject;
            if (!caseItem.ContainsKey("when"))
            {
                CheckModelType(caseItem["model"] as JObject);
                return;
            }

            if (caseItem["when"] is JArray)
            {
                int cnt = caseItem["when"].Count();
                for (int i = 0; i < cnt; i++)
                {
                    string when = caseItem["when"][i].ToString();
                    if (when == "gui")
                    {
                        CheckModelType(caseItem["model"] as JObject);
                        return;
                    }
                }
            }
            else
            {
                string when = caseItem["when"].ToString();
                if (when == "gui")
                {
                    CheckModelType(caseItem["model"] as JObject);
                    return;
                }
            }
        }

        CheckModelType(itemState["fallback"] as JObject);
    }

    void TypeSpecial(JObject itemState)
    {
        JObject specialModel = itemState["model"] as JObject;
        string baseModel = itemState["base"].ToString();
        //Debug.Log("Base: " + baseModel);

        switch (specialModel["type"].ToString())
        {
            case "minecraft:bed":
            case "minecraft:chest":
            case "minecraft:shulker_box":
            case "minecraft:conduit":
            case "minecraft:decorated_pot":
                GenerateUsingBlockModel(baseModel.Replace("item/", "block/"));
                break;
            case "minecraft:banner":
                GenerateUsingBlockModel(
                    "block/" + specialModel["type"].ToString(),
                    MinecraftColorExtensions.ToColorEnum(specialModel["color"].ToString()).ToColor()
                    );

                break;
            case "minecraft:head":
                var head = Instantiate(GameManager.GetManager<BDObjectManager>().headPrefab, transform);
                head.GenerateHead(specialModel["kind"].ToString());
                break;
            case "minecraft:shield":
                //CustomLog.Log("Shield: " + baseModel);
                GenerateUsingBlockModel(baseModel);
                break;

        }

        /*
         * ���, ����ü, ���ڱ�, ���� : �� ����, ��� ���÷��� �ʵ� �� ����
         * ħ��, ����, ��Ŀ ���� : ������� �ѱ�� (Done)
         * �Ӹ� : �� ����, ��� ���÷��� �ʵ� �� ���� (�÷��̾� �Ӹ��� profile ó�� �ؾ���)
         * ǥ����, �Ŵ޸� ǥ���� : �� ����, ��� ���÷��� �ʵ� �� ���� �ٵ� BDEngine�� ������ ����(???)
         */
    }


    protected void SetItemModel(string modelLocation)
    {
        // �ҷ��� ���� �������� �����ϱ�
        modelLocation = MinecraftFileManager.RemoveNamespace(modelLocation);

        modelData = MinecraftFileManager.GetModelData("models/" + modelLocation + ".json").UnpackParent();

        //CustomLog.Log("Model Data: " + modelData);
        string layer0 = GetTexturePath(modelData.textures["layer0"].ToString(), modelData.textures);
        Texture2D texture = MinecraftFileManager.GetTextureFile(layer0);
        Texture2D texture2 = null;

        if (modelData.textures.ContainsKey("layer1"))
        {
            string layer1 = GetTexturePath(modelData.textures["layer1"].ToString(), modelData.textures);
            texture2 = MinecraftFileManager.GetTextureFile(layer1);
        }

        if (currentItemState.TryGetValue("tints", out JToken value))
        {
            SetTint(texture, value[0] as JObject);

            if (texture2 != null)
            {
                SetTint(texture2, value[1] as JObject);
            }
        }

        itemModel = Instantiate(GameManager.GetManager<BDObjectManager>().itemPrefab, transform);
        itemModel.Init(texture, texture2);
    }

    void SetTint(Texture2D texture, JObject tint)
    {
        Debug.Log("Tint: " + tint);
        if (tint["type"].ToString() == "minecraft:constant")
        {
            // ���� ��ȯ
            int packedValue = int.Parse(tint["value"].ToString());

            // ��Ȯ�� RGB �� ���� (��Ʈ ����ũ ����)
            float r = ((packedValue >> 16) & 0xFF) / 255f;
            float g = ((packedValue >> 8) & 0xFF) / 255f;
            float b = (packedValue & 0xFF) / 255f;

            Color color = new Color(r, g, b, 1.0f);
           // CustomLog.Log("Color: " + color);

            // �ؽ�ó�� ���� ����
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a == 0) continue;
                pixels[i] = Color.Lerp(pixels[i], color, 0.9f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }



    }
}
