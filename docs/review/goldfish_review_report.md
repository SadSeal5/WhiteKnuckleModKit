# Goldfish Annotated Review Report — WKModKit (Round 1)

**Target:** WKModKit projesi — `README.md`, `src\WKModKit.Api\**`, `src\WKModKit.Loader\**`, `src\WKModKit.Sandbox\**`, `installer\install.ps1`, `Directory.Build.props`, `WKModKit.sln` (`full file`)
**Reviewers:** codebase_details (cb), implementation_readiness (imp), structural (str), style (sty)
**Status:** resolved (Round 1 tamamlandı — karar kapısı aşağıda)

---

## Özet

- 🔴 **Blocker:** 4 · 🟡 **Major:** 21 · 🟢 **Nit:** 18 (toplulaştırılmış 43 kayıt)
- Kategori dağılımı: **PROPOSE FIX** 40 · **DECLINE** 3 · **CONTEST** 0 · **ESCALATE** 0
- Oyun API doğrulaması: **0 imza uyumsuzluğu** — kodun tüm oyun-API çağrıları decompile kaynağıyla doğrulandı (imp#15, cb özet).

---

## 🔴 Blockers

### [B1] [imp] Kurulum 1. adımı: zip düzenü kullanıcı kararına bırakılmış
* **Kapsam:** README.md L16–21; install.ps1 L57–58, L70–73
* **Açıklama:** "BepInEx_win_x64_5.4.23.6.zip -> tools/bepinex" extraction düzenünü söylemiyor; `tools\bepinex\BepInEx\core\BepInEx.dll` ve `tools\configmanager\BepInEx\plugins\ConfigurationManager\ConfigurationManager.dll` düzenine sessiz bağımlılık var. Yanlış extract → her şey patlar.
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit:*** README hızlı başlangıç: (1) "zip'i `tools\bepinex` **içine aç** (doğrulama: `tools\bepinex\BepInEx\core\BepInEx.dll` mevcut olmalı)" satırları, (2) ConfigurationManager için `tools\configmanager` hedefi, (3) mevcut `tools\` durum notu (zip elle indirilecek; zip adı dağıtım adından farklı olabilir).

### [B2] [imp] "Kendi modunu yazma" bölümü uygulanamaz
* **Kapsam:** README.md L82–118
* **Açıklama:** csproj şablonu yok; hangi referansların ekleneceği (Assembly-CSharp, UnityEngine modülleri, 0Harmony, oyunun netstandard.dll façadı, `LangVersion`), ProjectReference vs DLL reference ayrımı, Api.dll'nin nereden alınacağı, dağıtım yolu belirsiz.
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit:*** Minimal kopyalanabilir csproj örneği (HintPath'ler `-p:GameDir` olmadan nasıl verilir: `$(GameDir)` yerine mutlak/relative kendi yolu), `dotnet build -c Release` komutu, Api.dll kaynağı (`src\WKModKit.Api\bin\Release\` veya kurulu `BepInEx\plugins\WKModKit\`), "Api.dll'yi mod klasörüne koymak opsiyonel — loader AssemblyResolve ile çözer" notu.

### [B3] [sty+imp+str] Kaldırma talimatı ters okunuyor + 3 farklı liste
* **Kapsam:** install.ps1 L121–122, L128; README.md L28
* **Açıklama:** "…ve UnityCrashHandler hariçini elle silmek yeterli" cümlesi dilbilgisi gereği "oyun dosyalarını sil, mod dosyalarını bırak" okunuyor. Üstelik kaldırma listesi README, installer çıktısı ve installer yorumunda 3 farklı türev.
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit (install.ps1):** Tek kanonik liste her iki dosyada birebir: `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `changelog.txt`, `BepInEx\` + "bunlar dışındaki dosyalara dokunma (oyunun kendi dosyaları)". UnityCrashHandler referansı kaldırılıyor. Her iki metin tek kaynaktan kopyalanıyor.

### [B4] [cb+imp+sty] Watchdog geri-kazanım iddiası Timescale/SpeedMult için yanlış
* **Kapsam:** State.cs L15–16, L72–77, L91–95 (önbellek karşılaştırması); README.md L51; SandboxMenu.cs L296
* **Açıklama:** Kod bu iki ayarı `-_appliedTimescale/_appliedSpeedMult` önbelleğiyle karşılaştırıyor; oyun `CL_GameManager.InitializeGlobals()→ResetTimescale()` (her sahne yükü, CL_GameManager.cs:339, 1154–1160) ve `ENT_Player.Revive()→speedMult=1` (ENT_Player.cs:3062) ile sıfırlıyor. Ölüm/sahne sonrası ayar sessizce 1'e döner, watchdog yeniden uygulamaz.
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit (State.cs):** `timeScale` ve `SpeedMult` her karede koşulsuz yeniden uygulanır (iki ucuz yazma işlemi; değişim-detect önbellekleri kaldırılır). README L51 kalibrasyonu: "watchdog her karede yeniden uygular; bool bayraklar fark-detect ile, sayısal ayarlar (zaman/hız) her karede yazılır."

---

## 🟡 Majors

### [M1] [cb+imp] DisableDeathFloor tek yönlü dünya mutasyonu
* **Kapsam:** State.cs L108–118, L121–134
* **Açıklama:** Zemin y=-100000'e ışınlanıyor; kapatınca geri dönmüyor; `ResetForExit` de geri getirmiyor; `FXManager.fxMan.corruptionHeight`'i de sürüklüyor (DEN_DeathFloor.cs:197–200). Oyun bunun için desteklenen API'ye sahip: `DEN_DeathFloor.SetActive(bool)` / `deathgoo-stop`.
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit (State.cs):** Yeni yaklaşım: pozisyon hilesi tamamen kaldırılır; toggle açılınca `SetActive(false)` (hareket+kill durur), kapatılınca `SetActive(true)` (kaldığı yerden devam eder). `ResetForExit`'e `SetActive(true)` eklenir. (Uygulamadan önce decompile'daki `SetActive` semantiği doğrulanacak.)

### [M2] [imp] Teleport okuma kültür hatası (tr-TR'de 12.5 → 125)
* **Kapsam:** GameApi.cs L306 (`ParseF`) vs L327 (`F` writer, invariant)
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit:** `float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)`.

### [M3] [imp+cb] OnLoad başarısızsa Harmony yamaları kalıyor, hata sayacı eksik
* **Kapsam:** WkLoaderPlugin.cs L174–195; SandboxMod.cs L38–41
* **Açıklama:** `_harmony.PatchAll(...)` sonrası `OnLoad` fırlatırsa mod `_mods`'a girmez → OnUnload çağrılmaz; yamalar config/State bağlantısı olmadan canlı kalır (Sandbox için FallingDamage prefix'i null `_cfg` okuma riski). Başarısız mod `failures` sayacına da eklenmiyor.
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit (WkLoaderPlugin.cs):** catch bloğu: hatayı `failures` listesine ekle + temizlik için `mod.OnUnload()`'u try/catch içinde çağır + açık log ("yama geri alınmayabilir" uyarısı).

### [M4] [imp] Menü imleci oyun menülerinde çalıyor
* **Kapsam:** SandboxMenu.cs L49–54, L96–104
* **Açıklama:** Açılınca her kare None, kapatınca koşulsuz Locked+invisible → ana menüde F6 kapatırsan imleç kaybolur.
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit:** Açılışta `Cursor.lockState/visible` önceki değerleri kaydet; kapanışta kayıtlı değerleri geri yaz (kilit atamayı oyun kendisine bırak).

### [M5] [imp] install.ps1 önce kopyalayıp sonra derliyor + oyun açıkken kilitleniyor
* **Kapsam:** install.ps1 L76–92
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit (install.ps1):** Adımlar yeniden sıralanır: (0) oyun süreci çalışıyor mu kontrolü (`Get-Process "White Knuckle"` → uyarı), (1) derleme, (2) kopyalama. Yarım kurulum riski ortadan kalkar.

### [M6] [imp+imp#11] `-GameDir` sözdizimi, çalışma dizini, sondaki ters eğik çizgi
* **Kapsam:** README.md L20–21; install.ps1 L67–76; Directory.Build.props L4
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit:** README'a tam komut (`powershell -ExecutionPolicy Bypass -File "...\install.ps1" -GameDir "D:\..."`, çalışma dizini bağımsız), installer'da `-GameDir`'den sondaki `\` temizleme, README L139'a "Directory.Build.props L4'ü düzenle veya hep `-p:GameDir` ver" notu.

### [M7] [str] csproj dosyalarında kodlama bozulması (mojibake)
* **Kapsam:** WKModKit.Api.csproj L7,16; WKModKit.Loader.csproj L7; WKModKit.Sandbox.csproj L7,19
* **Açıklama:** `net46→net472` değişimi sırasında ANSI okumalı yeniden yazım Türkçe karakterleri bozmuş ("â€”", "Ã§alÄ±ÅŸÄ±r"). Diğer tüm dosyalar temiz.
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit:** Üç csproj temiz UTF-8 olarak yeniden yazılır.

### [M8] [str] Depo kökünde boş `WKModKit.slnx` artığı
* **Kapsam:** WKModKit.slnx (2 satır, 0 proje); README ağacında yok
* ***AI Suggestion:*** **PROPOSE FIX** — silinir.

### [M9] [str] README'de zorunlu "Alternatives" ve "Risks & Sınırlar" bölümleri yok
* **Kapsam:** README.md genel
* **AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit:** "Tasarım alternatifleri" bölümü eklenir (BepInEx 5 vs 6 kendimiz-loader vs sıfırdan native enjektör vs BepInEx; Mono-Assembly-CSharp doğrudan referans vs reflection; paylaşılan AppDomain kabulü — ret gerekçeleriyle). "Sınırlar & riskler" bölümü eklenir (teleport mutlak koordinat, deathgoo davranışı, oyun güncellemesi, config ilk çalıştırma, test kapsamı).

### [M10] [sty+imp] Sorun giderme satırı non sequitur (doorstop enabled=true)
* **Kapsam:** README.md L148
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit:** Belirtilerle tutarlı teşhis: "oyun penceresi odağı / imleç kilidi; `BepInEx\LogOutput.log`'da sandbox satırını kontrol et".

### [M11] [sty+cb] State.cs yorumu yanlış config dosya adı
* **Kapsam:** State.cs L8 ("WKModKit.Sandbox.cfg" → gerçekte `WKModKit.wkmodkit.sandbox.cfg`)
* ***AI Suggestion:*** **PROPOSE FIX** — yorum düzeltilir (dosya adı `WKModKit.` + Id kalıbıyla yazılır).

### [M12] [sty+cb] Logger yorumu koddaki davranışla çelişiyor + per-mod önek yok
* **Kapsam:** WkLoaderPlugin.cs L253–259; ModContextImpl.cs L42–45
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit:** `BepInEx.Logging.Logger.CreateLogSource("WKModKit." + Id)` ile gerçek per-mod log kaynağı oluşturulur (ConfigurationManager/filtre ayrımı, kod yorumu artık doğru olur).

### [M13] [sty+str] WkHost yorumu var olmayan `GUI_Utils`'e atıf
* **Kapsam:** WkHost.cs L81 (ve L10 "kalbi" metaforu)
* ***AI Suggestion:*** **PROPOSE FIX** — yorum gerçek mekanizmayı yansıtır (receiver'lar kendi IMGUI input işlemlerini yapar; hotkey'ler Event.current'ten okunur).

### [M14] [sty] SandboxMenu alan yorumu yanlış ("Hız/zaman slider durumu")
* **Kapsam:** SandboxMenu.cs L30–32 (aslında toast durumu)
* ***AI Suggestion:*** **PROPOSE FIX** — "Toast bildirimi durumu".

### [M15] [sty] Yardım sekmesinde üç katlı spekülatif cümle + kanıtsız garanti
* **Kapsam:** SandboxMenu.cs L320
* ***AI Suggestion:*** **PROPOSE FIX** — garanti cümlesi DROP, kalan tek cümreyle CALIBRATE ("oyun istatistik kaydı sandbox'tan etkilenmez diye bir garanti yok; achievement/istatistik etkisi oyun sürümüne bağlı").

### [M16] [sty] Config açıklamasında anlamsız terim ("açılı hasarı")
* **Kapsam:** State.cs L32 (kullanıcıya bakan config metni)
* ***AI Suggestion:*** **PROPOSE FIX** — "düşüş hasarı".

### [M17] [cb] Teleport slotları mutlak koordinat — geçersizleşme belgelenmemiş
* **Kapsam:** GameApi.cs L249–326; README.md L46
* **Açıklama:** Oyun ~10000m'de dünyayı yeniden merkezler (`SetWorldOffset`), sahne değişiminde seviyeler yenilenir; kalıcı `teleports.txt` mutlak koordinat eski koşuda boş uzaya ışınlar.
* ***AI Suggestion:*** **PROPOSE FIX**
* ***Proposed Edit:** (a) Kayda koşu seed'i yazılır; ışınlanırken farklı seed uyarısı, (b) README + yardım sekmesine "slotlar koşuya özeldir; yeni koşuda geçersiz" notu. Seviye-göreli saklama v2 işi olarak Sınırlar bölümüne kaydedilir.

### [M18] [cb+sty+str] "gerçek imzalarıyla yorum satırlıdır" abartısı
* **Kapsam:** README.md L118
* ***AI Suggestion:*** **PROPOSE FIX** — "her çağrı oyunun gerçek metot çağrı yollarıyla örneklenmiştir" kalibrasyonu.

### [M19] [cb] `WKModInfo.Id` doc'ı fazla söz veriyor (klasör)
* **Kapsam:** Contracts.cs L11; Context.cs; WkLoaderPlugin.cs L147
* ***AI Suggestion:*** **PROPOSE FIX** — doc düzeltmesi: config dosyası, Harmony kimliği, veri klasörü (mod klasörü değil).

### [M20] [cb] Public olmayan IWKMod implementasyonu sessizce atlanıyor
* **Kapsam:** WkLoaderPlugin.cs L239–249; Contracts.cs L34–44
* ***AI Suggestion:*** **PROPOSE FIX** — açık uyarı logu + sözleşme doc'una "public olmalı" şartı.

### [M21] [cb] Config dosyası ilk çalıştırmada oluşmuyor
* **Kapsam:** ModContextImpl.cs L31–32 (`new ConfigFile(configPath, false)`)
* ***AI Suggestion:*** **PROPOSE FIX** — `saveOnInit: true`.

### [M22] [cb] "SpawnJack ile aynı düzen" iddiası yanlış (-40 vs -3)
* **Kapsam:** GameApi.cs L185–193
* ***AI Suggestion:*** **PROPOSE FIX** — yorum düzeltilir (SpawnJack'a *benzer*, -3 yakın çağrı için bilinçli seçim).

### [M23] [cb] Oyunun hazır `infiniteStamina` bayrağı yerine yeniden icat
* **Kapsam:** State.cs L97–106; ENT_Player.cs L1849, 5300–5303
* ***AI Suggestion:*** **PROPOSE FIX** — `AccessTools` ile `infiniteStamina` alanı set edilir (ammo/charge ile aynı desen); watchdog'da el-drain döngüsü kalkar.

### [M24] [imp] "Öldür (test)" butonu god mode'u config'e yansıtmıyor
* **Kapsam:** GameApi.cs L226–232; SandboxMenu.cs öldür butonu
* ***AI Suggestion:*** **PROPOSE FIX** — buton işleyicisi `State.Set("Player","GodMode",false)` çağırır; toast "God mode kapandı" ekler.

### [M25] [sty+str] Pencere başlığı sürüm + tuş hardcoded ("v1.0 — F6")
* **Kapsam:** SandboxMenu.cs L53; SandboxMod.cs L14, L25
* ***AI Suggestion:*** **PROPOSE FIX** — başlık çalışmada `Info.Version` + config'ten okunan güncel menü tuşu ile üretilir.

---

## 🟢 Nitler

### [N1] [str#7+str#8] Api klasör↔namespace uyumsuzluğu + çok-type dosyalar
* **Kapsam:** `src\WKModKit.Api\Mod\` altında `namespace WKModKit` — **PROPOSE FIX** (dosyalar csproj köküne taşınır: `Contracts.cs`, `Context.cs`; düz tüketicilere-yönelik namespace bilinçli, csproj yorumuna not).
### [N2] [str#5+imp] install.ps1 `.DESCRIPTION` adım sayısı (4 vs 5) — **PROPOSE FIX**
### [N3] [sty+str+cb] Yazım/kodlama kümesi — **PROPOSE FIX** (tek yama):
 * install.ps1 L68 "görümmüyor"→"görünmüyor"; State.cs L9 "menuden"→"menüden", L71 "öre:"→"ör.:"; README L51 `_watchdog_`→`` `watchdog` ``; README L72 "olacaklarından" cümle düzeltmesi; README L18 ConfigurationManager zip adı notu (B1); Context.cs L46 bozuk `///(` düzeltme; WkLoaderPlugin L77 `/*_Location…` → `/* Assembly.Location bazen…`; L165 "deterministlik"→"deterministik"; State.cs L62 "oyun'a"→"oyuna"; sandbox portu "Slot 2'e/4'e"→"2'ye/4'ye" (1/3 doğru kalır); overlay "pasif"→"etkin hile yok" + işaret düzeni (∞ önek / "kapalı" sonek tek düze, `TIRMAN✱`→`TIRMAN HER YERE`); buton-toast ad eşleşmesi ("Öne at"→toast "Öne atıldı: X"); "Işınlanma noktaları" başlığı yeniden yazımı; Dünya sekmesindeki yinelenen zaman slider'ı kaldırılır (Oyuncu sekmesinde kalır); tohum etiketlerinde "(seed)" gloss ikisine de; WkHost "kalbi" metaforu (M13 içinde); RIP toast "Öldün.".
