using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Data.Controllers.BotAdditions;

[BotController]
public class RoleMenuManagerController : BaseController
{
    private readonly DiscordSocketClient _discord;
    private readonly RoleMenuConfigController _menuConfigController;
    private readonly RoleMenuSelectConfigController _selectConfigController;
    private readonly RoleMenuOptionConfigController _optionConfigController;

    public RoleMenuManagerController(IServiceProvider services)
        : base(services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _menuConfigController = services.GetRequiredService<RoleMenuConfigController>();
        _selectConfigController = services.GetRequiredService<RoleMenuSelectConfigController>();
        _optionConfigController = services.GetRequiredService<RoleMenuOptionConfigController>();
    }

    public async Task<ComponentBuilder> GenerateComponentBuilder(string roleMenuId)
    {
        var selects = await _selectConfigController.GetLatestInMenu(roleMenuId);
        var row = new ActionRowBuilder();
        foreach (var sel in selects)
        {
            var selectBuilder = new SelectMenuBuilder()
                .WithPlaceholder(sel.Placeholder)
                .WithType(ComponentType.SelectMenu)
                .WithCustomId(sel.RoleSelectId);
            var selectOptions = await _optionConfigController.GetLatestInSelect(sel.RoleSelectId);
            foreach (var opt in selectOptions)
            {
                var optionBuilder = new SelectMenuOptionBuilder()
                    .WithLabel(opt.OptionName)
                    .WithValue(opt.RoleOptionId)
                    .WithDescription(opt.OptionDescription);
                if (opt.OptionEmoji.Length > 0)
                {
                    optionBuilder.WithEmote(new Emoji(opt.OptionEmoji));
                }

                selectBuilder.AddOption(optionBuilder);
            }

            row.AddComponent(selectBuilder.Build());
        }
        var compBuilder = new ComponentBuilder();
        compBuilder.AddRow(row);
        return compBuilder;
    }

    /// <summary>
    /// Generate a role menu selection message.
    /// Will not be tailored per-user and generating a new one will render all previous messages (for the same role menu) as broken.
    /// </summary>
    /// <param name="guildId">GuildId this RoleMenu belongs to</param>
    /// <param name="channel">Channel that this gets posted into.</param>
    /// <param name="model">Model to generate the role menu off.</param>
    public async Task Generate(ulong guildId, SocketTextChannel channel, RoleMenuConfigModel model)
    {
        var guild = _discord.GetGuild(model.GuildId);
        var oldChannel = guild.GetTextChannel(model.MessageChannelId);
        if (oldChannel != null)
        {
            try
            {
                await oldChannel.DeleteMessageAsync(model.MessageId);
            }
            catch
            {
                // ignored
            }
        }
        
        var comps = await GenerateComponentBuilder(model.RoleMenuId);
        var embed = model.GetEmbed();

        var msg = await channel.SendMessageAsync(embed: embed.Build(), components: comps.Build());

        model.MessageId = msg.Id;
        model.MessageChannelId = msg.Channel.Id;
        model.GuildId = guildId;
        await _menuConfigController.Add(model);
    }
}