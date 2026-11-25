using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace WKModKit.Loader
{
    /// <summary>Api'deki IModContext/IModConfig sözleşmelerinin Loader tarafındaki uygulaması.</summary>
    internal sealed class ModContextImpl : IModContext, IModConfig
    {
        public WKModInfo Info { get; }
        public string ModsRoot => WkLoaderPlugin.ModsRootPath;
        public string ModDirectory { get; }
        public string DataDirectory { get; }
        public string GameVersion => WkLoaderPlugin.GameAssemblyVersion;

        private readonly ManualLogSource _log;
        private readonly ConfigFile _configFile;
        private readonly Dictionary<string, object> _entries = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private DateTime _lastSaveUtc = DateTime.MinValue;
        private bool _saveDirty;

        internal ModContextImpl(WKModInfo info, string modDirectory, ManualLogSource log)
        {
            Info = info;
            ModDirectory = modDirectory ?? "";
            DataDirectory = Path.Combine(WkLoaderPlugin.DataRootPath, info.Id);
            Directory.CreateDirectory(DataDirectory);
            _log = log;
            string configPath = Path.Combine(Paths.ConfigPath, "WKModKit." + info.Id + ".cfg");
            // saveOnInit: true — dosya mod yüklenir yüklenmez diske yazılır; kullanıcı
            // ilk çalıştırmada bile config dosyasını bulup elle düzenleyebilir.
            _configFile = new ConfigFile(configPath, true);
        }

        public Harmony CreateHarmony()
        {
            return new Harmony("WKModKit." + Info.Id);
        }

        public IModConfig Config => this;

        public void LogInfo(object message) => _log.LogInfo(message);
        public void LogWarning(object message) => _log.LogWarning(message);
        public void LogError(object message) => _log.LogError(message);
        public void LogDebug(object message) => _log.LogDebug(message);

        // ---- IModConfig ----

        // Bölüm ve anahtarı tek anahtara birleştirirken \u0001 (SOH) kontrol karakteri ayırıcı
        // kullanılır: bölüm/anahtar adlarında bu karakter geçmeyeceği varsayılır, böylece
        // ("a.b", "c") ile ("a", "b.c") gibi çiftler asla aynı anahtara düşmez.
        private static string EntryKey(string section, string key) => section + "\u0001" + key;

        public T Bind<T>(string section, string key, T defaultValue, string description)
        {
            if (string.IsNullOrEmpty(section)) section = "General";
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Config anahtarı boş olamaz.", "key");

            string ek = EntryKey(section, key);
            object existing;
            if (_entries.TryGetValue(ek, out existing))
            {
                var entry = existing as ConfigEntry<T>;
                if (entry != null) return entry.Value;
                _log.LogWarning($"[config] '{section}.{key}' farklı tiple tekrar Bind edildi; ilk tip geçerli.");
                return Get<T>(section, key);
            }

            var desc = new ConfigDescription(string.IsNullOrEmpty(description) ? key : description);
            var newEntry = _configFile.Bind(section, key, defaultValue, desc);
            _entries[ek] = newEntry;
            return newEntry.Value;
        }

        public T Get<T>(string section, string key)
        {
            object existing;
            if (!_entries.TryGetValue(EntryKey(section, key), out existing)) return default(T);
            var entry = existing as ConfigEntry<T>;
            if (entry == null)
            {
                _log.LogWarning($"[config] '{section}.{key}' bu tip ile tanımlı değil.");
                return default(T);
            }
            return entry.Value;
        }

        public void Set<T>(string section, string key, T value)
        {
            object existing;
            if (!_entries.TryGetValue(EntryKey(section, key), out existing))
            {
                _log.LogWarning($"[config] '{section}.{key}' önce Bind ile tanımlanmalı; Set yok sayıldı.");
                return;
            }
            var entry = existing as ConfigEntry<T>;
            if (entry == null)
            {
                _log.LogWarning($"[config] '{section}.{key}' tip uyuşmazlığı; Set yok sayıldı.");
                return;
            }
            entry.Value = value;
            // Throttle: slider sürüklemeleri her IMGUI karesinde Set çağırır; diske
            // yazmayı 0.5 sn'de bir yapar, kalan değişiklik Flush ile (loader kapanışı) yazılır.
            _saveDirty = true;
            if ((DateTime.UtcNow - _lastSaveUtc).TotalSeconds >= 0.5)
            {
                _configFile.Save();
                _lastSaveUtc = DateTime.UtcNow;
                _saveDirty = false;
            }
        }

        /// <summary>Throttle'dan ortada kalan config değişikliklerini diske yazar (kapanışta loader çağırır).</summary>
        internal void Flush()
        {
            if (!_saveDirty) return;
            _configFile.Save();
            _lastSaveUtc = DateTime.UtcNow;
            _saveDirty = false;
        }
    }
}
