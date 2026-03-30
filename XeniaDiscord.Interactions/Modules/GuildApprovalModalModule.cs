using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared.Services;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Interactions.Modules;

[CommandContextType(InteractionContextType.Guild)]
public class GuildApprovalModalModule : InteractionModuleBase
{
    private readonly XeniaDbContext _db;
    private readonly ErrorReportService _err;
    private readonly ValidationService _validation;
    private readonly GuildApprovalRepository _repo;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public GuildApprovalModalModule(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _err = services.GetRequiredService<ErrorReportService>();
        _validation = services.GetRequiredService<ValidationService>();
        _repo = (scope?.ServiceProvider ?? services).GetRequiredService<GuildApprovalRepository>();
    }
    private async Task HandleSetupGreeterModalInternal(SetupGreeterModal modal)
    {
        var errors = new List<string>();
        if (modal.EnableGreeter == ModalYesNo.Yes)
        {
            if (modal.GreeterChannel == null)
            {
                errors.Add("- Greeter Channel is required when enabled.");
            }
            else
            {
                var validation = await _validation.ChannelPermissions(
                    Context.Guild.Id,
                    modal.GreeterChannel.Id,
                    GuildApprovalService.RequiredChannelPermissions);
                if (validation.IsSuccess)
                {
                    if (validation.Value.AnyMissing)
                    {
                        errors.Add("- Missing permissions on greeter channel: " + string.Join(", ", validation.Value.Missing.Select(e => e.ToString())));
                    }
                }
                else
                {
                    errors.Add($"- Failed to validate greeter channel permissions ({validation.Error.Kind})");
                }
            }
        }
        if (errors.Count > 0)
        {
            _log.Trace("Validation errors:\n" + string.Join("\n", errors));
            modal.ValidationErrors = "Failed to validate user input:\n" + string.Join("\n", errors);
            await RespondWithModalAsync("guild-approval-setup-greeter", modal);
            return;
        }

            await DeferAsync();
        var guildIdStr = Context.Guild.Id.ToString();
        var model = await _db.GuildApprovals.AsNoTracking().FirstOrDefaultAsync(e => e.GuildId == guildIdStr)
            ?? new()
            {
                GuildId = guildIdStr
            };
        
        model.EnableGreeter = modal.EnableGreeter == ModalYesNo.Yes;
        model.GreeterChannelId = modal.GreeterChannel?.Id.ToString();
        model.GreeterMessageTemplate = modal.GreeterMessageTemplate;
        model.GreeterAsEmbed = modal.GreeterAsEmbed == ModalYesNo.Yes;
        
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await _repo.InsertOrUpdate(db, model);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            var msg = $"Failed to save configuration for guild \"{Context.Guild.Name}\" ({Context.Guild.Id})";
            _log.Error(ex, msg);
            await trans.RollbackAsync();
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithContext(Context)
                .AddSerializedAttachment("modal.json", modal)
                .AddSerializedAttachment("guildApprovalModel.json", model));
            await FollowupAsync("Failed to save greeter configuration\n-# This error has been reported to the developers.");
            return;
        }
        try
        {
            await FollowupAsync("Successfully updated greeter configuration");
        }
        catch (Exception ex)
        {
            const string msg = "Failed to follow up to interaction!";
            _log.Error(ex, msg);
            await trans.RollbackAsync();
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithContext(Context)
                .AddSerializedAttachment("modal.json", modal)
                .AddSerializedAttachment("guildApprovalModel.json", model));
        }
    }
    
    [ModalInteraction("guild-approval-setup-greeter", runMode: RunMode.Async)]
    public async Task HandleSetupGreeterModal(SetupGreeterModal modal)
    {
        _log.Trace("Handling modal...");
        try
        {
            await HandleSetupGreeterModalInternal(modal);
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }
    }

    public class SetupGreeterModal : IModal
    {
        public string Title => "Approval - Setup Greeter";

        [ModalTextDisplay(content: "Validation errors would go here, but there isn't anything here!")]
        public string ValidationErrors { get; set; } = "Submit modal to check for any errors.";

        [ModalSelectMenu("enable")]
        [InputLabel("Enable", "Once the user is approved, a message similar in function to the greeter will be sent.")]
        public ModalYesNo EnableGreeter { get; set; } = ModalYesNo.Yes;

        [RequiredInput(false)]
        [ModalChannelSelect("greeter-channel")]
        [InputLabel("Channel", "Channel to greet new users once they've been approved. Required when Greeter is enabled")]
        public ITextChannel? GreeterChannel { get; set; }
        
        [RequiredInput(false)]
        [ModalTextInput("greeter-message", style: TextInputStyle.Paragraph)]
        [InputLabel("Message Template", "Message template for when greeting approved users.")]
        public string GreeterMessageTemplate { get; set; }= "Heya {user_mention}, welcome to {guild_name}!";

        [ModalSelectMenu("greeter-message-as-embed")]
        [InputLabel("Greeter - Display as Embed", "Render the parsed greeter template as an embed, instead of a standard message.")]
        public ModalYesNo GreeterAsEmbed { get; set; } = ModalYesNo.No;
    }
}