### [N4] [sty#12/13/14] README kalibrasyonları — **PROPOSE FIX**: L3 "tam özellikli" kaldırılır; özellik tablosuna tek satır test-kapsamı notu ("test kapsamı: kurulum+açılış; oynanış özellikleri bir koşuda gözden geçirilmeli"); oyuncu güncelleme uyarısı tek yerde (Sorun Giderme) toplanır.
### [N5] [cb#11+str#9] `ResetForExit` eksik kalıntılar — **PROPOSE FIX**: `p.SetSpeedMult(1f)` + private cheat alanları (ammo/charge/climbanywhere) kapatılır + death floor `SetActive(true)`; yorum daraltılır.
### [N6] [cb#6] Api mod klasöründen bağımsız doc — M19 içinde.
### [N7] [sty#15] Config adı `WKModKit.wkmodkit.sandbox.cfg` çifte marka — **DECLINE** (yeniden adlandırma mevcut kullanıcı configlerini ve ConfigurationManager eşleşmesini bozar; ad zaten doğru belgelenmiş — B6/M11 ile tutarlı). Yeniden adlandırma istenirse Id değişikliği gerekir; v2 kararı olarak Sınırlar bölümüne not.
### [N8] [sty#18] Caps vurgusu (README L77, Contracts L36) — **PROPOSE FIX** (bold).
### [N9] [sty#22+cb] `EntryKey` `\u0001` mantığı — **PROPOSE FIX** (tek satır gerekçe yorumu).
### [N10] [imp#11] Directory.Build.props makine-yolu — M6 içinde (README notu).
### [N11] [str#12] tools/ anlık durum ↔ beklenen düzen — B1 içinde (README'a net el-yapımı adımı yazılır).
### [N12] [sty#34+cb#12] GameApi eşdeğerlik yorumları ("spawnitem komutuyla aynı yol" vs doğrulanmış gerçek: CL_GameManager.SpawnItem L1801 aynı çağrı — bu doğrudur; SpawnJack farklı) — **PROPOSE FIX**: "spawnitem komutuyla aynı yol" iddiası geçerli (decomp onaylı), SpawnJack iddiası M22'de düzeltilir.
### [N13] [str#6] Version sabiti tekilleştirme — M25 içinde.
### [N14] [sty#31] Yinelenen zaman slider iki sekmede — N3 içinde.
### [N15] [sty#26] Başlıkta F6 sabiti — M25 içinde.
### [N16] [cb#7] Sessiz atlas → M20.
### [N17] [imp#12/14] Sorun giderme satırı → M10; WkHost yorumu → M13.
### [N18] [cb#5] State dosya adı → M11.

