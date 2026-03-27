using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using XeniaDiscord.Data.Extensions;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Models.PartialSnapshot;

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

    #region Discord Cache
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

        #region Discord Cache
        builder.Entity<GuildCacheModel>(b =>
        {
            b.ToTable(GuildCacheModel.TableName).HasKey(e => e.Id);

            b.HasMany(e => e.Members)
            .WithOne(e => e.Guild)
            .HasForeignKey(e => e.GuildId)
            .IsRequired();
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
