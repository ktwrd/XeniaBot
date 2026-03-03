using XeniaBot.MongoData.Models;

namespace XeniaBot.WebPanel.Models;

public interface ICountingViewModel
{
    public CounterGuildModel CounterConfig { get; set; }
}