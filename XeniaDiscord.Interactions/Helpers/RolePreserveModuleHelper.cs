using Discord;
using Microsoft.EntityFrameworkCore;
using XeniaBot.Shared;
using XeniaDiscord.Data;

namespace XeniaDiscord.Interactions.Helpers;

internal static class RolePreserveModuleHelper
{
    internal const string ViewBlacklistedRolesInteractionName = "rolepreserve_blacklistedroles_list:page=*";
    internal sealed record ListEmbedResult(EmbedBuilder Embed, ComponentBuilderV2? ComponentBuilder);
    internal static async Task<ListEmbedResult> ListEmbed(
        ConfigData config,
        XeniaDbContext db,
        IGuild guild,
        int page = 1)
    {
        const int pageSize = 10;

        page = int.Max(1, page);
        var skip = pageSize * (page - 1);
        var guildIdStr = guild.Id.ToString();

        var totalItemCount = await db.RolePreserveBlacklistedRoles
            .Where(e => e.GuildId == guildIdStr)
            .CountAsync();
        var lastPage = Math.Max(1, Convert.ToInt32(Math.Ceiling(totalItemCount / (float)pageSize)));

        var components = BuildBlacklistedRolesListingComponents(page, lastPage);

        var pageFmt = page.ToString("N0");
        var embed = new EmbedBuilder()
            .WithTitle("Role Preserve - Blacklisted Roles")
            .WithColor(Color.Blue)
            .WithFooter("Page " + pageFmt)
            .WithCurrentTimestamp();

        // only return early if we're past the last page
        if (page > lastPage)
        {
            embed.Description = DescriptionForEmptyPage(config, guild.Id, pageFmt);
            return new ListEmbedResult(embed, components);
        }

        var items = await db.RolePreserveBlacklistedRoles
            .Where(e => e.GuildId == guildIdStr)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        if (items.Count < 1)
        {
            embed.Description = page == 1
                ? "No blacklisted roles have been setup. You can do that with `/rolepreserve blacklist-add`"
                : DescriptionForEmptyPage(config, guild.Id, pageFmt);
        }
        else
        {
            embed.Description = string.Join("\n", items.Select(e => $"<@&{e.RoleId}>"));
        }

        return new ListEmbedResult(embed, components);
    }

    private static string DescriptionForEmptyPage(ConfigData config, ulong guildId, string pageFmt)
    {
        const string emptyPageMessageFmt = "No roles on page `{0}`\nRun `/rolepreserve blacklist` again to see the blacklisted roles.";
        const string emptyPageMessageWithDashFmt = emptyPageMessageFmt + "\n\nYou can also see what roles are blacklisted [via the web panel]({1}/Guild/{2}/RolePreserve/Settings).";
        if (config.HasDashboard)
            return string.Format(emptyPageMessageWithDashFmt,
                pageFmt,
                config.DashboardUrl,
                guildId);
        return string.Format(emptyPageMessageFmt, pageFmt);
    }
    private static ComponentBuilderV2? BuildBlacklistedRolesListingComponents(
        int currentPage,
        int lastPage)
    {
        var paginationRow = new List<ButtonBuilder>()
        {
            new ButtonBuilder()
                .WithCustomId(ViewBlacklistedRolesInteractionName.Replace("*", currentPage.ToString()))
                .WithLabel("Refresh")
                .WithStyle(ButtonStyle.Primary)
        };
        if (currentPage > 2)
        {
            paginationRow.Add(new ButtonBuilder()
                .WithCustomId(ViewBlacklistedRolesInteractionName.Replace("*", "1"))
                .WithLabel("Start")
                .WithStyle(ButtonStyle.Primary));
        }
        if (currentPage > 1)
        {
            paginationRow.Add(new ButtonBuilder()
                .WithCustomId(ViewBlacklistedRolesInteractionName.Replace("*", (currentPage - 1).ToString()))
                .WithLabel("Previous")
                .WithStyle(ButtonStyle.Primary));
        }
        if (currentPage < lastPage)
        {
            paginationRow.Add(new ButtonBuilder()
                .WithCustomId(ViewBlacklistedRolesInteractionName.Replace("*", (currentPage + 1).ToString()))
                .WithLabel("Next")
                .WithStyle(ButtonStyle.Primary));
        }
        if (currentPage < lastPage - 1)
        {
            paginationRow.Add(new ButtonBuilder()
                .WithCustomId(ViewBlacklistedRolesInteractionName.Replace("*", lastPage.ToString()))
                .WithLabel("Last")
                .WithStyle(ButtonStyle.Primary));
        }
        return new ComponentBuilderV2()
            .WithActionRow(paginationRow);
    }
}