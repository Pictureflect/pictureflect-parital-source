using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PictureflectPartialSource {

    public static class FileHelper {

        public static bool AreFilesEqual(StorageFile file1, StorageFile file2, bool fullCompare) { //fullCompare is more reliable but slow so don't call in a loop
            if (file1 == file2) {
                return true;
            }
            if (file1 == null || file2 == null) {
                return false;
            }
            if (fullCompare) {
                return file1.IsEqual(file2);
            }
            if (string.IsNullOrEmpty(file1.Path) && string.IsNullOrEmpty(file2.Path)) {
                return false;
            }
            return file1.Path == file2.Path;
        }

        public static bool AreFoldersEqual(StorageFolder folder1, StorageFolder folder2, bool fullCompare) { //fullCompare is more reliable but slow so don't call in a loop
            if (folder1 == folder2) {
                return true;
            }
            if (folder1 == null || folder2 == null) {
                return false;
            }
            if (fullCompare) {
                return folder1.IsEqual(folder2);
            }
            if (string.IsNullOrEmpty(folder1.Path) && string.IsNullOrEmpty(folder2.Path)) {
                return false;
            }
            return folder1.Path == folder2.Path;
        }

        public static string GetDirectoryName(string path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            try {
                return System.IO.Path.GetDirectoryName(path);
            } catch (Exception) { }
            return null;
        }

        public static string GetFileName(string path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            try {
                return System.IO.Path.GetFileName(path);
            } catch (Exception) { }
            return null;
        }

        public static string GetFileNameWithoutExtension(string path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            try {
                return System.IO.Path.GetFileNameWithoutExtension(path);
            } catch (Exception) { }
            return null;
        }

        public static bool IsValidFileName(string name) {
            if (string.IsNullOrEmpty(name)) {
                return false;
            }
            try {
                return name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0;
            } catch (Exception) {
                return false;
            }
        }

        public static string GetFileExtension(string path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            try {
                return System.IO.Path.GetExtension(path);
            } catch (Exception) { }
            return null;
        }

        public static bool IsReadOnly(StorageFile file) {
            return file != null && (file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
        }

        public static string CombinePaths(string path1, string path2) { //Note path1 must be absolute
            try {
                return System.IO.Path.Combine(path1 ?? "", path2 ?? "");
            } catch (Exception) { }
            return path2;
        }

        public static char[] PathSeparators { get; } = new char[] { '\\', '/' };

        public static char DirectorySeparatorChar { get => System.IO.Path.DirectorySeparatorChar; }

        public static string NormalizePath(string path) { //Defaults to using DirectorySeparatorChar for the new path
            return NormalizePath(path, DirectorySeparatorChar);
        }

        public static string NormalizePath(string path, char newSeparator) { //Does not strip trailing slash or leading two slashes. That is imprtant since it can be used to distinguish a directory. However, it will remove a leading ./.
            var segments = path.Split(PathSeparators);
            var newSegments = new List<string>();
            for (int i = 0; i < segments.Length; i++) {
                var segment = segments[i];
                if (segment == "") {
                    if (newSegments.Count == 0 || i == segments.Length - 1 || (newSegments.Count == 1 && newSegments[0] == "")) {
                        newSegments.Add(segment);
                    }
                } else if (segment == "..") {
                    if (!((newSegments.Count > 0 && newSegments[newSegments.Count - 1] == "") || (newSegments.Count == 1 && newSegments[0].EndsWith(System.IO.Path.VolumeSeparatorChar.ToString())))) { //If we are at the bginning of a root path then just drop the ..
                        if (newSegments.Count > 0 && newSegments[newSegments.Count - 1] != "..") {
                            newSegments.RemoveAt(newSegments.Count - 1);
                        } else {
                            newSegments.Add(segment);
                        }
                    }
                } else if (segment != ".") {
                    newSegments.Add(segment);
                }
            }
            return string.Join(newSeparator.ToString(), newSegments);
        }

    }

}
