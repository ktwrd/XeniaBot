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
                Message = $"Unable to request Ban Sync: Failed to get log channel. {ex.Message}"
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
                        Message = $"Unable to request Ban Sync: Failed to request. {ex.Message}"
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
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        var channelIdResult = ParseChannelId(logChannel);
        if (channelIdResult.ErrorContent != null)
        {
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to parse Log Channel Id. {channelIdResult.ErrorContent}"
            });
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

        return RedirectToAction("Index", new
        {
            Id = id,
            MessageType = "success",
            Message = "Successfully set Ban Sync log channel"
        });
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
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to parse ChannelId for Messages. {msgChannelIdRes.ErrorContent}"
            });
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
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to save confession settings: {ex.Message}"
            });
        }
        return RedirectToAction("Index", new
        {
            Id = id,
            MessageType = "success",
            Message = "Successfully set Ban Sync log channel"
        });
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
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "success",
                Message = "Successfully purged confession messages"
            });
        }
        catch (Exception ex)
        {
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to purge confession messages: {ex.Message}"
            });
        }
    }
}