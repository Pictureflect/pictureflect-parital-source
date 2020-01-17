using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BasicPhotoViewer {

    //This class is threadsafe. The Render handler runs on a different thread.
    public class CustomRenderLoop {

        readonly object stateLock = new object();
        CustomRenderLoopState state = CustomRenderLoopState.Stopped; //Is accessed from render loop thread as well as main.
        public CustomRenderLoopState State {
            get { lock (stateLock) { return state; } }
            private set { lock (stateLock) { state = value; } }
        }

        public event Action Render; //Is fired on the render loop thread.
        public event Action LoopExiting; //Is fired on the render loop thread.
        public event Action Stopped; //Will not fire if already stopped. Is fired on the main thread.

        TaskCompletionSource<bool> loopTaskSource = null;
        ManualResetEvent loopWaitEvent = null; //Only create and destroy this when the loop is not running.
        ManualResetEvent LoopWaitEvent { get { lock (stateLock) { return loopWaitEvent; } } } //Is accessed from render loop thread as was as main. Only create and destroy this when the loop is not running.
        bool initialLoopWaitEventState = false;
        bool restart = false;

        public async void Start() {
            lock (stateLock) {
                if (state == CustomRenderLoopState.Running) {
                    return;
                }
                if (state == CustomRenderLoopState.Stopping) {
                    restart = true;
                    return;
                }
                restart = false;
                if (loopWaitEvent == null) {
                    loopWaitEvent = new ManualResetEvent(initialLoopWaitEventState);
                }
                state = CustomRenderLoopState.Running;
                loopTaskSource = new TaskCompletionSource<bool>();
            }
            try {
                await Task.Factory.StartNew(() => LoopHandler(), CancellationToken.None, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning, TaskScheduler.Default);
            } finally {
                TaskCompletionSource<bool> localLoopTaskSource = null;
                lock (stateLock) {
                    localLoopTaskSource = loopTaskSource;
                    loopTaskSource = null;
                }
                localLoopTaskSource?.TrySetResult(true);
            }
        }

        public async void Stop() { //Note that it is essential to call Stop to ensure proper disposal
            Task localLoopTask = null;
            lock (stateLock) {
                restart = false;
                if (State == CustomRenderLoopState.Stopped || State == CustomRenderLoopState.Stopping) {
                    return;
                }
                State = CustomRenderLoopState.Stopping;
                Invalidate();
                localLoopTask = loopTaskSource?.Task;
            }
            if (localLoopTask != null) {
                await localLoopTask;
            }
            var localRestart = false;
            lock (stateLock) {
                if (loopWaitEvent != null) {
                    loopWaitEvent.Dispose();
                    loopWaitEvent = null;
                }
                State = CustomRenderLoopState.Stopped;
                Stopped?.Invoke();
                if (restart) {
                    localRestart = true;
                    restart = false;
                }
            }
            if (localRestart) {
                Start();
            }
        }

        public void Restart() {
            Stop();
            Start();
        }

        public void Invalidate() {
            ManualResetEvent localLoopWaitEvent = null;
            lock (stateLock) {
                if (loopWaitEvent != null) {
                    localLoopWaitEvent = loopWaitEvent;
                } else {
                    initialLoopWaitEventState = true;
                }
            }
            if (localLoopWaitEvent != null) {
                localLoopWaitEvent.Set();
            }
        }

        void LoopHandler() {
            while (State == CustomRenderLoopState.Running) {
                try {
                    LoopWaitEvent?.WaitOne();
                    if (State != CustomRenderLoopState.Running) {
                        break;
                    }
                    LoopWaitEvent?.Reset();
                } catch (Exception) {
                    break; //This should only occur if the owning thread has been disposed so we don't try to recover the loop here
                }
                Render?.Invoke();
            }
            LoopExiting?.Invoke();
        }

    }

    public enum CustomRenderLoopState {
        Stopped, Running, Stopping
    }

}
