using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers;
using XeniaBot.Data.Models;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public class BaseXeniaController : Controller
{
    protected readonly DiscordSocketClient _discord;
    protected readonly UserConfigController _userConfig;
    
    public BaseXeniaController()
        : base()
    {
        _discord = Program.Services.GetRequiredService<DiscordSocketClient>();
        _userConfig = Program.Services.GetRequiredService<UserConfigController>();
    }

    public virtual bool CanAccess(ulong guildId)
    {
        bool isAuth = User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth)
            return false;
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
        {
            return false;
        }

        return CanAccess(guildId, (ulong)userId);
    }
    public virtual bool CanAccess(ulong guildId, ulong userId)
    {
        var user = _discord.GetUser(userId);
        if (user == null)
            return false;

        var guild = _discord.GetGuild(guildId);
        var guildUser = guild.GetUser(user.Id);
        if (guildUser == null)
            return false;
        if (!guildUser.GuildPermissions.ManageGuild)
            return false;

        return true;
    }


    public async Task PopulateModel<T>(T model) where T : BaseViewModel
    {
        model.Client = _discord;
        var userConfig = await _userConfig.Get(GetCurrentUserId());
        userConfig ??= new UserConfigModel();
        model.UserConfig = userConfig;
        
        if (Request.Query.TryGetValue("Message", out var alertMessage))
            model.Message = alertMessage.ToString();
        if (Request.Query.TryGetValue("MessageType", out var alertType))
            if (AspHelper.ValidMessageTypes.Contains(alertType.ToString()))
                model.MessageType = alertType.ToString();
    }

    public async Task<BaseViewModel> PopulateModel()
    {
        var instance = new BaseViewModel();
        await PopulateModel(instance);
        return instance;
    }

    public ulong? GetCurrentUserId()
    {
        return AspHelper.GetUserId(HttpContext);
    }

    public bool CanAccessGuild(ulong guildId)
    {
        bool isAuth = User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth)
            return false;
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
        {
            return false;
        }

        return AspHelper.CanAccessGuild(guildId, (ulong)userId);
    }
    public bool CanAccessGuild(ulong guildId, ulong userId)
    {
        return AspHelper.CanAccessGuild(guildId, userId);
    }

    public class ParseChannelIdResult
    {
        public string? ErrorContent { get; set; }
        public ulong? ChannelId { get; set; }
    }
    
    public ParseChannelIdResult ParseChannelId(string? inputChannel)
    {
        ulong? channelId = null;
        try
        {
            if (inputChannel == null)
                throw new Exception("Input value not provided");
            channelId = ulong.Parse(inputChannel);
            if (channelId == null)
                throw new Exception("Failed to cast as ulong");
        }
        catch (Exception e)
        {
            return new ParseChannelIdResult()
            {
                ErrorContent = e.Message
            };
        }
        return new ParseChannelIdResult()
        {
            ChannelId = (ulong)channelId
        };
    }
}