using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.WebPanel.Helpers;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{

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
                Message = $"Failed to save Counting settings. {e.Message}"
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

    public class SettingLogSystemData
    {
        public ulong? DefaultLogChannel { get; set; }
        public ulong? MemberJoinChannel { get; set; }
        public ulong? MemberLeaveChannel { get; set; }
        public ulong? MemberBanChannel { get; set; }
        public ulong? MemberKickChannel { get; set; }
        public ulong? MessageEditChannel { get; set; }
        public ulong? MessageDeleteChannel { get; set; }
        public ulong? ChannelCreateChannel { get; set; }
        public ulong? ChannelEditChannel { get; set; }
        public ulong? ChannelDeleteChannel { get; set; }
        public ulong? MemberVoiceChangeChannel { get; set; }
    }
    
    [HttpPost("~/Server/{id}/Settings/Log")]
    public async Task<IActionResult> SaveSettings_LogSystem(ulong id, SettingLogSystemData data)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        try
        {
            var controller = Program.Services.GetRequiredService<ServerLogConfigController>();
            var currentConfig = await controller.Get(guild.Id) ?? new ServerLogModel()
            {
                ServerId = guild.Id
            };
            currentConfig.SetChannel(ServerLogEvent.Fallback, data.DefaultLogChannel);
            currentConfig.SetChannel(ServerLogEvent.MemberJoin, data.MemberJoinChannel);
            currentConfig.SetChannel(ServerLogEvent.MemberLeave, data.MemberLeaveChannel);
            currentConfig.SetChannel(ServerLogEvent.MemberBan, data.MemberBanChannel);
            currentConfig.SetChannel(ServerLogEvent.MemberKick, data.MemberKickChannel);
            currentConfig.SetChannel(ServerLogEvent.MessageEdit, data.MessageEditChannel);
            currentConfig.SetChannel(ServerLogEvent.MessageDelete, data.MessageDeleteChannel);
            currentConfig.SetChannel(ServerLogEvent.ChannelCreate, data.ChannelCreateChannel);
            currentConfig.SetChannel(ServerLogEvent.ChannelEdit, data.ChannelEditChannel);
            currentConfig.SetChannel(ServerLogEvent.ChannelDelete, data.ChannelDeleteChannel);
            currentConfig.SetChannel(ServerLogEvent.MemberVoiceChange, data.MemberVoiceChangeChannel);
            await controller.Set(currentConfig);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save logging settings. \n{e}");
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to save logging settings. {e.Message}"
            });
        }
        
        return RedirectToAction("Index", new
        {
            Id = id,
            MessageType = "success",
            Message = $"Logging Settings Saved"
        });
    }

    [HttpPost("~/Server/{id}/Settings/Xp")]
    public async Task<IActionResult> SaveSettings_Xp(ulong id, string channelId, bool show, bool enable)
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
                Message = $"Failed to save Level System settings. {e.Message}"
            });
        }

        try
        {
            var controller = Program.Services.GetRequiredService<LevelSystemGuildConfigController>();
            var data = await controller.Get(guild.Id) ?? new LevelSystemGuildConfigModel()
            {
                GuildId = guild.Id,
                LevelUpChannel = targetChannelId,
                ShowLeveUpMessage = show,
                Enable = enable
            };
            data.LevelUpChannel = targetChannelId;
            data.ShowLeveUpMessage = show;
            data.Enable = enable;
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
            Log.Error($"Failed to save level system config\n{e}");
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to save Level System Config. {e.Message}"
            });
        }
    }
}