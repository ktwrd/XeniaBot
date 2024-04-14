using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models;

public interface ICountingViewModel
{
    public CounterGuildModel CounterConfig { get; set; }
}