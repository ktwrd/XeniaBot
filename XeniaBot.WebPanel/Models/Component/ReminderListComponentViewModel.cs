using System.Threading.Tasks;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel.Models;

public class ReminderListComponentViewModel : BasePagination<ReminderModel>
{
    public async Task PopulateModel(ReminderRepository repo, ulong userId, int cursor)
    {
        var d = await repo.GetByUserPaginate(userId, cursor, PageSize);
        Items = d ?? [];
        Cursor = cursor;
    }
}