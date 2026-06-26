using Microsoft.EntityFrameworkCore;
using XeniaDiscord.Data.Extensions;
using XeniaDiscord.Data.Models;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Models.GuildApproval;
using XeniaDiscord.Data.Models.PartialSnapshot;
using XeniaDiscord.Data.Models.RolePreserve;
using XeniaDiscord.Data.Models.ServerLog;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Data;

public class XeniaDbContext : DbContext
{
    private readonly DbContextOptions<XeniaDbContext> _ops;
    public XeniaDbContext(DbContextOptions<XeniaDbContext> options)
        : base(options)
    {
        _ops = options;
    }
    public XeniaDbContext CreateSession() => new(_ops);

    #region Partial Snapshots
    public DbSet<UserPartialSnapshotModel> UserPartialSnapshots { get; set; }
    public DbSet<GuildPartialSnapshotModel> GuildPartialSnapshots { get; set; }
    #endregion

    #region Snapshots
    public DbSet<GuildMemberSnapshotModel> GuildMemberSnapshots { get; set; }
    public DbSet<GuildMemberPermissionSnapshotModel> GuildMemberPermissionSnapshots { get; set; }
    public DbSet<GuildMemberRoleSnapshotModel> GuildMemberRoleSnapshots { get; set; }

    public DbSet<GuildSnapshotModel> GuildSnapshots { get; set; }
    public DbSet<GuildRoleSnapshotModel> GuildRoleSnapshots { get; set; }
    public DbSet<GuildRolePermissionSnapshotModel> GuildRolePermissionSnapshots { get; set; }

    public DbSet<GuildSnapshotEventModel> GuildSnapshotEvent { get; set; }

    public DbSet<UserSnapshotModel> UserSnapshots { get; set; }
    public DbSet<PrimaryGuildSnapshotModel> PrimaryGuildSnapshots { get; set; }
    #endregion

    #region Discord Cache
    public DbSet<GuildChannelCacheModel> GuildChannelCache { get; set; }
    public DbSet<GuildMemberCacheModel> GuildMemberCache { get; set; }
    public DbSet<GuildCacheModel> GuildCache { get; set; }
    public DbSet<UserCacheModel> UserCache { get; set; }
    public DbSet<AuditLogBanCacheModel> AuditLogBanEntryCache { get; set; }
    #endregion

    #region BanSync
    public DbSet<BanSyncRecordModel> BanSyncRecords { get; set; }
    public DbSet<BanSyncGuildModel> BanSyncGuilds { get; set; }
    public DbSet<BanSyncGuildSnapshotModel> BanSyncGuildSnapshots { get; set; }
    #endregion

    #region Server Log
    public DbSet<ServerLogChannelModel> ServerLogChannels { get; set; }
    public DbSet<ServerLogGuildModel> ServerLogGuilds { get; set; }
    #endregion

    #region Role Preserve
    public DbSet<RolePreserveGuildModel> RolePreserveGuilds { get; set; }
    public DbSet<RolePreserveUserModel> RolePreserveUsers { get; set; }
    public DbSet<RolePreserveUserRoleModel> RolePreserveUserRoles { get; set; }
    #endregion
    
    public DbSet<GuildApprovalModel> GuildApprovals { get; set; }
    public DbSet<GuildApprovalLogEventModel> GuildApprovalLogEvents { get; set; }

