﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared.Services;

namespace XeniaBot.Core.Services.BotAdditions
{
    [XeniaController]
    public class CounterService : BaseService
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordService _discord;
        private readonly CounterConfigRepository _config;
        public CounterService(IServiceProvider services)
            : base(services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _discord = services.GetRequiredService<DiscordService>();
            _config = services.GetRequiredService<CounterConfigRepository>();
        }
        public override Task InitializeAsync()
        {
            _discord.MessageReceived += _discord_MessageReceived;

            return Task.CompletedTask;
        }
        public override async Task OnReady()
        {
            foreach (var item in await _config.GetAll())
            {
                _config.CachedItems.Add(item.ChannelId, item.Count);
            }
        }

        private Task _discord_MessageReceived(SocketMessage arg)
        {
            new Thread((ThreadStart)async delegate
            {
                try
                {
                    await DiscordMessageReceived(arg);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to run {nameof(DiscordMessageReceived)}\n{ex}");
                }
            }).Start();
            return Task.CompletedTask;
        }
        private async Task DiscordMessageReceived(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message))
                return;
            if (message.Source != MessageSource.User)
                return;
            if (!_config.CachedItems.ContainsKey(arg.Channel.Id))
                return;

            // Try and parse the content as a ulong, if we fail
            // then we delete the message. FormatException is
            // thrown when we fail to parse as a ulong.
            ulong value = 0;
            try
            {
                value = ulong.Parse(arg.Content);
            }
            catch (FormatException)
            {
                await DiscordHelper.DeleteMessage(_client, arg);
                return;
            }

            // If number is not the next number, then we delete the message.
            var context = new SocketCommandContext(_client, message);
            ulong targetValue = value + 1;
            if (value != targetValue)
            {
                await DiscordHelper.DeleteMessage(_client, arg);
                return;
            }

            // Update record
            CounterGuildModel data = await _config.Get(context.Guild, context.Channel);
            data.Count = value;
            await _config.Set(data);
        }
    }
}
