using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RoomPlanner.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoomPlanner.Placement
{
    // Layout files in Application.persistentDataPath, one per LayoutSlot: the session (autosave, resumed on start)
    // and the checkpoint (Save/Load buttons). A write goes to a temp file, is flushed to disk and then swapped in,
    // so a crash mid-write leaves the previous file intact. If a crash happened after the old file was removed but
    // before the swap, reading falls back to the complete temp file. IO errors are logged and reported as false;
    // nothing here throws into gameplay.
    public class LayoutStore : MonoBehaviour, ILayoutStorage
    {
        [FormerlySerializedAs("fileName")]
        [SerializeField] private string sessionFileName = "layout.json";
        [SerializeField] private string checkpointFileName = "checkpoint.json";

        public string PathFor(LayoutSlot slot) =>
            Path.Combine(Application.persistentDataPath, slot == LayoutSlot.Checkpoint ? checkpointFileName : sessionFileName);

        private static string TempPathOf(string path) => path + ".tmp";

        public bool Exists(LayoutSlot slot)
        {
            string path = PathFor(slot);
            return File.Exists(path) || File.Exists(TempPathOf(path));
        }

        public bool TryRead(LayoutSlot slot, out string json)
        {
            json = null;
            string finalPath = PathFor(slot);
            string tempPath = TempPathOf(finalPath);
            string path = File.Exists(finalPath) ? finalPath : File.Exists(tempPath) ? tempPath : null;
            if (path == null)
            {
                return false;
            }

            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(LayoutStore)}: could not read {path}: {exception.Message}");
                return false;
            }
        }

        public bool Write(LayoutSlot slot, string json)
        {
            string path = PathFor(slot);
            string tempPath = TempPathOf(path);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                SwapIn(tempPath, path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(LayoutStore)}: could not save the layout to {path}: {exception.Message}");
                return false;
            }
        }

        public bool Delete(LayoutSlot slot)
        {
            string path = PathFor(slot);
            try
            {
                File.Delete(path);
                File.Delete(TempPathOf(path));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(LayoutStore)}: could not delete {path}: {exception.Message}");
                return false;
            }
        }

        // Session-file shortcuts used by LayoutPersistence.
        public bool HasSavedLayout => Exists(LayoutSlot.Session);

        public bool Save(IReadOnlyList<PlacedItem> items) => Write(LayoutSlot.Session, LayoutSerializer.Serialize(items));

        // False when nothing has been saved yet (or the file cannot be read); otherwise the parsed layout,
        // which may itself carry an error for malformed content.
        public bool TryLoad(out LayoutLoadResult result)
        {
            result = TryRead(LayoutSlot.Session, out string json) ? LayoutSerializer.Deserialize(json) : null;
            return result != null;
        }

        public bool Clear() => Delete(LayoutSlot.Session);

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