    public DbSet<InteractionStatisticModel> InteractionStatistics { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        #region Partial Snapshots
        builder.Entity<UserPartialSnapshotModel>(b =>
        {
            b.ToTable(UserPartialSnapshotModel.TableName).HasKey(e => e.Id);
            b.HasIndex(e => new
            {
                e.Timestamp,
                e.UserId
            }).IsDescending();
        });
        builder.Entity<GuildPartialSnapshotModel>(b =>
        {
            b.ToTable(GuildPartialSnapshotModel.TableName).HasKey(e => e.Id);
            b.HasIndex(e => new
            {
                e.Timestamp,
                e.GuildId
            }).IsDescending();
        });
        #endregion

        #region Snapshots
        builder.HasPostgresEnum<DiscordSnapshotSource>();
        builder.Entity<GuildSnapshotModel>(b =>
        {
            b.ToTable(GuildSnapshotModel.TableName).HasKey(e => e.RecordId);

            b.HasIndex(e => new
            {
                e.RecordCreatedAt,
                e.GuildId
            }).IsDescending();
        });

        builder.Entity<GuildRoleSnapshotModel>(b =>
        {
            b.ToTable(GuildRoleSnapshotModel.TableName).HasKey(e => e.Id);

            b.HasIndex(e => new
            {
                e.RecordCreatedAt,
                e.GuildId,
                e.RoleId
            }).IsDescending();

            b.HasMany(e => e.Permissions)
             .WithOne()
             .HasForeignKey(e => e.GuildRoleSnapshotId);
        });
        builder.Entity<GuildRolePermissionSnapshotModel>(b =>
        {
            b.ToTable(GuildRolePermissionSnapshotModel.TableName).HasKey(e => e.RecordId);

            b.HasIndex(e => new
            {
                e.RecordCreatedAt,
                e.GuildRoleSnapshotId,
                e.GuildId,
                e.RoleId
            }).IsDescending();
        });
        builder.Entity<GuildMemberSnapshotModel>(b =>
        {
            b.ToTable(GuildMemberSnapshotModel.TableName)
             .HasKey(e => e.RecordId);

            b.HasIndex(e => new
            {
                e.RecordCreatedAt,
                e.UserId,
                e.GuildId
            }).IsDescending();

            b.HasMany(e => e.Roles)
             .WithOne()
             .HasForeignKey(e => e.GuildMemberSnapshotId);
            b.HasMany(e => e.Permissions)
             .WithOne()
             .HasForeignKey(e => e.GuildMemberSnapshotId);
        });
        builder.Entity<GuildMemberPermissionSnapshotModel>(b =>
        {
            b.ToTable(GuildMemberPermissionSnapshotModel.TableName)
             .HasKey(e => e.RecordId);

            b.HasIndex(e => new
            {
                e.RecordCreatedAt,
                e.GuildMemberSnapshotId,
                e.GuildId,
                e.UserId
            }).IsDescending();
        });
        builder.Entity<GuildMemberRoleSnapshotModel>(b =>
        {
            b.ToTable(GuildMemberRoleSnapshotModel.TableName)
             .HasKey(e => e.RecordId);
            
            b.HasIndex(e => new
            {
                e.RecordCreatedAt,
                e.GuildMemberSnapshotId,
                e.GuildId,
                e.UserId
            }).IsDescending();

            b.HasOne(e => e.GuildRoleSnapshot)
             .WithMany()
             .HasForeignKey(e => e.GuildRoleSnapshotId);
        });

        builder.Entity<UserSnapshotModel>(b =>
        {
            b.ToTable(UserSnapshotModel.TableName)
             .HasKey(e => e.RecordId);

            b.HasIndex(e => new
            {
                e.RecordCreatedAt,
                e.UserId
            }).IsDescending();

            b.HasOne(e => e.PrimaryGuild)
             .WithOne()
             .HasForeignKey<UserSnapshotModel>(e => e.PrimaryGuildId);
        });
        builder.Entity<PrimaryGuildSnapshotModel>(b =>
        {
            b.ToTable(PrimaryGuildSnapshotModel.TableName)
             .HasKey(e => e.RecordId);
        });

        builder.Entity<GuildSnapshotEventModel>(b =>
        {
            b.ToTable(GuildSnapshotEventModel.TableName).HasKey(e => e.Id);

            b.HasIndex(e => new
            {
                e.Timestamp,
                e.GuildId
            }).IsDescending();

            b.HasIndex(e => new
            {
                e.GuildId,
                e.Timestamp,
                e.Source
            }).IsDescending();

            b.HasOne(e => e.Before)
             .WithMany()
             .HasForeignKey(e => e.BeforeId);
            b.HasOne(e => e.Current)
             .WithMany()
             .HasForeignKey(e => e.CurrentId)
             .IsRequired();
        });
        #endregion

        #region Discord Cache
        builder.Entity<GuildCacheModel>(b =>
        {
            b.ToTable(GuildCacheModel.TableName).HasKey(e => e.Id);

            b.HasMany(e => e.Members)
            .WithOne(e => e.Guild)
            .HasForeignKey(e => e.GuildId)
            .IsRequired();

            b.HasMany(e => e.Channels)
            .WithOne(e => e.GuildCache)
            .HasForeignKey(e => e.GuildId)
            .IsRequired();
        });
        builder.Entity<GuildChannelCacheModel>(b =>
        {
            b.ToTable(GuildChannelCacheModel.TableName)
            .HasKey(e => e.ChannelId);

            b.HasIndex(e => new
            {
                e.GuildId,
                e.ChannelId
            });
        });
        builder.Entity<GuildMemberCacheModel>(b =>
        {
            b.ToTable(GuildMemberCacheModel.TableName)
            .HasKey(e => new
            {
                e.GuildId,
                e.UserId
            });
            b.HasIndex(e => new
            {
                e.GuildId,
                e.UserId,
                e.IsMember
            });
        });
        builder.Entity<UserCacheModel>(b =>
        {
            b.ToTable(UserCacheModel.TableName).HasKey(e => e.Id);
        });
        builder.AuditLogCacheEntity<AuditLogBanCacheModel>(b =>
        {
            b.ToTable(AuditLogBanCacheModel.TableName);
        });
        #endregion

        #region BanSync
        builder.Entity<BanSyncRecordModel>(b =>
        {
            b.ToTable(BanSyncRecordModel.TableName).HasKey(e => e.Id);

            b.HasIndex(e => new { e.GuildId, e.Ghost }).IsDescending();
            b.HasIndex(e => new { e.UserId, e.Ghost }).IsDescending();
            b.HasIndex(e => e.CreatedAt).IsDescending();

            b.HasOne(e => e.UserPartialSnapshot)
                .WithMany()
                .HasForeignKey(e => e.UserPartialSnapshotId)
                .IsRequired();
            b.HasOne(e => e.BanSyncGuild)
                .WithMany()
                .HasForeignKey(e => e.GuildId)
                .IsRequired();

            b.HasOne(e => e.CachedGuildMember)
                .WithMany()
                .HasForeignKey(e => new { e.GuildId, e.UserId })
                .IsConstrained(false);
        });
        builder.Entity<BanSyncGuildModel>(b =>
        {
            b.ToTable(BanSyncGuildModel.TableName).HasKey(e => e.GuildId);

            b.HasIndex(e => new { e.GuildId, e.State, e.Enable });
        });
        builder.Entity<BanSyncGuildSnapshotModel>(b =>
        {
            b.ToTable(BanSyncGuildSnapshotModel.TableName).HasKey(e => e.Id);
            b.HasIndex(e => new { e.Timestamp, e.GuildId }).IsDescending();
        });
        #endregion

        #region Server Log
        builder.Entity<ServerLogGuildModel>(b =>
        {
            b.ToTable(ServerLogGuildModel.TableName) 
            .HasKey(e => e.GuildId);

            b.HasIndex(e => new
            {
                e.GuildId,
                e.Enabled
            });

            b.HasOne(e => e.GuildCache)
            .WithMany()
            .HasForeignKey(e => e.GuildId);

            b.HasMany(e => e.ServerLogChannels)
            .WithOne()
            .HasForeignKey(e => e.GuildId);
        });
        builder.Entity<ServerLogChannelModel>(b =>
        {
            b.ToTable(ServerLogChannelModel.TableName)
            .HasKey(e => e.Id);

            b.HasIndex(e => new
            {
                e.GuildId,
                e.ChannelId,
                e.Event
            });

            b.HasOne(e => e.GuildCache)
            .WithMany()
            .HasForeignKey(e => e.GuildId);
        });
        #endregion

        #region Role Preserve
        builder.Entity<RolePreserveGuildModel>(b =>
        {
            b.ToTable(RolePreserveGuildModel.TableName).HasKey(e => e.GuildId);

            b.HasMany(e => e.Users)
                .WithOne(e => e.RolePreserveGuild)
                .HasForeignKey(e => e.GuildId)
                .IsRequired();
        });
        builder.Entity<RolePreserveUserModel>(b =>
        {
            b.ToTable(RolePreserveUserModel.TableName).HasKey(e => new { e.GuildId, e.UserId });

            b.HasMany(e => e.Roles)
             .WithOne()
             .HasForeignKey(e => new { e.GuildId, e.UserId })
             .IsRequired();
        });
        builder.Entity<RolePreserveUserRoleModel>(b =>
        {
            b.ToTable(RolePreserveUserRoleModel.TableName).HasKey(e => new
            {
                e.GuildId,
                e.UserId,
                e.RoleId
            });
        });
        #endregion
        
        #region Guild User Approval
        builder.Entity<GuildApprovalModel>(b =>
        {
            b.ToTable(GuildApprovalModel.TableName).HasKey(e => e.GuildId); 

            b.HasIndex(e => new
            {
                e.GuildId,
                e.Enabled
            });
            b.HasIndex(e => new
            {
                e.GuildId,
                e.Enabled,
                e.EnableGreeter
            });
        });
        builder.Entity<GuildApprovalLogEventModel>(b =>
        {
            b.ToTable(GuildApprovalLogEventModel.TableName).HasKey(e => e.Id);

            b.HasIndex(e => new
            {
                e.GuildId,
                e.UserId
            });
        });
        #endregion

        #region Statistics
        builder.Entity<InteractionStatisticModel>(b =>
        {
            b.ToTable(InteractionStatisticModel.TableName).HasKey(e => e.Id);
            b.HasIndex(e => new
            {
                e.InteractionGroup,
                e.InteractionName,
                e.GuildId,
                e.ChannelId,
                e.UserId
            });
        });
        #endregion
        
        builder.HasDbFunction(typeof(XeniaDbContext).GetMethod(nameof(spBanSyncGetMutualRecordsForGuild), [typeof(string)]))
            .HasName("spBanSyncGetMutualRecordsForGuild");
        builder.HasDbFunction(typeof(XeniaDbContext).GetMethod(nameof(spBanSyncGetMutualRecordsForGuild_Paginate), [typeof(string), typeof(int), typeof(int)]))
            .HasName("spBanSyncGetMutualRecordsForGuild_Paginate");
    }

    public IQueryable<BanSyncRecordModel> spBanSyncGetMutualRecordsForGuild(
        string guildId)
        => FromExpression(() => spBanSyncGetMutualRecordsForGuild(guildId));
    public IQueryable<BanSyncRecordModel> spBanSyncGetMutualRecordsForGuild_Paginate(
        string guildId,
        int page,
        int pageSize = 50)
        => FromExpression(() => spBanSyncGetMutualRecordsForGuild_Paginate(guildId, page, pageSize));
}
