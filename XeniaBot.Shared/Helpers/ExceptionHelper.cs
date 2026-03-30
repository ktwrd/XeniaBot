using System;
using System.Threading.Tasks;

namespace XeniaBot.Shared.Helpers;

public static class ExceptionHelper
{
    public static async Task RetryOnTimedOut(Func<Task> callback, int count = 3)
    {
        for (int i = 0; i <= count; i++)
        {
            try
            {
                await callback();
                return;
            }
            catch (Exception ex)
            {
                var exStr = ex.ToString();
                if (!exStr.Contains("timed out", StringComparison.OrdinalIgnoreCase) || i >= 3)
                {
                    throw;
                }
            }
        }
    }
}