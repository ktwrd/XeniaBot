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
    public static async Task<TResult> RetryOnTimedOut<TResult>(Func<Task<TResult>> callback, int count = 3)
    {
        for (int i = 0; i <= count; i++)
        {
            try
            {
                return await callback();
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
        throw new NotImplementedException();
    }
}