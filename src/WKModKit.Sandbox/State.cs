using UnityEngine;
using WKModKit;

namespace WKModKit.Sandbox
{
    /// <summary>
    /// Sandbox'un tüm ayar/durum merkezi. Değerler BepInEx config'inden okunur
    /// (dosya adı: "WKModKit." + mod Id = WKModKit.wkmodkit.sandbox.cfg —
    /// oyun içi F1 ConfigurationManager ile de düzenlenebilir);
    /// her karede "watchdog" istenen durumu oyuna uygular. Böylece menüden,
    /// config dosyasından veya hotkey'den yapılan değişiklikler aynı yoldan işler.
    /// </summary>
    internal static class State
    {
        private static IModConfig _cfg;

        // Death-goo durumu: "sandbox durdurdu" bilgisini örnek kimliğiyle birlikte saklar.
        // Statik bayrak sahne geçişlerinde hayatta kalırken DEN_DeathFloor.instance
        // her sahnede yeniden doğar; örnek değiştiğinde bayrak sıfırlanır ki oyunun kendi
        // zemin durumuna (ör. kayıttan gelen aktifsizlik) yanlışlıkla dokunulmasın.
        private static bool _deathFloorNeutralized;
        private static DEN_DeathFloor _deathFloorInstance;

        public static bool MenuOpen;

        public static void Initialize(IModContext ctx)
        {
            _cfg = ctx.Config;

            // ---- Toggles ----
            _cfg.Bind("Player", "GodMode", false, "Hasar ve ölüme karşı tam bağışıklık.");
            _cfg.Bind("Player", "InfiniteStamina", false, "Kavrama gücü (stamina) hiç azalmaz (oyunun kendi infiniteStamina bayrağı).");
            _cfg.Bind("Player", "InfiniteAmmo", false, "Eldiven/eşya kullanımları tükenmez.");
            _cfg.Bind("Player", "InfiniteCharge", false, "Şarj gerektiren eşyalar hiç boşalmaz.");
            _cfg.Bind("Player", "NoFallDamage", false, "Düşüş hasarı tamamen kapalı (ölümcül düşüş eşiği dahil).");
            _cfg.Bind("Player", "ClimbAnywhere", false, "Her yüzeye tutunabilirsin (tutam/ağaç/küpeşte gerekmez).");
            _cfg.Bind("Player", "Fly", false, "Uçuş modu.");
            _cfg.Bind("Player", "Noclip", false, "Noclip — duvarlardan geç; yerçekimi yok, düşüş hasarı alınmaz (oyunun kendi noclip'i).");
            _cfg.Bind("Player", "NoTarget", false, "Düşmanlar seni hedeflemez.");
            _cfg.Bind("Player", "DisableDeathFloor", false, "Yükselen ölümcül zemin (death goo) durdurulur (oyunun kendi SetActive API'si; kapatınca devam eder).");
            _cfg.Bind("Player", "SpeedMult", 1f, "Yürüme/koşma hız çarpanı (1 = normal).");

            _cfg.Bind("Time", "Timescale", 1f, "Oyun zaman çarpanı (0.05 = ağır çekim, 2 = hızlı). Oyunun RefreshTimescale işlevi değeri her karede sıfırladığı için mod geçerli değeri her karede yeniden uygular.");

            // ---- Hotkey'ler ----
            _cfg.Bind("Keys", "ToggleMenu", KeyCode.F6, "Sandbox menüsünü aç/kapat.");
            _cfg.Bind("Keys", "GodMode", KeyCode.F7, "God mode aç/kapat (config'e yazılır).");
            _cfg.Bind("Keys", "InfiniteStamina", KeyCode.F8, "Sonsuz stamina aç/kapat.");
            _cfg.Bind("Keys", "Fly", KeyCode.F9, "Uçuş modu aç/kapat.");
            _cfg.Bind("Keys", "Noclip", KeyCode.F10, "Noclip aç/kapat.");
        }

        public static bool GodMode => _cfg.Get<bool>("Player", "GodMode");
        public static bool InfiniteStamina => _cfg.Get<bool>("Player", "InfiniteStamina");
        public static bool InfiniteAmmo => _cfg.Get<bool>("Player", "InfiniteAmmo");
        public static bool InfiniteCharge => _cfg.Get<bool>("Player", "InfiniteCharge");
        public static bool NoFallDamage => _cfg.Get<bool>("Player", "NoFallDamage");
        public static bool ClimbAnywhere => _cfg.Get<bool>("Player", "ClimbAnywhere");
        public static bool Fly => _cfg.Get<bool>("Player", "Fly");
        public static bool Noclip => _cfg.Get<bool>("Player", "Noclip");
        public static bool NoTarget => _cfg.Get<bool>("Player", "NoTarget");
        public static bool DisableDeathFloor => _cfg.Get<bool>("Player", "DisableDeathFloor");
        public static float SpeedMult => _cfg.Get<float>("Player", "SpeedMult");
        public static float Timescale => _cfg.Get<float>("Time", "Timescale");

