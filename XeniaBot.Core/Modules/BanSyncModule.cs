using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Services.BotAdditions;
using XeniaBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Services;
using XeniaBot.Shared.Services;

namespace XeniaBot.Core.Modules
{
    [Group("bansync", "Sync Bans between servers")]
    public class BanSyncModule : InteractionModuleBase
    {
        [SlashCommand("refresh", "Refresh bans in this guild")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Refresh()
        {
            await DeferAsync();
            try
            {
                var controller = Program.Core.GetRequiredService<BanSyncService>();
                await controller.RefreshBans(Context.Guild.Id);
                
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle($"BanSync - Refresh")
                        .WithDescription($"Bans were refreshed successfully.")
                        .WithColor(Color.Red)
                        .WithCurrentTimestamp()
                        .Build());
            }
            catch (Exception ex)
            {
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle($"BanSync - Action Failed")
                        .WithDescription($"Failed to refresh bans in this guild. `{ex.Message}`")
                        .WithColor(Color.Red)
                        .WithCurrentTimestamp()
                        .Build());
                await Program.Core.GetRequiredService<ErrorReportService>().ReportError(ex, Context);
            }
        }
        
        [SlashCommand("userinfo", "Get ban sync details about user")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task UserDetails(IUser user)
        {
            var controller = Program.Core.GetRequiredService<BanSyncService>();
            var infoController = Program.Core.GetRequiredService<BanSyncInfoRepository>();
            var data = await infoController.GetInfoEnumerable(user.Id);

            if (!data.Any())
            {
                await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                {
                    Description = $"No bans found for <@{user.Id}> ({user.Id})",
                    Color = Color.Orange
                }.Build());
                return;
            }

            var embed = await controller.GenerateEmbed(data);
            await Context.Interaction.RespondAsync(embed: embed.Build());
        }

        [SlashCommand("setchannel", "Set the log channel where ban notifications get sent.")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task SetChannel(
            [Discord.Interactions.Summary(description: "Channel where BanSync notifications will be sent to.")]
            [ChannelTypes(ChannelType.Text)]
            ITextChannel logChannel)
        {
            try
            {
                var controller = Program.Core.GetRequiredService<BanSyncConfigRepository>();
                var data = await controller.Get(Context.Guild.Id);
                if (data == null)
                {
                    data = new ConfigBanSyncModel()
                    {
                        GuildId = Context.Guild.Id,
                        LogChannel = logChannel.Id,
                        Enable = false
                    };
                }

                data.LogChannel = logChannel.Id;
                await controller.Set(data);
            }
            catch (Exception ex)
            {
                await Context.Interaction.RespondAsync($"Failed to set log channel\n```\n{ex.Message}\n```");
                await DiscordHelper.ReportError(ex, Context);
                return;
            }
            await Context.Interaction.RespondAsync($"Updated Log Channel to <#{logChannel.Id}>");
        }
        [SlashCommand("setguildstate", "Set state field of guild")]
        public async Task SetGuildState(string guild, BanSyncGuildState state, string reason = "")
        {
            if (!Program.Core.Config.Data.UserWhitelist.Contains(Context.User.Id))
            {
                await Context.Interaction.RespondAsync($"Invalid permissions.");
                return;
            }
            ulong guildId = 0;
            try
            {
                guildId = ulong.Parse(guild);
            }
            catch (Exception ex)
            {
                await Context.Interaction.RespondAsync($"Failed to parse guildId\n```\n{ex.Message}\n```", ephemeral: true);
                return;
            }
            var targetGuild = await Context.Client.GetGuildAsync(guildId);
            if (targetGuild == null)
            {
                await Context.Interaction.RespondAsync($"Guild `{guildId}` not found (GetGuildAsync responded with null)", ephemeral: true);
                return;
            }

            try
            {
                var controller = Program.Core.GetRequiredService<BanSyncService>();
                await controller.SetGuildState(guildId, state, reason);
            }
            catch (Exception ex)
            {
                await Context.Interaction.RespondAsync($"Failed to set guild state\n```\n{ex.Message}\n```", ephemeral: true);
                await DiscordHelper.ReportError(ex, Context);
                return;
            }
            await Context.Interaction.RespondAsync($"Set state of `{targetGuild.Name}` to `{state}`", ephemeral: true);
        }

        [SlashCommand("request", "Request for this guild to have Ban Sync support")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task RequestGuild()
        {
            var controller = Program.Core.GetRequiredService<BanSyncService>();
            var kind = await controller.GetGuildKind(Context.Guild.Id);

            var embed = new EmbedBuilder()
            {
                Title = "Request BanSync Access",
                Color = Color.Red
            }.WithCurrentTimestamp();

            if (kind != BanSyncService.BanSyncGuildKind.Valid)
            {
                embed.Description = "Your server doesn't meet the requirements";
                embed.AddField("Reason", $"`{kind}`", true);
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
            }
            ConfigBanSyncModel? response;
            try
            {
                response = await controller.RequestGuildEnable(Context.Guild.Id);
            }
            catch (Exception ex)
            {
                embed.Description = $"Failed to request guild BanSync support.\n```\n{ex.Message}\n```";
                await Context.Interaction.RespondAsync(embed: embed.Build());
                await DiscordHelper.ReportError(ex, Context);
                return;
            }
            if (response.State == BanSyncGuildState.PendingRequest)
            {
                embed.Color = Color.Green;
                embed.Description = "Your guild is under review for Ban Sync to be enabled.";
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
            }

            embed.Description = $"Failed to request BanSync for this guild.\n`{response.State}`";
            if (response.State == BanSyncGuildState.Blacklisted || response.State == BanSyncGuildState.RequestDenied)
            {
                embed.AddField("Reason", $"```\n{response.Reason}\n```", true);
            }
            await Context.Interaction.RespondAsync(embed: embed.Build());
        }
    }
}
