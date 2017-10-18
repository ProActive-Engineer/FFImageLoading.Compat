﻿using System;
using System.Threading.Tasks;
using Foundation;
using CoreFoundation;

namespace FFImageLoading.Helpers
{
    public class MainThreadDispatcher : IMainThreadDispatcher
    {
        public void Post(Action action)
        {
            if (NSThread.Current.IsMainThread)
            {
                action?.Invoke();
            }
            else
            {
                DispatchQueue.MainQueue.DispatchSync(action);
            }
        }

        public Task PostAsync(Action action)
        {
            var tcs = new TaskCompletionSource<object>();

            if (NSThread.Current.IsMainThread)
            {
                try
                {
                    action?.Invoke();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }
            else
            {
                DispatchQueue.MainQueue.DispatchAsync(() =>
                {
                    try
                    {
                        action?.Invoke();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
            }

            return tcs.Task;
        }

        public async Task PostAsync(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<object>();

            if (NSThread.Current.IsMainThread)
            {
                try
                {
                    await action?.Invoke();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }
            else
            {
                DispatchQueue.MainQueue.DispatchAsync(async () =>
                {
                    try
                    {
                        await action?.Invoke();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
            }

            await tcs.Task;
        }
    }
}
