using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Helpers;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    [HttpPost("~/Server/{id}/Settings/Counting")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> SaveSettings_Counting(ulong id, string? inputChannelId)
    {
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
            
            var controller = Program.Core.GetRequiredService<CounterConfigRepository>();
            var counterData = await controller.Get(guild) ?? new CounterGuildModel()
            {
                GuildId = guild.Id,
                ChannelId = (ulong)channelId
            };
            counterData.ChannelId = (ulong)channelId;
            await controller.Set(counterData);

            return await CountingView(id,
                messageType: "success",
                message: $"Saved settings.");
        }
        catch (Exception ex)
        {
            Program.Core.GetRequiredService<ErrorReportService>()
                .ReportException(ex, $"Failed to save counter settings");
            Log.Error(ex);
            return await CountingView(id,
                messageType: "danger",
                message: $"Failed to save Counting settings. {ex.Message}");
        }
    }
}