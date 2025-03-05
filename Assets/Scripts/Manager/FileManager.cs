using UnityEngine;
using System.Collections;
using SimpleFileBrowser;
using System.IO;
using System.Text;
using System;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public class FileManager : BaseManager
{
    public BDObjectManager bdObjManager;
    public AnimObjList animObjList;
    public HashSet<HeadGenerator> WorkingGenerators = new HashSet<HeadGenerator>();

    private void Start()
    {
        bdObjManager = GameManager.GetManager<BDObjectManager>();

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

    IEnumerator ShowLoadDialogCoroutine(Action<List<string>> callback)
    {
        // ���� �������� ���� ����ڰ� ������ �����ϰų� ����� ������ ���
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Select Files", "Load");

        // ���� �������� ������ �ҷ����� �ݹ� �Լ� ȣ��
        if (FileBrowser.Success)
            // ���� Success�� false��� Result�� null�� �ȴ�.
        { 
            // ���� �и��ϱ�
            List<string> files = new List<string>();
            string[] result = FileBrowser.Result;
            for (int i = 0; i < result.Length; i++)
            {
                // ���� �� ��� ���ϵ� ����Ʈ�� �߰�
                if (Directory.Exists(result[i]))
                {
                    string[] folderFiles = Directory.GetFiles(result[i], "*.bdengine", SearchOption.AllDirectories);

                    files.AddRange(folderFiles);
                }
                else
                {
                    // �ƴ϶�� �׳� �߰�
                    files.Add(result[i]);
                }
            }

            callback?.Invoke(files); 
        }
        else
        {
            CustomLog.Log("Failed to load file");
        }
        
    }

    // ���� ����Ʈ
    public void ImportFile()
    {
        StartCoroutine(ShowLoadDialogCoroutine(AfterLoadFile));
    }

    // ������ ����Ʈ
    public void ImportFrame(AnimObject target, int tick)
    {
        StartCoroutine(ShowLoadDialogCoroutine((filepaths) => AfterLoadFrame(filepaths[0], target, tick)));
    }

    // ���� �ҷ��ͼ� ���÷��� �����ϱ�
    public async void AfterLoadFile(List<string> filepaths)
    {
        GameManager.GetManager<UIManger>().SetLoadingPanel(true);

        //List<Task> runningTasks = new List<Task>();

        // ù��° ���Ϸ� ���÷��� ����
        AnimObject animObject = await MakeDisplay(filepaths[0]);

        // ���� ���Ϻ��ʹ� ������ �߰��ϱ�
        for (int i = 1; i < filepaths.Count; i++)
        {
            BDObject[] bdObjects = await ProcessFileAsync(filepaths[i]);
            animObject.AddFrame(bdObjects[0], Path.GetFileNameWithoutExtension(filepaths[i]));
        }
        while (WorkingGenerators.Count > 0) await Task.Delay(500);


        //for (int i = 0; i < filepaths.Count; i++)
        //{
        //    runningTasks.Add(MakeDisplay(filepaths[i]));
        //    // ���� ������ �ʹ� ������ ������ �۾�
        //    if (runningTasks.Count >= batch)
        //    {
        //        await Task.WhenAll(runningTasks);
        //        runningTasks.Clear();
        //    }
        //}
        //if (runningTasks.Count > 0)
        //{
        //    await Task.WhenAll(runningTasks);
        //}

        GameManager.GetManager<UIManger>().SetLoadingPanel(false);

        CustomLog.Log($"Import is Done! BDObject Count: {GameManager.GetManager<BDObjectManager>().BDObjectCount}");
    }

    // ���� �ҷ��ͼ� ������ �����ϱ�
    public async void AfterLoadFrame(string filepath, AnimObject target, int tick)
    {
        GameManager.GetManager<UIManger>().SetLoadingPanel(true);

        BDObject[] bdObjects = await ProcessFileAsync(filepath);
        target.AddFrame(Path.GetFileNameWithoutExtension(filepath), bdObjects[0], tick);

        GameManager.GetManager<UIManger>().SetLoadingPanel(false);
    }

    // ���� ���� ó�� �񵿱� �Լ�
    private async Task<BDObject[]> ProcessFileAsync(string filepath)
    {
        return await Task.Run(() =>
        {
            // ���� �б�(�ؽ�Ʈ)
            string base64Text = FileBrowserHelpers.ReadTextFromFile(filepath);

            // Base64 �� gzipData
            byte[] gzipData = Convert.FromBase64String(base64Text);

            // gzip ���� �� json
            string jsonData = DecompressGzip(gzipData);

#if UNITY_EDITOR
            Debug.Log(jsonData);
#endif

            // BDObject[] ������ȭ
            return JsonConvert.DeserializeObject<BDObject[]>(jsonData);
        });
    }

    // ���÷��� �����
    public async Task<AnimObject> MakeDisplay(string filepath)
    {
        BDObject[] bdObjects = await ProcessFileAsync(filepath);

        string fileName = Path.GetFileNameWithoutExtension(filepath);
        var bdobjCon = await bdObjManager.AddObject(bdObjects[0], fileName);
        return animObjList.AddAnimObject(bdobjCon, fileName);
    }

    // gzipData�� string���� ��ȯ�ϱ�
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
