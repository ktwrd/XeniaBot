using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Common.Interfaces;
using XeniaDiscord.Common.Repositories;
using XeniaDiscord.Data.Models.Ticket;

namespace XeniaDiscord.Common.Services;

public class TicketService : ITicketService
{
    private readonly DiscordSocketClient _client;
    private readonly GuildTicketConfigRepository _configRepo;
    private readonly GuildTicketRepository _repo;
    public TicketService(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _repo = services.GetRequiredService<GuildTicketRepository>();
        _configRepo = services.GetRequiredService<GuildTicketConfigRepository>();
    }

    public async Task<GuildTicketModel> CreateTicket(ulong guildId)
    {
        var config = await _configRepo.GetAsync(guildId);
        if (config == null)
            throw new TicketException("Guild not setup (config is null)");

        // Fetch guild and category, throw errors if they are null
        var guild = _client.GetGuild(guildId);
        if (guild == null)
            throw new TicketException($"Guild not found ({guildId})");

        var role = await guild.GetRoleAsync(config.GetRoleId());
        if (role == null)
            throw new TicketException($"Ticket Manager Role not found ({config.RoleId})");

        var category = guild.GetCategoryChannel(config.GetCategoryId());
        if (category == null)
            throw new TicketException($"Category not found ({config.CategoryId})");

        var ticketDetails = new GuildTicketModel()
        {
            GuildId = guildId.ToString(),
            Status = GuildTicketStatus.Open
        };

        RestTextChannel? textChannel = null;

        // Attempt to create channel, then move it to category
        try
        {
            textChannel = await guild.CreateTextChannelAsync($"ticket-{ticketDetails.Id}");
            await textChannel.ModifyAsync(prop => prop.CategoryId = category.Id);
            ticketDetails.ChannelId = textChannel.Id.ToString();
            var ignoreOverwrite = new OverwritePermissions();
            ignoreOverwrite.Modify(viewChannel: PermValue.Deny);
            await textChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, ignoreOverwrite);
            await textChannel.AddPermissionOverwriteAsync(role, GetOverwritePermissions());
        }
        catch (Exception ex)
        {
            throw new TicketException($"Failed to create ticket channel", ex);
        }

        if (textChannel == null) throw new TicketException("Created Text Channel is null");