---

## Uygulama planı (onay sonrası)

1. Kod düzeltmeleri: State.cs (watchdog re-apply, deathfloor SetActive, config ad yorumu, ResetForExit, typo'lar), GameApi.cs (invariant parse, spawn yorumu, seed uyarı altyapısı), SandboxMenu.cs (imleç restore, başlık, yardım metni, overlay, buton-toast), WkLoaderPlugin.cs (OnLoad rollback, sessiz skip logu, per-mod logger, EntryKey gerekçesi), ModContextImpl.cs (saveOnInit), Contracts.cs/Context.cs (doc düzeltmeleri), Api dosya taşıma.
2. README: B1, B2, M9, M10, M16–M18, N4 bölümleri/revizyonları; uninstall listesi tekil kaynak.
3. install.ps1: sıra değişikliği (derle→kopyala), sürec Kontrolü, kaldırma metni, açıklama adımları, GameDir trim.
4. Silme: `WKModKit.slnx`; csproj'lar temiz UTF-8 yeniden yazım.
5. Doğrulama: `dotnet build` (0 uyarı hedefi) → `install.ps1` → oyunla boot smoke test → raporu güncelle.

---

## Round 1 Çözüm Kaydı (uygulandı)

**Karar:** Kullanıcı "round 2 ile devam et" — tüm PROPOSE FIX öğeleri uygulandı, DECLINE öğeleri atlandı ve belgelendi (N7 → "Sınırlar & riskler" bölümüne taşındı).

**Uygulananlar:**
- **Kod:** B4 (Timescale/SpeedMult her karede koşulsuz yazma; önbellek kaldırıldı), M1 (death goo → `DEN_DeathFloor.SetActive`; sadece bizim durdurduğumuzda geri açar), M2 (`ParseF` invariant + `NumberStyles`), M3 (OnLoad hatası → `failures` listesi + `OnUnload` temizliği + XML sözleşme notu), M4 (imleç kaydet/geri yükle; koşulsuz kilit kaldırıldı), M11, M12 (`CreateLogSource("WKModKit." + Id)`), M13, M14, M15, M16, M17 (teleport dosyasına seed; bayat slot uyarısı; eski 3-alanlı dosyalar geriye uyumlu okunuyor), M19, M20 (görünürlük kontrolü artık logluyor + doc), M21 (`saveOnInit: true`), M22, M23 (oyunun `infiniteStamina` bayrağı; el-drain döngüsü kaldırıldı), M24 (öldür butonu config'i de eşitliyor), M25 (başlıkta `ModVersion` + config'den okunan tuş), N1 (Api dosyaları csproj köküne taşındı), N3 (yazım/kodlama kümesi), N5 (ResetForExit tam kapanış), N8, N9.
- **Altyapı:** M7 (üç csproj temiz UTF-8), M8 (`WKModKit.slnx` silindi).
- **install.ps1:** M5 (ön denetimler + derleme önce; oyun süreci kontrolü), M6 (`-GameDir` trim), B3 (tek kanonik kaldırma listesi), N2 (5 adım doğru belgelendi).
- **README:** B1 (zip çıkarma düzeni + doğrulama satırları), B2 (tam minimal csproj şablonu, Api.dll kaynakları, dağıtım notları), B4 kalibrasyonu, M9 ("Tasarım alternatifleri" + "Sınırlar & riskler" bölümleri), M10, M18, N4 (hiperbol kaldırıldı; test-kapsamı notu; uyarı tekelize), N3, N10/N11.
- **DECLINE (belgelendi):** N7 config adı çifte öneki — yeniden adlandırma mevcut kullanıcı configlerini bozar; "Sınırlar & riskler"e not olarak alındı.

**Doğrulama:**
- `dotnet build -c Release` → **0 uyarı / 0 hata**.
- `installer\install.ps1` → yeni akışla (derle → kopyala) sorunsuz; ön denetimler geçti.
- Canlı boot smoke test → `WKModKit: 1 mod yüklendi, 0 hata`; per-mod log kaynağı etiketi görünüyor (`[Info :WKModKit.wkmodkit.sandbox]`); tam logda Error/Exception yok.
- `infiniteStamina` alan adı decompile'da doğrulandı (ENT_Player.cs:1849, kullanım: 5300).

**Bilinen açık maddeler (oyun içi gözlem gerektirir, kullanıcı testinde):** menü tıklama akışı, teleport/hız/zaman görsel davranışı, death goo toggle görsel davranışı.

