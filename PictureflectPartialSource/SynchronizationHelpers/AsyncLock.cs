using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureflectPartialSource.SynchronizationHelpers {

    /**
     * This is a non-re-entrant (which is actually good) lock to be used like a normal lock as follows.
     * ```
     * using(await customAsyncLockInstance.LockAsync()){
     *     //execute lock-protected code here
     * }
     * ```
     */
    public class AsyncLock {

        object lockObject = new object();
        Task ongoingTask = null;

        public async Task<IDisposable> LockAsync() {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            Task waitTask = null;
            lock (lockObject) {
                waitTask = ongoingTask;
                ongoingTask = tcs.Task;
            }
            if (waitTask != null) {
                await waitTask;
            }
            var result = new CustomAsyncLockResult(tcs);
            return result;
        }

        private class CustomAsyncLockResult : IDisposable {
            TaskCompletionSource<bool> taskCompletionSource;
            public CustomAsyncLockResult(TaskCompletionSource<bool> taskCompletionSource) {
                this.taskCompletionSource = taskCompletionSource;
            }
            public void Dispose() {
                taskCompletionSource.TrySetResult(true);
            }
        }

    }

}
