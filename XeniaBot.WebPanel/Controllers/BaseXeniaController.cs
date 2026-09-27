using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.MongoData.Repositories;
using XeniaBot.MongoData.Models;
using XeniaBot.Shared.Helpers;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public class BaseXeniaController : Controller
{
    protected readonly IServiceProvider Services;
    protected readonly DiscordShardedClient _discord;
    protected readonly UserConfigRepository _userConfig;
    private readonly XeniaWebHelper _wh;
    
    public BaseXeniaController(IServiceProvider services)
    {
        Services = services;
        _discord = services.GetRequiredService<DiscordShardedClient>();
        _userConfig = services.GetRequiredService<UserConfigRepository>();
        _wh = services.GetRequiredService<XeniaWebHelper>();
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
        var guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
        var guildUser = ExceptionHelper.RetryOnTimedOut(() => guild?.GetUser(userId.Value));
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
        var isAuth = User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth || !userId.HasValue)
        {
            result = View("NotAuthorized", new NotAuthorizedViewModel()
            {
                ShowLoginButton = true
            });
            return false;
        }
        var user = ExceptionHelper.RetryOnTimedOut(() => _discord.GetUser(userId.Value));
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
        var user = ExceptionHelper.RetryOnTimedOut(() => _discord.GetUser(userId));
        if (user == null)
        {
            result = View("NotAuthorized");
            return false;
        }

        var guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
        var guildUser = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(user.Id));
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
        if (User?.Identity?.IsAuthenticated != true)
            return false;
        var userId = AspHelper.GetUserId(HttpContext);
        if (!userId.HasValue)
        {
            return false;
        }

        return _wh.CanAccessGuild(guildId, userId.Value);
    }
    public bool CanAccessGuild(ulong guildId, ulong userId)
    {
        return _wh.CanAccessGuild(guildId, userId);
    }

    public class ParseChannelIdResult
    {
        public string? ErrorContent { get; set; }
        public ulong ChannelId { get; set; }
    }

    public class ParseIdResult<T>
    {
        public string? ErrorContent { get; set; }
        public T Value { get; set; }
    }
    
    public bool ParseChannelId(string? inputChannel, out ParseChannelIdResult result)
    {
        ulong? channelId;
        try
        {
            if (inputChannel == null)
                throw new Exception("Input value not provided");
            channelId = ulong.Parse(inputChannel);
            if (!channelId.HasValue)
                throw new Exception("Failed to cast as ulong");
        }
        catch (Exception e)
        {
            result = new ParseChannelIdResult()
            {
                ErrorContent = e.Message
            };
            return false;
        }
        result = new ParseChannelIdResult()
        {
            ChannelId = channelId.Value
        };
        return true;
    }

    public bool ParseUlong(string? inputNumber, out ParseIdResult<ulong> result)
    {
        ulong? id;
        try
        {
            if (inputNumber == null)
                throw new Exception("Input value is null");

            id = ulong.Parse(inputNumber);
            if (!id.HasValue)
                throw new Exception("Failed to cast as ulong");
        }
        catch (Exception ex)
        {
            result = new ParseIdResult<ulong>()
            {
                ErrorContent = ex.Message
            };
            return false;
        }

        result = new ParseIdResult<ulong>()
        {
            Value = id.Value
        };
        return true;
    }
}