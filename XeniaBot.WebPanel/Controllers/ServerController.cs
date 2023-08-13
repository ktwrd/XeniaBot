using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Web;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.WebPanel.Extensions;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController : Controller
{
    private readonly ILogger<ServerController> _logger;
    private readonly DiscordSocketClient _discord;

    public ServerController(ILogger<ServerController> logger)
    {
        _logger = logger;
        _discord = Program.Services.GetRequiredService<DiscordSocketClient>();
    }

    public bool CanAccess(ulong id)
    {
        bool isAuth = User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth)
            return false;
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
        {
            return false;
        }
        var user = _discord.GetUser((ulong)userId);
        if (user == null)
            return false;

        var guild = _discord.GetGuild(id);
        var guildUser = guild.GetUser(user.Id);
        if (guildUser == null)
            return false;
        if (!guildUser.GuildPermissions.ManageGuild)
            return false;

        return true;
    }
    [HttpGet("~/Server/{id}")]
    public async Task<IActionResult> Index(ulong id, string? messageType = null, string? message = null)
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
        var guildUser = guild.GetUser(user.Id);

        var data = await GetDetails(guild.Id);
        data.User = guildUser;
        data.MessageType = messageType;
        data.Message = message;

        if (data.MessageType != null)
        {
            var valid = new string[]
            {
                "primary",
                "secondary",
                "success",
                "danger",
                "warning",
                "info"
            };
            if (!valid.Contains(data.MessageType))
                data.MessageType = "primary";
        }

        if (data.MessageType == null)
            data.MessageType = "primary";
        
        return View("Details", data);
    }

    public class ServerSettingsCountingModel
    {
        [Required]
        [MaxLength(140)]
        public string ChannelId { get; set; }
    }
    [HttpPost("~/Server/{id}/Settings/Counting")]
    public async Task<IActionResult> SaveSettings_Counting(ulong id, ServerSettingsCountingModel data)
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

        ulong? channelId = null;
        try
        {
            channelId = ulong.Parse(data.ChannelId);
            if (channelId == null)
                throw new Exception("ChannelId is null");
        }
        catch (Exception e)
        {
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to parse ChannelId.<br/><pre><code>{e.Message}</code></pre>"
            });
        }

        var controller = Program.Services.GetRequiredService<CounterConfigController>();
        var counterData = await controller.Get(guild) ?? new CounterGuildModel()
        {
            GuildId = guild.Id,
            ChannelId = (ulong)channelId
        };
        counterData.ChannelId = (ulong)channelId;
        await controller.Set(counterData);

        return RedirectToAction("Index", new
        {
            Id = id,
            MessageType = "success",
            Message = "Successfully set counting settings"
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

        ulong? channelId = null;
        try
        {
            channelId = ulong.Parse(logChannel);
            if (channelId == null)
                throw new Exception("ChannelId is null");
        }
        catch (Exception e)
        {
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to parse ChannelId.<br/><pre><code>{e.Message}</code></pre>"
            });
        }
        
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

    [HttpPost("~/Server/{id}/Settings/Xp")]
    public async Task<IActionResult> SaveSettings_Xp(ulong id, string channelId, bool show)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        ulong? targetChannelId = null;
        try
        {
            targetChannelId = ulong.Parse(channelId);
            if (targetChannelId == null)
                throw new Exception("ChannelId is null");
        }
        catch (Exception e)
        {
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to parse ChannelId.<br/><pre><code>{e.Message}</code></pre>"
            });
        }

        try
        {
            var controller = Program.Services.GetRequiredService<LevelSystemGuildConfigController>();
            var data = await controller.Get(guild.Id) ?? new LevelSystemGuildConfigModel()
            {
                GuildId = guild.Id,
                LevelUpChannel = targetChannelId,
                ShowLeveUpMessage = show
            };
            data.LevelUpChannel = targetChannelId;
            data.ShowLeveUpMessage = show;
            await controller.Set(data);
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "success",
                Message = $"Level System Settings Saved"
            });
        }
        catch (Exception e)
        {
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to save Level System Config. <br/><pre><code>{e.Message}</code></pre>"
            });
        }
    }

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
    
    [HttpGet("~/Server/")]
    [HttpGet("~/Server/List")]
    public async Task<IActionResult> List()
    {
        bool isAuth = User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth)
            return View("NotAuthorized");

        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
        {
            return View("NotFound", "User could not be found.");
        }
        var user = _discord.GetUser((ulong)userId);
        var data = new ServerListViewModel()
        {
            UserId = (ulong)userId,
            UserAvatar = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl()
        };
        var dataItems = new List<ServerListViewModelItem>();
        foreach (var item in _discord.Guilds)
        {
            var guildUser = item.GetUser((ulong)userId);
            if (guildUser == null)
                continue;
            if (!guildUser.GuildPermissions.ManageGuild)
                continue;
            dataItems.Add(new ServerListViewModelItem()
            {
                Guild = item,
                GuildUser = guildUser
            });
        }

        data.Items = dataItems.ToArray();
        return View("List", data);
    }
}