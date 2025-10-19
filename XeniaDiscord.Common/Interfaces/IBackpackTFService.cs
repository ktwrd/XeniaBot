using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaDiscord.Common;

public interface IBackpackTFService
{
    public string ApiKey { get; }
    public Task<Services.BackpackTFCurrencyResult?> GetCurrenciesAsync();
}
