using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using XeniaBot.Evidence.Models;
using XeniaBot.Evidence.Repositories;
using XeniaBot.Evidence.Responses;
using XeniaBot.Evidence.Services;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;

namespace XeniaBot.Evidence.StorageProxy.Controllers;

[ApiController]
public class FileController : Controller
{
    [HttpGet("~/File/{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404, Type = typeof(FileControllerErrorResponse))]
    [ProducesResponseType(500, Type = typeof(FileControllerErrorResponse))]
    public async Task<IActionResult> Get(string id)
    {
        // safely get EvidenceFileRepository and EvidenceFileService
        var fileRepo = CoreContext.Instance?.GetRequiredService<EvidenceFileRepository>();
        if (fileRepo == null)
        {
            Response.StatusCode = 500;
            return Json(new FileControllerErrorResponse()
            {
                Message = "EvidenceFileRepository not found"
            }, XeniaHelper.SerializerOptions);
        }
        var fileService = CoreContext.Instance?.GetRequiredService<EvidenceFileService>();
        if (fileService == null)
        {
            Response.StatusCode = 500;
            return Json(new FileControllerErrorResponse()
            {
                Message = "EvidenceFileService not found"
            }, XeniaHelper.SerializerOptions);
        }

        // get the document in EvidenceFileRepository
        var document = await fileRepo.Get(id);
        if (document == null || document.IsDeleted || document.IsMarkedForDeletion)
        {
            Response.StatusCode = 404;
            return Json(new FileControllerErrorResponse()
            {
                Message = "Document not found"
            }, XeniaHelper.SerializerOptions);
        }

        // write to a memory stream so we can use FileStreamResult
        var ms = new MemoryStream();
        if (!fileService.WriteFileToStream(document, ms, out var err))
        {
            Response.StatusCode = 500;
            return Json(new FileControllerErrorResponse()
            {
                Message = err ?? "Failed to get file"
            }, XeniaHelper.SerializerOptions);
        }

        Response.StatusCode = 200;
        Response.ContentType = document.ContentType;
        return new FileStreamResult(ms, new MediaTypeHeaderValue(document.ContentType))
        {
            FileDownloadName = document.Filename
        };
    }

    [HttpGet("~/File/{id}/Details")]
    [ProducesResponseType(200, Type = typeof(EvidenceFileModelDetails))]
    [ProducesResponseType(404, Type = typeof(FileControllerErrorResponse))]
    [ProducesResponseType(500, Type = typeof(FileControllerErrorResponse))]
    public async Task<IActionResult> GetDetails(string id)
    {
        // safely get the evidence file repo
        var fileRepo = CoreContext.Instance?.GetRequiredService<EvidenceFileRepository>();
        if (fileRepo == null)
        {
            Response.StatusCode = 500;
            return Json(new FileControllerErrorResponse()
            {
                Message = "EvidenceFileRepository not found"
            }, XeniaHelper.SerializerOptions);
        }

        // pretend file doesn't exist when marked for deletion.
        var document = await fileRepo.Get(id);
        if (document == null || document.IsDeleted || document.IsMarkedForDeletion)
        {
            Response.StatusCode = 404;
            return Json(new FileControllerErrorResponse()
            {
                Message = "Document not found"
            }, XeniaHelper.SerializerOptions);
        }

        var details = document.ToDetails();
        return Json(details, XeniaHelper.SerializerOptions);
    }
}