        public static void Set(string section, string key, object value) => _cfg.Set(section, key, value);

        /// <summary>Toggle helper: config'e yazar, watchdog bir sonraki karede oyuna uygular.</summary>
        public static void Toggle(string section, string key) => Set(section, key, !_cfg.Get<bool>(section, key));

        /// <summary>
        /// Watchdog: istenen sandbox durumunu oyuna uygular.
        /// Bool bayraklar istenen değerle oyun durumu arasında fark varsa yazılır;
        /// sayısal ayarlar (zaman/hız) oyuncu varken her karede yazılır (oyuncu yokken
        /// dokunulmaz: ana menüde çarpan kalıcılaşmaz), çünkü oyun bunları
        /// sahne yüklemesinde (CL_GameManager.InitializeGlobals → ResetTimescale) ve
        /// ölüm/yeniden doğuşta (ENT_Player.Revive → speedMult = 1) kendi kendine sıfırlar.
        /// </summary>
        public static void ApplyWatchdog(GameApi api)
        {
            var p = api.Player;
            if (p == null) return;

            // Zaman ölçeği: her karede yaz (oyun her sahne yükünde sıfırlıyor).
            // Yalnız oyuncu varken yazılır: ana menüde/sahne geçişinde çarpan kalıcılaşmasın
            // ve RefreshTimescale'in GameObject.Find("ENT_Player") çağrısı boşa tetiklenmesin.
            CL_GameManager.SetTimescaleMult(Timescale);

            if (p.GetGodMode() != GodMode) p.SetGodMode(GodMode);
            if (p.fly != Fly) p.fly = Fly;
            if (p.noclip != Noclip) p.noclip = Noclip;

            if (api.HasInfiniteAmmo() != InfiniteAmmo) api.SetInfiniteAmmo(InfiniteAmmo);
            if (api.HasInfiniteCharge() != InfiniteCharge) api.SetInfiniteCharge(InfiniteCharge);
            if (api.IsClimbAnywhere() != ClimbAnywhere) api.SetClimbAnywhere(ClimbAnywhere);
            if (api.HasInfiniteStamina() != InfiniteStamina) api.SetInfiniteStamina(InfiniteStamina);

            if (CL_GameManager.noTarget != NoTarget) CL_GameManager.noTarget = NoTarget;

            // Hız çarpanı: her karede yaz (oyun Revive'da 1'e sıfırlıyor).
            p.SetSpeedMult(SpeedMult);

            // Death goo: oyunun kendi SetActive'i ile durdur/devam ettir. Yalnız sandbox'ın
            // durdurduğu durumda geri açar; oyunun kendisi kapatmışsa dokunmaz.
            var df = DEN_DeathFloor.instance;
            if (df != null)
            {
                if (!ReferenceEquals(df, _deathFloorInstance))
                {
                    // Yeni sahne = yeni zemin örneği; "sandbox durdurdu" bilgisi geçersiz.
                    _deathFloorInstance = df;
                    _deathFloorNeutralized = false;
                }

                if (DisableDeathFloor)
                {
                    if (df.IsActive()) { df.SetActive(false); _deathFloorNeutralized = true; }
                }
                else if (_deathFloorNeutralized)
                {
                    df.SetActive(true);
                    _deathFloorNeutralized = false;
                }
            }
        }

        public static void ResetForExit(GameApi api)
        {
            // Kapanışta cheat'leri doğrudan oyuna kapat (artık watchdog karesi
            // çalışmayacağı için config'e değil oyuna yazarız).
            CL_GameManager.SetTimescaleMult(1f);
            CL_GameManager.noTarget = false;
            var df = DEN_DeathFloor.instance;
            if (df != null && _deathFloorNeutralized && ReferenceEquals(df, _deathFloorInstance))
            {
                df.SetActive(true);
                _deathFloorNeutralized = false;
            }
            var p = api.Player;
            if (p != null)
            {
                p.SetGodMode(false);
                p.fly = false;
                p.noclip = false;
                p.SetSpeedMult(1f);
                api.SetInfiniteAmmo(false);
                api.SetInfiniteCharge(false);
                api.SetClimbAnywhere(false);
                api.SetInfiniteStamina(false);
            }
        }
    }
}
