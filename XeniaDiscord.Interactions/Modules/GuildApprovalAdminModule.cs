using System.Text;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.GuildApproval;
using XeniaDiscord.Data.Repositories;

using SetupGreeterModal = XeniaDiscord.Interactions.Modules.GuildApprovalModalModule.SetupGreeterModal;

namespace XeniaDiscord.Interactions.Modules;

[Group("approval-admin", "Guild Config: Approval")]
[CommandContextType(InteractionContextType.Guild)]
public class GuildApprovalAdminModule : InteractionModuleBase
{
    private readonly XeniaDbContext _db;
    private readonly ErrorReportService _err;
    private readonly GuildApprovalService _service;
    private readonly GuildApprovalRepository _repo;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public GuildApprovalAdminModule(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _err = services.GetRequiredService<ErrorReportService>();
        _service = (scope?.ServiceProvider ?? services).GetRequiredService<GuildApprovalService>();
        _repo = (scope?.ServiceProvider ?? services).GetRequiredService<GuildApprovalRepository>();
    }
    
    [SlashCommand("enable", "Enable approval module")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public async Task Enable()
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Approval - Enable")
            .WithColor(Color.Blue);
        try
        {
            var guildIdStr = Context.Guild.Id.ToString();
            var model = await _db.GuildApprovals.AsNoTracking().FirstOrDefaultAsync(e => e.GuildId == guildIdStr)
                ?? new()
                {
                    GuildId = guildIdStr
                };
            model.Enabled = true;
            
            await using var db = _db.CreateSession();
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {
                await _repo.InsertOrUpdate(db, model);
                await db.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
            embed.WithDescription("Successfully enabled module.");
            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            const string msg = "Failed to enable approval module";
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithContext(Context));
            
            await FollowupAsync(embed:
                embed.WithDescription(string.Join("\n", "Failed to enable approval module.", $"`{errorType}`"))
                    .WithColor(Color.Red)
                    .Build());
        }
    }

