using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Placement
{
    // Saves, loads and clears the furniture layout file in Application.persistentDataPath (JSON from LayoutSerializer).
    // A save writes a temp file, flushes it to disk and then swaps it in, so a crash mid-write leaves the previous
    // layout intact. If a crash happened after the old file was removed but before the swap, Load falls back to the
    // complete temp file. IO errors are logged and reported as false; nothing here throws into gameplay.
    public class LayoutStore : MonoBehaviour
    {
        [SerializeField] private string fileName = "layout.json";

        public string FilePath => Path.Combine(Application.persistentDataPath, fileName);
        private string TempPath => FilePath + ".tmp";

        public bool HasSavedLayout => File.Exists(FilePath) || File.Exists(TempPath);

        public bool Save(IReadOnlyList<PlacedItem> items)
        {
            try
            {
                string json = LayoutSerializer.Serialize(items);
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

                using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                SwapIn(TempPath, FilePath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(LayoutStore)}: could not save the layout to {FilePath}: {exception.Message}");
                return false;
            }
        }

        // False when nothing has been saved yet (or the file cannot be read); otherwise the parsed layout,
        // which may itself carry an error for malformed content.
        public bool TryLoad(out LayoutLoadResult result)
        {
            result = null;
            string path = File.Exists(FilePath) ? FilePath : File.Exists(TempPath) ? TempPath : null;
            if (path == null)
            {
                return false;
            }

            try
            {
                result = LayoutSerializer.Deserialize(File.ReadAllText(path, Encoding.UTF8));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(LayoutStore)}: could not read {path}: {exception.Message}");
                return false;
            }
        }

        public bool Clear()
        {
            try
            {
                File.Delete(FilePath);
                File.Delete(TempPath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(LayoutStore)}: could not delete {FilePath}: {exception.Message}");
                return false;
            }
        }

        private static void SwapIn(string source, string destination)
        {
            if (!File.Exists(destination))
            {
                File.Move(source, destination);
                return;
            }

            try
            {
                File.Replace(source, destination, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Delete(destination);
                File.Move(source, destination);
            }
        }
    }
}
