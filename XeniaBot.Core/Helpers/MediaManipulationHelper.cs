using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;

namespace XeniaBot.Core.Helpers;

public static class MediaManipulationHelper
{
    public static async Task<(byte[], string)> GetUrlBytes(IAttachment file)
    {
        if (!SupportedContentTypes.Contains(file.ContentType))
            throw new Exception($"Unsupported content type {file.ContentType}");

        var client = new HttpClient();
        var response = await client.GetAsync(file.ProxyUrl);
        string responseContentType = "";
        if (response.Content.Headers.ContentType?.MediaType != null)
            responseContentType = response.Content.Headers.ContentType.MediaType;
        if (!SupportedContentTypes.Contains(responseContentType))
            throw new Exception($"Unsupported content type {responseContentType}");

        var data = response.Content.ReadAsByteArrayAsync().Result;
        return (data, responseContentType);
    }

    public static bool IsAnimatedType(string contentType)
    {
        return new string[]
        {
            "image/gif",
        }.Contains(contentType);
    }
    public static string[] SupportedContentTypes => new string[]
    {
        "image/png",
        "image/gif",
        "image/jpeg",
        "image/webp",
    };
}