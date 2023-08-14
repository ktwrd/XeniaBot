using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
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
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "warning",
                Message = "Unable to request Ban Sync: Log Channel not set."
            });
        }

        try
        {
            if (guild.GetTextChannel(configData.LogChannel) == null)
                throw new Exception("Not found");
        }
        catch (Exception ex)
        {
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Unable to request Ban Sync: Failed to get log channel.<br/><pre><code>{ex.Message}</code></pre>"
            });
        }
        
        switch (configData.State)
        {
            case BanSyncGuildState.PendingRequest:
                return RedirectToAction("Index", new
                {
                    Id = id,
                    MessageType = "warning",
                    Message = "Ban Sync access has already been requested"
                });
                break;
            case BanSyncGuildState.RequestDenied:
                return RedirectToAction("Index", new
                {
                    Id = id,
                    MessageType = "danger",
                    Message = "Ban Sync access has already been requested and denied."
                });
            case BanSyncGuildState.Blacklisted:
                return RedirectToAction("Index", new
                {
                    Id = id,
                    MessageType = "danger",
                    Message = "Your server has been blacklisted."
                });
                break;
            case BanSyncGuildState.Active:
                return RedirectToAction("Index", new
                {
                    Id = id,
                    MessageType = "danger",
                    Message = "Your server already has Ban Sync enabled"
                });
                break;
            case BanSyncGuildState.Unknown:
                // Request ban sync
                try
                {
                    var dcon = Program.Services.GetRequiredService<BanSyncController>();
                    if (dcon == null)
                        throw new Exception($"Failed to get BanSyncController");

                    var res = await dcon.RequestGuildEnable(guild.Id);
                    return RedirectToAction(
                        "Index", new
                        {
                            Id = id,
                            MessageType = "success",
                            Message = "Ban Sync: Your server is pending approval"
                        });
                }
                catch (Exception ex)
                {
                    return RedirectToAction("Index", new
                    {
                        Id = id,
                        MessageType = "danger",
                        Message = $"Unable to request Ban Sync: Failed to request.<br/><pre><code>{ex.Message}</code></pre>"
                    });
                }
                break;
        }
        return RedirectToAction("Index", new
        {
            Id = id,
            MessageType = "warning",
            Message = $"Ban Sync Fail: Unhandled state {configData.State}"
        });
    }
    [HttpPost("~/Server/{id}/Settings/BanSync")]
    public async Task<IActionResult> SaveSettings_BanSync(
        ulong id,
        string? logChannel = null)
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

        var channelIdRes = ParseChannelId(id, logChannel, out var channelId);
        if (channelIdRes != null)
            return channelIdRes;
        
        var controller = Program.Services.GetRequiredService<BanSyncConfigController>();
        var configData = await controller.Get(guild.Id) ?? new ConfigBanSyncModel()
        {
            GuildId = guild.Id,
            LogChannel = (ulong)channelId
        };
        configData.LogChannel = (ulong)channelId;
        await controller.Set(configData);

        return RedirectToAction("Index", new
        {
            Id = id,
            MessageType = "success",
            Message = "Successfully set Ban Sync log channel"
        });
    }
}