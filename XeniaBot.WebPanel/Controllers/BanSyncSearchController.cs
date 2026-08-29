using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaBot.WebPanel.Models.BanSync;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Repositories;
// ReSharper disable AsyncMethodWithoutAwait

namespace XeniaBot.WebPanel.Controllers;

[Controller]
[Route("~/BanSync/Search")]
public class BanSyncSearchController : BaseXeniaController
{
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;
    private readonly BanSyncGuildRepository _bansyncGuildRepo;
    private readonly BanSyncRecordRepository _bansyncRecordRepo;
    private readonly GuildCacheService _guildCacheService;
    private readonly UserCacheService _userCacheService;
    public BanSyncSearchController(IServiceProvider services)
    {
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        _bansyncGuildRepo = services.GetRequiredService<BanSyncGuildRepository>();
        _bansyncRecordRepo = services.GetRequiredService<BanSyncRecordRepository>();
        _guildCacheService = services.GetRequiredService<GuildCacheService>();
        _userCacheService = services.GetRequiredService<UserCacheService>();
    }
    public const int MaxPageSize = 25;

}
