using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Modules
{
    [Group("bansync", "Sync Bans between servers")]
    public class BanSyncModule : InteractionModuleBase
    {
        [SlashCommand("userinfo", "Get ban sync details about user")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task UserDetails(IUser user)
        {
            var controller = Program.Services.GetRequiredService<BanSyncController>();
            var data = await controller.GetInfoEnumerable(user.Id);

            if (data.Count() < 1)
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
        public async Task SetChannel([ChannelTypes(ChannelType.Text)] ITextChannel logChannel)
        {
            var controller = Program.Services.GetRequiredService<BanSyncConfigController>();
            var data = await controller.Get(Context.Guild.Id);
            if (data == null)
            {
                data = new Models.ConfigBanSyncModel()
                {
                    GuildId = Context.Guild.Id,
                    LogChannel = logChannel.Id,
                    Enable = false
                };
            }

            data.LogChannel = logChannel.Id;
            await controller.Set(data);
            await Context.Interaction.RespondAsync($"Updated Log Channel to <#{logChannel.Id}>");
        }

        [SlashCommand("enableguild", "Enable a guilds ability to use the Ban Sync module")]
        public async Task EnableGuild(string guild)
        {
            if (!Program.Config.UserWhitelist.Contains(Context.User.Id))
                return;
            ulong guildId = 0;
            try
            {
                guildId = ulong.Parse(guild);
            }
            catch (Exception ex)
            {
                await Context.Interaction.RespondAsync($"Failed to parse guildId\n```\n{ex.Message}\n```");
                return;
            }
            var targetGuild = await Context.Client.GetGuildAsync(guildId);
            if (targetGuild == null)
            {
                await Context.Interaction.RespondAsync($"Guild `{guildId}` not found (GetGuildAsync responded with null)");
                return;
            }

            var controller = Program.Services.GetRequiredService<BanSyncConfigController>();
            var data = await controller.Get(guildId);
            var notes = new List<string>();
            if (data == null)
            {
                data = new Models.ConfigBanSyncModel()
                {
                    GuildId = guildId
                };
                notes.Add("Created new database entry");
            }
            
            data.Enable = true;
            await controller.Set(data);

            var responseContent = $"Enabled guild `{guildId}`";
            if (notes.Count > 0)
            {
                responseContent += "\n***Notes***\n";
                responseContent += string.Join(
                    "\n",
                    notes.Select(v => $"`{v}`"));
            }
            await Context.Interaction.RespondAsync(responseContent);
        }

        [SlashCommand("request", "Request for this guild to have Ban Sync support")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task RequestGuild()
        {
            var controller = Program.Services.GetRequiredService<BanSyncController>();
            var kind = controller.GetGuildKind(Context.Guild.Id);
            if (kind != BanSyncController.BanSyncGuildKind.Valid)
            {
                await Context.Interaction.RespondAsync($"Your server does not meet the requirements.\nReason: `{kind}`", ephemeral: true);
                return;
            }

            await controller.RequestGuildEnable(Context.Guild.Id);
            await Context.Interaction.RespondAsync($"Your guild is under review for Ban Sync to be enabled.");
        }
    }
}
