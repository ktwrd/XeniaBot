using System.Collections.Generic;

namespace XeniaBot.Shared.Helpers;

public static class ArrayHelper
{
    /// <summary>
    /// Split <paramref name="source"/> into arrays with the maximum size of <paramref name="itemsPerChunk"/>.
    /// </summary>
    /// <param name="source">Source Array to chunk</param>
    /// <param name="itemsPerChunk">Maximum amount of items in inner array</param>
    /// <returns>2D chunked array of <paramref name="source"/> with the inner array having the maximum size of <paramref name="itemsPerChunk"/></returns>
    public static T[][] Chunk<T>(IEnumerable<T> source, int itemsPerChunk)
    {
        var result = new List<T[]>();
        var innerList = new List<T>();
        foreach (var item in source)
        {
            if (innerList.Count + 1 >= itemsPerChunk)
            {
                result.Add(innerList.ToArray());
                innerList = [];
            }
            innerList.Add(item);
        }
        if (innerList.Count > 0)
        {
            result.Add(innerList.ToArray());
        }

        return result.ToArray();
    }

    public static string[][] ChunkLength(IEnumerable<string> source, int maximumLengthPerChunk)
    {
        var result = new List<string[]>();
        var innerList = new List<string>();
        var innerListCount = 0;
        foreach (var item in source)
        {
            if (innerListCount + item.Length >= maximumLengthPerChunk)
            {
                result.Add(innerList.ToArray());
                innerList = [];
                innerListCount = 0;
            }
            innerList.Add(item);
            innerListCount += item.Length;
        }

        if (innerList.Count > 0)
        {
            result.Add(innerList.ToArray());
        }

        return result.ToArray();
    }
}