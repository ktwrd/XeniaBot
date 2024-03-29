using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Services.BotAdditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Core.Helpers;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Services;
using XeniaBot.Shared.Services;

namespace XeniaBot.Core.Modules
{
    [Discord.Interactions.Group("config", "Configure Xenia")]
    public class ConfigModule : InteractionModuleBase
    {
        private CoreContext _core => CoreContext.Instance!;
        [SlashCommand("ticket", "Configure the Ticket Module. To view the current config, run with no options.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task TicketConfig(
            [Discord.Interactions.Summary(description: "Parent Category for all channels that will be created when a ticket is made.")]
            [ChannelTypes(ChannelType.Category)] ICategoryChannel ticketCategory = null,
            [Discord.Interactions.Summary(description: "Role for who will be able to manage tickets.")]
            IRole managerRole = null,
            [Discord.Interactions.Summary(description: "Channel where ticket states will be logged and archived.")]
            [ChannelTypes(ChannelType.Text)] ITextChannel logChannel = null)
        {
            TicketService service = Program.Core.GetRequiredService<TicketService>();

            ConfigGuildTicketModel? model = await service.GetGuildConfig(Context.Guild.Id);
            if (model == null)
            {
                model = new ConfigGuildTicketModel()
                {
                    GuildId = Context.Guild.Id
                };
            }

            bool updateCategory = ticketCategory != null;
            bool updateRole = managerRole != null;
            bool updateChannel = logChannel != null;
            model.CategoryId = ticketCategory?.Id ?? model.CategoryId;
            model.RoleId = managerRole?.Id ?? model.RoleId;
            model.LogChannelId = logChannel?.Id ?? model.LogChannelId;

            var content = new List<string>();
            if (updateCategory)
                content.Add($"Ticket Category to <#{ticketCategory.Id}>");
            if (updateRole)
                content.Add($"Manager Role to <@&{managerRole.Id}>");
            if (updateChannel)
                content.Add($"Logging Channel to <#{logChannel.Id}>");

            var embed = new EmbedBuilder()
            {
                Title = "Ticket Config",
                Color = new Color(255, 255, 255)
            };

            embed.AddField("Current Config", string.Join("\n", new string[]
            {
                    $"Channel Category: <#{model.CategoryId}> (`{model.CategoryId}`)",
                    $"Manager Role: <@&{model.RoleId}> (`{model.RoleId}`)",
                    $"Log: <#{model.LogChannelId}> (`{model.LogChannelId}`)"
            }));
            if (content.Count > 0)
            {
                embed.Description = "Updated " + string.Join(" and ", content);
            }
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);

            await service.SetGuildConfig(model);
        }

