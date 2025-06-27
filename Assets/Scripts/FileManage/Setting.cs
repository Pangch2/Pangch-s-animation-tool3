using System;
using System.IO;
using System.Runtime.Serialization.Json;
using UnityEngine;

/*
������ ������ġ, �������� ������ġ

���� �̸�
�������
�ӽ��÷��̾�
���ھ� �̸�
�⺻ ������
�⺻ ���ھ�������
���� ���ھ� ��
summon�����̸� , ������ġ
score�����̸�
frame������ġ
*/


public class Setting
{
    public int generationMode = 0;
    public int defaultInterpolationDuration = 3;
    public int defaultScoreIncreaseValue = 1;
    public int startScoreValue = 1;

    //�ʰ� ���� �߿��� �Ӹ� �� �Է� ���ϸ� ����� �� ���������̿�
    public string pangchFolderPath;

    public string datapackSavePath;
    public string selectDatapackName = "Pangch's Animation Datapack";
    public string scoreTempPlayerName;
    public string scoreObjectName;
    public string scoreSaveName = "frame";
    public string scoreSavePath;
    public string frameSavePath;
    public void SaveToFile(string path)
    {
        try
        {
            using Stream stream = File.OpenWrite(path);
            new DataContractJsonSerializer(typeof(Setting)).WriteObject(stream, this);
            Console.WriteLine("Datapack Save Path: " + datapackSavePath);
        }
        catch
        {
            Debug.Log("������;; ����..! ������ �� �� �ǤӤӤӤӤ�");
        }
    }
}