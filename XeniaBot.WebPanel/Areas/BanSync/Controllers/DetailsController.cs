using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XeniaBot.WebPanel.Areas.BanSync.Models;
using XeniaBot.WebPanel.Controllers;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Data.Repositories;
// ReSharper disable ConvertToPrimaryConstructor

namespace XeniaBot.WebPanel.Areas.BanSync.Controllers;

[Controller]
[Area("BanSync")]
[Route("~/BanSync/Record")]
public class DetailsController : BaseXeniaController
{
    private readonly ILogger<DetailsController> _logger;
    private readonly BanSyncRecordRepository _recordRepository;

    public DetailsController(
        IServiceProvider services,
        ILogger<DetailsController> logger)
    {
        _recordRepository = services.GetRequiredService<BanSyncRecordRepository>();
        _logger = logger;
    }
    
    [AuthRequired]
    [HttpGet("{id}")]
    public async Task<IActionResult> Index(string id)
    {
        try
        {
            var record = await _recordRepository.GetInfo(
                id,
                new BanSyncRecordRepository.QueryOptions
                {
                    IncludeBanSyncGuild = true,
                    IncludeUserPartialSnapshot = true,
                    IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext)
                });
            if (record == null)
            {
                return View("NotFound", new NotFoundViewModel
                {
                    Message = "Record does not exist: " + id
                });
            }

            var data = new BanSyncRecordViewModel()
            {
                Record = record
            };
            return View("Details", data);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get information for BanSync record: {RecordId}", id);
            throw;
        }
    }

    [AuthRequired]
    [RequireSuperuser]
    [HttpGet("{id:guid}/Ghost")]
    public async Task<IActionResult> UpdateGhost(Guid id, [FromQuery] int value)
    {
        var ghost = value != 1;
        try
        {
            var record = await _recordRepository.GetInfo(
                id,
                new BanSyncRecordRepository.QueryOptions
                {
                    IncludeBanSyncGuild = true,
                    IncludeUserPartialSnapshot = true,
                    IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext)
                });
            if (record == null)
            {
                return View("NotFound", new NotFoundViewModel
                {
                    Message = "Record does not exist: " + id
                });
            }
            await _recordRepository.SetGhostState(id, ghost);
            record.Ghost = ghost;
            var data = new BanSyncRecordViewModel
            {
                Record = record,
                Alert = new AlertComponentViewModel()
                {
                    Message = $"Successfully updated ghost state to: `{ghost}`",
                    Type = AlertComponentType.Success,
                    RenderMessageAsMarkdown = true,
                    ShowClose = true
                }
            };
            return View("Details", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Ghost={NewValue} for record {RecordId}", ghost, id);
            throw;
        }
    }

    [AuthRequired]
    [RequireSuperuser]
    [HttpGet("~/BanSync/Record/{id:guid}/Ghost/True")]
    public Task<IActionResult> UpdateGhostTrue(Guid id)
    {
        return UpdateGhost(id, 1);
    }
    
    [AuthRequired]
    [RequireSuperuser]
    [HttpGet("~/BanSync/Record/{id:guid}/Ghost/False")]
    public Task<IActionResult> UpdateGhostFalse(Guid id)
    {
        return UpdateGhost(id, 0);
    }
}