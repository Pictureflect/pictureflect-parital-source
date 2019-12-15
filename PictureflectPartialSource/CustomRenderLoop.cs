using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PictureflectPartialSource {

    //This class is not threadsafe - the public methods should all be called from the same thread. The Render handler runs on a different thread.
    public class CustomRenderLoop {

        readonly object stateLock = new object(); //Only to be used for locking state
        CustomRenderLoopState state = CustomRenderLoopState.Stopped; //Is accessed from render loop thread as well as main.
        public CustomRenderLoopState State {
            get { lock (stateLock) { return state; } }
            set { lock (stateLock) { state = value; } }
        }

        public event Action Render; //Is fired on the render loop thread.
        public event Action LoopExiting; //Is fired on the render loop thread.
        public event Action Stopped; //Will not fire if already stopped. Is fired on the main thread.

        Task loopTask = null;
        ManualResetEvent loopWaitEvent = null; //Is accessed from render loop thread as was as main. Only create and destroy this when the loop is not running.
        bool initialLoopWaitEventState = false;
        bool restart = false;

        public void Start() {
            if (State == CustomRenderLoopState.Running) {
                return;
            }
            if (State == CustomRenderLoopState.Stopping) {
                restart = true;
                return;
            }
            restart = false;
            if (loopWaitEvent == null) {
                loopWaitEvent = new ManualResetEvent(initialLoopWaitEventState);
            }
            State = CustomRenderLoopState.Running;
            loopTask = Task.Factory.StartNew(() => LoopHandler(), CancellationToken.None, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async void Stop() { //Note that it is essential to call Stop to ensure proper disposal
            restart = false;
            if (State == CustomRenderLoopState.Stopped || State == CustomRenderLoopState.Stopping) {
                return;
            }
            State = CustomRenderLoopState.Stopping;
            Invalidate();
            if (loopTask != null) {
                await loopTask;
                loopTask = null;
            }
            if (loopWaitEvent != null) {
                loopWaitEvent.Dispose();
                loopWaitEvent = null;
            }
            State = CustomRenderLoopState.Stopped;
            Stopped?.Invoke();
            if (restart) {
                restart = false;
                Start();
            }
        }

        public void Restart() {
            Stop();
            Start();
        }

        public void Invalidate() {
            if (loopWaitEvent != null) {
                loopWaitEvent.Set();
            } else {
                initialLoopWaitEventState = true;
            }
        }

        void LoopHandler() {
            while (State == CustomRenderLoopState.Running) {
                try {
                    loopWaitEvent.WaitOne();
                    if (State != CustomRenderLoopState.Running) {
                        break;
                    }
                    loopWaitEvent.Reset();
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
