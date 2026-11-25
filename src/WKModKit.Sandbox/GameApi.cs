using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WKModKit.Sandbox
{
    /// <summary>
    /// Oyunun iç API'sine ince bir sarmalayıcı: oyuncu erişimi, private cheat bayrakları,
    /// eşya/düşman spawn, ışınlanma noktaları ve yardımcı işlemler.
    /// </summary>
    internal sealed class GameApi
    {
        private readonly IModContext _ctx;
        private FieldInfo _fiInfiniteAmmo;
        private FieldInfo _fiInfiniteCharge;
        private FieldInfo _fiForceGrabAnywhere;
        private FieldInfo _fiInfiniteStamina;
        private List<GameObject> _itemPrefabs;
        private List<GameObject> _denizenPrefabs;
        private readonly Dictionary<int, TeleportPoint> _teleports = new Dictionary<int, TeleportPoint>();

        public GameApi(IModContext ctx)
        {
            _ctx = ctx;
        }

        // ---- Oyuncu ----

        private ENT_Player _playerCache;
        private int _playerCacheFrame = -1;

        // Oyuncu araması sahne taranmasıdır (ENT_Player.GetPlayer → GameObject.Find);
        // menü çizimi kare içinde defalarca sorduğu için sonuç kare boyunca önbelleklenir.
        public ENT_Player Player
        {
            get
            {
                int frame = Time.frameCount;
                if (_playerCacheFrame == frame) return _playerCache;
                _playerCache = ENT_Player.GetPlayer();
                _playerCacheFrame = frame;
                return _playerCache;
            }
        }

        public bool InGame => Player != null && Inventory.instance != null;

        public bool HasInfiniteAmmo()
        {
            var p = Player;
            return p != null && p.HasInfiniteAmmo();
        }

        public bool HasInfiniteCharge()
        {
            var p = Player;
            return p != null && p.HasInfiniteCharge();
        }

        public bool IsClimbAnywhere()
        {
            var p = Player;
            var f = ForceGrabAnywhereField;
            // Alan bulunamazsa (oyun güncellemesi) özellik "kapalı" sayılır; yüklemede
            // tek seferlik hata zaten loglanır (ValidateGameReflection).
            return p != null && f != null && (bool)f.GetValue(p);
        }

        public bool HasInfiniteStamina()
        {
            var p = Player;
            var f = InfiniteStaminaField;
            return p != null && f != null && (bool)f.GetValue(p);
        }

        // Her Set* yönteminde FieldInfo null denetimi zorunlu: oyun güncellenip alan adı
        // değişirse özellik sessizce "kapalı" sayılır — watchdog her kare NRE basmaz,
        // ApplyWatchdog gövdesinin kalanı (noTarget, hız, death-goo) çalışmaya devam eder.
        public void SetInfiniteAmmo(bool b) { var p = Player; var f = InfiniteAmmoField; if (p != null && f != null) f.SetValue(p, b); }
        public void SetInfiniteCharge(bool b) { var p = Player; var f = InfiniteChargeField; if (p != null && f != null) f.SetValue(p, b); }
        public void SetClimbAnywhere(bool b) { var p = Player; var f = ForceGrabAnywhereField; if (p != null && f != null) f.SetValue(p, b); }
        public void SetInfiniteStamina(bool b) { var p = Player; var f = InfiniteStaminaField; if (p != null && f != null) f.SetValue(p, b); }

        private FieldInfo InfiniteAmmoField => _fiInfiniteAmmo ?? (_fiInfiniteAmmo = AccessTools.Field(typeof(ENT_Player), "infiniteAmmo"));
        private FieldInfo InfiniteChargeField => _fiInfiniteCharge ?? (_fiInfiniteCharge = AccessTools.Field(typeof(ENT_Player), "infiniteCharge"));
        private FieldInfo ForceGrabAnywhereField => _fiForceGrabAnywhere ?? (_fiForceGrabAnywhere = AccessTools.Field(typeof(ENT_Player), "forceGrabAnywhere"));
        private FieldInfo InfiniteStaminaField => _fiInfiniteStamina ?? (_fiInfiniteStamina = AccessTools.Field(typeof(ENT_Player), "infiniteStamina"));

        // ---- Kamera ----

        public Transform CameraTransform
        {
            get
            {
                var p = Player;
                if (p != null && p.cam != null) return p.cam.transform;
                if (Camera.main != null) return Camera.main.transform;
                return null;
            }
        }

        /// <summary>Baktığın noktayı ray ile bulur. Not: "hedefe spawn" kipi (atAimPoint: true)
        /// menüde şu an kullanılmıyor — menü tuşları hep "önüne" koyar; kip yalnız API düzeyinde erişilebilir.</summary>
        public Vector3 AimPoint(float maxDistance, out bool hitSomething)
        {
            var cam = CameraTransform;
            if (cam == null) { hitSomething = false; return Vector3.zero; }
            RaycastHit rh;
            // Trigger collider'lar (görünmez hacimler/alan tetikleyicileri) ışını yakalamasın;
            // yoksa spawn noktası katı geometri yerine görünmez bir hacmin üstüne düşebilir.
            if (Physics.Raycast(cam.position, cam.forward, out rh, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                hitSomething = true;
                return rh.point + rh.normal * 0.35f;
            }
            hitSomething = false;
            return cam.position + cam.forward * (maxDistance * 0.6f);
        }

        // ---- Eşya işlemleri ----

        public List<GameObject> ItemPrefabs
        {
            get
            {
                if (_itemPrefabs == null) RefreshPrefabCaches();
                return _itemPrefabs ?? new List<GameObject>();
            }
        }

        public List<GameObject> DenizenPrefabs
        {
            get
            {
                if (_denizenPrefabs == null) RefreshPrefabCaches();
                return _denizenPrefabs ?? new List<GameObject>();
            }
        }

        /// <summary>Veritabanını yeniden okuyup prefab listelerini tazeler. Veritabanı henüz
        /// hazır değilse (oyun içine girilmemiş/yüklenmemiş) false döner ve listeler boş kalır.</summary>
        public bool RefreshPrefabCaches()
        {
            try
            {
                var db = CL_AssetManager.GetFullCombinedAssetDatabase();
                _itemPrefabs = db != null && db.itemPrefabs != null
                    ? db.itemPrefabs.Where(g => g != null).Distinct().OrderBy(g => g.name, StringComparer.OrdinalIgnoreCase).ToList()
                    : new List<GameObject>();
                _denizenPrefabs = db != null && db.denizenPrefabs != null
                    ? db.denizenPrefabs.Where(g => g != null).Distinct().OrderBy(g => g.name, StringComparer.OrdinalIgnoreCase).ToList()
                    : new List<GameObject>();
                _ctx.LogInfo($"[sandbox] Prefab önbelleği: {_itemPrefabs.Count} eşya, {_denizenPrefabs.Count} düşman.");
                return true;
            }
            catch (Exception e)
            {
                _ctx.LogWarning($"[sandbox] Prefab veritabanı henüz hazır değil: {e.Message}");
                _itemPrefabs = new List<GameObject>();
                _denizenPrefabs = new List<GameObject>();
                return false;
            }
        }

        /// <summary>Eşyayı envanter ekranına (çantaya) verir — oyunun kendi spawnitem komutuyla aynı yol.</summary>
        public string GiveItemToInventory(GameObject itemPrefab)
        {
            if (itemPrefab == null) return "Prefab yok";
            var itemObj = itemPrefab.GetComponent<Item_Object>();
            if (itemObj == null || itemObj.itemData == null) return $"{itemPrefab.name}: Item_Object/itemData bulunamadı";
            if (Inventory.instance == null) return "Envanter henüz hazır değil (oyun içine gir).";

            Vector3 slot = new Vector3(0f, 0f, 1f) + UnityEngine.Random.insideUnitSphere * 0.01f;
            Inventory.instance.AddItemToInventoryScreen(slot, itemObj.itemData.GetClone(), true, false, true);
            return null;
        }

        /// <summary>Eşyayı direkt eline verir (0 = sol, 1 = sağ).</summary>
        public string GiveItemToHand(GameObject itemPrefab, int handId)
        {
            var p = Player;
            if (p == null) return "Oyuncu yok";
            if (itemPrefab == null) return "Prefab yok";
            var itemObj = itemPrefab.GetComponent<Item_Object>();
            if (itemObj == null || itemObj.itemData == null) return $"{itemPrefab.name}: Item_Object/itemData bulunamadı";
            if (Inventory.instance == null) return "Envanter henüz hazır değil (oyun içine gir).";

            var hand = p.hands != null && handId >= 0 && handId < p.hands.Length ? p.hands[handId] : null;
            if (hand == null) return "El bulunamadı";
            // Oyun, el doluysa eşyayı sessizce çantaya yönlendirir (Inventory.AddItemToHand).
            // El doluluğu çağrıdan ÖNCE kontrol edilir: çağrıdan sonra oyun her durumda elde
            // tutulan bir item bileşeni döndürdüğü için post-check hiçbir zaman bilgi vermez.
            bool handWasFull = hand.GetHandItem() != null;
            Inventory.instance.AddItemToHand(itemObj.itemData.GetClone(), hand);
            return handWasFull
                ? "El doluydu; oyun çantaya yönlendirdi: " + itemPrefab.name
                : null;
        }

        /// <summary>Prefabı dünyaya, baktığın noktaya (veya önüne) düşürür.</summary>
        public string SpawnPrefabToWorld(GameObject prefab, bool atAimPoint)
        {
            var cam = CameraTransform;
            if (cam == null) return "Kamera yok — oyuna gir.";
            if (prefab == null) return "Prefab yok";

            Vector3 pos;
            if (atAimPoint)
            {
                bool hit;
                pos = AimPoint(30f, out hit);
            }
            else
            {
                pos = cam.position + cam.forward * 1.4f;
            }

            var rot = Quaternion.LookRotation(cam.forward);
            var go = UnityEngine.Object.Instantiate(prefab, pos, rot);
            var root = WorldLoader.GetCurrentLevelParentRoot();
            if (root != null) go.transform.SetParent(root);
            return null;
        }

        /// <summary>Düşmanı oyuncunun yakınında, alttan yukarı çağırır (oyunun SpawnJack'i -40 birim altta çağırır; burada -3 ile daha yakına).
        /// Not: SpawnPrefabToWorld eşyayı seviye köküne (GetCurrentLevelParentRoot) bağlar — seviye teardown'ı ile birlikte yok olur;
        /// bu çağrı ebeveynsiz doğurur; düşman seviye yeniden üretilse de sahnede kalır.</summary>
        public string SpawnDenizenNearby(GameObject prefab)
        {
            var p = Player;
            if (p == null) return "Oyuncu yok";
            if (prefab == null) return "Prefab yok";
            UnityEngine.Object.Instantiate(prefab, p.transform.position + Vector3.up * -3f, Quaternion.identity);
            return null;
        }

        /// <summary>Sahnedeki tüm canavarları öldürür.</summary>
        public int KillAllDenizens()
        {
            int count = 0;
            // Liste üzerinde gezinirken değişiklik olabilir → kopya üzerinde gez.
            var snapshot = GameEntity.entities != null ? GameEntity.entities.ToList() : new List<GameEntity>();
            foreach (var ent in snapshot)
            {
                var d = ent as Denizen;
                if (d == null || d.dead) continue;
                count++;
                d.Kill("sandbox", null);
            }
            return count;
        }

        // ---- Oyuncu eylemleri ----

        /// <summary>Canı ve kavgrama gücünü (stamina) doldurur. Oyuncu yoksa mesaj döner.</summary>
        public string HealFull()
        {
            var p = Player;
            if (p == null) return "Oyuncu yok";
            p.Heal(p.maxHealth);
            // Stamina da dolsun (oyuncu seviyesinde): tüm eller maksimuma.
            for (int i = 0; i < p.hands.Length; i++)
            {
                var hand = p.hands[i];
                if (hand != null) hand.SetGripStrength(p.GetCurrentGripStrengthTimer(i));
            }
            return null;
        }

        /// <summary>
        /// Oyuncuyu öldürür. Oyun Kill'i yalnızca !dead && !IsReviving() && !godmode
        /// koşulunda işler (ENT_Player.Kill): godmode burada kapatılır; oyuncu ölüyse ya
        /// da oyun diriliş sekansındaysa oyun sessizce işlemez, bu metot bunu mesajla belirtir.
        /// </summary>
        public string KillPlayer()
        {
            var p = Player;
            if (p == null) return "Oyuncu yok";
            if (p.dead) return "Oyuncu zaten ölü.";
            if (CL_GameManager.gMan != null && CL_GameManager.gMan.IsReviving())
                return "Oyun şu an diriliş sekansında — biraz sonra dene.";
            p.SetGodMode(false);
            p.Kill("sandbox", null);
            return null;
        }

        /// <summary>
        /// Oyuncuyu diriltir. Dikkat: oyunun ölüm akışı (CL_GameManager.Die → ölüme/revive
        /// sekansı, statik dead bayrağı, timescale) aktifken tek başına Revive() o akışı
        /// düzeltmez ve tutarsız durum bırakır — bu durumda oyunun kendi akışı beklenmelidir
        /// (disk kaydı varsa oyun otomatik ReviveSequence çalıştırır).
        /// </summary>
        public string RevivePlayer()
        {
            var p = Player;
            if (p == null) return "Oyuncu yok";
            if (CL_GameManager.isDead())
                return "Ölüm akışı aktif — oyunun kendi dirilme ekranını bekle.";
            p.Revive();
            return null;
        }

        // ---- Işınlanma noktaları ----

        public class TeleportPoint
        {
            public Vector3 Pos;
            public Quaternion Rot;
            // Slot kaydedildiğindeki koşunun kararlı tohumu (WorldLoader.startingSeed).
            // Koşunun nesil tohumu (WorldLoader.seed) ilerledikçe artar; mutlak dünya
            // koordinatı oyun sahne/kayıt geçişlerinde geçersiz olabilir, tohum uyuşmazlığı
            // taşınan kullanıcıyı uyarır.
            public string Seed;
        }

        private string TeleportFilePath => Path.Combine(_ctx.DataDirectory, "teleports.txt");

        /// <summary>O anın konumunu slot'a yazar. Oyuncu yoksa (oyun dışı) veya disk yazma
        /// başarısızsa false döner; disk hatasında uyarı loglanmıştır.</summary>
        public bool SaveTeleport(int slot)
        {
            var p = Player;
            if (p == null) return false;
            _teleports[slot] = new TeleportPoint { Pos = p.transform.position, Rot = p.transform.rotation, Seed = GetRunStableSeed() };
            return SaveTeleportsToDisk();
        }

        /// <summary>
        /// Slot'a ışınlanır. Slot başka bir koşuda kaydedildiyse — ya da eski biçimli
        /// (seed'siz) dosyadan geldiği için hangi koşuda kaydedildiği bilinmiyorsa —
        /// <paramref name="staleSeed"/> true döner: mutlak koordinat o koşuda geçersiz
        /// olabilir (boş uzaya düşebilirsin).
        /// </summary>
        public bool LoadTeleport(int slot, out bool staleSeed)
        {
            staleSeed = false;
            TeleportPoint t;
            if (!_teleports.TryGetValue(slot, out t)) return false;
            var p = Player;
            if (p == null) return false;
            string currentSeed = GetRunStableSeed();
            // Koşu tohumu bilinmiyorsa (eski biçimli kayıt: slot|pos|rot) karşılaştırma
            // yapılamaz: "aynı koşu" garantisi verilemediği için oyunda iken uyarı verilir.
            // Koşu etkin değilken (currentSeed "-") karşılaştırma anlamsızdır; uyarı basılmaz.
            if (currentSeed != "-")
                staleSeed = string.IsNullOrEmpty(t.Seed) || t.Seed != currentSeed;
            p.Teleport(t.Pos, t.Rot, false);
            return true;
        }

        public bool HasTeleport(int slot) { return _teleports.ContainsKey(slot); }

        /// <summary>Slot'u siler ve diske yazar. Slot yoksa veya disk yazma başarısızsa
        /// false döner (yazma hatasında uyarı loglanmıştır).</summary>
        public bool DeleteTeleport(int slot)
        {
            if (!_teleports.Remove(slot)) return false;
            return SaveTeleportsToDisk();
        }

        public void LoadTeleportsFromDisk()
        {
            _teleports.Clear();
            try
            {
                if (!File.Exists(TeleportFilePath)) return;
                foreach (var line in File.ReadAllLines(TeleportFilePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    // Yeni biçim: slot|seed|pos|rot — eski dosyalar: slot|pos|rot (seed bilinmiyor).
                    if (parts.Length != 4 && parts.Length != 3) continue;
                    int slot;
                    if (!int.TryParse(parts[0], out slot)) continue;
                    int off = parts.Length == 4 ? 1 : 0;
                    string seed = parts.Length == 4 ? parts[1] : null;
                    var posParts = parts[1 + off].Split(',');
                    var rotParts = parts[2 + off].Split(',');
                    if (posParts.Length != 3 || rotParts.Length != 4) continue;
                    _teleports[slot] = new TeleportPoint
                    {
                        Seed = seed,
                        Pos = new Vector3(ParseF(posParts[0]), ParseF(posParts[1]), ParseF(posParts[2])),
                        Rot = new Quaternion(ParseF(rotParts[0]), ParseF(rotParts[1]), ParseF(rotParts[2]), ParseF(rotParts[3])),
                    };
                }
            }
            catch (Exception e)
            {
                _ctx.LogWarning($"[sandbox] Işınlanma dosyası okunamadı: {e.Message}");
            }
        }

        // Okuma invariant kültür ile yapılmalı: dosya da invariant yazılır (bkz. SaveTeleportsToDisk).
        // tr-TR gibi kültürlerde kültür-duyarlı okuma "12.5"teki noktayı binlik ayıracı sanıp
        // Float biçimini reddederdi (TryParse false döner, sonuç 0): koordinatlar sessizce (0,0,0)'a düşerdi.
        private static float ParseF(string s) { float v; float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v); return v; }

        private bool SaveTeleportsToDisk()
        {
            try
            {
                var lines = _teleports
                    .OrderBy(kv => kv.Key)
                    .Select(kv => string.Join("|",
                        kv.Key.ToString(),
                        kv.Value.Seed ?? "-",
                        F(kv.Value.Pos.x) + "," + F(kv.Value.Pos.y) + "," + F(kv.Value.Pos.z),
                        F(kv.Value.Rot.x) + "," + F(kv.Value.Rot.y) + "," + F(kv.Value.Rot.z) + "," + F(kv.Value.Rot.w)))
                    .ToArray();
                File.WriteAllLines(TeleportFilePath, lines);
                return true;
            }
            catch (Exception e)
            {
                _ctx.LogWarning($"[sandbox] Işınlanma dosyasına yazılamadı: {e.Message}");
                return false;
            }
        }

        private static string F(float v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }

        // ---- Bilgi ----

        /// <summary>Koşunun kararlı tohumu: WorldLoader.seed nesil devamlarında artar (seed++),
        /// startingSeed ise Initialize'da bir kez set edilir ve koşu boyunca değişmediği için
        /// "aynı koşu mu" karşılaştırmasında bu kullanılır.</summary>
        public string GetRunStableSeed()
        {
            try { return WorldLoader.instance != null ? WorldLoader.instance.startingSeed.ToString() : "-"; }
            catch { return "-"; }
        }

        public string GetRunSeed()
        {
            // Oyun hiçbir şey yüklenmemişken 69420 başlangıç değerini döndürür; gMan doğduğunda
            // bu, koşu başına bir kez üretilen rastgele fallbackSeed ile değişir. Yani oyun
            // dışında gösterilen değer hangisi olursa olsun gerçek koşu tohumu değildir —
            // bu yüzden yalnız oyun içindeyken gösterilir.
            try { return InGame ? CL_GameManager.GetRunSeed().ToString() : "-"; }
            catch { return "-"; }
        }

        public string GetWorldSeed()
        {
            try { return WorldLoader.instance != null ? WorldLoader.instance.GetSeed().ToString() : "-"; }
            catch { return "-"; }
        }

        // ---- Oyun alan doğrulama ----

        // Doğrulamada bulunamayan private alan adları: menü/overlay bu özellikleri
        // "kullanılamıyor" olarak işaretler (bkz. SandboxMenu).
        internal readonly HashSet<string> MissingReflectionFields = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// AccessTools ile erişilen private alanların varlığını bir kez doğrular.
        /// Oyun güncellenip alan adı değişirse, watchdog içinde her kare istisna basmak
        /// yerine yüklemede net bir hata verilmiş olur. Alan bulunamayan özellikler
        /// güvenli biçimde "kapalı" sayılır (bkz. Is*/Set* yöntemleri).
        /// </summary>
        public void ValidateGameReflection()
        {
            string[] fieldNames = { "infiniteAmmo", "infiniteCharge", "forceGrabAnywhere", "infiniteStamina" };
            foreach (var name in fieldNames)
            {
                if (AccessTools.Field(typeof(ENT_Player), name) == null)
                {
                    MissingReflectionFields.Add(name);
                    _ctx.LogError($"[sandbox] ENT_Player '{name}' alanı bulunamadı — oyun güncellenmiş olabilir; WKModKit'i yeniden derle.");
                }
            }
        }
    }
}
