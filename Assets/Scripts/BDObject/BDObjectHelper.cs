using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

public static class BDObjectHelper
{
    private static readonly Regex tagsRegex = new Regex(@"Tags:\[([^\]]+)\]");
    private static readonly Regex uuidRegex = new Regex(@"UUID:\[I;(-?\d+),(-?\d+),(-?\d+),(-?\d+)\]");

    // Tags:[]���� �±� ���ڿ� ����
    public static string GetTags(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;

        Match match = tagsRegex.Match(input);
        return match.Success ? match.Groups[1].Value : null;
    }

    // UUID:[]���� UUID ���ڿ� ����
    public static string GetUUID(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;

        Match match = uuidRegex.Match(input);
        return match.Success
            ? $"{match.Groups[1].Value},{match.Groups[2].Value},{match.Groups[3].Value},{match.Groups[4].Value}"
            : null;
    }

    /// <summary>
    /// ��ġ ��Ÿ�� �̸����� �����Ӱ� ���� ����
    /// </summary>
    public static int ExtractNumber(string input, string key, int defaultValue = 0)
    {
        Match match = Regex.Match(input, $@"\b{key}(\d+)\b");
        return match.Success ? int.Parse(match.Groups[1].Value) : defaultValue;
    }

    public static Dictionary<string, T> SetDictionary<T>(T root, Func<T, BDObject> getBDObj, Func<T, IEnumerable<T>> getChildren)
    {
        int count = 0;
        Dictionary<string, T> IDDataDict = new Dictionary<string, T>();
        Queue<T> queue = new Queue<T>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            T obj = queue.Dequeue();
            BDObject bdObj = getBDObj(obj);

            if (string.IsNullOrEmpty(bdObj.ID))
            {
                bdObj.ID = count.ToString();
                count++;
            }

            IDDataDict[bdObj.ID] = obj;


            // �ڽĵ� ť�� �߰�
            foreach (var child in getChildren(obj))
            {
                queue.Enqueue(child);
            }
        }
        return IDDataDict;
    }
}
