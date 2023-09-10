using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.Data;
using XeniaBot.WebPanel.Helpers;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    [HttpPost("~/Server/{id}/Settings/BanSync/Request")]
    public async Task<IActionResult> SaveSettings_BanSync_Request(ulong id)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
            return View("NotFound", "User not found");
        var user = _discord.GetUser((ulong)userId);
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");
        
        var controller = Program.Services.GetRequiredService<BanSyncConfigController>();
        var configData = await controller.Get(guild.Id) ?? new ConfigBanSyncModel()
        {
            GuildId = guild.Id
        };

        if (configData.LogChannel == 0)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Unable to request Ban Sync: Log Channel not set.");
        }

        try
        {
            if (guild.GetTextChannel(configData.LogChannel) == null)
                throw new Exception("Not found");
        }
        catch (Exception ex)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Unable to request Ban Sync: Failed to get log channel: {ex.Message}");
        }
        
        switch (configData.State)
        {
            case BanSyncGuildState.PendingRequest:
                return await Index(id,
                    messageType: "danger",
                    message: "Ban Sync access has already been requested");
                return RedirectToAction("Index", new
                {
                    Id = id,
                    MessageType = "warning",
                    Message = "Ban Sync access has already been requested"
                });
                break;
            case BanSyncGuildState.RequestDenied:
                return await Index(id,
                    messageType: "danger",
                    message: "Ban Sync access has already been requested and denied.");
            case BanSyncGuildState.Blacklisted:
                return await Index(id,
                    messageType: "danger",
                    message: $"Your server has been blacklisted");
                return RedirectToAction("Index", new
                {
                    Id = id,
                    MessageType = "danger",
                    Message = "Your server has been blacklisted."
                });
                break;
            case BanSyncGuildState.Active:
                return await Index(id,
                    messageType: "danger",
                    message: $"Your server already has Ban Sync enabled");
                break;
            case BanSyncGuildState.Unknown:
                // Request ban sync
                try
                {
                    var dcon = Program.Services.GetRequiredService<BanSyncController>();
                    if (dcon == null)
                        throw new Exception($"Failed to get BanSyncController");

                    var res = await dcon.RequestGuildEnable(guild.Id);
                    return await Index(id,
                        messageType: "success",
                        message: $"Ban Sync: Your server is pending approval");
                }
                catch (Exception ex)
                {
                    return await Index(id,
                        messageType: "danger",
                        message: $"Unable to request Ban Sync: Failed to request. {ex.Message}");
                }
                break;
        }
        return await Index(id,
            messageType: "warning",
            message: $"Ban Sync Fail: Unhandled state {configData.State}");
    }
    [HttpPost("~/Server/{id}/Settings/BanSync")]
    public async Task<IActionResult> SaveSettings_BanSync(
        ulong id,
        string? logChannel = null)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        var channelIdResult = ParseChannelId(logChannel);
        if (channelIdResult.ErrorContent != null)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Failed to parse Ban Sync Log Channel Id. {channelIdResult.ErrorContent}");
        }
        var channelId = (ulong)channelIdResult.ChannelId;
        
        var controller = Program.Services.GetRequiredService<BanSyncConfigController>();
        var configData = await controller.Get(guild.Id) ?? new ConfigBanSyncModel()
        {
            GuildId = guild.Id,
            LogChannel = channelId
        };
        configData.LogChannel = channelId;
        await controller.Set(configData);

        return await Index(id,
            messageType: "success",
            message: $"Successfully saved Ban Sync Log channel");
    }
    [HttpPost("~/Server/{id}/Settings/Confession")]
    public async Task<IActionResult> SaveSettings_Confession(ulong id, string? modalChannelId, string? messageChannelId)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");
        
        var modalChannelIdRes = ParseChannelId(modalChannelId);
        if (modalChannelIdRes.ErrorContent != null)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Failed to parse ChannelId for confession modal. {modalChannelIdRes.ErrorContent}");
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to parse ChannelId for Modal. {modalChannelIdRes.ErrorContent}"
            });
        }
        var modalId = (ulong)modalChannelIdRes.ChannelId;
        
        var msgChannelIdRes = ParseChannelId(messageChannelId);
        if (msgChannelIdRes.ErrorContent != null)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Failed to parse ChannelId for messages. {msgChannelIdRes.ErrorContent}");
        }
        var msgId = (ulong)msgChannelIdRes.ChannelId;

        try
        {
            var controller = Program.Services.GetRequiredService<ConfessionConfigController>();
            var data = await controller.GetGuild(id) ??
                       new ConfessionGuildModel()
                       {
                           GuildId = id
                       };
            if (data.ModalChannelId != modalId)
            {
                await controller.InitializeModal(id, msgId, modalId);
            }

            data = await controller.GetGuild(id) ??
                   new ConfessionGuildModel()
                   {
                       GuildId = id
                   };
            data.ModalChannelId = modalId;
            data.ChannelId = msgId;
            await controller.Set(data);
        }
        catch (Exception ex)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Failed to save confession settings. {ex.Message}");
        }
        return await Index(id,
            messageType: "success",
            message: $"Successfully saved Ban Sync Log channel");
    }

    [HttpGet("~/Server/{id}/Settings/Confession/Purge")]
    public async Task<IActionResult> SaveSettings_Confession_Purge(ulong id)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        try
        {
            var controller = Program.Services.GetRequiredService<ConfessionConfigController>();
            var data = await controller.GetGuild(id)
                ?? new ConfessionGuildModel()
                {
                   GuildId = id
                };
            await controller.Delete(data);
            return await Index(id,
                messageType: "success",
                message: $"Successfully purged confession messages");
        }
        catch (Exception ex)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Failed to purge confession messages. {ex.Message}");
        }
    }
}