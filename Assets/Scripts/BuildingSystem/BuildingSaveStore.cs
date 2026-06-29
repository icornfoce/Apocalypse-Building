using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Simulation.Building
{
    public static class BuildingSaveStore
    {
        private const string FolderName = "BuildingSaves";
        private const string JsonExtension = ".json";
        private const string ThumbnailExtension = ".png";

        public static string SaveFolder => Path.Combine(Application.persistentDataPath, FolderName);

        public static List<BuildingSaveSummary> GetSummaries()
        {
            EnsureFolder();
            List<BuildingSaveSummary> summaries = new List<BuildingSaveSummary>();
            foreach (string path in Directory.GetFiles(SaveFolder, "*" + JsonExtension))
            {
                BuildingSaveData data = LoadFromPath(path);
                if (data == null) continue;

                summaries.Add(ToSummary(data));
            }

            summaries.Sort((a, b) => b.savedAtTicks.CompareTo(a.savedAtTicks));
            return summaries;
        }

        public static bool HasSaves()
        {
            EnsureFolder();
            return Directory.GetFiles(SaveFolder, "*" + JsonExtension).Length > 0;
        }

        public static BuildingSaveData Load(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return LoadFromPath(GetJsonPath(id));
        }

        public static BuildingSaveData LoadRandom()
        {
            List<BuildingSaveSummary> summaries = GetSummaries();
            if (summaries.Count == 0) return null;
            return Load(summaries[Random.Range(0, summaries.Count)].id);
        }

        public static void Save(BuildingSaveData data, Texture2D thumbnail)
        {
            if (data == null) return;
            EnsureFolder();

            if (string.IsNullOrEmpty(data.id))
            {
                data.id = System.Guid.NewGuid().ToString("N");
            }

            data.savedAtTicks = System.DateTime.UtcNow.Ticks;
            data.thumbnailFileName = data.id + ThumbnailExtension;

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetJsonPath(data.id), json);

            if (thumbnail != null)
            {
                File.WriteAllBytes(GetThumbnailPath(data.id), thumbnail.EncodeToPNG());
            }
        }

        public static void Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            string jsonPath = GetJsonPath(id);
            if (File.Exists(jsonPath)) File.Delete(jsonPath);

            string thumbnailPath = GetThumbnailPath(id);
            if (File.Exists(thumbnailPath)) File.Delete(thumbnailPath);
        }

        public static bool Rename(string id, string newName)
        {
            BuildingSaveData data = Load(id);
            if (data == null) return false;

            data.displayName = string.IsNullOrWhiteSpace(newName) ? data.displayName : newName.Trim();
            File.WriteAllText(GetJsonPath(id), JsonUtility.ToJson(data, true));
            return true;
        }

        public static Texture2D LoadThumbnail(string id)
        {
            string path = GetThumbnailPath(id);
            if (!File.Exists(path)) return null;

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Object.Destroy(texture);
                return null;
            }

            return texture;
        }

        public static BuildingSaveSummary ToSummary(BuildingSaveData data)
        {
            if (data == null) return null;
            return new BuildingSaveSummary
            {
                id = data.id,
                displayName = data.displayName,
                thumbnailPath = GetThumbnailPath(data.id),
                pieceCount = data.pieces != null ? data.pieces.Count : 0,
                savedAtTicks = data.savedAtTicks
            };
        }

        private static BuildingSaveData LoadFromPath(string path)
        {
            if (!File.Exists(path)) return null;

            try
            {
                return JsonUtility.FromJson<BuildingSaveData>(File.ReadAllText(path));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildingSaveStore] Could not load save '{path}': {ex.Message}");
                return null;
            }
        }

        private static string GetJsonPath(string id)
        {
            EnsureFolder();
            return Path.Combine(SaveFolder, id + JsonExtension);
        }

        private static string GetThumbnailPath(string id)
        {
            EnsureFolder();
            return Path.Combine(SaveFolder, id + ThumbnailExtension);
        }

        private static void EnsureFolder()
        {
            if (!Directory.Exists(SaveFolder))
            {
                Directory.CreateDirectory(SaveFolder);
            }
        }
    }
}
