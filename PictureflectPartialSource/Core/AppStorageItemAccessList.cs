using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace PictureflectPartialSource.Core {

    //Use this class instead of the StorageApplicationPermissions.FutureAccessList directly so that we can cache StorageItems to get the same instance. //This class is thread-safe.
    public static class AppStorageItemAccessList {

        private static readonly ConcurrentDictionary<string, IStorageItem> cache = new ConcurrentDictionary<string, IStorageItem>();

        static AppStorageItemAccessList() { }

        private static readonly object cachedMaxEntriesLock = new object();
        private static uint cachedMaxEntries = 0; //Must be protected by cachedMaxEntriesLock
        public static uint MaxExtries {
            get {
                uint localCachedMaxEntries;
                lock (cachedMaxEntriesLock) {
                    localCachedMaxEntries = cachedMaxEntries;
                }
                if(localCachedMaxEntries > 0) {
                    return localCachedMaxEntries;
                }
                var maxEntries = StorageApplicationPermissions.FutureAccessList.MaximumItemsAllowed; //Note that this can take a few milliseconds
                lock (cachedMaxEntriesLock) {
                    cachedMaxEntries = maxEntries;
                }
                return maxEntries;
            }
        }

        private static int cachedEntryCount = -1; //Must be protected by cachedEntryCountLock
        private static readonly object cachedEntryCountLock = new object();
        public static async Task<int> GetEntryCount() {
            lock (cachedEntryCountLock) {
                if(cachedEntryCount >= 0) {
                    return cachedEntryCount;
                }
            }
            int count = 0; ;
            await Task.Run(() => {
                count = GetEntryCountSync();
            });
            return count;
        }

        public static int GetEntryCountSync() {
            lock (cachedEntryCountLock) {
                if (cachedEntryCount >= 0) {
                    return cachedEntryCount;
                }
            }
            var newCount = 0;
            try {
                newCount = StorageApplicationPermissions.FutureAccessList.Entries.Count; //Note that this can take a few milliseconds
            } catch (Exception) {
                return 0;
            }
            lock (cachedEntryCountLock) {
                cachedEntryCount = newCount;
            }
            return newCount;
        }

        private static void InvalidateEntryCount() {
            lock (cachedEntryCountLock) {
                cachedEntryCount = -1;
            }
        }

        public static async Task<string> Add(IStorageItem item) {
            if(item == null) {
                return null;
            }
            string token = null;
            await Task.Run(() => {
                token = AddSync(item);
            });
            return token;
        }

        public static string AddSync(IStorageItem item) {
            if (item == null) {
                return null;
            }
            string token = null;
            try {
                token = Guid.NewGuid().ToString(); //We generate our own tokens so that this always returns a unique token and not one that is shared for the same StorageItem
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(token, item);
                InvalidateEntryCount();
                cache.AddOrUpdate(token, item, (s, i) => item);
            } catch (Exception) { }
            return token;
        }

        public static async Task RemoveItemsNotInGivenSet(HashSet<string> itemsToKeep) {
            if(itemsToKeep == null) {
                return;
            }
            await Task.Run(() => {
                if (itemsToKeep.Count == 0) {
                    Clear();
                    return;
                }
                try {
                    List<string> toRemove = new List<string>();
                    foreach (var entry in StorageApplicationPermissions.FutureAccessList.Entries) {
                        if (!itemsToKeep.Contains(entry.Token)) {
                            toRemove.Add(entry.Token);
                        }
                    }
                    foreach (var tokenToRemove in toRemove) {
                        RemoveSync(tokenToRemove);
                    }
                } catch (Exception) { }
            });
        }

        public static async Task Remove(string token) {
            if (string.IsNullOrEmpty(token)) {
                return;
            }
            await Task.Run(() => RemoveSync(token));
        }

        public static void RemoveSync(string token) {
            if (string.IsNullOrEmpty(token)) {
                return;
            }
            try {
                StorageApplicationPermissions.FutureAccessList.Remove(token);
                InvalidateEntryCount();
            } catch (Exception) { }
            cache.TryRemove(token, out var temp);
        }

        public static async Task<StorageFile> GetFileAsync(string token, bool allowUserCredentialsPrompt) {
            if (string.IsNullOrEmpty(token)) {
                return null;
            }
            if(cache.TryGetValue(token, out var storageItem) && storageItem != null && storageItem is StorageFile) {
                return storageItem as StorageFile;
            }
            StorageFile item = null;
            try {
                item = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(token, allowUserCredentialsPrompt ? AccessCacheOptions.None : AccessCacheOptions.DisallowUserInput);
            } catch (Exception) { }
            if (item != null) {
                cache.TryAdd(token, item);
            }
            return item;
        }

        public static async Task<StorageFolder> GetFolderAsync(string token, bool allowUserCredentialsPrompt) {
            if (string.IsNullOrEmpty(token)) {
                return null;
            }
            if (cache.TryGetValue(token, out var storageItem) && storageItem != null && storageItem is StorageFolder) {
                return storageItem as StorageFolder;
            }
            StorageFolder item = null;
            try {
                item = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token, allowUserCredentialsPrompt ? AccessCacheOptions.None : AccessCacheOptions.DisallowUserInput);
            } catch (Exception) { }
            if (item != null) {
                cache.TryAdd(token, item);
            }
            return item;
        }

        public static void Clear() {
            try {
                StorageApplicationPermissions.FutureAccessList.Clear();
                InvalidateEntryCount();
            } catch (Exception) { }
            cache.Clear();
        }

    }

    public class AppStorageItemAccessListBeforeCapacityExceedArgs {
        public Task<HashSet<string>> ItemsToKeep { get; set; } = null;
    }

}
