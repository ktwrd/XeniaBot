using System.Threading.Tasks;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Helpers;

namespace XeniaBot.WebPanel.Models;

public class ReminderListComponentViewModel : BasePagination<ReminderModel>
{
    public async Task PopulateModel(ulong userId, int cursor)
    {
        var repo = CoreContext.Instance!.GetRequiredService<ReminderRepository>();
        var d = await repo.GetByUserPaginate(userId, cursor, PageSize);
        Items = d ?? [];
        Cursor = cursor;
    }
}