        return await _repo.UpdateAsync(ticketDetails);
    }
    public async Task UserAccessGrant(ulong channelId, ulong userId)
    {
        var ticket = await _repo.GetForChannelAsync(channelId);
        if (ticket == null)
            throw new TicketException("Ticket Details not found");

        if (ticket.Users.Any(e => e.GetUserId() == userId))
            throw new TicketException("User already exists in ticket");

        var guild = _client.GetGuild(ticket.GetGuildId());
        if (guild == null)
            throw new TicketException($"Guild not found ({ticket.GuildId})");

        var user = guild.GetUser(userId);
        if (user == null)
            throw new TicketException($"User not found ({userId})");

        var channel = guild.GetTextChannel(ticket.GetChannelId());
        if (channel == null)
            throw new TicketException($"Ticket Channel not found ({ticket.ChannelId})");

        // Modify channel permission overwrites
        try
        {
            await channel.AddPermissionOverwriteAsync(user, GetOverwritePermissions());
        }
        catch (Exception ex)
        {
            throw new TicketException($"Failed to modify channel permissions. {ex.Message}", ex);
        }

        // Add user to model then save.
        ticket.Users.Add(new()
        {
            TicketId = ticket.Id,
            UserId = userId.ToString()
        });
        await _repo.UpdateAsync(ticket);
    }
    public async Task UserAccessRevoke(ulong channelId, ulong userId)
    {
        var ticket = await _repo.GetForChannelAsync(channelId);
        if (ticket == null)
            throw new TicketException("Ticket Details not found");

        if (!ticket.Users.Any(e => e.GetUserId() == userId))
            throw new TicketException("User not included in ticket");

        var guild = _client.GetGuild(ticket.GetGuildId());
        if (guild == null)
            throw new TicketException($"Guild not found ({ticket.GuildId})");

        var user = guild.GetUser(userId);
        if (user == null)
            throw new TicketException($"User not found ({userId})");

        var channel = guild.GetTextChannel(ticket.GetChannelId());
        if (channel == null)
            throw new TicketException($"Ticket Channel not found ({ticket.ChannelId})");

        // Remove user override
        try
        {
            await channel.RemovePermissionOverwriteAsync(user);
        }
        catch (Exception ex)
        {
            throw new TicketException($"Failed to modify channel permissions. {ex.Message}", ex);
        }

        // Remove userId from model then save
        ticket.Users.RemoveAll(e => e.GetUserId() == userId);
        await _repo.UpdateAsync(ticket);
    }
    private async Task<InternalTicketDetails> GetTicketDetails(ulong ticketChannelId)
    {
        var ticket = await _repo.GetForChannelAsync(ticketChannelId);
        if (ticket == null)
            throw new TicketException("Ticket Details not found");

        var config = await _configRepo.GetAsync(ticket.GetGuildId());
        if (config == null)
            throw new TicketException("Guild not setup");

        var guild = _client.GetGuild(ticket.GetGuildId());
        if (guild == null)
            throw new TicketException($"Guild not found ({ticket.GuildId})");

        var role = await guild.GetRoleAsync(config.GetRoleId());
        if (role == null)
            throw new TicketException($"Ticket Manager Role not found ({config.RoleId})");

        var logChannel = guild.GetTextChannel(config.GetLogChannelId());
        if (logChannel == null)
            throw new TicketException($"Ticket Log Channel not found ({config.LogChannelId})");

        var ticketChannel = guild.GetTextChannel(ticket.GetChannelId());
        if (ticketChannel == null)
            throw new TicketException($"Ticket Channel not found ({ticket.ChannelId})");

        return new InternalTicketDetails()
        {
            Ticket = ticket,
            Config = config,
            Guild = guild,
            Role = role,
            LogChannel = logChannel,
            TicketChannel = ticketChannel
        };
    }
    private async Task<InternalTicketDetails> GetTicketDetails(ulong ticketChannelId, ulong closingUserId)
    {
        var details = await GetTicketDetails(ticketChannelId);
        var closingUser = details.Guild.GetUser(closingUserId);
        if (closingUser == null)
            throw new TicketException($"Could not find user who closed this ticket ({closingUserId})");

        details.ClosingUser = closingUser;

        return details;
    }

    internal class InternalTicketDetails
    {

        public required GuildTicketModel Ticket { get; set; }
        public required GuildTicketConfigModel Config {get;set;}
        public required SocketGuild Guild {get;set;}
        public required IRole Role {get;set;}
        public required ITextChannel LogChannel {get;set;}
        public required ITextChannel TicketChannel {get;set;}
        public IGuildUser? ClosingUser {get;set;}
    }
    /// <summary>
    /// Mark a ticket as closed.
    ///
    /// This will take a backup of the channel content, mark the ticket as closed and who did it. After all of those things are done, it will delete the channel.
    /// </summary>
    /// <param name="channelId"></param>
    /// <param name="status"></param>
    /// <param name="closingUserId"></param>
    /// <returns></returns>
    /// <exception cref="TicketException"></exception>
    public async Task<GuildTicketTranscriptModel> CloseTicket(ulong channelId, GuildTicketStatus status, ulong closingUserId)
    {
        var details = await GetTicketDetails(channelId, closingUserId);

        details.Ticket.ClosedAt = DateTimeOffset.UtcNow;
        details.Ticket.Status = status;
        details.Ticket.ClosedByUserId = closingUserId.ToString();
        throw new NotImplementedException();
        // details.Ticket = await _configRepo.UpdateAsync(details.Ticket);
        // old code from mongodb era
        /*details.Ticket = await Get(details.Ticket.ChannelId);
        if (details.Ticket == null)
            throw new TicketException("Ticket Details not found");

        var transcript = await GenerateTranscript(details.Ticket);
        if (transcript == null)
            throw new TicketException("Failed to generate transcript");

        var channelBackup = new List<CacheMessageModel>();
        try
        {
            channelBackup = await GenerateChannelBackup(details.Ticket);
        }
        catch (Exception ex)
        {
            throw new TicketException("Failed to generate channel backup", ex);
        }

        var channelBackupSer = JsonSerializer.Serialize(
            channelBackup, new JsonSerializerOptions()
            {
                IncludeFields = true,
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.Preserve
            });

        var attachment = string.Join("\n", transcript.ToString());
        var embed = new EmbedBuilder()
        {
            Title = $"Ticket Marked as {status}",
            Description = "Users Included\n"
                + string.Join("\n", details.Ticket.Users.Select(v => $"<@{v}>"))
                + "\n\n"
                + string.Join("\n", details.Role.Members.Select(v => $"<@{v.Id}>")),
        };
        embed.WithFooter($"Closed by {details.ClosingUser.Username}#{details.ClosingUser.Discriminator} {details.ClosingUser.Id}", details.ClosingUser.GetAvatarUrl());

        var attachmentList = new List<FileAttachment>();
        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(attachment)))
        {
            attachmentList.Add(new FileAttachment(ms, "transcript.txt"));
        }

        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(channelBackupSer)))
        {
            attachmentList.Add(new FileAttachment(ms, "channelContent.json"));
        }
        await details.LogChannel.SendFilesAsync(attachmentList, "", embed: embed.Build());

        await details.TicketChannel.DeleteAsync();

        return transcript;*/
    }
    private static OverwritePermissions GetOverwritePermissions()
    {
        var perms = new OverwritePermissions();
        perms.Modify(
            viewChannel: PermValue.Allow,
            readMessageHistory: PermValue.Allow,
            sendMessages: PermValue.Allow);
        return perms;
    }

}
