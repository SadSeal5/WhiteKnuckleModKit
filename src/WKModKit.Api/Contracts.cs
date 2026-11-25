namespace WKModKit
{
    /// <summary>
    /// WKModKit ile yüklenen bir modun temel metadata'sı.
    /// Kurallar: <see cref="Id"/> benzersiz, boşluksuz ve kararlı olmalı (ör. "mycoolsandboxmod").
    /// </summary>
    public sealed class WKModInfo
    {
        /// <summary>Modun benzersiz kimliği. Config dosyası adında (WKModKit.&lt;Id&gt;.cfg), veri klasöründe (wkmods-data\&lt;Id&gt;\) ve Harmony instance kimliğinde kullanılır.</summary>
        public string Id;

        /// <summary>Kullanıcıya görünen mod adı.</summary>
        public string Name;

        /// <summary>Modun sürümü (serbest metin, ör. "1.0.0").</summary>
        public string Version = "1.0.0";

        /// <summary>Modun yazarı ya da yazarları.</summary>
        public string Author = "";

        /// <summary>Kısa açıklama.</summary>
        public string Description = "";

        /// <summary>
        /// Yükleme önceliği: küçük değer önce yüklenir (varsayılan 500).
        /// Örn. başka modlardan önce davranış sağlayan çekirdek modlar 0-100 kullanır.
        /// </summary>
        public int LoadPriority = 500;
    }

    /// <summary>
    /// Bir WKModKit modunun uygulaması gereken sözleşme.
    /// Modlar public, somut ve parametresiz kurucusu olan bir sınıf içinde bu arayüzü uygular
    /// (public olmayan sınıflar loader tarafından atlanır). Kurucu hafif olmalı:
    /// tüm başlatma <see cref="OnLoad"/> içinde yapılır.
    /// </summary>
    public interface IWKMod
    {
        /// <summary>Modun metadata'sı. Loader bu bilgiyle yükleme sırasını ve dosya yollarını belirler.</summary>
        WKModInfo Info { get; }

        /// <summary>
        /// Mod yüklenirken çağrılır. Harmony patch'leri, event abonelikleri ve tüm hazırlık burada yapılır.
        /// OnLoad bir hata fırlatırsa mod devre dışı kalır ve loader temizlik için <see cref="OnUnload"/>'u çağırır:
        /// uygulanan Harmony patch'leri burada (ör. harmony.UnpatchSelf()) geri alınmalıdır.
        /// </summary>
        void OnLoad(IModContext context);

        /// <summary>Oyun kapanırken çağrılır. Kaynakların serbest bırakılması için son şans (assembly bellekten boşaltılamaz).</summary>
        void OnUnload();
    }

    /// <summary>
    /// İsteğe bağlı: mod bunu uygularsa Loader'ın host MonoBehaviour'i her karede ilgili metotları çağırır.
    /// </summary>
    public interface IModUpdateReceiver
    {
        /// <summary>UnityEngine'in her Update adımında çağrılır.</summary>
        void OnModUpdate();

        /// <summary>UnityEngine'in her LateUpdate adımında çağrılır.</summary>
        void OnModLateUpdate();

        /// <summary>UnityEngine'in her FixedUpdate adımında çağrılır.</summary>
        void OnModFixedUpdate();

        /// <summary>IMGUI çizim adımında çağrılır (oyun içi menüler için).</summary>
        void OnModGUI();
    }
}
