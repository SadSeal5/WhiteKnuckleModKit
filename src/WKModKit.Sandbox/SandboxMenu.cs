using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WKModKit.Sandbox
{
    /// <summary>
    /// Sandbox IMGUI menüsü ve hotkey yönetimi. F6 ile açılır/kapanır (config'den değiştirilebilir).
    /// Hotkey'ler Event.current (IMGUI) üzerinden okunur: oyun hangi input sistemiyle çalışırsa çalışsın geçerlidir.
    /// </summary>
    internal sealed class SandboxMenu
    {
        private static readonly string[] Tabs = { "Oyuncu", "Eşyalar", "Düşmanlar", "Dünya", "Yardım" };

        private readonly IModContext _ctx;
        private readonly GameApi _api;
        private readonly string _version;

        private int _tab;

        private Rect _winRect = new Rect(60f, 60f, 600f, 640f);

        // Eşya listesi durumu
        private string _itemSearch = "";
        private Vector2 _itemScroll;
        private int _itemHand = 0;

        // Düşman listesi durumu
        private string _denSearch = "";
        private Vector2 _denScroll;

        // Toast bildirimi durumu
        private string _toast;
        private float _toastUntil;

        // Overlay metni her IMGUI karesinde yeniden kurulmaz; değerler değişince yeniden inşa edilir.
        private string _overlayText;
        private readonly bool[] _ovb = new bool[10];
        private readonly float[] _ovf = new float[2];

        // Menü açılmadan önceki imleç durumu — kapatınca geri yüklenir.
        // Oyunun kendi menülerindeyken (ana menü, duraklatma vb.) imleç çalınmaz:
        // koşulsuz kilitlenmez, yalnız önceden kaydedilen değere döner.
        private bool _menuWasOpen;
        private bool _cursorSaved;
        private CursorLockMode _savedCursorLock;
        private bool _savedCursorVisible;

        // Basılı tutulan tuşun OS otomatik tekrarı (KeyClick ~0.5 sn sonra tekrar KeyDown
        // üretir) toggle'ları flap'lamasın: tuş bırakılana (KeyUp) kadar yeni tetikleme yok.
        // KeyUp oda kaybında kaçarsa tuş latched kalır; bir sonraki bas-bırak döngüsü temizler.
        private readonly HashSet<KeyCode> _keysDown = new HashSet<KeyCode>();

        public SandboxMenu(IModContext ctx, GameApi api, string version)
        {
            _ctx = ctx;
            _api = api;
            _version = version;
        }

        // =============================================================== Ana

        public void Draw()
        {
            Styles.Ensure();
            HandleHotkeys();

            // İmleç geçişi: menü açılırken önceki imleç durumunu kaydet,
            // kapanınca geri yükle. Kilit yönetimini (Locked) oyun kendisi yapar.
            bool open = State.MenuOpen;
            if (open && !_menuWasOpen)
            {
                _savedCursorLock = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
                _cursorSaved = true;
            }
            else if (!open && _menuWasOpen && _cursorSaved)
            {
                Cursor.lockState = _savedCursorLock;
                Cursor.visible = _savedCursorVisible;
                _cursorSaved = false;
            }
            _menuWasOpen = open;

            if (open)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // Küçük çözünürlükte pencere ekran dışına taşmasın (toast/uyarı satırı en altta kalır).
                _winRect.x = Mathf.Clamp(_winRect.x, 0f, Mathf.Max(0f, Screen.width - _winRect.width));
                _winRect.y = Mathf.Clamp(_winRect.y, 0f, Mathf.Max(0f, Screen.height - _winRect.height));
                _winRect = GUI.Window(481523, _winRect, DrawWindow,
                    "WKModKit Sandbox v" + _version + " — kapat: " + _ctx.Config.Get<KeyCode>("Keys", "ToggleMenu"));
            }
            else
            {
                DrawOverlay();
            }
        }

        private void DrawWindow(int id)
        {
            GUILayout.Space(4f);
            _tab = GUILayout.Toolbar(_tab, Tabs, Styles.Tab, GUILayout.Height(26f));
            GUILayout.Space(6f);

            switch (_tab)
            {
                case 0: DrawPlayerTab(); break;
                case 1: DrawItemsTab(); break;
                case 2: DrawDenizensTab(); break;
                case 3: DrawWorldTab(); break;
                default: DrawHelpTab(); break;
            }

            GUILayout.Space(4f);
            if (!string.IsNullOrEmpty(_toast) && Time.unscaledTime < _toastUntil)
                GUILayout.Label(_toast, Styles.Toast);
            if (!_api.InGame)
                GUILayout.Label("⚠ Oyuncu/envanter hazır değil — oyun içine girince tekrar dene.", Styles.Warn);

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        // =============================================================== Hotkey'ler

        private void HandleHotkeys()
        {
            Event e = Event.current;
            if (e == null || e.keyCode == KeyCode.None) return;
            if (e.type == EventType.KeyUp) { _keysDown.Remove(e.keyCode); return; }
            if (e.type != EventType.KeyDown) return;
            // Latch aynı karedeki IMGUI çoklu pass'leri de kapsar (ilk KeyDown tuşu ekler,
            // aynı karedeki sonraki pass'ler atlanır); farklı tuşlar aynı karede özgürce işler.
            if (_keysDown.Contains(e.keyCode)) return; // OS tuş otomatik tekrarı

            var cfg = _ctx.Config;
            var kc = e.keyCode;

            // Menü tuşu (varsayılan F6) işlev tuşudur: arama kutusu odaklıyken bile
            // menüyü kapatma/açma çalışsın; odak denetimi yalnız durum toggle'larına uygulanır.
            bool isMenuKey = kc == cfg.Get<KeyCode>("Keys", "ToggleMenu");

            // Modun arama kutularına yazım yapılıyorken kısayol tetiklenmesin ve e.Use()
            // yazılan karakteri yutmasın (kısayol harf/rakam tuşuna bağlıysa).
            if (!isMenuKey)
            {
                string focused = GUI.GetNameOfFocusedControl();
                if (focused == "itemSearch" || focused == "denSearch") return;
            }

            if (isMenuKey)
            {
                // İmleç kaydet/geri yükle, Draw() içindeki geçiş algısıyla yapılır.
                State.MenuOpen = !State.MenuOpen;
            }
            else if (kc == cfg.Get<KeyCode>("Keys", "GodMode")) { State.Toggle("Player", "GodMode"); Toast("God mode: " + OnOff(State.GodMode)); }
            else if (kc == cfg.Get<KeyCode>("Keys", "InfiniteStamina")) { State.Toggle("Player", "InfiniteStamina"); Toast("Sonsuz stamina: " + OnOff(State.InfiniteStamina) + Degraded("infiniteStamina")); }
            else if (kc == cfg.Get<KeyCode>("Keys", "Fly")) { State.Toggle("Player", "Fly"); Toast("Uçuş: " + OnOff(State.Fly)); }
            else if (kc == cfg.Get<KeyCode>("Keys", "Noclip")) { State.Toggle("Player", "Noclip"); Toast("Noclip: " + OnOff(State.Noclip)); }
            else
            {
                return; // Sandbox'a ait değil; oyunun tuşu olabilir
            }

            _keysDown.Add(kc);
            e.Use();
        }

        // =============================================================== Oyuncu sekmesi

        private void DrawPlayerTab()
        {
            // Alan adları, GameApi.ValidateGameReflection'ın eriştiği ENT_Player private alanlarıdır.
            var miss = _api.MissingReflectionFields;
            GUILayout.BeginVertical(Styles.Box);
            ToggleRow("God mode (hasar/ölüm bağışıklığı)", "Player", "GodMode");
            ToggleRow("Sonsuz stamina (kavrama gücü)", "Player", "InfiniteStamina", !miss.Contains("infiniteStamina"));
            ToggleRow("Sonsuz mermi/kullanım", "Player", "InfiniteAmmo", !miss.Contains("infiniteAmmo"));
            ToggleRow("Sonsuz şarj", "Player", "InfiniteCharge", !miss.Contains("infiniteCharge"));
            ToggleRow("Düşüş hasarı kapalı", "Player", "NoFallDamage");
            ToggleRow("Her yere tutunma (climb anywhere)", "Player", "ClimbAnywhere", !miss.Contains("forceGrabAnywhere"));
            ToggleRow("Uçuş modu (fly)", "Player", "Fly");
            ToggleRow("Noclip", "Player", "Noclip");
            ToggleRow("Düşmanlar hedeflemesin (no target)", "Player", "NoTarget");
            ToggleRow("Ölümcül zemin (death goo) kapalı", "Player", "DisableDeathFloor");
            GUILayout.EndVertical();

            GUILayout.Space(8f);
            GUILayout.Label("Hız ve zaman", Styles.Header);
            GUILayout.BeginVertical(Styles.Box);
            SliderRow("Oyuncu hızı ×", "Player", "SpeedMult", 0.25f, 4f);
            SliderRow("Zaman ölçeği ×", "Time", "Timescale", 0.05f, 2f);
            GUILayout.EndVertical();

            GUILayout.Space(8f);
            GUILayout.Label("Eylemler", Styles.Header);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("İyileş (HP + stamina dolu)", Styles.Button, GUILayout.Height(26f))) { var err = _api.HealFull(); Toast(err ?? "İyileştin."); }
            if (GUILayout.Button("Dirilt", Styles.Button, GUILayout.Height(26f))) { var err = _api.RevivePlayer(); Toast(err ?? "Diriltildin."); }
            if (GUILayout.Button("Öldür", Styles.DangerButton, GUILayout.Height(26f)))
            {
                var err = _api.KillPlayer();
                if (err != null)
                {
                    Toast(err);
                }
                else
                {
                    // Öldürmek oyuncuda god mode'u kapatır; config'i de eşitle ki
                    // watchdog bir sonraki karede tekrar açmasın.
                    if (State.GodMode)
                    {
                        State.Set("Player", "GodMode", false);
                        Toast("Öldün. (God mode de kapatıldı)");
                    }
                    else
                    {
                        Toast("Öldün.");
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        // =============================================================== Eşyalar sekmesi

        private void DrawItemsTab()
        {
            if (_api.ItemPrefabs.Count == 0)
            {
                GUILayout.Label("Eşya veritabanı henüz yüklenmedi. Oyun içine gir ve 'Listeyi tazele'ye bas.", Styles.Warn);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ara:", Styles.Label, GUILayout.Width(40f));
            GUI.SetNextControlName("itemSearch");
            _itemSearch = GUILayout.TextField(_itemSearch, GUILayout.Width(260f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Listeyi tazele", Styles.Button, GUILayout.Width(130f))) { Toast(_api.RefreshPrefabCaches() ? "Liste tazelendi." : "Veritabanı henüz hazır değil — oyun içine gir."); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Eline ver:", Styles.Label, GUILayout.Width(70f));
            _itemHand = GUILayout.Toolbar(_itemHand, new[] { "Sol (0)", "Sağ (1)" }, Styles.Tab, GUILayout.Width(150f));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Toplam: {_api.ItemPrefabs.Count}", Styles.Label);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            _itemScroll = GUILayout.BeginScrollView(_itemScroll, Styles.Box, GUILayout.Height(430f));

            string q = (_itemSearch ?? "").Trim();
            var items = _api.ItemPrefabs.Where(g => g != null && (q.Length == 0 || g.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            if (items.Count == 0)
                GUILayout.Label("Eşleşme yok.", Styles.Label);

            for (int i = 0; i < items.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(i + 1 + ". " + items[i].name, Styles.Label, GUILayout.Width(330f));
                if (GUILayout.Button("Çantaya", Styles.Button, GUILayout.Width(80f)))
                {
                    var err = _api.GiveItemToInventory(items[i]);
                    Toast(err ?? "Çantaya verildi: " + items[i].name);
                }
                if (GUILayout.Button(_itemHand == 0 ? "Sol ele" : "Sağ ele", Styles.Button, GUILayout.Width(80f)))
                {
                    var err = _api.GiveItemToHand(items[i], _itemHand);
                    Toast(err ?? "Ele verildi: " + items[i].name);
                }
                if (GUILayout.Button("Öne at", Styles.Button, GUILayout.Width(70f)))
                {
                    var err = _api.SpawnPrefabToWorld(items[i], false);
                    Toast(err ?? "Öne atıldı: " + items[i].name);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        // =============================================================== Düşmanlar sekmesi

        private void DrawDenizensTab()
        {
            if (_api.DenizenPrefabs.Count == 0)
                GUILayout.Label("Düşman veritabanı henüz yüklenmedi. Oyun içine gir ve 'Listeyi tazele'ye bas.", Styles.Warn);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ara:", Styles.Label, GUILayout.Width(40f));
            GUI.SetNextControlName("denSearch");
            _denSearch = GUILayout.TextField(_denSearch, GUILayout.Width(260f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Listeyi tazele", Styles.Button, GUILayout.Width(130f))) { Toast(_api.RefreshPrefabCaches() ? "Liste tazelendi." : "Veritabanı henüz hazır değil — oyun içine gir."); }
            if (GUILayout.Button("TÜMÜNÜ ÖLDÜR", Styles.DangerButton, GUILayout.Width(140f)))
            {
                int n = _api.KillAllDenizens();
                Toast(n + " düşman öldürüldü.");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            _denScroll = GUILayout.BeginScrollView(_denScroll, Styles.Box, GUILayout.Height(470f));

            string q = (_denSearch ?? "").Trim();
            var dens = _api.DenizenPrefabs.Where(g => g != null && (q.Length == 0 || g.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            if (dens.Count == 0)
                GUILayout.Label("Eşleşme yok.", Styles.Label);

            for (int i = 0; i < dens.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(i + 1 + ". " + dens[i].name, Styles.Label, GUILayout.Width(400f));
                if (GUILayout.Button("Önüne çağır", Styles.Button, GUILayout.Width(110f)))
                {
                    var err = _api.SpawnPrefabToWorld(dens[i], false);
                    Toast(err ?? "Önüne çağrıldı: " + dens[i].name);
                }
                if (GUILayout.Button("Altına çağır", Styles.Button, GUILayout.Width(110f)))
                {
                    var err = _api.SpawnDenizenNearby(dens[i]);
                    Toast(err ?? "Altına çağrıldı: " + dens[i].name);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        // =============================================================== Dünya sekmesi

        private void DrawWorldTab()
        {
            GUILayout.Label("Koşu bilgisi", Styles.Header);
            GUILayout.BeginVertical(Styles.Box);
            GUILayout.Label("Koşu tohumu (seed): " + _api.GetRunSeed(), Styles.Label);
            GUILayout.Label("Dünya tohumu (seed): " + _api.GetWorldSeed(), Styles.Label);
            GUILayout.Label("Oyun sürümü: " + _ctx.GameVersion, Styles.Label);
            GUILayout.EndVertical();

            GUILayout.Space(10f);
            GUILayout.Label("Işınlanma noktaları (konumu slot'a kaydet, kaydedilen konuma ışınlan)", Styles.Header);
            GUILayout.BeginVertical(Styles.Box);
            for (int slot = 1; slot <= 4; slot++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Slot " + slot + (_api.HasTeleport(slot) ? "  ●" : "  ○"), Styles.Label, GUILayout.Width(80f));
                if (GUILayout.Button("Konumu kaydet", Styles.Button, GUILayout.Width(130f)))
                {
                    Toast(_api.SaveTeleport(slot)
                        ? "Slot " + slot + " kaydedildi."
                        : "Oyuncu yok — konum kaydedilmedi.");
                }
                GUI.enabled = _api.HasTeleport(slot);
                if (GUILayout.Button("Işınlan", Styles.Button, GUILayout.Width(90f)))
                {
                    bool staleSeed;
                    if (_api.LoadTeleport(slot, out staleSeed))
                    {
                        var p = _api.Player;
                        if (p != null) p.AddIgnoreFallDamageTime(3f); // oyunun Teleport'u zaten 2sn verir; yüksekten ışınlanmalar için garanti
                        Toast(staleSeed
                            ? "Slot " + slot + " kaydına ışınlandın — dikkat: slot başka bir koşuda (veya eski biçimde) kaydedilmiş olabilir."
                            : "Slot " + slot + " kaydına ışınlandın.");
                    }
                    else
                    {
                        Toast("Işınlanılamadı — slot boş ya da oyuncu yok.");
                    }
                }
                if (GUILayout.Button("Sil", Styles.Button, GUILayout.Width(60f))) { Toast(_api.DeleteTeleport(slot) ? "Slot " + slot + " silindi." : "Slot " + slot + " silinemedi — disk yazma hatası (bkz. BepInEx günlüğü)."); }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        // =============================================================== Yardım sekmesi

        private void DrawHelpTab()
        {
            var cfg = _ctx.Config;
            GUILayout.Label("Kısayollar (config'den değiştirilebilir)", Styles.Header);
            GUILayout.BeginVertical(Styles.Box);
            KeyRow("Menü aç/kapat", cfg.Get<KeyCode>("Keys", "ToggleMenu"));
            KeyRow("God mode", cfg.Get<KeyCode>("Keys", "GodMode"));
            KeyRow("Sonsuz stamina", cfg.Get<KeyCode>("Keys", "InfiniteStamina"));
            KeyRow("Uçuş", cfg.Get<KeyCode>("Keys", "Fly"));
            KeyRow("Noclip", cfg.Get<KeyCode>("Keys", "Noclip"));
            GUILayout.EndVertical();

            GUILayout.Space(8f);
            GUILayout.Label("Notlar", Styles.Header);
            GUILayout.BeginVertical(Styles.Box);
            GUILayout.Label("• Ayarlar BepInEx\\config\\WKModKit.wkmodkit.sandbox.cfg dosyasında saklanır.", Styles.Label);
            GUILayout.Label("• ConfigurationManager kuruluysa F1 ile de tüm ayarlar değiştirilebilir.", Styles.Label);
            GUILayout.Label("• 'Öne at'/'Önüne çağır' baktığın yere değil, doğrultuğa yakın bir noktaya koyar.", Styles.Label);
            GUILayout.Label("• Işınlanma slotları mutlak dünya koordinatı saklar; yeni bir koşuda geçersiz kalabilir (başka koşuda kaydedilen veya tohumu bilinmeyen eski kayıtlar ışınlanırken uyarı verir).", Styles.Label);
            GUILayout.Label("• Hotkey'ler (F7-F10) oyun içine girmeden basılsa da config'e yazar; etkisi oyuna girince uygulanır.", Styles.Label);
            GUILayout.Label("• Kısayolu harf/rakam tuşuna bağlıyorsan oyun içi arama kutusuna yazarken kısayol tetiklenmez (yazdığın karakterler toggle başlatmaz); menü tuşu (varsayılan F6) yazarken de çalışır.", Styles.Label);
            GUILayout.Label("• Hotkey toast bildirimleri yalnız menü açıkken görünür; menü kapalıyken özellik durumunu overlay'den izleyebilirsin.", Styles.Label);
            GUILayout.Label("• Düşüş hasarı kapalıyken oyunun düşüş takibi duraklar: havadayken kapatıp tekrar açarsan yere değdiğinde birikmiş hasar gelebilir — bu ayarı yere değmişken değiştirmen önerilir.", Styles.Label);
            GUILayout.Label("• Hileli koşularda oyunun istatistik/ödül kayıtları etkilenebilir.", Styles.Label);
            GUILayout.Label("• Oyun güncellenirse Assembly-CSharp değişir; modu yeniden derlemek gerekebilir.", Styles.Label);
            GUILayout.EndVertical();
        }

        // =============================================================== Overlay (menü kapalıyken)

        private void DrawOverlay()
        {
            var miss = _api.MissingReflectionFields;
            bool[] b =
            {
                State.GodMode,
                State.InfiniteStamina && !miss.Contains("infiniteStamina"),
                State.InfiniteAmmo && !miss.Contains("infiniteAmmo"),
                State.InfiniteCharge && !miss.Contains("infiniteCharge"),
                State.NoFallDamage,
                State.ClimbAnywhere && !miss.Contains("forceGrabAnywhere"),
                State.Fly,
                State.Noclip,
                State.NoTarget,
                State.DisableDeathFloor,
            };
            float ts = State.Timescale, sp = State.SpeedMult;
            bool changed = _overlayText == null || ts != _ovf[0] || sp != _ovf[1];
            if (!changed)
                for (int i = 0; i < b.Length; i++)
                    if (b[i] != _ovb[i]) { changed = true; break; }

            if (changed)
            {
                var active = new StringBuilder();
                // Gösterim düzeni: özellik adı + durum soneki (∞ = tükenmez, ✕ = kapalı/devre dışı).
                if (b[0]) active.AppendLine("GOD");
                if (b[1]) active.AppendLine("STAMINA ∞");
                if (b[2]) active.AppendLine("MERMİ ∞");
                if (b[3]) active.AppendLine("ŞARJ ∞");
                if (b[4]) active.AppendLine("DÜŞÜŞ HASARI ✕");
                if (b[5]) active.AppendLine("HER YERE TUTUN");
                if (b[6]) active.AppendLine("UÇUŞ");
                if (b[7]) active.AppendLine("NOCLIP");
                if (b[8]) active.AppendLine("HEDEFSİZ");
                if (b[9]) active.AppendLine("ÖLÜMCÜL ZEMİN ✕");
                if (Math.Abs(ts - 1f) > 0.001f) active.AppendLine("ZAMAN ×" + ts.ToString("0.##"));
                if (Math.Abs(sp - 1f) > 0.001f) active.AppendLine("HIZ ×" + sp.ToString("0.##"));
                _overlayText = active.Length > 0 ? active.ToString() : "etkin hile yok";
                for (int i = 0; i < b.Length; i++) _ovb[i] = b[i];
                _ovf[0] = ts;
                _ovf[1] = sp;
            }

            var rect = new Rect(10f, Screen.height - 130f, 170f, 120f);
            GUILayout.BeginArea(rect, Styles.Box);
            GUILayout.Label("WKModKit Sandbox", Styles.OverlayTitle);
            GUILayout.Label(_overlayText, Styles.Overlay);
            GUILayout.EndArea();
        }

        // =============================================================== Yardımcı UI bileşenleri

        private void ToggleRow(string label, string section, string key) { ToggleRow(label, section, key, true); }

        /// <summary>enabled=false: oyun alanı bulunamadığı için özellik bu oturumda kullanılamıyor.</summary>
        private void ToggleRow(string label, string section, string key, bool enabled)
        {
            GUILayout.BeginHorizontal();
            if (!enabled) GUI.enabled = false;
            bool old = _ctx.Config.Get<bool>(section, key);
            bool val = GUILayout.Toggle(old, enabled ? label : label + " (oyun güncellendi — kullanılamıyor)", enabled ? Styles.Toggle : Styles.Label, GUILayout.Width(540f));
            if (enabled && val != old) State.Set(section, key, val);
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void SliderRow(string label, string section, string key, float min, float max)
        {
            GUILayout.BeginHorizontal();
            float old = _ctx.Config.Get<float>(section, key);
            GUILayout.Label(label + "  " + old.ToString("0.##"), Styles.Label, GUILayout.Width(180f));
            float val = GUILayout.HorizontalSlider(old, min, max, GUILayout.Width(320f));
            GUILayout.FlexibleSpace();
            if (Math.Abs(val - old) > 0.0001f) State.Set(section, key, val);
            GUILayout.EndHorizontal();
        }

        private void KeyRow(string label, KeyCode key)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, Styles.Label, GUILayout.Width(240f));
            GUILayout.Label(key.ToString(), Styles.Key, GUILayout.Width(120f));
            GUILayout.EndHorizontal();
        }

        private void Toast(string msg)
        {
            _toast = msg;
            _toastUntil = Time.unscaledTime + 4f;
            _ctx.LogDebug("[sandbox] " + msg);
        }

        private static string OnOff(bool b) { return b ? "AÇIK" : "KAPALI"; }

        // Oyun güncellemesiyle ilgili private alan kaybolduysa: ayar config'e yazılır ama
        // oyuna uygulanamaz (menü satırı da "kullanılamıyor" olarak devre dışı görünür).
        private string Degraded(string field)
        {
            return _api.MissingReflectionFields.Contains(field) ? " (oyun güncellendi — oyuna uygulanamaz)" : "";
        }

        // =============================================================== Stiller

        private static class Styles
        {
            public static GUIStyle Box;
            public static GUIStyle Header;
            public static GUIStyle Label;
            public static GUIStyle Warn;
            public static GUIStyle Button;
            public static GUIStyle DangerButton;
            public static GUIStyle Tab;
            public static GUIStyle Toggle;
            public static GUIStyle Key;
            public static GUIStyle Toast;
            public static GUIStyle OverlayTitle;
            public static GUIStyle Overlay;
            public static Color ToastColor = new Color(1f, 0.85f, 0.4f);

            public static void Ensure()
            {
                if (Box != null) return;

                var bg = MakeTex(new Color(0.10f, 0.10f, 0.12f, 0.95f));
                var bgLight = MakeTex(new Color(0.16f, 0.16f, 0.19f, 0.95f));
                var bgHover = MakeTex(new Color(0.28f, 0.30f, 0.36f, 0.95f));
                var bgDanger = MakeTex(new Color(0.45f, 0.12f, 0.12f, 1f));
                var bgDangerHover = MakeTex(new Color(0.65f, 0.18f, 0.18f, 1f));

                Box = new GUIStyle(GUI.skin.box) { normal = { textColor = Color.white, background = bg }, padding = new RectOffset(8, 8, 8, 8) };
                Header = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(1f, 0.82f, 0.45f) }, fontStyle = FontStyle.Bold, fontSize = 13 };
                Label = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(0.92f, 0.92f, 0.95f) } };
                Warn = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(1f, 0.75f, 0.3f) }, wordWrap = true };
                Button = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.white, background = bgLight }, hover = { textColor = Color.white, background = bgHover }, active = { textColor = Color.white, background = bg }, fontSize = 12 };
                DangerButton = new GUIStyle(Button) { normal = { textColor = new Color(1f, 0.75f, 0.7f), background = bgDanger }, hover = { background = bgDangerHover } };
                Tab = new GUIStyle(GUI.skin.button) { normal = { textColor = new Color(0.85f, 0.85f, 0.9f), background = bgLight }, hover = { textColor = Color.white, background = bgHover }, active = { textColor = new Color(1f, 0.82f, 0.45f), background = bg }, fontSize = 12 };
                Toggle = new GUIStyle(GUI.skin.toggle) { normal = { textColor = new Color(0.92f, 0.92f, 0.95f) }, hover = { textColor = Color.white }, active = { textColor = Color.white }, onNormal = { textColor = new Color(1f, 0.82f, 0.45f) }, onHover = { textColor = new Color(1f, 0.9f, 0.6f) }, fontSize = 12 };
                Key = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(0.6f, 0.85f, 1f) }, alignment = TextAnchor.MiddleRight };
                Toast = new GUIStyle(GUI.skin.label) { normal = { textColor = ToastColor }, fontStyle = FontStyle.Bold };
                OverlayTitle = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(1f, 0.82f, 0.45f) }, fontStyle = FontStyle.Bold, fontSize = 12 };
                Overlay = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(0.8f, 0.85f, 0.9f) }, fontSize = 11, wordWrap = false };
            }

            private static Texture2D MakeTex(Color c)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, c);
                tex.Apply();
                return tex;
            }
        }
    }
}
