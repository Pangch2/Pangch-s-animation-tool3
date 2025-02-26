using UnityEngine;
using System.Collections;
using SimpleFileBrowser;
using System.IO;
using System.Text;
using System;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

public class FileManager : BaseManager
{
    public BDObjectManager BDObjManager;
    public HashSet<HeadGenerator> WorkingGenerators = new HashSet<HeadGenerator>();

    private void Start()
    {
        BDObjManager = GameManager.GetManager<BDObjectManager>();

        // .bdengine, .bdstudio Ȯ���ڸ� ���͸�
        FileBrowser.SetFilters(false,
            new FileBrowser.Filter("Files", ".bdengine", ".bdstudio"));

        // ��ó ��� �߰�
#if UNITY_EDITOR
        FileBrowser.AddQuickLink("Launcher Folder", Application.dataPath);
#else
        FileBrowser.AddQuickLink("Launcher Folder", Application.dataPath + "/../");
#endif

        // �ٿ�ε� ���� �߰�
        string download = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        download = Path.Combine(download, "Downloads");

        FileBrowser.AddQuickLink("Downloads", download);
    }

    IEnumerator ShowLoadDialogCoroutine(Action<string[]> callback)
    {
        BDEngineStyleCameraMovement.CanMoveCamera = false;
        // ���� �������� ���� ����ڰ� ������ �����ϰų� ����� ������ ���
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Select Files", "Load");

        // ���� �������� ������ �ҷ����� �ݹ� �Լ� ȣ��
        if (FileBrowser.Success)
            // ���� Success�� false��� Result�� null�� �ȴ�.
            callback?.Invoke(FileBrowser.Result); 
        else
        {
            CustomLog.Log("Failed to load file");
            BDEngineStyleCameraMovement.CanMoveCamera = true;
        }
        
    }

    public void ImportFile()
    {
        StartCoroutine(ShowLoadDialogCoroutine(AfterLoadFile));
    }

    public void ImportFrame(AnimObject target, int tick)
    {
        StartCoroutine(ShowLoadDialogCoroutine((filepaths) => AfterLoadFrame(filepaths[0], target, tick)));
    }

    public async void AfterLoadFile(string[] filepaths)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        GameManager.GetManager<UIManger>().SetLoadingPanel(true);

        Task[] tasks = new Task[filepaths.Length];

        for (int i = 0; i < filepaths.Length; i++)
        {
            tasks[i] = MakeDisplay(filepaths[i]);
        }
        await Task.WhenAll(tasks);
        await WaitWhileAsync(() => WorkingGenerators.Count > 0);
        stopwatch.Stop();

        GameManager.GetManager<UIManger>().SetLoadingPanel(false);
        BDEngineStyleCameraMovement.CanMoveCamera = true;

        CustomLog.Log($"BDObject Count: {GameManager.GetManager<BDObjectManager>().BDObjectCount}, Import Time: {stopwatch.ElapsedMilliseconds}ms");
    }

    public async void AfterLoadFrame(string filepath, AnimObject target, int tick)
    {
        GameManager.GetManager<UIManger>().SetLoadingPanel(true);

        BDObject[] bdObjects = await ProcessFileAsync(filepath);
        target.AddFrame(bdObjects, tick);

        GameManager.GetManager<UIManger>().SetLoadingPanel(false);
    }

    public async Task WaitWhileAsync(Func<bool> conditionFunc, int checkIntervalMs = 500)
    {
        while (conditionFunc())
        {
            await Task.Delay(checkIntervalMs); // ������ �ð�(ms)��ŭ ��� �� �ٽ� üũ
        }
    }

    // ���� ���� ó�� �񵿱� �Լ�
    private async Task<BDObject[]> ProcessFileAsync(string filepath)
    {
        // 1. ���� �б� (�񵿱�)
        byte[] file = await Task.Run(() => FileBrowserHelpers.ReadBytesFromFile(filepath));

        // 2. Base64 ���ڵ�
        byte[] gzipData = Convert.FromBase64String(Encoding.UTF8.GetString(file));

        // 3. GZip ���� ���� (�񵿱�)
        string jsonData = await Task.Run(() => DecompressGzip(gzipData));

        // 4. JSON �Ľ�
        return await Task.Run(() => JsonConvert.DeserializeObject<BDObject[]>(jsonData));
    }

    public async Task MakeDisplay(string filepath)
    {
        BDObject[] bdObjects = await ProcessFileAsync(filepath);
        await BDObjManager.AddObjects(bdObjects);
    }

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
