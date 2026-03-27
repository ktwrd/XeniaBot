using XeniaBot.MongoData.Models;

namespace XeniaBot.WebPanel.Models;

public interface IConfessionViewModel
{
    public ConfessionGuildModel ConfessionModel { get; set; }
}