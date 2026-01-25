using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.ComponentModel.DataAnnotations;
using XeniaBot.Shared.Config;
using XeniaDiscord.Common.Interfaces;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Warn;

namespace XeniaDiscord.Common.Services;

public class WarnService : IWarnService
{
    private readonly IDiscordClient _discord;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDbContext _db;
    public WarnService(IServiceProvider services)
    {
        _discord = services.GetRequiredService<IDiscordClient>();
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    /// <inheritdoc/>
    public async Task<CreateWarnResult> CreateAsync(
        IGuild guild,
        IUser targetUser,
        [MinLength(1)]
        [MaxLength(DbGlobals.MaxStringSize)]
        string reason,
        IUser createdByUser)
    {
        var guildIdStr = guild.Id.ToString();
        var guildConfig = await _db.GuildWarnConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == guildIdStr);
        if (string.IsNullOrEmpty(reason))
        {
            return new CreateWarnResult
            {
                Kind = CreateWarnResultKind.MissingReason,
                IsGuildConfigured = guildConfig != null
            };
        }
        if (reason.Length > DbGlobals.MaxStringSize)
        {
            return new CreateWarnResult
            {
                Kind = CreateWarnResultKind.ReasonTooLong,
                IsGuildConfigured = guildConfig != null
            };
        }

        var createdByGuildUser = await guild.GetUserAsync(createdByUser.Id);
        if (!createdByGuildUser.GuildPermissions.ModerateMembers)
        {
            return new CreateWarnResult
            {
                Kind = CreateWarnResultKind.MissingPermissions,
                IsGuildConfigured = guildConfig != null
            };
        }

        await using var ctx = _db.CreateSession();
        await using var transaction = await ctx.Database.BeginTransactionAsync();
        GuildWarnModel record;
        try
        {
            record = new GuildWarnModel
            {
                GuildId = guildIdStr,
                TargetUserId = targetUser.Id.ToString(),
                CreatedByUserId = createdByUser.Id.ToString(),
                Description = reason,
            };
            await ctx.GuildWarns.AddAsync(record);
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _log.Error(ex, $"Failed to create warn record with:\n" +
                           $"Guild: \"{guild.Name}\" ({guild.Id})\n" +
                           $"targetUser: \"{targetUser.GlobalName}\" (username: {targetUser.Username}, {targetUser.Id})\n" +
                           $"createdByUser: \"{createdByUser.GlobalName}\" (username: {createdByUser.Username}, {createdByUser.Id})");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("guild.id", guild.Id.ToString());
                scope.SetTag("guild.name", guild.Name);
                scope.SetTag("author.id", createdByUser.Id.ToString());
                scope.SetTag("author.username", createdByUser.Username);
                scope.SetTag("author.global_name", createdByUser.GlobalName);
                scope.SetExtra("param.guild.id", guild.Id);
                scope.SetExtra("param.guild.name", guild.Name);
                scope.SetExtra("param.targetUser.id", targetUser.Id);
                scope.SetExtra("param.targetUser.username", targetUser.Username);
                scope.SetExtra("param.targetUser.global_name", targetUser.GlobalName);
                scope.SetExtra("param.reason", reason);
                scope.SetExtra("param.createdByUser.id", createdByUser.Id);
                scope.SetExtra("param.createdByUser.username", createdByUser.Username);
                scope.SetExtra("param.createdByUser.global_name", createdByUser.GlobalName);
            });
            throw;
        }

        return new CreateWarnResult
        {
            Kind = CreateWarnResultKind.Success,
            IsGuildConfigured = guildConfig != null,
            Model = record
        };
    }

    public Task<string?> GetDashboardUrl(GuildWarnModel? model)
    {
        return Task.Run(() =>
        {
            if (model == null) return null;
            var cfg = XeniaConfig.Get();
            if (string.IsNullOrEmpty(cfg.Dashboard.Url))
                return null;

            return cfg.Dashboard.Url = "/Warn/View/" + model.Id;
        });
    }
    #region Add
    /// <inheritdoc />
    public async Task<AddWarnCommentResult> AddCommentAsync(
        Guid warnId,
        [MinLength(1)]
        [MaxLength(DbGlobals.MaxStringSize)]
        string content,
        IUser createdByUser)
    {
        var warnRecord = await _db.GuildWarns
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == warnId);
        var guildConfig = warnRecord == null
            ? null
            : await _db.GuildWarnConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == warnRecord.GuildId);

        return await AddCommentAsync(warnRecord, guildConfig, content, createdByUser);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when failed to get guild via <see cref="GuildWarnModel.GuildId"/> or <see cref="GuildWarnConfigModel.Id"/>
    /// </exception>
    public async Task<AddWarnCommentResult> AddCommentAsync(
        GuildWarnModel? warnRecord,
        GuildWarnConfigModel? guildConfig,

        [MinLength(1)]
        [MaxLength(DbGlobals.MaxStringSize)]
        string content,
        IUser createdByUser)
    {
        IGuild? guild = null;
        IGuildUser? createdByGuildUser = null;
        var createdBySuperuser = XeniaConfig.Instance?.Discord.SuperuserIds.Contains(createdByUser.Id) ?? false;

        void CaptureException(Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("guild.id", guild?.Id.ToString() ?? "null");
                scope.SetTag("guild.name", guild?.Name ?? "null");
                scope.SetTag("author.id", createdByUser?.Id.ToString() ?? "null");
                scope.SetTag("author.username", createdByUser?.Username ?? "null");
                scope.SetTag("author.global_name", createdByUser?.GlobalName ?? "null");

                scope.SetExtra("param.warnRecord.id", warnRecord?.Id);
                scope.SetExtra("param.warnRecord.createdAt", warnRecord?.CreatedAt);
                scope.SetExtra("param.warnRecord.guildId", warnRecord?.GuildId);
                scope.SetExtra("param.guildConfig.id", guildConfig?.Id);

                scope.SetExtra("param.content", content);
                scope.SetExtra("param.createdByUser.id", createdByUser?.Id);
                scope.SetExtra("param.createdByUser.username", createdByUser?.Username);
                scope.SetExtra("param.createdByUser.global_name", createdByUser?.GlobalName);

                scope.SetExtra("local.createdByGuildUser.id", createdByGuildUser?.Id);
                scope.SetExtra("local.createdByGuildUser.username", createdByGuildUser?.Username);
                scope.SetExtra("local.createdByGuildUser.global_name", createdByGuildUser?.GlobalName);
                scope.SetExtra("local.createdBySuperuser", createdBySuperuser);
            });
        }
        if (warnRecord == null)
        {
            return new AddWarnCommentResult
            {
                Kind = AddWarnCommentResultKind.WarnRecordNotFound,
            };
        }
        if (string.IsNullOrEmpty(content))
        {
            return new AddWarnCommentResult
            {
                Kind = AddWarnCommentResultKind.ContentRequired,
            };
        }
        if (content.Length > DbGlobals.MaxStringSize)
        {
            return new AddWarnCommentResult
            {
                Kind = AddWarnCommentResultKind.ContentTooLong,
            };
        }

        var guildId = guildConfig?.GetGuildId() ?? warnRecord.GetGuildId();
        var guildIdViaWarnRecord = guildConfig?.GetGuildId() == null;

        if (!createdBySuperuser)
        {
            try
            {
                guild = await _discord.GetGuildAsync(guildId);
            }
            catch (Exception ex)
            {
                CaptureException(ex);
                throw new InvalidOperationException(guildIdViaWarnRecord
                    ? $"Failed to get Guild with Id {guildId} via Warn Record {warnRecord.Id}"
                    : $"Failed to get Guild with Id {guildId} via Guild Warn Config", ex);
            }
            if (guild == null)
            {
                return new AddWarnCommentResult
                {
                    Kind = AddWarnCommentResultKind.BotNotInGuildAnymore,
                    IsGuildConfigured = null,
                    WarnModel = warnRecord
                };
            }

            createdByGuildUser = await guild.GetUserAsync(createdByUser.Id);
            if (!(createdByGuildUser?.GuildPermissions.ModerateMembers ?? false) &&
                !createdBySuperuser &&
                warnRecord.GetCreatedByUserId().GetValueOrDefault(0) != createdByUser.Id)
            {
                return new AddWarnCommentResult
                {
                    Kind = AddWarnCommentResultKind.MissingPermissions,
                    IsGuildConfigured = guildConfig != null,
                    WarnModel = warnRecord
                };
            }
        }

        await using var ctx = _db.CreateSession();
        await using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var record = new GuildWarnCommentModel
            {
                WarnId = warnRecord.Id,
                Content = content,
                CreatedByUserId = createdByUser.Id.ToString()
            };
            await ctx.GuildWarnComments.AddAsync(record);
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            return new AddWarnCommentResult
            {
                Kind = AddWarnCommentResultKind.Success,
                IsGuildConfigured = guildConfig != null,
                Model = record,
                WarnModel = warnRecord
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _log.Error(ex, $"Failed to create comment record for warn {warnRecord.Id} with:\n" +
                           $"Guild: \"{guild?.Name}\" ({guild?.Id})\n" +
                           $"Created by superuser: {createdBySuperuser}\n" +
                           $"Comment Creator: \"{createdByUser.GlobalName}\" (username: {createdByUser.Username}, {createdByUser.Id})\n" +
                           $"Content: {content}");
            CaptureException(ex);
            throw;
        }
    }
    #endregion

    #region Delete
    /// <inheritdoc/>
    public async Task<DeleteWarnCommentResult> DeleteCommentAsync(
        Guid commentId,
        IUser deletedByUser,
        [MaxLength(DbGlobals.MaxStringSize)]
        string? reason)
    {
        var commentRecord = await _db.GuildWarnComments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == commentId);
        if (commentRecord == null)
        {
            return new DeleteWarnCommentResult
            {
                Kind = DeleteWarnCommentResultKind.CommentNotFound,
            };
        }
        var warnRecord = await _db.GuildWarns
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == commentRecord.WarnId);
        if (warnRecord == null)
        {
            return new DeleteWarnCommentResult
            {
                Kind = DeleteWarnCommentResultKind.WarnNotFound,
            };
        }
        return await DeleteCommentAsync(warnRecord, commentRecord, deletedByUser, reason);
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Thrown when failed to get guild via <see cref="GuildWarnModel.GuildId"/> or <see cref="GuildWarnConfigModel.Id"/>
    /// </exception>
    public async Task<DeleteWarnCommentResult> DeleteCommentAsync(
        GuildWarnModel? warnRecord,
        GuildWarnCommentModel? commentModel,
        IUser deletedByUser,
        [MaxLength(DbGlobals.MaxStringSize)]
        string? reason)
    {
        if (warnRecord == null)
        {
            return new DeleteWarnCommentResult
            {
                Kind = DeleteWarnCommentResultKind.WarnNotFound,
            };
        }
        if (commentModel == null)
        {
            return new DeleteWarnCommentResult
            {
                Kind = DeleteWarnCommentResultKind.CommentNotFound,
                WarnModel = warnRecord,
            };
        }

        // ignore comments that are already deleted.
        if (commentModel.IsDeleted ||
            commentModel.DeletedAt != null ||
            !string.IsNullOrEmpty(commentModel.DeletedByUserId) ||
            !string.IsNullOrEmpty(commentModel.DeleteReason))
        {
            return new DeleteWarnCommentResult
            {
                Kind = DeleteWarnCommentResultKind.CommentNotFound,
                WarnModel = warnRecord,
            };
        }

        if (commentModel.WarnId != warnRecord.Id)
        {
            throw new ArgumentException(
                $"The provided Comment Record {commentModel.Id} doesn't belong to the provided Warn Record {warnRecord.Id}",
                nameof(commentModel));
        }

        IGuild? guild = null;
        IGuildUser? deletedByGuildUser = null;
        var deletedBySuperuser = XeniaConfig.Instance?.Discord.SuperuserIds.Contains(deletedByUser.Id) ?? false;

        var guildConfig = await _db.GuildWarnConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == warnRecord.GuildId);

        void CaptureException(Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("guild.id", guild?.Id.ToString() ?? "null");
                scope.SetTag("guild.name", guild?.Name ?? "null");
                scope.SetTag("author.id", deletedByUser?.Id.ToString() ?? "null");
                scope.SetTag("author.username", deletedByUser?.Username ?? "null");
                scope.SetTag("author.global_name", deletedByUser?.GlobalName ?? "null");

                scope.SetExtra("param.warnRecord.id", warnRecord?.Id);
                scope.SetExtra("param.warnRecord.createdAt", warnRecord?.CreatedAt);
                scope.SetExtra("param.warnRecord.guildId", warnRecord?.GuildId);
                scope.SetExtra("param.guildConfig.id", guildConfig?.Id);

                scope.SetExtra("param.reason", reason);
                scope.SetExtra("param.deletedByUser.id", deletedByUser?.Id);
                scope.SetExtra("param.deletedByUser.username", deletedByUser?.Username);
                scope.SetExtra("param.deletedByUser.global_name", deletedByUser?.GlobalName);

                scope.SetExtra("local.deletedByGuildUser.id", deletedByGuildUser?.Id);
                scope.SetExtra("local.deletedByGuildUser.username", deletedByGuildUser?.Username);
                scope.SetExtra("local.deletedByGuildUser.global_name", deletedByGuildUser?.GlobalName);
                scope.SetExtra("local.deletedBySuperuser", deletedBySuperuser);
            });
        }

        var guildId = guildConfig?.GetGuildId() ?? warnRecord.GetGuildId();
        var guildIdViaWarnRecord = guildConfig?.GetGuildId() == null;

        if (!deletedBySuperuser && deletedByUser.Id != commentModel.GetCreatedByUserId().GetValueOrDefault(0))
        {
            try
            {
                guild = await _discord.GetGuildAsync(guildId);
            }
            catch (Exception ex)
            {
                CaptureException(ex);
                throw new InvalidOperationException(guildIdViaWarnRecord
                    ? $"Failed to get Guild with Id {guildId} via Warn Record {warnRecord.Id}"
                    : $"Failed to get Guild with Id {guildId} via Guild Warn Config", ex);
            }
            if (guild == null)
            {
                return new DeleteWarnCommentResult
                {
                    Kind = DeleteWarnCommentResultKind.MissingPermissions,
                    WarnModel = warnRecord,
                };
            }

            deletedByGuildUser = await guild.GetUserAsync(deletedByUser.Id);
            if (!(deletedByGuildUser?.GuildPermissions.Administrator ?? false))
            {
                return new DeleteWarnCommentResult
                {
                    Kind = DeleteWarnCommentResultKind.MissingPermissions,
                    WarnModel = warnRecord,
                };
            }
        }

        await using var ctx = _db.CreateSession();
        await using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            // creating new instance so we don't accidentally fuck with an ef core proxy class.
            var record = new GuildWarnCommentModel
            {
                Id = commentModel.Id,
                WarnId = commentModel.WarnId,
                CreatedAt = commentModel.CreatedAt,
                Content = commentModel.Content,
                CreatedByUserId = commentModel.CreatedByUserId,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                DeletedByUserId = deletedByUser.Id.ToString(),
                DeleteReason = reason
            };
            if (!await ctx.GuildWarns.AnyAsync(e => e.Id == warnRecord.Id))
            {
                await ctx.GuildWarns.AddAsync(warnRecord);
            }

            if (await ctx.GuildWarnComments.AnyAsync(e => e.Id == commentModel.Id))
            {
                await ctx.GuildWarnComments.Where(e => e.Id == commentModel.Id)
                    .ExecuteUpdateAsync(e => e
                        .SetProperty(p => p.IsDeleted, true)
                        .SetProperty(p => p.CreatedAt, record.DeletedAt)
                        .SetProperty(p => p.DeletedByUserId, record.DeletedByUserId)
                        .SetProperty(p => p.DeleteReason, record.DeleteReason));
            }
            else
            {
                await ctx.GuildWarnComments.AddAsync(record);
            }
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
            return new DeleteWarnCommentResult
            {
                Kind = DeleteWarnCommentResultKind.Success,
                CommentModel = record,
                WarnModel = warnRecord
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _log.Error(ex, $"Failed to delete comment {commentModel.Id} record for warn {warnRecord.Id} with:\n" +
                           $"Guild: \"{guild?.Name}\" ({guild?.Id})\n" +
                           $"Deleted by superuser: {deletedBySuperuser}\n" +
                           $"Comment Deletor: \"{deletedByUser.GlobalName}\" (username: {deletedByUser.Username}, {deletedByUser.Id})\n" +
                           $"Reason: {reason}");
            CaptureException(ex);
            throw;
        }
    }
    #endregion
}
