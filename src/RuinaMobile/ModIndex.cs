using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using BepInEx;
using UnityEngine;

namespace RuinaMobile
{
    [Serializable]
    public class ModEntry
    {
        public string folder = "";
        public string path = "";
        public string source = "";
        public string id = "";
        public string name = "";
        public string version = "";
        public string[] assemblies = new string[0];
        public bool hasInfoXml;
        public string problem = "";
        public long infoXmlTicks;
    }

    [Serializable]
    public class ModIndexFile
    {
        public int schema = 1;
        public string generatedUtc = "";
        public string gameRoot = "";
        public List<ModEntry> mods = new List<ModEntry>();
    }

    internal sealed class ModFolder
    {
        public string Label = "";
        public string FullPath = "";
        public bool Exists;
    }

    /// <summary>
    /// Checks the folders Library of Ruina loads mods from and reports what it finds, so you can
    /// confirm on the phone that copied Workshop / local mods are in the right place.
    /// Library of Ruina itself loads, enables and orders the mods (title screen > Mods);
    /// this index never loads mod DLLs, which would make them run twice.
    /// Parsed StageModInfo.xml data is cached in BepInEx/config/RuinaMobile/mod_index_cache.json
    /// and only re-read when the file changes.
    /// </summary>
    internal static class ModIndex
    {
        internal static readonly List<ModEntry> Mods = new List<ModEntry>();
        internal static readonly List<ModFolder> Folders = new List<ModFolder>();
        internal static string LastScan = "never";
        internal static string CachePath = "";

        internal static void Rescan()
        {
            try
            {
                RescanInternal();
            }
            catch (Exception e)
            {
                LastScan = "failed: " + e.Message;
                RuinaMobilePlugin.Log.LogWarning("Mod scan failed: " + e);
            }
        }

        private static void RescanInternal()
        {
            CachePath = Path.Combine(Path.Combine(Paths.ConfigPath, "RuinaMobile"), "mod_index_cache.json");
            var cache = LoadCache();
            BuildFolderList();

            Mods.Clear();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var folder in Folders)
            {
                if (!folder.Exists) continue;
                foreach (var dir in SafeDirectories(folder.FullPath))
                {
                    string full = SafeFull(dir);
                    if (!seen.Add(full)) continue;
                    Mods.Add(ScanMod(full, folder.Label, cache));
                }
            }

            SaveCache();
            LastScan = DateTime.Now.ToString("HH:mm:ss");
            RuinaMobilePlugin.Log.LogInfo("Mod check: " + Mods.Count + " mod folder(s), "
                                          + Mods.Count(m => m.problem.Length > 0) + " with problems. Cache: " + CachePath);
        }

        private static void BuildFolderList()
        {
            Folders.Clear();
            string data = Application.dataPath;
            string game = Paths.GameRootPath;
            AddFolder("LibraryOfRuina_Data\\Mods", Path.Combine(data, "Mods"));
            AddFolder("Game folder\\Mods", Path.Combine(game, "Mods"));
            AddFolder("Game folder\\Workshop", Path.Combine(game, "Workshop"));
            AddFolder("Steam Workshop 1256670", Path.Combine(game, "..", "..", "workshop", "content", "1256670"));

            string extra = RuinaMobilePlugin.Cfg.ExtraModFolders.Value ?? "";
            foreach (var raw in extra.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string p = raw.Trim();
                if (p.Length > 0) AddFolder("Extra", p);
            }
        }

        private static void AddFolder(string label, string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            string full = SafeFull(path);
            if (Folders.Any(f => string.Equals(f.FullPath, full, StringComparison.OrdinalIgnoreCase))) return;
            Folders.Add(new ModFolder { Label = label, FullPath = full, Exists = Directory.Exists(full) });
        }

