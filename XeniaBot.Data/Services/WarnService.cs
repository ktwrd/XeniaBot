using System;
using System.Data;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared;

namespace XeniaBot.Data.Services;

[XeniaController]
public class WarnService : BaseService
{
    private readonly GuildWarnItemRepository _warnRepo;
    public WarnService(IServiceProvider services)
        : base(services)
    {
        _warnRepo = services.GetRequiredService<GuildWarnItemRepository>();
    }

    /// <summary>
    /// Create a Warn
    /// </summary>
    /// <param name="guildId">Guild Snowflake</param>
    /// <param name="targetUserId"><inheritdoc cref="GuildWarnItemModel.TargetUserId"/></param>
    /// <param name="actionedByUserId"><inheritdoc cref="GuildWarnItemModel.ActionedUserId"/></param>
    /// <param name="reason"><inheritdoc cref="GuildWarnItemModel.Description" /></param>
    /// <param name="relatedMessageIds"><inheritdoc cref="GuildWarnItemModel.RelatedMessageIds"/></param>
    /// <param name="relatedAttachmentGuids"><inheritdoc cref="GuildWarnItemModel.RelatedAttachmentGuids"/></param>
    /// <exception cref="NoNullAllowedException">Thrown when unable to find the document in <see cref="GuildWarnItemRepository"/></exception>
    public async Task<GuildWarnItemModel> CreateWarnAsync(
        ulong guildId,
        ulong targetUserId,
        ulong actionedByUserId,
        string reason,
        ulong[] relatedMessageIds,
        string[] relatedAttachmentGuids)
    {
        var model = new GuildWarnItemModel()
        {
            GuildId = guildId,
            TargetUserId = targetUserId,
            ActionedUserId = actionedByUserId,
            UpdatedByUserId = actionedByUserId,
            Description = reason,
            RelatedMessageIds = relatedMessageIds,
            RelatedAttachmentGuids = relatedAttachmentGuids,
            CreatedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await _warnRepo.Add(model);
        var existing = await _warnRepo.GetLatest(model.WarnId);
        if (existing == null)
            throw new NoNullAllowedException($"Document not found with WarnId of {model.WarnId}");
        
        return existing;
    }

    /// <inheritdoc cref="CreateWarnAsync(ulong,ulong,ulong,string,ulong[],string[])"/>
    public Task<GuildWarnItemModel> CreateWarnAsync(ulong guildId,
        ulong targetUserId,
        ulong actionedByUserId,
        string reason)
    {
        return CreateWarnAsync(
            guildId, targetUserId, actionedByUserId, reason, Array.Empty<ulong>(), Array.Empty<string>());
    }

    /// <summary>
    /// Create a Warn
    /// <inheritdoc cref="CreateWarnAsync(ulong,ulong,ulong,string,ulong[],string[])"/>
    /// </summary>
    /// <param name="guild"><see cref="GuildWarnItemModel.GuildId"/></param>
    /// <param name="targetUser"><see cref="GuildWarnItemModel.TargetUserId"/></param>
    /// <param name="actionedByUser"><see cref="GuildWarnItemModel.ActionedUserId"/></param>
    /// <param name="reason"><see cref="GuildWarnItemModel.Description"/></param>
    public Task<GuildWarnItemModel> CreateWarnAsync(
        IGuild guild,
        IUser targetUser,
        IUser actionedByUser,
        string reason)
    {
        return CreateWarnAsync(guild.Id, targetUser.Id, actionedByUser.Id, reason);
    }
    
    /// <summary>
    /// Create a Warn
    /// <inheritdoc cref="CreateWarnAsync(ulong,ulong,ulong,string,ulong[],string[])"/>
    /// </summary>
    /// <param name="targetUser"><see cref="GuildWarnItemModel.TargetUserId"/></param>
    /// <param name="actionedByUser"><see cref="GuildWarnItemModel.ActionedUserId"/></param>
    /// <param name="reason"><see cref="GuildWarnItemModel.Description"/></param>
    public Task<GuildWarnItemModel> CreateWarnAsync(
        IGuildUser targetUser,
        IUser actionedByUser,
        string reason)
    {
        return CreateWarnAsync(targetUser.Guild, targetUser, actionedByUser, reason);
    }

    /// <summary>
    /// Update a Warn.
    /// </summary>
    /// <param name="warnId"><see cref="GuildWarnItemModel.WarnId"/></param>
    /// <param name="reason"><inheritdoc cref="GuildWarnItemModel.Description" /></param>
    /// <param name="updatedByUserId"><inheritdoc cref="GuildWarnItemModel.UpdatedByUserId"/></param>
    /// <param name="relatedMessageIds"><inheritdoc cref="GuildWarnItemModel.RelatedMessageIds"/></param>
    /// <param name="relatedAttachmentGuids"><inheritdoc cref="GuildWarnItemModel.RelatedAttachmentGuids"/></param>
    /// <exception cref="NoNullAllowedException">Thrown when unable to find the document in <see cref="GuildWarnItemRepository"/></exception>
    public async Task<GuildWarnItemModel> UpdateWarn(
        string warnId,
        string reason,
        ulong updatedByUserId,
        ulong[]? relatedMessageIds = null,
        string[]? relatedAttachmentGuids = null)
    {
        var existing = await _warnRepo.GetLatest(warnId);
        if (existing == null)
            throw new NoNullAllowedException($"Document not found with WarnId of {warnId}");

        existing.Description = reason;
        existing.UpdatedByUserId = updatedByUserId;
        if (relatedMessageIds != null)
            existing.RelatedMessageIds = relatedMessageIds;
        if (relatedAttachmentGuids != null)
            existing.RelatedAttachmentGuids = relatedAttachmentGuids;

        await _warnRepo.Add(existing);

        var existingUpdated = await _warnRepo.GetLatest(warnId);
        if (existingUpdated == null)
            throw new NoNullAllowedException($"Document not found with WarnId of {warnId}");
        return existingUpdated;
    }
}