        #region Warn Strike
        [SlashCommand("strike-enable", "Enable Warn Strikes")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task WarnStrikeConfigEnable()
        {
            await DeferAsync();
            var embed = DiscordHelper.BaseEmbed().WithTitle("Warn Strikes - Config");
            try
            {
                var strikeService = _core.GetRequiredService<WarnStrikeService>();
                var configRepo = _core.GetRequiredService<GuildConfigWarnStrikeRepository>();
                var data = await strikeService.GetStrikeConfig(Context.Guild.Id);
                data.EnableStrikeSystem = true;
                await configRepo.InsertOrUpdate(data);
                embed.WithDescription("Enabled Warn Strikes.").WithColor(Color.Blue);
                await FollowupAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                embed.WithDescription(string.Join("\n", new string[]
                {
                    "Failed to enable the Warn Strike System",
                    "```",
                    ex.Message,
                    "```"
                }));
                embed.WithColor(Color.Red);
                await FollowupAsync(
                    embed: embed.Build());
                await DiscordHelper.ReportError(ex, Context);
                return;
            }
        }

        [SlashCommand("strike-disable", "Enable Warn Strikes")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task WarnStrikeConfigDisable()
        {
            await DeferAsync();
            var embed = DiscordHelper.BaseEmbed().WithTitle("Warn Strikes - Config");
            try
            {
                var strikeService = _core.GetRequiredService<WarnStrikeService>();
                var configRepo = _core.GetRequiredService<GuildConfigWarnStrikeRepository>();
                var data = await strikeService.GetStrikeConfig(Context.Guild.Id);
                data.EnableStrikeSystem = false;
                await configRepo.InsertOrUpdate(data);
                embed.WithDescription("Disabled Warn Strikes.").WithColor(Color.Blue);
                await FollowupAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                embed.WithDescription(string.Join("\n", new string[]
                {
                    "Failed to disable the Warn Strike System",
                    "```",
                    ex.Message,
                    "```"
                }));
                embed.WithColor(Color.Red);
                await FollowupAsync(
                    embed: embed.Build());
                await DiscordHelper.ReportError(ex, Context);
                return;
            }
        }

        [SlashCommand("strike-window", "Set maximum age for a Warn to be Active")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task WarnStrikeConfigSetWindow(
            [Summary(description: "Maximum age for a Warn to count as an Active Strike")]
            double days)
        {
            await DeferAsync();
            var embed = DiscordHelper.BaseEmbed().WithTitle("Warn Strikes - Config");
            try
            {
                if (days < 1)
                {
                    embed.WithDescription($"Days must be a positive number!")
                        .WithColor(Color.Red);
                    await FollowupAsync(embed: embed.Build());
                    return;
                }
                var strikeService = _core.GetRequiredService<WarnStrikeService>();
                var configRepo = _core.GetRequiredService<GuildConfigWarnStrikeRepository>();
                var data = await strikeService.GetStrikeConfig(Context.Guild.Id);
                data.StrikeWindow = days;
                await configRepo.InsertOrUpdate(data);
                var years = Math.Floor(days / 365f);
                var daysFormatted = days % 365;
                var description = "Set Strike Window to ";
                if (years > 0)
                    description += $"{years} year";
                description += years > 1 ? "s " : "";

                if (daysFormatted > 0)
                    description += $"{daysFormatted} day";
                if (daysFormatted > 1)
                    description += "s";

                if (!data.EnableStrikeSystem)
                {
                    description += "\n**NOTE:** Strike System is currently disabled";
                }
                
                embed.WithDescription(description).WithColor(Color.Blue);
                await FollowupAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                embed.WithDescription(string.Join("\n", new string[]
                {
                    "Failed to set the Strike Window",
                    "```",
                    ex.Message,
                    "```"
                }));
                embed.WithColor(Color.Red);
                await FollowupAsync(
                    embed: embed.Build());
                await DiscordHelper.ReportError(ex, Context);
                return;
            }
        }

        [SlashCommand("strike-limit", "Set the amount Warns before action should be taken.")]
        public async Task WarnStrikeConfigSetLimit(
            [Summary(description: "Maximum amount of warns before action should be taken")]
            int limit)
        {
            await DeferAsync();
            var embed = DiscordHelper.BaseEmbed().WithTitle("Warn Strikes - Config");
            try
            {
                var strikeService = _core.GetRequiredService<WarnStrikeService>();
                var configRepo = _core.GetRequiredService<GuildConfigWarnStrikeRepository>();
                var data = await strikeService.GetStrikeConfig(Context.Guild.Id);
                data.MaxStrike = limit;
                await configRepo.InsertOrUpdate(data);

                embed.WithDescription($"Set Warn Limit to {limit}");

                if (!data.EnableStrikeSystem)
                {
                    embed.Description += "\n**NOTE:** Strike System is currently disabled";
                }
                
                embed.WithColor(Color.Blue);
                await FollowupAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                embed.WithDescription(string.Join("\n", new string[]
                {
                    "Failed to set Warn Limit",
                    "```",
                    ex.Message,
                    "```"
                }));
                embed.WithColor(Color.Red);
                await FollowupAsync(
                    embed: embed.Build());
                await DiscordHelper.ReportError(ex, Context);
                return;
            }
        }
        #endregion
    }
}