    [SlashCommand("set-role", "Set role to be given to approved users")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    public async Task SetRoleAsync(
        [Summary("Role", "Role that users will get once they've been approved.")]
        IRole role)
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Approval - Set Role")
            .WithColor(Color.Blue);
        try
        {
            embed = await _service.SetApprovedRoleEmbed(Context.Guild, role, Context);
            /*
            var ourMember = await Context.Guild.GetCurrentUserAsync();
            var ourRoles = await Task.WhenAll(ourMember.RoleIds.Select(id => Context.Guild.GetRoleAsync(id)));
            var ourHighestRole = ourRoles.OrderByDescending(e => e.Position).FirstOrDefault();
            if (ourHighestRole == null || role.Position > ourHighestRole.Position)
            {
                embed.WithDescription(
                    "Unable to use role, since we won't have permission to give it to users.\n\n" +
                    $"The Xenia role needs to be higher than {role.Mention} in the \"Role\" settings of your Guild.")
                    .WithColor(Color.Red);
                await FollowupAsync(embed: embed.Build());
                return;
            }
            else if (!ourMember.GuildPermissions.ManageRoles)
            {
                embed.WithDescription("Xenia does not have (direct) permission to manage roles. This permissions is required for this module.").WithColor(Color.Red);
                await FollowupAsync(embed: embed.Build());
                return;
            }

            await using var db = _db.CreateSession();
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {
                var guildIdStr = Context.Guild.Id.ToString();
                var roleIdStr = role.Id.ToString();
                if (await db.GuildApprovals.AnyAsync(e => e.GuildId == guildIdStr))
                {
                    await db.GuildApprovals.Where(e => e.GuildId == guildIdStr)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.ApprovedRoleId, roleIdStr));
                }
                else
                {
                    await db.GuildApprovals.AddAsync(new GuildApprovalModel
                    {
                        GuildId = guildIdStr,
                        ApprovedRoleId = roleIdStr
                    });
                }
                await db.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }

            embed
                .WithDescription($"Successfully set approved role to: {role.Mention} ({role.Id})")
                .WithColor(Color.Green);
            */
            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithContext(Context));
            
            await FollowupAsync(embed:
                embed.WithDescription(string.Join("\n", "Failed to add event to channel.", $"`{errorType}`"))
                .WithColor(Color.Red)
                .Build());
        }
    }
    
    [SlashCommand("set-channel", "Set log channel for user approvals")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task SetLogChannel(
        [ChannelTypes(ChannelType.Text)] ITextChannel channel)
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Approval - Set Log Channel")
            .WithColor(Color.Blue);
        try
        {
            embed = await _service.SetLogChannelEmbed(Context.Guild, channel, Context);
            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithContext(Context));
            
            await FollowupAsync(embed:
                embed.WithDescription(string.Join("\n", $"Failed to set log channel to: `{channel.Id}`", $"`{errorType}`"))
                .WithColor(Color.Red)
                .Build());
        }
        throw new NotImplementedException();
    }
    
    [SlashCommand("set-greeter-channel", "Set channel to send message for greeting user (post-approval)")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task SetGreeterChannel(
        [ChannelTypes(ChannelType.Text)] ITextChannel channel)
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Approval - Set Greeter Channel")
            .WithColor(Color.Blue);
        try
        {
            embed = await _service.SetGreeterChannelEmbed(Context.Guild, channel, Context);
            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithContext(Context));
            
            await FollowupAsync(embed:
                embed.WithDescription(string.Join("\n", $"Failed to set greeter channel to: `{channel.Id}`", $"`{errorType}`"))
                .WithColor(Color.Red)
                .Build());
        }
    }

    [SlashCommand("get-greeter-msg", "Get the message used for greeting users.")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task GetGreeterMessage()
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Approval - Get Greeter Message")
            .WithColor(Color.Blue);
        try
        {
            var guildIdStr = Context.Guild.Id.ToString();
            if (!await _db.GuildApprovals.AnyAsync(e => e.GuildId == guildIdStr && !string.IsNullOrEmpty(e.GreeterMessageTemplate)))
            {
                embed.WithDescription("No greeter message has been configured.")
                    .WithColor(Color.Orange);
                await FollowupAsync(embed: embed.Build());
                return;
            }
            
            var content = await _db.GuildApprovals.Where(e => e.GuildId == guildIdStr)
                .Select(e => e.GreeterMessageTemplate)
                .FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(content?.Trim()))
            {
                embed.WithDescription("No greeter message has been configured (or it's empty)")
                    .WithColor(Color.Orange);
                await FollowupAsync(embed: embed.Build());
            }
            else
            {
                if (content.Length > 2000)
                {
                    embed.WithDescription("Greeter message has been attached as `greeter-template.txt`");
                    await FollowupWithFileAsync(
                        new MemoryStream(Encoding.UTF8.GetBytes(content)), "greeter-template.txt",
                        embed: embed.Build());
                }
                else
                {
                    embed.WithDescription(content);
                    await FollowupAsync(embed: embed.Build());
                }
            }
        }
        catch (Exception ex)
        {
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithContext(Context));
            
            await FollowupAsync(embed:
                embed.WithDescription(string.Join("\n", "Failed to get greeter message.", $"`{errorType}`"))
                    .WithColor(Color.Red)
                    .Build());
        }
    }


    [SlashCommand("setup-greeter", "Setup post-approval greeter", runMode: RunMode.Async)]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task SetupGreeter()
    {
        ITextChannel? greeterChannel = null;
        GuildApprovalModel? config = null;
        try
        {
            var guildId = Context.Guild.Id.ToString();
            config = await _db.GuildApprovals.AsNoTracking().FirstOrDefaultAsync(e => e.GuildId == guildId);
            var modal = new SetupGreeterModal();
            if (config != null)
            {
                modal.EnableGreeter = config.EnableGreeter ? ModalYesNo.Yes : ModalYesNo.No;
                if (!string.IsNullOrEmpty(config.GreeterMessageTemplate))
                {
                    modal.GreeterMessageTemplate = config.GreeterMessageTemplate;
                }
                modal.GreeterAsEmbed = config.GreeterAsEmbed ? ModalYesNo.Yes : ModalYesNo.No;
            }

            await ExceptionHelper.RetryOnTimedOut(SetGreeterChannelData);
        
            modal.GreeterChannel = greeterChannel;

            await RespondWithModalAsync("guild-approval-setup-greeter", modal);
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }

        async Task SetGreeterChannelData()
        {
            var channelId = config?.GetApprovedRoleId();
            if (!channelId.HasValue) return;

            var role = await Context.Guild.GetTextChannelAsync(channelId.Value);
            greeterChannel = role;
        }
    }


    // TODO use GuildApprovalService to validate SetupModal
    // TODO update GuildApprovalModel from the SetupModal data
    /*
    [SlashCommand("setup", "Setup Approvals in your Guild")]
    public async Task SetupCommand()
    {
        var guildId = Context.Guild.Id.ToString();
        var config = await _db.GuildApprovals.AsNoTracking().FirstOrDefaultAsync(e => e.GuildId == guildId);
        ITextChannel? logChannel = null;
        ITextChannel? greeterChannel = null;
        IRole? approvedRole = null;
        IRole? approverRole = null;

        var modal = new SetupModal();
        if (config != null)
        {
            modal.Enabled = config.Enabled ? ModalYesNo.Yes : ModalYesNo.No;
            modal.EnableGreeter = config.EnableGreeter ? ModalYesNo.Yes : ModalYesNo.No;

            if (!string.IsNullOrEmpty(config.GreeterMessageTemplate))
            {
                modal.GreeterTemplate = config.GreeterMessageTemplate;
            }
            modal.GreeterAsEmbed = config.GreeterAsEmbed ? ModalYesNo.Yes : ModalYesNo.No;
            modal.GreeterMentionUser = config.GreeterMentionUser ? ModalYesNo.Yes : ModalYesNo.No;
        }

        await Task.WhenAll(
            SetLogChannel(),
            SetGreeterChannel(),
            SetApprovedRole(),
            SetApproverRole());

        await RespondWithModalAsync("guild-approval-admin-setup", modal);

        async Task SetLogChannel()
        {
            await WrapError(async () =>
            {
                var channelId = config?.GetLogChannelId();
                if (!channelId.HasValue) return;

                var role = await Context.Guild.GetTextChannelAsync(channelId.Value);
                logChannel = role;
            });
        }
        async Task SetGreeterChannel()
        {
            await WrapError(async () =>
            {
                var channelId = config?.GetApprovedRoleId();
                if (!channelId.HasValue) return;

                var role = await Context.Guild.GetTextChannelAsync(channelId.Value);
                greeterChannel = role;
            });
        }
        async Task SetApprovedRole()
        {
            await WrapError(async () =>
            {
                var roleId = config?.GetApprovedRoleId();
                if (!roleId.HasValue) return;

                var role = await Context.Guild.GetRoleAsync(roleId.Value);
                approvedRole = role;
            });
        }
        async Task SetApproverRole()
        {
            await WrapError(async () =>
            {
                var roleId = config?.GetApproverRoleId();
                if (!roleId.HasValue) return;

                var role = await Context.Guild.GetRoleAsync(roleId.Value);
                approverRole = role;
            });
        }
        async Task WrapError(Func<Task> callback)
        {
            for (int i = 0; i <= 3; i++)
            {
                try
                {
                    await callback();
                }
                catch (Exception ex)
                {
                    var exStr = ex.ToString();
                    if (!exStr.Contains("timed out", StringComparison.OrdinalIgnoreCase) || i >= 3)
                    {
                        throw;
                    }
                }
            }
        }
    }

    [ModalInteraction("guild-approval-admin-setup")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task HandleSetupModal(SetupModal modal)
    {
        Debugger.Break();
    }

    public class SetupModal : IModal
    {
        public string Title => "Approval - Setup";
        
        [ModalTextDisplay(content: "Validation errors would go here, but there isn't anything here!")]
        public string ValidationErrors { get; set; } = "Submit modal to check for any errors.";

        [ModalChannelSelect("enable")]
        [InputLabel("Enable", "Should this feature be enabled?")]
        public ModalYesNo Enabled { get; set; } = ModalYesNo.Yes;

        [ModalChannelSelect("log-channel")]
        [InputLabel("Log Channel", "Channel to log things like who approved who.")]
        public ITextChannel LogChannel { get; set; }

        [ModalRoleSelect("approved-role")]
        [InputLabel("Approved Role", "Role to give users once they've been approved.")]
        public IRole ApprovedRole { get; set; }

        [RequiredInput(false)]
        [ModalRoleSelect("approver-role")]
        [InputLabel("Approver Role (optional)", "Users in this role can always approve new users, but only that.")]
        public IRole? ApproverRole { get; set; }

        [ModalSelectMenu("greeter-enable")]
        [InputLabel("Greeter - Enable", "Once the user is approved, a pre-generated message would be sent in a channel to \"greet\" the new user.")]
        public ModalYesNo EnableGreeter { get; set; } = ModalYesNo.Yes;

        [RequiredInput(false)]
        [ModalChannelSelect("greeter-channel")]
        [InputLabel("Greeter - Channel", "Channel to greet new users once they've been approved. Required when Greeter is enabled")]
        public ITextChannel? GreeterChannel { get; set; }
        
        [RequiredInput(false)]
        [ModalTextInput("greeter-message", style: TextInputStyle.Paragraph)]
        [InputLabel("Greeter - Message Template", "Message template for when greeting approved users.")]
        public string GreeterTemplate { get; set; }= "Heya {user_mention}, welcome to {server_name}!";

        [ModalSelectMenu("greeter-mention-user")]
        [InputLabel("Greeter - Always Mention User", "Should the user always be mentioned in the greeter message?")]
        public ModalYesNo GreeterMentionUser { get; set; } = ModalYesNo.Yes;

        [ModalSelectMenu("greeter-as-embed")]
        [InputLabel("Greeter - Display as Embed", "Render the parsed greeter template as an embed, instead of a standard message.")]
        public ModalYesNo GreeterAsEmbed { get; set; } = ModalYesNo.No;
    }
    
    */
}