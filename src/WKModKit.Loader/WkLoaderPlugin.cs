using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace WKModKit.Loader
{
    /// <summary>
    /// WKModKit Loader — BepInEx 5 plugin'i. Görevi:
    ///   1. &lt;oyun&gt;\BepInEx\wkmods\ altındaki mod DLL'lerini keşfetmek ve Assembly.LoadFrom ile yüklemek,
    ///   2. IWKMod uygulayan sınıfları bulmak, LoadPriority sırasına dizmek ve OnLoad ile başlatmak,
    ///   3. Her moda IModContext (log, config, Harmony) ve frame event'leri sağlamak.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class WkLoaderPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "wkmodkit.loader";
        public const string PluginName = "WKModKit Loader";
        public const string PluginVersion = "1.0.0";

        internal const string ModsFolderName = "wkmods";
        internal const string DataFolderName = "wkmods-data";

        internal static ManualLogSource Log;
        internal static string ModsRootPath;
        internal static string DataRootPath;
        internal static string GameAssemblyVersion = "unknown";

        internal static readonly List<WKModInfo> LoadedMods = new List<WKModInfo>();

        private readonly List<LoadedMod> _mods = new List<LoadedMod>();

        private void Awake()
        {
            Log = Logger;
            ModsRootPath = Path.Combine(Paths.BepInExRootPath, ModsFolderName);
            DataRootPath = Path.Combine(Paths.BepInExRootPath, DataFolderName);

            Log.LogInfo($"WKModKit {PluginVersion} başlatılıyor...");
            Log.LogInfo($"Mod klasörü: {ModsRootPath}");
            Log.LogInfo($"Veri klasörü: {DataRootPath}");

            GameAssemblyVersion = ReadGameAssemblyVersion();

            Directory.CreateDirectory(ModsRootPath);
            Directory.CreateDirectory(DataRootPath);

            // Modların bağımlılıkları (WKModKit.Api, ortak kütüphaneler) mod klasöründen çözülsün.
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            var hostObject = new GameObject("WKModKit.Host");
            hostObject.AddComponent<WkHost>();
            DontDestroyOnLoad(hostObject);

            LoadAllMods();
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            if (string.IsNullOrEmpty(name)) return null;

            var searchDirs = new List<string> { ModsRootPath, Path.GetDirectoryName(typeof(WkLoaderPlugin).Assembly.Location) ?? "" };
            try
            {
                // İstek, bir mod DLL'inden geliyorsa önce o modun kendi klasörüne bak.
                if (args.RequestingAssembly != null)
                {
                    var reqDir = Path.GetDirectoryName(args.RequestingAssembly.Location);
                    if (!string.IsNullOrEmpty(reqDir)) searchDirs.Insert(0, reqDir);
                }
            }
            catch { /* Assembly.Location bazı yükleme bağlamlarında desteklenmez; sorun değil. */ }

            foreach (var dir in searchDirs)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                var candidate = Path.Combine(dir, name + ".dll");
                if (File.Exists(candidate))
                {
                    try
                    {
                        var asm = Assembly.LoadFrom(candidate);
                        Log.LogDebug($"[bağımlılık] {name} ← {candidate}");
                        return asm;
                    }
                    catch (Exception e)
                    {
                        Log.LogWarning($"[bağımlılık] {candidate} yüklenemedi: {e.Message}");
                    }
                }
            }
            return null;
        }

        private static string ReadGameAssemblyVersion()
        {
            try
            {
                var asm = Assembly.LoadFrom(Path.Combine(Paths.ManagedPath, "Assembly-CSharp.dll"));
                return asm.GetName().Version?.ToString() ?? "unknown";
            }
            catch (Exception e)
            {
                Log.LogWarning($"Assembly-CSharp.dll sürümü okunamadı: {e.Message}");
                return "unknown";
            }
        }

        private void LoadAllMods()
        {
            var candidates = DiscoverModAssemblies();
            if (candidates.Count == 0)
            {
                Log.LogWarning($"Hiç mod bulunamadı. '{ModsRootPath}' klasörüne mod DLL'lerini kopyala.");
            }

            var loaded = new List<LoadedMod>();
            var failures = new List<string>();
            // Aynı derlemenin iki kopyası (ör. hem wkmods\*.dll hem wkmods\<Id>\*.dll)
            // iki kez yüklenmesin: aynı Harmony yamaları çift uygulanırdı.
            var seenAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Aynı Id'ye sahip iki mod tek config/tek Harmony kimliğini paylaşamaz.
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dllPath in candidates)
            {
                try
                {
                    // Kopya koruması: aynı derleme adı ikinci kez yüklenmez.
                    string assemblyName;
                    try { assemblyName = AssemblyName.GetAssemblyName(dllPath).Name; }
                    catch { assemblyName = Path.GetFileName(dllPath); }
                    if (!seenAssemblyNames.Add(assemblyName))
                    {
                        Log.LogWarning($"[yükleme] '{Path.GetFileName(dllPath)}' aynı derlemenin ikinci kopyası (aynı mod iki klasöre kopyalanmış olabilir); atlanıyor.");
                        continue;
                    }

                    var asm = Assembly.LoadFrom(dllPath);
                    var modTypes = FindModTypes(asm);
                    if (modTypes.Count == 0)
                    {
                        Log.LogDebug($"[yükleme] {Path.GetFileName(dllPath)} içinde IWKMod yok; atlanıyor.");
                        continue;
                    }

                    foreach (var type in modTypes)
                    {
                        try
                        {
                            var mod = (IWKMod)Activator.CreateInstance(type);
                            var info = mod.Info ?? NewDefaultInfo(type);
                            // Boş/yalnızca boşluktan oluşan Id'ler: tip adına düşer; dolu Id'lerde
                            // baş/son boşluklar config dosya adına sızmasın diye trim edilir.
                            if (string.IsNullOrWhiteSpace(info.Id)) info.Id = type.FullName;
                            else info.Id = info.Id.Trim();
                            // Id, config dosya adının ve veri klasörünün parçasıdır: dosya
                            // sisteminde geçersiz karakter taşıyan Id'ler yolda hata verir.
                            if (info.Id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                                throw new InvalidOperationException($"Id dosya sisteminde geçersiz karakter içeriyor: '{info.Id}' (ör. '/' veya ':' kullanma)");

                            // Mod kendi DLL'inin yanındaki dosyaları bulabilsin diye klasörü kaydet.
                            var modDir = Path.GetDirectoryName(dllPath) ?? "";
                            loaded.Add(new LoadedMod(mod, info, modDir, Path.GetFileName(dllPath)));
                            Log.LogInfo($"[yükleme] Bulundu: {info.Name} v{info.Version} ({info.Id}) ← {Path.GetFileName(dllPath)}");
                        }
                        catch (Exception e)
                        {
                            failures.Add($"{type.FullName}: {e.Message}");
                            Log.LogError($"[yükleme] '{type.FullName}' oluşturulamadı: {e}");
                        }
                    }
                }
                catch (Exception e)
                {
                    failures.Add($"{Path.GetFileName(dllPath)}: {e.Message}");
                    Log.LogError($"[yükleme] '{dllPath}' yüklenemedi: {e}");
                }
            }

            // Önceliğe göre sırala; aynı öncelik olanları Id'ye göre alfabetik diz (deterministik sıra için).
            loaded.Sort((a, b) =>
            {
                int c = a.Info.LoadPriority.CompareTo(b.Info.LoadPriority);
                if (c != 0) return c;
                return string.Compare(a.Info.Id, b.Info.Id, StringComparison.Ordinal);
            });

            int successCount = 0;
            foreach (var mod in loaded)
            {
                // Id benzersizliği: config dosyası, veri klasörü ve Harmony kimliği
                // Id'den türediği için çakışan iki mod birbirinin üstüne yazar.
                if (!seenIds.Add(mod.Info.Id))
                {
                    failures.Add($"{mod.Info.Name}: Id çakışması ('{mod.Info.Id}' zaten kullanımda)");
                    Log.LogError($"[yükleme] ✗ '{mod.Info.Name}' atlandı: '{mod.Info.Id}' Id'si zaten yüklü bir modda var (Id'ler benzersiz olmalı).");
                    continue;
                }

                try
                {
                    var ctx = new ModContextImpl(mod.Info, mod.ModDirectory, MakeModLogger(mod.Info));
                    mod.Context = ctx;
                    mod.Mod.OnLoad(ctx);

                    var receiver = mod.Mod as IModUpdateReceiver;
                    if (receiver != null && WkHost.Instance != null)
                        WkHost.Instance.Register(receiver);

                    _mods.Add(mod);
                    LoadedMods.Add(mod.Info);
                    successCount++;
                    Log.LogInfo($"[yükleme] ✓ {mod.Info.Name} v{mod.Info.Version} (id: {mod.Info.Id}, öncelik: {mod.Info.LoadPriority})");
                }
                catch (Exception e)
                {
                    failures.Add($"{mod.Info.Name}: OnLoad — {e.Message}");
                    Log.LogError($"[yükleme] ✗ '{mod.Info.Name}' OnLoad başarısız: {e}");
                    // OnLoad yarıda kaldıysa mod işleme alınmaz; temizlik şansı verilir.
                    // Dikkat: mod OnLoad içinde Harmony PatchAll yaptıysa yamalar yalnızca
                    // modun kendi OnUnload temizliğine (ör. harmony.UnpatchSelf()) geri alınabilir.
                    try
                    {
                        mod.Mod.OnUnload();
                        Log.LogWarning($"[yükleme] '{mod.Info.Name}' OnLoad hatası sonrası OnUnload temizliği çağrıldı.");
                    }
                    catch (Exception e2)
                    {
                        Log.LogWarning($"[yükleme] '{mod.Info.Name}' OnLoad temizliği de başarısız; yamaları geri alınmamış olabilir: {e2.Message}");
                    }
                }
            }

            Log.LogInfo($"WKModKit: {successCount} mod yüklendi, {failures.Count} hata.");
            if (failures.Count > 0)
                foreach (var f in failures)
                    Log.LogWarning($" - Hata: {f}");
        }

        private static WKModInfo NewDefaultInfo(Type type)
        {
            return new WKModInfo
            {
                Id = type.FullName,
                Name = type.Name,
                Version = "0.0.0",
            };
        }

        private static List<string> DiscoverModAssemblies()
        {
            var result = new List<string>();
            if (!Directory.Exists(ModsRootPath))
                return result;

            // Üst düzey DLL'ler
            foreach (var dll in Directory.GetFiles(ModsRootPath, "*.dll", SearchOption.TopDirectoryOnly))
                result.Add(dll);

            // Alt klasör düzeni: wkmods/<ModId>/<ModId>.dll — her alt klasörde üst düzey DLL'ler
            foreach (var dir in Directory.GetDirectories(ModsRootPath))
                foreach (var dll in Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly))
                    result.Add(dll);

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static List<Type> FindModTypes(Assembly asm)
        {
            var found = new List<Type>();
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

            foreach (var t in types)
            {
                if (!typeof(IWKMod).IsAssignableFrom(t)) continue;
                if (!t.IsClass || t.IsAbstract) continue;
                if (!t.IsVisible)
                {
                    Log.LogWarning($"[yükleme] '{t.FullName}' IWKMod uyguluyor ancak public değil; atlanıyor.");
                    continue;
                }
                if (t.GetConstructor(Type.EmptyTypes) == null)
                {
                    Log.LogWarning($"[yükleme] '{t.FullName}' parametresiz kurucuya sahip değil; atlanıyor.");
                    continue;
                }
                found.Add(t);
            }
            return found;
        }

        private static ManualLogSource MakeModLogger(WKModInfo info)
        {
            // Her moda kendi log kaynağı ver: BepInEx konsolunda ve filtrelerde
            // mod satırları "WKModKit.<Id>" etiketiyle ayırt edilir.
            // (ManualLogSource kayıtları BepInEx 5'te oturum boyunca kalıcıdır; modlar
            // dinamik boşaltılmadığı için oyun oturumu başına bu kabul edilir.)
            return BepInEx.Logging.Logger.CreateLogSource("WKModKit." + info.Id);
        }

        private void OnDestroy()
        {
            // Önce throttled config yazıcıda ortada kalan değişiklikleri diske yaz.
            for (int i = 0; i < _mods.Count; i++)
            {
                try { if (_mods[i].Context != null) _mods[i].Context.Flush(); }
                catch (Exception e) { Log.LogError($"[kapanış] '{_mods[i].Info.Name}' config yazma hatası: {e.Message}"); }
            }

            // Oyun kapanırken modlara OnUnload fırsatı ver (deterministik sırada, öncelik ters).
            for (int i = _mods.Count - 1; i >= 0; i--)
            {
                try
                {
                    var receiver = _mods[i].Mod as IModUpdateReceiver;
                    if (receiver != null && WkHost.Instance != null)
                        WkHost.Instance.Unregister(receiver);
                    _mods[i].Mod.OnUnload();
                }
                catch (Exception e)
                {
                    Log.LogError($"[kapanış] '{_mods[i].Info.Name}' OnUnload hatası: {e}");
                }
            }
            // Mod OnUnload içinde de config yazabilir (ör. kapanış ayarı) — o yüzden
            // OnUnload döngüsünden sonra kalan değişiklikleri bir kez daha diske yaz.
            for (int i = 0; i < _mods.Count; i++)
            {
                try { if (_mods[i].Context != null) _mods[i].Context.Flush(); }
                catch (Exception e) { Log.LogError($"[kapanış] '{_mods[i].Info.Name}' config yazma hatası: {e.Message}"); }
            }
            Log.LogInfo($"WKModKit kapatıldı ({_mods.Count} mod sonlandırıldı).");
            _mods.Clear();
        }

        private sealed class LoadedMod
        {
            public readonly IWKMod Mod;
            public readonly WKModInfo Info;
            public readonly string ModDirectory;
            public readonly string SourceDll;
            public ModContextImpl Context;

            public LoadedMod(IWKMod mod, WKModInfo info, string modDirectory, string sourceDll)
            {
                Mod = mod;
                Info = info;
                ModDirectory = modDirectory;
                SourceDll = sourceDll;
            }
        }
    }
}
