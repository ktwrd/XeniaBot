using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace XeniaBot.Shared.Helpers;

public static class ExceptionHelper
{
    /// <summary>
    /// Retry <paramref name="callback"/> multiple times (defined by <paramref name="count"/>) if the exception that's thrown is assumed to be a timeout exception (detected by <see cref="IsTimedOut"/>)
    /// </summary>
    /// <param name="callback">Method to call. Will be invoked multiple times, no more than the amount of times defined in <paramref name="count"/></param>
    /// <param name="count">Amount of retries before giving up and throwing <see cref="AggregateException"/> with all the exceptions that were thrown by <paramref name="callback"/>.
    /// If this is &lt;1, then it'll be reset to <c>1</c></param>
    /// <exception cref="AggregateException">
    /// Thrown if the amount of retries exceeds the <paramref name="count"/> defined, but if <paramref name="count"/> is <c>1</c>, then it'll re-throw the captured exception type.
    /// </exception>
    /// <remarks>
    /// If <paramref name="count"/> is set to <c>1</c> and it fails, then the exception that was captured will be re-thrown instead of being wrapped in an <see cref="ArgumentException"/>
    /// </remarks>
    public static async Task RetryOnTimedOut(Func<Task> callback, int count = 3)
    {
        count = Math.Max(1, count);
        var exceptions = new List<Exception>(count);
        for (int i = 0; i <= count; i++)
        {
            try
            {
                await callback();
                return;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                if (IsTimedOut(ex) && i < 3) continue;
                // just rethrow if this is the only exception, otherwise throw AggregateException
                if (exceptions.Count == 1) throw;
                throw new AggregateException(exceptions);
            }
        }
    }

    /// <returns>
    /// Result data from a successful attempt of calling the <paramref name="callback"/> provided.
    /// </returns>
    /// <inheritdoc cref="RetryOnTimedOut(Func{Task}, int)"/>
    public static async Task<TResult> RetryOnTimedOut<TResult>(Func<Task<TResult>> callback, int count = 3)
    {
        count = Math.Max(1, count);
        var exceptions = new List<Exception>(count);
        for (int i = 0; i <= count; i++)
        {
            try
            {
                return await callback();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                if (IsTimedOut(ex) && i < 3) continue;
                // just rethrow if this is the only exception, otherwise throw AggregateException
                if (exceptions.Count == 1) throw;
                throw new AggregateException(exceptions);
            }
        }
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="RetryOnTimedOut(Func{Task}, int)"/>
    public static void RetryOnTimedOut(Action callback, int count = 3)
    {
        RetryOnTimedOut(InnerCallback, count).GetAwaiter().GetResult();
        return;

        Task InnerCallback()
        {
            callback();
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc cref="RetryOnTimedOut{TResult}(Func{Task{TResult}}, int)"/>
    public static TResult RetryOnTimedOut<TResult>(Func<TResult> callback, int count = 3)
    {
        return RetryOnTimedOut(InnerCallback, count).GetAwaiter().GetResult();

        Task<TResult> InnerCallback()
        {
            var result = callback();
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Is the exception provided assumed to be a timeout exception?
    /// Checks if it's <see cref="TimeoutException"/>, then if it's <see cref="TaskCanceledException"/> and the inner exception is <see cref="TimeoutException"/>
    /// or if the exception as a string matches the following regular expression: <c>time(?:d)?\s?out</c>
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    public static bool IsTimedOut(Exception exception)
    {
        var sc = StringComparison.OrdinalIgnoreCase;
        switch (exception)
        {
            case TimeoutException:
            case TaskCanceledException { InnerException: TimeoutException }:
            case GatewayReconnectException:
                return true;
            default:
            {
                var exceptionStr = exception.ToString();
                return exceptionStr.Contains("timed out", sc)
                       || exceptionStr.Contains("time out", sc)
                       || exceptionStr.Contains("timeout", sc)
                       || exceptionStr.Contains("error 503", sc)
                       || exceptionStr.Contains("service unavailable", sc)
                       || (exceptionStr.Contains("502", sc) && (exceptionStr.Contains("bad gateway", sc) || exceptionStr.Contains("badgateway", sc)))
                       || exceptionStr.Contains("unable to connect to the remote server", sc)
                       || exceptionStr.Contains("connection was closed", sc);
            }
        }
    }
}