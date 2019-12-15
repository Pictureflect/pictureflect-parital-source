using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Foundation;
using Windows.UI.Core;
using PictureflectPartialSource.SynchronizationHelpers;

namespace PictureflectPartialSource {

    //This class is thread-safe.
    public static class AppSuspensionManager {

        private static readonly object lockObject = new object();
        private static ExtendedExecutionSession extendedExecutionSession = null; //Should always be protected by lockObject when getting or setting a reference
        private static int extendedExecutionRequestCount = 0; //Should always be protected by lockObject

        static AppSuspensionManager() { }

        public static void AddExtendedExecutionSessionRequest() {
            lock (lockObject) {
                if (extendedExecutionRequestCount == 0) {
                    extendedExecutionRequestCount++;
                } else {
                    extendedExecutionRequestCount++;
                }
            }
            CreateOrClearExtendedExecutionSessionBasedOnCount();
        }

        public static void RemoveExtendedExecutionSessionRequest() {
            lock (lockObject) {
                if (extendedExecutionRequestCount == 0) { //This check is important in case the request was cleared by being revoked
                    return;
                }
                extendedExecutionRequestCount--;
            }
            CreateOrClearExtendedExecutionSessionBasedOnCount();
        }

        private static void ExtendedExecutionSessionRevoked(object sender, ExtendedExecutionRevokedEventArgs args) {
            lock (lockObject) {
                extendedExecutionRequestCount = 0;
            }
            CreateOrClearExtendedExecutionSessionBasedOnCount();
        }

        private static readonly AsyncLock createOrClearExtendedExecutionSessionBasedOnCountLock = new AsyncLock();
        private static readonly InterlockedBoolean createOrClearExtendedExecutionSessionBasedOnCountNeedsRefresh = new InterlockedBoolean(false);
        public static async void CreateOrClearExtendedExecutionSessionBasedOnCount() {
            createOrClearExtendedExecutionSessionBasedOnCountNeedsRefresh.Value = true;
            using (await createOrClearExtendedExecutionSessionBasedOnCountLock.LockAsync()) {
                if (!createOrClearExtendedExecutionSessionBasedOnCountNeedsRefresh.Value) {
                    return;
                }
                createOrClearExtendedExecutionSessionBasedOnCountNeedsRefresh.Value = false;
                await CreateOrClearExtendedExecutionSessionBasedOnCountDirect();
            }
        }

        private static async Task CreateOrClearExtendedExecutionSessionBasedOnCountDirect() {
            bool createNew = false;
            ExtendedExecutionSession localExtendedExecutionSession = null;
            lock (lockObject) {
                localExtendedExecutionSession = extendedExecutionSession;
                if (extendedExecutionRequestCount > 0) {
                    createNew = true;
                } else {
                    createNew = false;
                }
            }
            if (createNew && localExtendedExecutionSession == null) {
                var newSession = new ExtendedExecutionSession() { Reason = ExtendedExecutionReason.Unspecified };
                newSession.Revoked += ExtendedExecutionSessionRevoked;
                ExtendedExecutionResult result = await newSession.RequestExtensionAsync();
                if (result == ExtendedExecutionResult.Allowed) {
                    lock (lockObject) {
                        extendedExecutionSession = newSession;
                    }
                } else {
                    newSession.Dispose();
                }
            } else if (localExtendedExecutionSession != null) {
                localExtendedExecutionSession.Revoked -= ExtendedExecutionSessionRevoked;
                localExtendedExecutionSession.Dispose();
                lock (lockObject) {
                    extendedExecutionSession = null;
                }
            }
        }

    }

}