        private static ModEntry ScanMod(string dir, string source, Dictionary<string, ModEntry> cache)
        {
            var entry = new ModEntry { folder = Path.GetFileName(dir), path = dir, source = source };
            entry.assemblies = FindAssemblies(dir);

            string xml = FindFile(dir, "StageModInfo.xml");
            if (xml == null)
            {
                string nested = null;
                foreach (var sub in SafeDirectories(dir))
                {
                    if (FindFile(sub, "StageModInfo.xml") == null) continue;
                    nested = Path.GetFileName(sub);
                    break;
                }

                entry.problem = nested != null
                    ? "StageModInfo.xml is inside '" + nested + "'. Move that inner folder up one level."
                    : "No StageModInfo.xml here, so the game will not see this as a mod.";
                return entry;
            }

            entry.hasInfoXml = true;
            long ticks = File.GetLastWriteTimeUtc(xml).Ticks;
            entry.infoXmlTicks = ticks;

            ModEntry cached;
            if (cache.TryGetValue(dir, out cached) && cached.hasInfoXml && cached.infoXmlTicks == ticks)
            {
                entry.id = cached.id ?? "";
                entry.name = cached.name ?? "";
                entry.version = cached.version ?? "";
                entry.problem = cached.problem ?? "";
                return entry;
            }

            try
            {
                var doc = new XmlDocument { XmlResolver = null };
                doc.Load(xml);
                entry.id = FirstText(doc, "ID", "Id", "id");
                entry.name = FirstText(doc, "Name", "Title", "name", "title");
                entry.version = FirstText(doc, "Version", "version");
                entry.problem = "";
            }
            catch (Exception e)
            {
                entry.problem = "StageModInfo.xml could not be read: " + e.Message;
            }

            return entry;
        }

        private static string FirstText(XmlDocument doc, params string[] tags)
        {
            foreach (var tag in tags)
            {
                var nodes = doc.GetElementsByTagName(tag);
                for (int i = 0; i < nodes.Count; i++)
                {
                    string text = Collapse(nodes[i].InnerText);
                    if (text.Length > 0) return text.Length > 80 ? text.Substring(0, 80) + "..." : text;
                }
            }

            return "";
        }

        private static string Collapse(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            bool space = false;
            foreach (char ch in s.Trim())
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!space) sb.Append(' ');
                    space = true;
                }
                else
                {
                    sb.Append(ch);
                    space = false;
                }
            }

            return sb.ToString();
        }

        private static string[] FindAssemblies(string dir)
        {
            foreach (var sub in SafeDirectories(dir))
            {
                if (!string.Equals(Path.GetFileName(sub), "Assemblies", StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    return Directory.GetFiles(sub)
                        .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        .Select(Path.GetFileName)
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }
                catch (Exception)
                {
                    return new string[0];
                }
            }

            return new string[0];
        }

        private static string FindFile(string dir, string fileName)
        {
            try
            {
                foreach (var f in Directory.GetFiles(dir))
                    if (string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase)) return f;
            }
            catch (Exception)
            {
                // unreadable folder: treated as "not found"
            }

            return null;
        }

        private static IEnumerable<string> SafeDirectories(string dir)
        {
            try
            {
                var dirs = Directory.GetDirectories(dir);
                Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
                return dirs;
            }
            catch (Exception)
            {
                return new string[0];
            }
        }

        private static string SafeFull(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception)
            {
                return path;
            }
        }

        private static Dictionary<string, ModEntry> LoadCache()
        {
            var result = new Dictionary<string, ModEntry>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(CachePath)) return result;
                var file = JsonUtility.FromJson<ModIndexFile>(File.ReadAllText(CachePath));
                if (file == null || file.mods == null) return result;
                foreach (var m in file.mods)
                    if (m != null && !string.IsNullOrEmpty(m.path)) result[m.path] = m;
            }
            catch (Exception e)
            {
                RuinaMobilePlugin.Log.LogDebug("Ignoring unreadable mod cache: " + e.Message);
            }

            return result;
        }

        private static void SaveCache()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CachePath));
                var file = new ModIndexFile
                {
                    generatedUtc = DateTime.UtcNow.ToString("o"),
                    gameRoot = Paths.GameRootPath,
                    mods = new List<ModEntry>(Mods)
                };
                File.WriteAllText(CachePath, JsonUtility.ToJson(file, true));
            }
            catch (Exception e)
            {
                RuinaMobilePlugin.Log.LogWarning("Could not write the mod cache: " + e.Message);
            }
        }
    }
}
