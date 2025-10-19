using Microsoft.EntityFrameworkCore;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Models.Confession;
using XeniaDiscord.Data.Models.Ticket;

namespace XeniaDiscord.Data;

public class ApplicationDbContext : DbContext
{
    private readonly DbContextOptions<ApplicationDbContext> _ops;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        _ops = options;
    }
    public ApplicationDbContext CreateSession()
    {
        return new(_ops);
    }


    public DbSet<BanSyncGuildModel> BanSyncGuilds { get; set; }
    public DbSet<BanSyncGuildSnapshotModel> BanSyncGuildSnapshots { get; set; }
    public DbSet<BanSyncRecordModel> BanSyncRecords { get; set; }

    public DbSet<GuildTicketModel> GuildTickets { get; set; }
    public DbSet<GuildTicketConfigModel> GuildTicketConfigs { get; set; }


    public DbSet<GuildConfessionModel> GuildConfessions { get; set; }
    public DbSet<GuildConfessionConfigModel> GuildConfessionConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        #region Bansync
        builder.Entity<BanSyncGuildModel>(b =>
        {
            b.ToTable(BanSyncGuildModel.TableName).HasKey(e => e.Id);
        });
        builder.Entity<BanSyncGuildSnapshotModel>(b =>
        {
            b.ToTable(BanSyncGuildSnapshotModel.TableName).HasKey(e => e.Id);
        });
        builder.Entity<BanSyncRecordModel>(b =>
        {
            b.ToTable(BanSyncRecordModel.TableName).HasKey(e => e.Id);

            b.HasIndex(e => e.UserId).IsUnique(false);
            b.HasIndex(e => e.GuildId).IsUnique(false);
            b.HasIndex(e => e.CreatedAt).IsUnique(false).IsDescending(true);
            b.HasIndex(e => e.CreatedByUserId).IsUnique(false);
        });
        #endregion

        #region Tickets
        builder.Entity<GuildTicketModel>(b =>
        {
            b.ToTable(GuildTicketModel.TableName).HasKey(e => e.Id);
        });
        builder.Entity<GuildTicketConfigModel>(b =>
        {
            b.ToTable(GuildConfessionConfigModel.TableName).HasKey(e => e.Id);
        });
        #endregion

        #region Confession
        builder.Entity<GuildConfessionConfigModel>(b =>
        {
            b.ToTable(GuildConfessionConfigModel.TableName).HasKey(e => e.Id);

            b.HasIndex(e => e.CreatedByUserId).IsUnique(false);
            b.HasIndex(e => e.CreatedAt).IsUnique(false);
        });
        builder.Entity<GuildConfessionModel>(b =>
        {
            b.ToTable(GuildConfessionModel.TableName).HasKey(e => e.Id);

            b.HasIndex(e => e.GuildId).IsUnique(false);
            b.HasIndex(e => e.CreatedAt).IsUnique(false).IsDescending(true);

            b.HasOne<GuildConfessionConfigModel>(e => e.GuildConfessionConfig)
                .WithMany()
                .HasForeignKey(e => e.GuildConfessionConfigId)
                .IsRequired(false);
        });
        #endregion
    }
}
