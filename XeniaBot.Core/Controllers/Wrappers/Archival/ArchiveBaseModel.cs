using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text.Json;
using System.Threading.Tasks;
using XeniaBot.Core.Helpers;
using XeniaBot.Core.Models;

namespace XeniaBot.Core.Controllers.Wrappers.Archival;

public class ArchiveBaseModel : BaseModel
{
    public ulong Snowflake;
}
