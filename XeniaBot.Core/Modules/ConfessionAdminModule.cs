using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Helpers;
using XeniaBot.Core.Services.BotAdditions;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared;

namespace XeniaBot.Core.Modules;

[Group("confessadmin", "Administrative tools for the Confession Module")]
[CommandContextType(InteractionContextType.Guild)]
public class ConfessionAdminModule : InteractionModuleBase
{
    private readonly ConfessionService _service;
    private readonly ConfessionConfigRepository _repo;

    public ConfessionAdminModule(IServiceProvider services)
    {
        _service = services.GetRequiredService<ConfessionService>();
        _repo = services.GetRequiredService<ConfessionConfigRepository>();
    }

    [SlashCommand("purge", "Remove all traces of the Confession Module from this guild")]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RegisterDBLCommand]
    public async Task Purge()
    {
        var item = await _repo.GetGuild(Context.Guild.Id);
        if (item == null)
        {
            await Context.Interaction.RespondAsync("Guild not registered in database");
            return;
        }

        try
        {
            await _repo.Delete(item);
        }
        catch (Exception ex)
        {
            await Context.Interaction.RespondAsync($"Failed to delete from database\n```\n{ex.Message}\n```");
            await DiscordHelper.ReportError(ex, Context);
            return;
        }
        await Context.Interaction.RespondAsync("Deleted Guild from Database");
    }

    [SlashCommand("set", "Setup the Confession Module")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    [RegisterDBLCommand]
    public async Task Set(
        [Summary(name: "output-channel", description: "Channel where confessions will be sent to")]
        [ChannelTypes(ChannelType.Text)]
        IChannel confessionOutputChannel,
        [ChannelTypes(ChannelType.Text)]
        [Summary(name: "modal-channel", description: "Channel where users will interact with a modal to create a confession.")]
        IChannel confessionModalChannel)
    {
        if (!await DiscordHelper.HasGuildPermission(Context, GuildPermission.ManageChannels, true))
            return;

        if (confessionOutputChannel.Id == confessionModalChannel.Id)
        {
            await Context.Interaction.RespondAsync("Output Channel and Modal Channel cannot be the same", ephemeral: true);
            return;
        }

        var item = await _repo.GetGuild(Context.Guild.Id);
        if (item != null)
        {
            await _repo.Delete(item);
        }
        await _service.InitializeModal(
            Context.Guild.Id,
            confessionOutputChannel.Id,
            confessionModalChannel.Id);
        await Context.Interaction.RespondAsync($"Done! See <#{confessionOutputChannel.Id}> to see where the confessions get sent to, and use the button in <#{confessionModalChannel.Id}> to add a confession.");
    }
}
