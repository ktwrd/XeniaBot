const fs = require('fs');
const inputContent = fs.readFileSync('generateDiscordStatisticsEvents.txt').toString().replaceAll('\r\n', '\n').split('\n').filter(e => e.trim().length > 0);

const eventAdders = [];
const eventRemovers = [];
const eventHandlerFunctions = [];

for (const line of inputContent) {
    const lineSepIdx = line.indexOf('|');
    if (lineSepIdx < 0) {
        console.log('skipped: ' + line);
        continue;
    }
    const name = line.substring(0, lineSepIdx);
    const paramsTxt = line.substring(lineSepIdx + 1);
    const paramsSplitChar = paramsTxt.includes('<') || paramsTxt.includes(';') ? ';' : ',';
    const params = paramsTxt.split(paramsSplitChar).map((e, i) => {
        const nameIdx = e.indexOf('`');
        let argType = e;
        let argName = `arg${i + 1}`;
        if (nameIdx > 0) {
            argType = e.substring(0, nameIdx);
            argName = e.substring(nameIdx + 1);
        }
        return {
            type: argType,
            name: argName
        };
    });
    const funcName = `ClientIncOn${name}`;
    eventAdders.push(`_client.${name} += ${funcName};`);
    eventRemovers.push(`_client.${name} -= ${funcName};`);

    const args = params.map(e => `${e.type} ${e.name}`).join(', ');
    eventHandlerFunctions.push([
        `private Task ${funcName}(${args})`,
        `{`,
        `    IncreaseEvent(DiscordStatisticsEventType.${name});`,
        `    return Task.CompletedTask;`,
        `}`
    ]);
}

const eventAddersText = eventAdders.map(e => ''.padStart(4, ' ') + e).join('\n');
const eventRemoversText = eventRemovers.map(e => ''.padStart(4, ' ') + e).join('\n');
const initEventContent = `protected void InitializeIncreaseEvents()
{
${eventAddersText}
}

protected void ShutdownIncreaseEvents()
{
${eventRemoversText}
}
`.split('\n').map(e => ''.padStart(4, ' ') + e).join('\n');
const eventHandlerContent = eventHandlerFunctions.map(e => e.join('\n').split('\n').map(x => ''.padStart(4, ' ') + x).join('\n')).join('\n');

fs.writeFileSync('DiscordStatisticsService.IncreaseEvents.Generated.cs', `using Discord;
using Discord.Rest;
using Discord.WebSocket;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaDiscord.Common.Services;

partial class DiscordStatisticsService
{
${initEventContent}

${eventHandlerContent}
}`);