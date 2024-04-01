using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Models;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public class BaseXeniaController : Controller
{
    protected readonly DiscordSocketClient _discord;
    protected readonly UserConfigRepository _userConfig;
    
    public BaseXeniaController()
        : base()
    {
        _discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        _userConfig = Program.Core.GetRequiredService<UserConfigRepository>();
    }


    /// <inheritdoc cref="IsLoggedIn(out IActionResult)"/>
    public virtual bool CanAccess(out IActionResult? result)
    {
        return IsLoggedIn(out result);
    }

    /// <summary>
    /// Can the current logged in user access the guild provided?
    /// </summary>
    /// <param name="guildId">Guild Id</param>
    /// <param name="result">View to Show. Will be not null when result is `false`</param>
    /// <returns>Can the current logged in user access the guild provided?</returns>
    public virtual bool CanAccess(ulong guildId, out IActionResult? result)
    {
        if (!CanAccess(out result))
            return false;
        
        var userId = AspHelper.GetUserId(HttpContext)!;
        var guild = _discord.GetGuild(guildId);
        var guildUser = guild.GetUser((ulong)userId!);
        if (guildUser == null)
        {
            result = View("NotAuthorized");
            return false;
        }
        
        if (!guildUser.GuildPermissions.ManageGuild)
        {
            result = View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = $"Missing permission \"Manage Server\""
            });
            return false;
        }

        return CanAccess(guildId, (ulong)userId, out result);
    }
    
    /// <inheritdoc cref="CanAccess(ulong, out IActionResult)"/>
    public virtual bool CanAccess(ulong guildId)
    {
        return CanAccess(guildId, out var _);
    }

    /// <summary>
    /// Is the current user logged in?
    /// </summary>
    /// <param name="result">View to Show. Will be not null when result is `false`</param>
    /// <returns>Is the user logged in?</returns>
    public virtual bool IsLoggedIn(out IActionResult? result)
    {
        return IsLoggedIn(AspHelper.GetUserId(HttpContext), out result);
    }
    /// <summary>
    /// Check if a provided User Id is logged in
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public virtual bool IsLoggedIn(ulong? userId, out IActionResult? result)
    {
        bool isAuth = User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth || userId == null)
        {
            result = View("NotAuthorized", new NotAuthorizedViewModel()
            {
                ShowLoginButton = true
            });
            return false;
        }
        var user = _discord.GetUser((ulong)userId!);
        if (user == null)
        {
            result = View("NotAuthorized");
            return false;
        }

        result = null;
        return true;
    }
    
    /// <summary>
    /// Can a User access a Guild
    /// </summary>
    /// <param name="guildId">Guild Id to check</param>
    /// <param name="userId">User Id to check</param>
    /// <param name="result">Result View. Will only be null when `true` is returned.</param>
    /// <returns>Can access</returns>
    public virtual bool CanAccess(ulong guildId, ulong userId, out IActionResult? result)
    {
        var user = _discord.GetUser(userId);
        if (user == null)
        {
            result = View("NotAuthorized");
            return false;
        }

        var guild = _discord.GetGuild(guildId);
        var guildUser = guild.GetUser(user.Id);
        if (guildUser == null)
        {
            result = View("NotAuthorized");
            return false;
        }

        if (!guildUser.GuildPermissions.ManageGuild)
        {
            result = View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = $"Missing permission \"Manage Server\""
            });
            return false;
        }
        result = null;

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