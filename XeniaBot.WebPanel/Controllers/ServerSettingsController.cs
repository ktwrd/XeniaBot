using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.WebPanel.Helpers;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    [HttpPost("~/Server/{id}/Settings/Counting")]
    public async Task<IActionResult> SaveSettings_Counting(ulong id, string? inputChannelId)
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
            channelId = ulong.Parse(inputChannelId ?? "0");
            if (channelId == null)
                throw new Exception("ChannelId is null");
        }
        catch (Exception e)
        {
            Log.Error(e);
            return await Index(id,
                messageType: "danger",
                message: $"Failed to save Counting settings. {e.Message}");
        }

        var controller = Program.Services.GetRequiredService<CounterConfigController>();
        var counterData = await controller.Get(guild) ?? new CounterGuildModel()
        {
            GuildId = guild.Id,
            ChannelId = (ulong)channelId
        };
        counterData.ChannelId = (ulong)channelId;
        await controller.Set(counterData);

        return await Index(id,
            messageType: "success",
            message: $"Counting settings saved");
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
            return await Index(id,
                messageType: "danger",
                message: $"Failed to save Logging settings. {e.Message}");
        }
        
        return await Index(id,
            messageType: "success",
            message: $"Logging settings saved");
    }

    [HttpPost("~/Server/{id}/Settings/Xp")]
    public async Task<IActionResult> SaveSettings_Xp(ulong id, string? channelId, bool show, bool enable)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        ulong? targetChannelId = null;
        try
        {
            if (channelId == null)
                targetChannelId = null;
            else
                targetChannelId = ulong.Parse(channelId);
        }
        catch (Exception e)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Failed to save Level System settings. {e.Message}");
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
            return await Index(id,
                messageType: "success",
                message: $"Level System Settings Saved");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save level system config\n{e}");
            return await Index(id,
                messageType: "danger",
                message: $"Failed to save Level System settings. {e.Message}");
        }
    }

    [HttpPost("~/Server/{id}/Settings/Greeter")]
    public async Task<IActionResult> SaveSettings_Greeter(
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
        if (!CanAccess(id))
            return View("NotAuthorized");

        
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
            catch (Exception e)
            {
                return await Index(id,
                    messageType: "danger",
                    message: $"Failed to save Greeter settings. {e.Message}");
            }
        }
        
        try
        {
            var controller = Program.Services.GetRequiredService<GuildGreeterConfigController>();
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
        
            return await Index(id,
                messageType: "success",
                message: $"Greeter settings saved");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save greeter settings\n{e}");
            return await Index(id,
                messageType: "danger",
                message: $"Failed to save Greeter settings. {e.Message}");
        }
    }

    [HttpPost("~/Server/{id}/Settings/GreeterBye")]
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
        if (!CanAccess(id))
            return View("NotAuthorized");

        
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
            catch (Exception e)
            {
                return await Index(id,
                    messageType: "danger",
                    message: $"Failed to save Greeter Goodbye settings. {e.Message}");
            }
        }
        
        try
        {
            var controller = Program.Services.GetRequiredService<GuildGreetByeConfigController>();
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
        
            return await Index(id,
                messageType: "success",
                message: $"Greeter Goodbye settings saved");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save greeter goodbye settings\n{e}");
            return await Index(id,
                messageType: "danger",
                message: $"Failed to save Greeter Goodbye settings. {e.Message}");
        }
    }
}