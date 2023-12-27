using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{

    [HttpPost("~/Server/{id}/Settings/Xp/Save")]
    public async Task<IActionResult> SaveSettings_Xp(ulong id, string? channelId, bool show, bool enable)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        ulong? targetChannelId;
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

}