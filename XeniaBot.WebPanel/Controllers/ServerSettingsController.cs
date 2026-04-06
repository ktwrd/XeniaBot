using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Repositories;
using XeniaBot.MongoData.Services;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.ServerLog;
using ServerLogEvent = XeniaDiscord.Data.Models.ServerLog.ServerLogEvent;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    [HttpPost("~/Server/{id}/Settings/Log")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id", RequiredPermission = Discord.GuildPermission.ManageChannels)]
    public async Task<IActionResult> SaveSettings_LogSystem(
        ulong id,
        [FromForm] string jsonData)
    {
        var guildIdStr = id.ToString();
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", $"Guild not found: {id}");
        var currentUser = guild.GetUser(AspHelper.GetUserId(HttpContext) ?? 0);
        if (currentUser == null)
        {
            return View("NotAuthorized");
        }

        string? jsonStringData = null;
        List<JsTypeServerLogConfigItem>? data = null;

        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            jsonStringData = Encoding.UTF8.GetString(Convert.FromBase64String(jsonData));
            data = JsonSerializer.Deserialize<List<JsTypeServerLogConfigItem>>(jsonStringData) ?? [];

            var guildModel = await _serverLogRepository.GetGuild(id)
                ?? new ServerLogGuildModel(id)
                {
                    Enabled = true
                };
            await _guildCacheRepo.Ensure(db, guild.Id, guild);
            if (!await db.ServerLogGuilds.AnyAsync(e => e.GuildId == guildModel.GuildId))
            {
                await db.ServerLogGuilds.AddAsync(guildModel);
            }
            var existing = await db.ServerLogChannels.AsNoTracking().Where(e => e.GuildId == guildIdStr).ToListAsync();

            var delete = existing
                .Where(model => !data.Any(item => item.ChannelId == model.ChannelId && model.Event.ToString() == item.Event))
                .ToList();
            var deleteIds = delete.Select(e => e.Id).Distinct().ToHashSet();
            var add = data.Where(item => !existing.Any(model => item.ChannelId == model.ChannelId && model.Event.ToString() == item.Event))
                .DistinctBy(e => new { e.ChannelId, e.Event })
                .Select(item => new
                {
                    item,
                    ChannelId = item.ChannelId.ParseULong(false),
                    Event = Enum.Parse<ServerLogEvent>(item.Event)
                })
                .Where(e => e.ChannelId.HasValue)
                .Select(item => new ServerLogChannelModel
                {
                    GuildId = guildIdStr,
                    ChannelId = item.ChannelId!.Value.ToString(), // checked in previous Where statement
                    Event = item.Event,
                    CreatedByUserId = currentUser.Id.ToString()
                })
                .ToList();

            var deleteCount = await db.ServerLogChannels
                .Where(e => deleteIds.Contains(e.Id))
                .ExecuteDeleteAsync();

            await db.ServerLogChannels.AddRangeAsync(add);

            await db.SaveChangesAsync();
            await trans.CommitAsync();
            _logger.LogTrace("Deleted {DeleteCount} record(s), Inserted {InsertCount} records. (guildName={GuildName}, guildId={GuildId}, userId={UserId}, username={Username})",
                deleteCount,
                add.Count,
                guild.Name,
                guild.Id,
                currentUser.Id,
                currentUser.Username);
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();

            try
            {
                await _errorReporting.Submit(new ErrorReportBuilder()
                    .WithException(ex)
                    .WithNotes($"Failed to save server log settings for Guild \"{guild.Name}\" ({guild.Id})")
                    .WithUser(currentUser)
                    .WithGuild(guild)
                    .AddSerializedAttachment("postJsonData.txt", jsonStringData));
            }
            catch (Exception iex)
            {
                _logger.LogCritical(iex, "Failed to report exception!!!");
            }
            return await ModerationView(id,
                messageType: "danger",
                message: $"Failed to save Server Log settings: {ex.GetType().Name} {ex.Message}");
        }
        
        return await ModerationView(id,
            messageType: "success",
            message: "Server Log settings saved");
    }

    [HttpPost("~/Server/{id}/Settings/RolePreserve")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> SaveSettings_RolePreserve(ulong id, bool enable)
    {
        try
        {
            await using var db = _db.CreateSession();
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {
                await _rolePreserveGuildRepo.EnableAsync(db, id, enable);
                
                await db.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }

            return await ModerationView(
                id, messageType: "success", message: $"Role Preserve " + (enable ? "Enabled" : "Disabled"));
        }
        catch (Exception ex)
        {
            Program.Core.GetRequiredService<ErrorReportService>()?
                .ReportException(ex, $"Failed to save role preserve settings");
            _logger.LogError(ex, "Failed to save role preserve settings for Guild {GuildId}",
                id);
            return await ModerationView(id,
                messageType: "danger",
                message: $"Failed to save Role Preserve settings. {ex.Message}");
        }
    }

    [HttpPost("~/Server/{id}/Settings/WarnStrike")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> SaveSettings_WarnStrike(ulong id, bool enable, int maxStrike, int strikeWindow)
    {
        try
        {
            if (maxStrike < 1)
            {
                return await ModerationView(id,
                    messageType: "danger",
                    message: $"Failed to save Warn Strike settings. Max Strike must be greater than one");
            }

            if (strikeWindow < 1)
            {
                return await ModerationView(id,
                    messageType: "danger",
                    message: $"Failed to save Warn Strike settings. Strike Window must be greater than one");
            }
            var warnStrikeService = CoreContext.Instance?.GetRequiredService<WarnStrikeService>()
                ?? throw new InvalidOperationException($"Could not find service {typeof(WarnStrikeService)}");
            var configRepo = CoreContext.Instance.GetRequiredService<GuildConfigWarnStrikeRepository>();
            var model = await warnStrikeService.GetStrikeConfig(id);

            model.EnableStrikeSystem = enable;
            model.MaxStrike = maxStrike;
            model.StrikeWindow = TimeSpan.FromDays(strikeWindow).TotalSeconds;

            await configRepo.InsertOrUpdate(model);
            return await ModerationView(
                id, messageType: "success", message: $"Warn Strike settings saved");
        }
        catch (Exception ex)
        {
            Program.Core.GetRequiredService<ErrorReportService>()?
                .ReportException(ex, $"Failed to save Warn Strike settings");
            _logger.LogError(ex, "Failed to save Warn Strike settings for Guild {GuildId}", id);
            return await ModerationView(id,
                messageType: "danger",
                message: $"Failed to save Warn Strike settings. {ex.Message}");
        }
    }
    
    [HttpPost("~/Server/{id}/Settings/Greeter")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> SaveSettings_Greeter(
        ulong id,
        bool inputMentionUser = false,
        string? inputChannelId = null,
        string? inputTitle = null,
        string? inputDescription = null,
        string? inputImgUrl = null,
        string? inputThumbUrl = null,
        string? inputFooterText = null,
        string? inputFooterImgUrl = null,
        string? inputAuthorText = null,
        string? inputAuthorImgUrl = null,
        string? inputColor = null,
        string? messageType = null,
        string? message = null)
    {
        ulong targetChannelId = 0;
        if (inputChannelId == null || inputChannelId?.Length < 1)
            targetChannelId = 0;
        else
        {
            try
            {
                targetChannelId = ulong.Parse(inputChannelId);
                if (targetChannelId == null)
                    throw new Exception("ChannelId is null");
            }
            catch (Exception ex)
            {
                return await GreeterJoinView(id,
                    messageType: "danger",
                    message: $"Failed to parse Channel Id. {ex.Message}");
            }
        }
        
        try
        {
            var controller = Program.Core.GetRequiredService<GuildGreeterConfigRepository>();
            var data = await controller.GetLatest(id)
                ?? new GuildGreeterConfigModel()
                {
                    GuildId = id
                };
            data.T_Title = inputTitle;
            data.T_Description = inputDescription;
            data.T_ImageUrl = inputImgUrl;
            data.T_ThumbnailUrl = inputThumbUrl;
            data.T_FooterText = inputFooterText;
            data.T_FooterImgUrl = inputFooterImgUrl;
            data.T_AuthorName = inputAuthorText;
            data.T_AuthorIconUrl = inputAuthorImgUrl;
            data.T_Color_Hex = inputColor;
            data.MentionNewUser = inputMentionUser;
            if (targetChannelId == 0)
                data.ChannelId = null;
            else
                data.ChannelId = targetChannelId;
            await controller.Add(data);
        
            return await GreeterJoinView(id,
                messageType: "success",
                message: $"Greeter settings saved");
        }
        catch (Exception ex)
        {
            Program.Core.GetRequiredService<ErrorReportService>()?
                .ReportException(ex, $"Failed to save greeter settings");
            _logger.LogError(ex, "Failed to save greeter settings in Guild {GuildId}",
                id);
            return await GreeterJoinView(id,
                messageType: "danger",
                message: $"Failed to save. {ex.Message}");
        }
    }

    [HttpPost("~/Server/{id}/Settings/GreeterBye")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> SaveSettings_GreeterBye(
        ulong id,
        bool inputMentionUser,
        string? inputChannelId,
        string? inputTitle,
        string? inputDescription,
        string? inputImgUrl,
        string? inputThumbUrl,
        string? inputFooterText,
        string? inputFooterImgUrl,
        string? inputAuthorText,
        string? inputAuthorImgUrl,
        string? inputColor)
    {
        ulong targetChannelId = 0;
        if (inputChannelId == null || inputChannelId?.Length < 1)
            targetChannelId = 0;
        else
        {
            try
            {
                targetChannelId = ulong.Parse(inputChannelId);
                if (targetChannelId == null)
                    throw new Exception("ChannelId is null");
            }
            catch (Exception ex)
            {
                Program.Core.GetRequiredService<ErrorReportService>()
                    .ReportException(ex, $"Failed to save goodbye settings");
                return await GreeterLeaveView(id,
                    messageType: "danger",
                    message: $"Failed to parse Channel Id. {ex.Message}");
            }
        }
        
        try
        {
            var controller = Program.Core.GetRequiredService<GuildGreetByeConfigRepository>();
            var data = await controller.GetLatest(id)
                ?? new GuildByeGreeterConfigModel()
                {
                    GuildId = id
                };
            data.T_Title = inputTitle;
            data.T_Description = inputDescription;
            data.T_ImageUrl = inputImgUrl;
            data.T_ThumbnailUrl = inputThumbUrl;
            data.T_FooterText = inputFooterText;
            data.T_FooterImgUrl = inputFooterImgUrl;
            data.T_AuthorName = inputAuthorText;
            data.T_AuthorIconUrl = inputAuthorImgUrl;
            data.T_Color_Hex = inputColor;
            data.MentionNewUser = inputMentionUser;
            if (targetChannelId == 0)
                data.ChannelId = null;
            else
                data.ChannelId = targetChannelId;
            await controller.Add(data);
        
            return await GreeterLeaveView(id,
                messageType: "success",
                message: $"Settings Saved");
        }
        catch (Exception ex)
        {
            Program.Core.GetRequiredService<ErrorReportService>()?
                .ReportException(ex, $"Failed to save goodbye settings");
            _logger.LogError(ex, "Failed to save goodbye settings for Guild {GuildId}",
                id);
            return await GreeterLeaveView(id,
                messageType: "danger",
                message: $"Failed to save settings. {ex.Message}");
        }
    }
}