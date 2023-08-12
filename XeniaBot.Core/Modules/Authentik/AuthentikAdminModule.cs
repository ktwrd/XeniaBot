using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;

namespace XeniaBot.Core.Modules;


[Group("auth", "Authentik Administration")]
[RequireOwner]
public partial class AuthentikAdminModule : InteractionModuleBase
{
}