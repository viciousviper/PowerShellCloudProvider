/*
The MIT License(MIT)

Copyright(c) 2015 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using IgorSoft.PowerShellCloudProvider.Interface.IO;

namespace IgorSoft.PowerShellCloudProvider
{
    /// <summary>
    /// Thread-affine implementation of <see cref="IProgress{T}"/>.
    /// </summary>
    internal sealed class ProgressProxy : IProgress<ProgressValue>
    {
        public delegate Task<T> ProgressFunc<T>(IProgress<ProgressValue> progress);

        private sealed class CurrentThreadSynchronizationContext : SynchronizationContext, IDisposable
        {
            private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> queue = new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

            public override void Post(SendOrPostCallback d, object state)
            {
                queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
            }

            public void RunOnCurrentThread()
            {
                var workItem = default(KeyValuePair<SendOrPostCallback, object>);
                while (queue.TryTake(out workItem, Timeout.Infinite))
                    workItem.Key(workItem.Value);
            }

            public void Complete() { queue.CompleteAdding(); }

            public void Dispose()
            {
                queue.Dispose();
            }
        }

        private IProviderContext providerContext;

        private string activity;

        private string target;

        private Progress<ProgressValue> progress;

        public ProgressProxy(IProviderContext providerContext, string activity, string target)
        {
            if (providerContext == null)
                throw new ArgumentNullException(nameof(providerContext));

            this.providerContext = providerContext;
            this.activity = activity;
            this.target = target;
        }

        private static string FormatBytes(decimal bytes)
        {
            var units = new[] { "b", "Kb", "Mb", "Gb", "Tb" };

            for (int i = 0;; ++i)
                if (bytes < 1024 || i == units.Length)
                    return bytes.ToString("###0.##", CultureInfo.CurrentCulture) + units[i];
                else
                    bytes /= 1024;
        }

        public void Report(ProgressValue value)
        {
            providerContext.WriteProgress(new ProgressRecord(0, activity, target) {
                CurrentOperation = string.Format(CultureInfo.CurrentCulture, Resources.ProgressTransferring, FormatBytes(value.BytesTransferred), FormatBytes(value.BytesTotal)), PercentComplete = value.PercentCompleted
            });
        }

        public static T TraceProgressOn<T>(ProgressFunc<T> func, ProgressProxy proxy)
        {
            var previousContext = SynchronizationContext.Current;
            try {
                var syncContext = new CurrentThreadSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(syncContext);

                proxy.progress = new Progress<ProgressValue>(v => proxy.Report(v));

                var task = Task.Run(() => func(proxy.progress));
                task.ContinueWith(delegate { syncContext.Complete(); }, TaskScheduler.Default);

                syncContext.RunOnCurrentThread();

                return task.GetAwaiter().GetResult();
            } finally {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }
    }
}
