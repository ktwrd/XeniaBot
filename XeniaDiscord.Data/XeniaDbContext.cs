using Microsoft.EntityFrameworkCore;
using XeniaDiscord.Data.Models;
using XeniaDiscord.Data.Models.BanSync;

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

    public DbSet<UserPartialSnapshotModel> UserPartialSnapshots { get; set; }

    public DbSet<BanSyncRecordModel> BanSyncRecords { get; set; }
    public DbSet<BanSyncGuildModel> BanSyncGuilds { get; set; }
    public DbSet<BanSyncGuildSnapshotModel> BanSyncGuildSnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserPartialSnapshotModel>(b =>
        {
            b.ToTable(UserPartialSnapshotModel.TableName).HasKey(e => e.Id);
            b.HasIndex(e => new
            {
                e.CreatedAt,
                e.UserId
            }).IsDescending();
        });

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
    }
}
