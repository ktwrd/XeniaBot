using Discord;
using XeniaBot.Shared.Services;

namespace XeniaDiscord.Common.Services;

partial class GuildApprovalService
{
    public async Task<EmbedBuilder> SetApprovedRoleEmbed(
        IGuild guild,
        IRole role,
        IInteractionContext? context = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Approval - Set Role")
            .WithColor(Color.Blue);
        try
        {
            var result = await SetApprovedRole(guild, role, context?.User);
            
            embed.Color = result.IsSuccess ? Color.Green : Color.Red;
            embed.WithDescription(result.FormatForEmbed());
            return embed;
        }
        catch (Exception ex)
        {
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithContext(context)
                .WithGuild(guild));
            return embed
                .WithDescription(string.Join("\n", $"Failed to set \"approved\" role to: {role.Mention}", $"`{errorType}`"))
                .WithColor(Color.Red);
        }
    }

    public async Task<EmbedBuilder> SetGreeterChannelEmbed(
        IGuild guild,
        ITextChannel channel,
        IInteractionContext? context = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Approval - Set Greeter channel")
            .WithColor(Color.Blue);
        try
        {
            var result = await SetGreeterChannel(guild, channel, context?.User);
            
            embed.Color = result.IsSuccess ? Color.Green : Color.Red;
            embed.WithDescription(result.FormatForEmbed());
            return embed;
        }
        catch (Exception ex)
        {
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithContext(context)
                .WithGuild(guild));
            return embed
                .WithDescription(string.Join("\n", $"Failed to set \"greeter channel\" to: {channel.Mention}", $"`{errorType}`"))
                .WithColor(Color.Red);
        }
    }

    public async Task<EmbedBuilder> SetLogChannelEmbed(
        IGuild guild,
        ITextChannel channel,
        IInteractionContext? context = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Approval - Set Log channel")
            .WithColor(Color.Blue);
        try
        {
            var result = await SetLogChannel(guild, channel, context?.User);
            
            embed.Color = result.IsSuccess ? Color.Green : Color.Red;
            embed.WithDescription(result.FormatForEmbed());
            return embed;
        }
        catch (Exception ex)
        {
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithContext(context)
                .WithGuild(guild));
            return embed
                .WithDescription(string.Join("\n", $"Failed to set \"log channel\" to: {channel.Mention}", $"`{errorType}`"))
                .WithColor(Color.Red);
        }
    }
}