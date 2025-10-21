using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Common.Interfaces;

public interface IBanSyncRepository
{
    public Task<bool> AnyAsync(Discord.IGuild guild, Discord.IBan ban);
    public Task<bool> AnyAsync(Discord.IGuild guild, Discord.IUser user);
    public Task<ICollection<BanSyncRecordModel>> GetAllForUser(Discord.IUser user, int? limit = null);
    public Task<long> GetCountForUser(Discord.IUser user);
    public Task<BanSyncRecordModel> CreateAsync(Discord.IGuild guild, Discord.IBan ban);
}
