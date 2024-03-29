using System.Diagnostics;
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
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var currentModel = new XeniaVersionModel()
        {
            ParsedVersionTimestamp = new DateTimeOffset(_details.VersionDate).ToUnixTimeSeconds(),
            Version = _details.Version,
            Name = calling.FullName?.Split(",")[0] ?? ""
        };
        currentModel.FillAssemblies(allAssemblies);
        SetFlags(currentModel);
        
        if (currentModel.Name.Length < 1)
            throw new Exception("Name for executing assembly is empty?");
        await _repo.Insert(currentModel);
        var previousModel = await _repo.GetPrevious(currentModel.Id);
        if (currentModel.Name.StartsWith("XeniaBot.Core"))
            await InitializeAsync_Bot(currentModel, previousModel);
    }

    /// <summary>
    /// Set <see cref="XeniaVersionModel.Flags"/> based off what version is being used.
    /// </summary>
    private void SetFlags(XeniaVersionModel model)
    {
        var version = new Version(model.Version);
        if (version.Major <= 1 && version.Minor <= 10)
        {
            model.Flags.TryAdd("idColumnType", "ObjectId");
        }
        else
        {
            model.Flags.TryAdd("idColumnType", "Guid");
        }
    }
    private async Task InitializeAsync_Bot(XeniaVersionModel currentModel, XeniaVersionModel? previousModel)
    {
        if (currentModel.Flags.TryGetValue("idColumnType", out var currentColumnType) &&
            currentColumnType.ToString() == "Guid")
        {
            if ((previousModel?.Flags.TryGetValue("idColumnType", out var previousColumnType) ?? false) &&
                previousColumnType.ToString() == "ObjectId")
            {
                await UpgradeObjectIdToGuid();
            }
        }
    }

    private async Task UpgradeObjectIdToGuid()
    {
        // TODO
    }
}