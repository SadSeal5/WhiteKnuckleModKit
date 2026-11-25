using HarmonyLib;

namespace WKModKit
{
    /// <summary>
    /// Loader'ın bir moda sağladığı servisler: günlükleme, config, Harmony ve dosya yolları.
    /// </summary>
    public interface IModContext
    {
        /// <summary>Modun metadata'sı (Load aşamasında doldurulmuş haliyle).</summary>
        WKModInfo Info { get; }

        /// <summary>Tüm WKModKit modlarının kök klasörü: &lt;oyun&gt;\BepInEx\wkmods\</summary>
        string ModsRoot { get; }

        /// <summary>Bu modun DLL dosyasının bulunduğu klasör (mod kendi yan dosyalarını buraya koyar).</summary>
        string ModDirectory { get; }

        /// <summary>Modun kalıcı veri klasörü: &lt;oyun&gt;\BepInEx\wkmods-data\&lt;Id&gt;\ (otomatik oluşturulur).</summary>
        string DataDirectory { get; }

        /// <summary>WKModKit'in çalıştığı oyunun derleme sürümü (Assembly-CSharp'in sürümü).</summary>
        string GameVersion { get; }

        /// <summary>Bu moda ait, önceden tanımlanmış "WKModKit.&lt;Id&gt;" kimlikli yeni bir Harmony instance oluşturur.</summary>
        Harmony CreateHarmony();

        /// <summary>Bu moda özel config servisini döndürür (BepInEx config + ConfigurationManager entegrasyonlu).</summary>
        IModConfig Config { get; }

        /// <summary>Bilgi seviyesinde günlük kaydı.</summary>
        void LogInfo(object message);

        /// <summary>Uyarı seviyesinde günlük kaydı.</summary>
        void LogWarning(object message);

        /// <summary>Hata seviyesinde günlük kaydı.</summary>
        void LogError(object message);

        /// <summary>Hata ayıklama seviyesinde günlük kaydı (konsolda Debug kapalıysa gösterilmez).</summary>
        void LogDebug(object message);
    }

    /// <summary>
    /// Mod config'i: BepInEx config dosyasına (&lt;oyun&gt;\BepInEx\config\WKModKit.&lt;Id&gt;.cfg) yazar,
    /// ConfigurationManager kuruluysa oyun içinde F1 menüsünden de düzenlenebilir.
    /// </summary>
    public interface IModConfig
    {
        /// <summary>
        /// Bir config girdisi tanımlar ve mevcut değerini döndürür. Aynı (section, key) için yapılacak
        /// sonraki çağrılar yeni girdi oluşturmaz; dosyadan okunan mevcut değeri döndürür.
        /// Desteklenen tipler: bool, int, float, string ve enum'lar.
        /// </summary>
        T Bind<T>(string section, string key, T defaultValue, string description);

        /// <summary>Tanımlı bir girdinin güncel değerini döndürür. Tanımsızsa default(T) döner.</summary>
        T Get<T>(string section, string key);

        /// <summary>Tanımlı bir girdinin değerini ayarlar ve diske kaydeder.</summary>
        void Set<T>(string section, string key, T value);
    }
}
