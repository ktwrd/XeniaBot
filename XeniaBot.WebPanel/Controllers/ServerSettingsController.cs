using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
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
}