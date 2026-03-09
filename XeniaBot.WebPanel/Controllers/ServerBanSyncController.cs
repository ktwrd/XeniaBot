using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaBot.WebPanel.Models.Component;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
public class ServerBanSyncController : BaseXeniaController
{
    private readonly ILogger<ServerBanSyncController> _logger;
    private readonly BanSyncRecordRepository _bansyncRecordRepository;
    public ServerBanSyncController(
        IServiceProvider services,
        ILogger<ServerBanSyncController> logger)
        : base()
    {
        _bansyncRecordRepository = services.GetRequiredService<BanSyncRecordRepository>();
        _logger = logger;
    }

    [HttpGet("~/BanSync/Record/{id}")]
    [AuthRequired]
    public async Task<IActionResult> RecordInfo(string id)
    {
        var record = await _bansyncRecordRepository.GetInfo(id, new()
        {
            IncludeBanSyncGuild = true,
            IncludeUserPartialSnapshot = true,
            IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext)
        });
        if (record == null)
        {
            return View("NotFound");
        }

        var data = new BanSyncRecordViewModel()
        {
            Record = record
        };
        await PopulateModel(data);
        return View("Details", data);
    }

    [HttpGet("~/BanSync/Record/{id}/Ghost/True")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> GhostEnable(Guid id)
    {
        try
        {
            var record = await _bansyncRecordRepository.GetInfo(id, new()
            {
                IncludeBanSyncGuild = true,
                IncludeUserPartialSnapshot = true,
                IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext)
            });
            if (record == null)
            {
                return View("NotFound");
            }
            await _bansyncRecordRepository.SetGhostState(id, true);
            record.Ghost = true;
            var data = new BanSyncRecordViewModel()
            {
                Record = record
            };
            await PopulateModel(data);

            return View("DetailsComponent", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Ghost=True for record {RecordId}", id);
            throw;
        }
    }

    [HttpGet("~/BanSync/Record/{id}/Ghost/False")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> GhostDisable(Guid id)
    {
        try
        {
            var record = await _bansyncRecordRepository.GetInfo(id, new()
            {
                IncludeBanSyncGuild = true,
                IncludeUserPartialSnapshot = true,
                IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext)
            });
            if (record == null)
            {
                return View("NotFound");
            }
            await _bansyncRecordRepository.SetGhostState(id, false);
            record.Ghost = false;
            var data = new BanSyncRecordViewModel()
            {
                Record = record
            };
            await PopulateModel(data);

            return View("DetailsComponent", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Ghost=False for record {RecordId}", id);
            throw;
        }
    }
}