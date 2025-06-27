using System;
using System.IO;
using System.Runtime.Serialization.Json;
using UnityEngine;

/*
프레임 파일위치, 데이터팩 저장위치

폴더 이름
생성모드
임시플레이어
스코어 이름
기본 보간값
기본 스코어증가값
시작 스코어 값
summon저장이름 , 저장위치
score저장이름
frame저장위치
*/


public class Setting
{
    public int generationMode = 0;
    public int defaultInterpolationDuration = 3;
    public int defaultScoreIncreaseValue = 1;
    public int startScoreValue = 1;

    //너가 가장 중요해 임마 너 입력 안하면 딴놈들 다 무용지물이여
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
            Debug.Log("끼에엑;; 저장..! 저장이 외 않 되ㅣㅣㅣㅣㅣ");
        }
    }
}