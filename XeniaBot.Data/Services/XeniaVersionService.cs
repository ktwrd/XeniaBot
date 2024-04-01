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
        
        if (_configData.IsUpgradeAgent && currentModel.Name.StartsWith("XeniaBot.Core"))
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

        await UpgradeReminder_MillisecondsToSeconds(currentModel, previousModel);
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

    /// <summary>
    /// <para><b>Change was made in Data v1.1</b></para>
    /// 
    /// <para>Convert all of <see cref="ReminderModel.CreatedAt"/> and <see cref="ReminderModel.RemindedAt"/> from Milliseconds to Seconds</para>
    /// </summary>
    public async Task UpgradeReminder_MillisecondsToSeconds(XeniaVersionModel currentModel, XeniaVersionModel previousModel)
    {
        if (previousModel == null)
        {
            Log.Warn("Ignoring. Previous model is null");
            return;
        }
        var currentDataAsm = currentModel?.GetAssemblyByName("XeniaBot.Data");
        var previousDataAsm = previousModel?.GetAssemblyByName("XeniaBot.Data");
        if (currentDataAsm == null)
            throw new Exception($"Couldn't get XeniaBot.Data version info from {nameof(currentModel)}");
        if (previousDataAsm == null)
            throw new Exception($"Couldn't get XeniaBot.Data version info from {nameof(previousModel)}");

        var changedAtVersion = new Version("1.1.0.0");
        
        var parsedCurrentVersion = new Version(currentDataAsm.Version);
        var parsedPreviousVersion = new Version(previousDataAsm.Version);

        // Ignore when current version is <1.1
        if (parsedCurrentVersion < changedAtVersion)
        {
            Log.Warn("Not Eligible. Current version is <1.1");
            return;
        }

        // Ignore when previous version is >=1.1
        if (parsedPreviousVersion >= changedAtVersion)
        {
            Log.Warn("Not Eligible. Previous version is >=1.1");
            return;
        }

        var reminderRepo = _services.GetRequiredService<ReminderRepository>();
        if (reminderRepo == null)
            throw new NoNullAllowedException("ReminderRepository is null");

        Log.WriteLine($"Running Upgrade (current {parsedCurrentVersion}, previous {parsedPreviousVersion}");
        
        var start = DateTimeOffset.UtcNow;
        
        var allItems = (await reminderRepo.GetAll()).ToList();
        
        var taskBuckets = new List<Task>[4];
        for (int i = 0; i < taskBuckets.Length; i++)
            taskBuckets[i] = new List<Task>();

        // remove last 3 char of timestamp (the millisecond part)
        long adjustToSeconds(long value)
        {
            var s = value.ToString();
            if (s.Length > 3)
                s = s.Substring(0, s.Length - 3);
            else
                s = "0";
            return long.Parse(s);
        }
        
        for (int i = 0; i < allItems.Count; i++)
        {
            allItems[i].CreatedAt = adjustToSeconds(allItems[i].CreatedAt);
            allItems[i].RemindedAt = adjustToSeconds(allItems[i].RemindedAt);
        }

        // set the new model in 4 task lists
        for (int i = 0; i < allItems.Count; i++)
        {
            var bucket = i % 4;
            var index = i;
            taskBuckets[bucket].Add(new Task(
                delegate
                {
                    reminderRepo.Set(allItems[index]).Wait();
                }));
        }

        // run all tasks in 4 threads
        // each thread processes it's tasks synchronously
        // then waits for those 4 threads to finish.
        var taskList = new List<Task>();
        for (int i = 0; i < taskBuckets.Length; i++)
        {
            var index = i;
            taskList.Add(new Task(delegate
            {
                var bucketStart = DateTimeOffset.UtcNow;
                foreach (var item in taskBuckets[index])
                {
                    item.Start();
                    item.Wait();
                };
                Log.Debug($"bucket[{index}] took {XeniaHelper.FormatDuration(bucketStart)}");
            }));
        }

        foreach (var i in taskList)
            i.Start();
        await Task.WhenAll(taskList);
        Log.Debug($"Upgrade took {XeniaHelper.FormatDuration(start)}");
    }
}