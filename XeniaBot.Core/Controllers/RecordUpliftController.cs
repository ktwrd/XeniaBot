using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Shared;

namespace XeniaBot.Core.Controllers;

public class RecordUpliftController : BaseController
{
    public RecordUpliftController(IServiceProvider services)
        : base(services)
    {}

    public override async Task OnReadyDelay()
    {
        await UpliftController(_services.GetRequiredService<BanSyncInfoConfigController>());
    }
    
    public async Task UpliftController(BanSyncInfoConfigController controller)
    {
        var records = await controller.GetAll();
        var count = 0;
        foreach (var item in records)
        {
            if (string.IsNullOrEmpty(item.RecordId) || string.IsNullOrWhiteSpace(item.RecordId))
            {
                item.RecordId = Guid.NewGuid().ToString();
                await controller.SetInfo(item, true);
                count++;
            }
        }
        Log.Debug($"BanSyncInfoConfigController - {count} records");
    }
}