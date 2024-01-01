using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using XeniaBot.Data.Models;

namespace XeniaBot.Data.DbContexts;

public class UserConfigDbContext : DbContext
{
    public DbSet<UserConfigModel> Users { get; init; }

    public static UserConfigDbContext Create(IMongoDatabase database)
    {
        var opts = new DbContextOptionsBuilder<UserConfigDbContext>()
            .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
            .Options;
        return new UserConfigDbContext(opts);
    }
    
    public UserConfigDbContext(DbContextOptions options)
        : base(options)
    {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserConfigModel>().ToCollection(UserConfigModel.CollectionName);
    }
}