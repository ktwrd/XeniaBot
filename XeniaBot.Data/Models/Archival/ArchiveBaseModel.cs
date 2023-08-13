using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text.Json;
using System.Threading.Tasks;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models.Archival;

public class ArchiveBaseModel : BaseModel
{
    public ulong Snowflake;
}
