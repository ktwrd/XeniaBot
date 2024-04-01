using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Data.Services;

[XeniaController]
public class XeniaVersionService : BaseService
{
    private readonly ProgramDetails _details;
    private readonly ConfigData _configData;
    private readonly XeniaVersionRepository _repo;
    private readonly IMongoDatabase _mongoDb;
    public XeniaVersionService(IServiceProvider services)
        : base(services)
    {
        Priority = 11;
        _configData = services.GetRequiredService<ConfigData>();
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
        
        if (_configData.IsUpgradeAgent)
        {
            Log.Debug("Initializing Upgrades");
            await InitializeUpgrade(currentModel, previousModel);
        }
        else
        {
            Log.Warn("Ignoring DB Upgrade (Config.IsUpgradeAgent is false)");
        }
    }

    /// <summary>
    /// Set <see cref="XeniaVersionModel.Flags"/> based off what version is being used.
    /// </summary>
    private void SetFlags(XeniaVersionModel model)
    {
        // Insert flags safely into model.Flags
        void InsertFlags((string, object) pair)
        {
            var (key, value) = pair;
            model.Flags.TryAdd(key, value);
            model.Flags[key] = value;
        }

        foreach (var item in model.Assemblies)
        {
            switch (item.Name)
            {
                case "XeniaBot.Shared":
                    SetFlags_XeniaShared(model, item).ForEach(InsertFlags);
                    break;
            }
        }
    }

    /// <summary>
    /// Set flags for the XeniaBot.Shared project.
    /// </summary>
    private List<(string, object)> SetFlags_XeniaShared(XeniaVersionModel model, XeniaVersionModel.XeniaVersionAssemblyItem asm)
    {
        var result = new List<(string, object)>();
        var version = new Version(asm.Version);
        
        // versions <2 uses ObjectId for the primary key
        result.Add(("idColumnType", version.Major < 2
                ? "ObjectId"
                : "Guid"));

        return result;
    }

    /// <summary>
    /// Initialize Database Upgrades.
    /// </summary>
    private async Task InitializeUpgrade(XeniaVersionModel currentModel, XeniaVersionModel? previousModel)
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

    /// <summary>
    /// <para><b>Change was made in Shared v2.0</b></para>
    /// 
    /// <para>Replace all `_id` fields with a Guid instead of an ObjectId in all collections in the current database</para>
    /// </summary>
    private async Task UpgradeObjectIdToGuid()
    {
        if (!_configData.IsUpgradeAgent)
        {
            Log.Warn($"Not Upgrade Agent. Ignoring");
            return;
        }
        Log.WriteLine("Converting the type of _id in all documents from ObjectId to Guid");
        // TODO
    }
}