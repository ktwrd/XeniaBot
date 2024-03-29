using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared;

namespace XeniaBot.Data.Services;

[XeniaController]
public class XeniaVersionService : BaseService
{
    private readonly ProgramDetails _details;
    private readonly XeniaVersionRepository _repo;
    private readonly IMongoDatabase _mongoDb;
    public XeniaVersionService(IServiceProvider services)
        : base(services)
    {
        Priority = 11;
        _details = services.GetRequiredService<ProgramDetails>();
        _repo = services.GetRequiredService<XeniaVersionRepository>();
        _mongoDb = services.GetRequiredService<IMongoDatabase>();
    }

    public override async Task InitializeAsync()
    {
        var calling = Assembly.GetEntryAssembly();
        var currentModel = new XeniaVersionModel()
        {
            ParsedVersionTimestamp = new DateTimeOffset(_details.VersionDate).ToUnixTimeSeconds(),
            Version = _details.Version,
            Name = calling.FullName?.Split(",")[0] ?? ""
        };
        if (currentModel.Version.StartsWith("1.10."))
        {
            currentModel.Flags.TryAdd("idColumnType", "ObjectId");
        }
        else if (currentModel.Version.StartsWith("1.11.0"))
        {
            currentModel.Flags.TryAdd("idColumnType", "Guid");
        }
        if (currentModel.Name.Length < 1)
            throw new Exception("Name for executing assembly is empty?");
        await _repo.Insert(currentModel);
    }

    private async Task UpgradeObjectIdToGuid()
    {
        
    }
}