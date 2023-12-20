using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        var result = new List<List<T>>();
        for (int i = 0; i < source.Count(); i++)
        {
            int rootArrayIndex = (int)Math.Floor(i / (float)itemsPerChunk);
            if (result.Count <= rootArrayIndex)
                result.Add(new List<T>());

            int innerArrayIndex = i % itemsPerChunk;

            result[rootArrayIndex].Add(source.ElementAt(i));
        }

        return result.Select(v => v.ToArray()).ToArray();
    }

    public static string[][] ChunkLength(IEnumerable<string> source, int maximumLengthPerChunk)
    {
        var result = new List<List<string>>();
        for (int i = 0; i < source.Count(); i++)
        {
            if (result.Count < 1)
            {
                result.Add(new List<string>());
            }

            if (result.Last().Select(v => v.Length).Sum() > maximumLengthPerChunk)
            {
                result.Add(new List<string>());
            }
            result.Last().Add(source.ElementAt(i));
        }

        return result.Select(v => v.ToArray()).ToArray();
    }
}