using UnityEngine;
using System.Collections;
using SimpleFileBrowser;
using System.IO;
using System.Text;
using System;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Collections.Generic;

public class FileManager : RootManager
{
    private void Start()
    {
        // .bdengine, .bdstudio Ȯ���ڸ� ���͸�
        FileBrowser.SetFilters(false,
            new FileBrowser.Filter("Files", ".bdengine", ".bdstudio"));

        // ��ó ��� �߰�
        FileBrowser.AddQuickLink("Launcher File", Application.dataPath);

        // �ٿ�ε� ���� �߰�
        string download = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        download = Path.Combine(download, "Downloads");

        FileBrowser.AddQuickLink("Downloads", download);
    }

    IEnumerator ShowLoadDialogCoroutine(Action<string[]> callback)
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, true, null, null, "Select Files", "Load");

        // Dialog is closed
        // Print whether the user has selected some files or cancelled the operation (FileBrowser.Success)
        //Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
            callback?.Invoke(FileBrowser.Result); // FileBrowser.Result is null, if FileBrowser.Success is false
        else
        {
            Debug.LogError("Failed to load file");
        }
    }

    public void ImportFile()
    {
        StartCoroutine(ShowLoadDialogCoroutine(AfterLoadFile));
    }

    void AfterLoadFile(string[] filepaths)
    {
        foreach (var path in filepaths)
        {
            byte[] file = FileBrowserHelpers.ReadBytesFromFile(path);

            string base64Data = Encoding.UTF8.GetString(file);
            // 2. Base64 ���ڵ�
            byte[] gzipData = Convert.FromBase64String(base64Data);

            // 3. GZip ���� ����
            string jsonData = DecompressGzip(gzipData);

            // 4. JSON ������ ���
            //Debug.Log("������ JSON ������:");
            Debug.Log(jsonData);

            MakeDisplay(jsonData);

            //var projects = JsonConvert.DeserializeObject<List<BDObject>>(jsonData);

            //GameManager.GetManager<BDObjectManager>().AddObjectUsingOld(projects);

            //System.Diagnostics.Stopwatch stopwatch = new();
            //stopwatch.Start();
            //GameManager.GetManager<BDObjectManager>().AddObjectUsingOld(projects);
            //stopwatch.Stop();
            //Debug.Log("AddObjectOld Time: " + stopwatch.ElapsedMilliseconds + "ms");

            //GameManager.GetManager<BDObjectManager>().ClearAllObject();


            //GameManager.GetManager<BDObjectManager>().AddObjects(projects);

            Debug.Log("end add objects");
        }
    }

    public void MakeDisplay(string jsonData) => 
        GameManager.GetManager<BDObjectManager>()
        .AddObjectUsingOld(
            JsonConvert.DeserializeObject<List<BDObject>>(jsonData)
            );

    string DecompressGzip(byte[] gzipData)
    {
        using (var compressedStream = new MemoryStream(gzipData))
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        using (var reader = new StreamReader(gzipStream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }
}
