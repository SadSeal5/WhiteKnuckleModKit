# Goldfish Annotated Review Report — WKModKit (Round 2)

**Target:** WKModKit projesi — `README.md`, `src\WKModKit.Api\**`, `src\WKModKit.Loader\**`, `src\WKModKit.Sandbox\**`, `installer\install.ps1`, `Directory.Build.props`, `WKModKit.sln`, csproj'lar (`full file`) — Round 1 düzeltmeleri sonrası haliyle
**Reviewers:** codebase_details (cb), implementation_readiness (imp), structural (str), style (sty)
**Status:** resolved — karar kapısı aşağıda

---

## Özet

- 🔴 **Blocker:** 0 (Round 1'de 4 vardı) · 🟡 **Major:** 10 · 🟢 **Nit:** 26 (toplulaştırılmış)
- Kategori: **PROPOSE FIX** 32 · **DECLINE** 3 · **CONTEST** 1 (kanıtla çürütüldü) · **ESCALATE** 0
- Doğrulanan iyileşmeler (reviewer onaylı): mojibake sıfır; "Alternatives/Risks" bölümleri standartı karşılıyor; oyun API kullanımı tamamen doğrulu (0 imza hatası); uninstall listesi tek kaynak; per-mod log kaynağı çalışıyor.
- Yakınsama: Round 2 temiz çıkmadı → Round 2 düzeltmeleri sonrası Round 3 gerekli.

---

## 🔴 Blockers

Yok.

---

## 🟡 Majors

### [B1] [imp#3] "Dirilt" butonu oyunun ölüm akışını yarım bırakıyor
* **Kapsam:** GameApi.cs L244–249 (`RevivePlayer` → `ENT_Player.Revive()`); grounding: CL_GameManager.cs L2129–2237, ENT_Player.cs L3046–3067
* **Açıklama:** `ENT_Player.Kill` → `gMan.Die()` static private `CL_GameManager.dead=true` yapar ve `ReviveSequence`/`EndGameSequence` korutini başlatır (ölüm ekranı, `SetTimescale(0.1f)`, `isPaused=true`). `Revive()` yalnızca oyuncu iç durumunu sıfırlar; static `dead` bayrağına, korutine, zaman ölçeği lerp'ine ve modal ekrana dokunamaz → düğme basıldığında tutarsız durum (ölüm UI'sı açık, imleç kilitli, timescale 0'a lerp'leniyor).
* ***Karar önerisi:*** **PROPOSE FIX** — Düzeltme sırasında oyunun kendi diriltme yolu araştırılacak (console `revive` komutu / `ReviveSequence`); tam yol bulunamazsa düğme "gMan ölüm akışı aktifken" uygun oyun API'siyle güvenli hale getirilecek ya da etki belgelenecek.

### [B2] [imp#4] Loader mod Id benzersizliğini / çift yüklemeyi zorlamıyor
* **Kapsam:** WkLoaderPlugin.cs L114–243; Contracts.cs L7–12; ModContextImpl.cs L28–40
* **Açıklama:** Aynı DLL `wkmods\` üst düzeyde VE `wkmods\<Id>\` altında durursa iki kez yüklenir: tek config dosyası, tek Harmony kimliği ("WKModKit."+Id), yamalar iki kez uygulanır (prefix için zararsız, postfix/transpiler için değil). Id çakışması denetlenmiyor.
* ***Karar önerisi:*** **PROPOSE FIX** — Discover aşamasında (a) aynı dosya yoluna göre dedupe, (b) aynı assembly kısa adına göre ikinci kopya log-error ile atlanır, (c) OnLoad'da Id çakışması denetlenip log-error ile atlanır.

### [B3] [imp#1 + cb#3] README mod csproj şablonu Sandbox desenlerini derleyemez (eksik referanslar)
* **Kapsam:** README.md L99–140 (şablon), L181 (GameApi'ye yönlendirme); çapraz: WKModKit.Sandbox.csproj
* **Açıklama:** Şablon `UnityEngine.dll` façadını içermiyor; rehberin okuyucuyu yönlendirdiği `GameApi.cs` desenleri `PhysicsModule` (Raycast), `TextRenderingModule`, `InputLegacyModule` (KeyCode tabanlı değil ama GUI), ALINE (Item_Object temel tipi) gerektirir. Kopyalayan mod CS0012/eksik-tip hatalarıyla baş başa kalır.
* ***Karar önerisi:*** **PROPOSE FIX** — Şablona `UnityEngine` façad referansı + `UnityEngine.PhysicsModule` eklenir; "kullanacağın Unity tipine göre ilgili modül DLL'ini ekle" notu eklenir.

### [B4] [sty#1] `OnModGUI` catch yorumu kodun yapmadığını söylüyor ("menüyü kapat")
* **Kapsam:** SandboxMod.cs L86–88
* ***Karar önerisi:*** **PROPOSE FIX** — Kod davranışı düzeltilir: menü çizim hatasında `State.MenuOpen = false` (imleç geri yükleme Draw'daki geçiş algısıyla otomatik), log korunur, her kare spam önlanır.

### [B5] [sty#2] Hitap kayması: "sen" vs "siz" karışık (README + UI metinleri)
* **Kapsam:** README.md L16,20–21,30,97,142,179,188–189 vs L39,181,220,226–229; SandboxMenu.cs L109 vs L196; GameApi.cs L147
* ***Karar önerisi:*** **PROPOSE FIX** — Tüm kullanıcıya bakan metinlerde tek hitap: **"sen" (informal)** seçilir (UI metinlerinde baskın olan bu).

### [B6] [sty#3] Overlay–menü–README öznite terminolojisi uyumsuz ("TIRMAN" vs "tutunma", "GOO" vs "ölümcül zemin")
* **Kapsam:** SandboxMenu.cs L373 ("HER YERE TIRMAN"), L377 ("GOO ✕") vs L153/L54
* ***Karar önerisi:*** **PROPOSE FIX** — Overlay satırları menü/tab adlarıyla hizalanır: "HER YERE TUTUN" ve "ZEMİN ✕".

### [B7] [sty#4] Smoke-test kapsamı iddiaları troubleshooting tablosuna kadar kalibre edilmemiş
* **Kapsam:** README.md L57, L62, L226–229 (+ L64 mevcut nota rağmen)
* ***Karar önerisi:*** **PROPOSE FIX** — Üstte tek belirgin "Test durumu" notu (boot düzeyinde doğrulandı; oynanış davranış bekintileri kod kaynaklı) + tablo satırlarına kalibre edilmiş dil.

### [B8] [cb#1] Teleport bayat-seed uyarısı koşu içinde de atıyor (yanlış pozitif)
* **Kapsam:** GameApi.cs L274–291; grounding: CL_GameManager.cs L1261–1280 (`GetRunSeed` = `WorldLoader.seed`), WorldLoader.cs L1312 (`seed++` her nesil devamında)
* **Açıklama:** `GetRunSeed()` nesil devamlarında artan `WorldLoader.seed`'i döndürüyor → aynı koşuda kaydedilen slot sonradan "başka koşuda kaydedilmiş" uyarısı veriyor. Kalıcı seviyeler taşınmadığı için koordinat hâlâ geçerli.
* ***Karar önerisi:*** **PROPOSE FIX** — Kayıt ve karşılaştırmada koşu boyunca kararlı `WorldLoader.startingSeed` (L320, Initialize'da bir kez set) kullanılır.

### [B9] [imp#2] install.ps1 kodlama gereksinimi (UTF-8 BOM) belgelenmemiş
* **Kapsam:** installer\install.ps1; README
* **Açıklama:** BOM diskte **doğrulandı** (EF BB BF — bu oturumda; imp'in "doğrulayamadım" notu çözüldü). Yalnızca gereksinim hiçbir yerde yazmıyor: ileride dosyayı düzenleyen biri BOM'suz kaydederse PS 5.1 ANSI okur, Türkçe mesajlar bozulur.
* ***Karar önerisi:*** **PROPOSE FIX** — README "Gereksinimler"e tek satır: "install.ps1 UTF-8 BOM ile kaydedilmelidir (PowerShell 5.1)."

### [B10] [cb#2 bağlantılı; str#1 ile çelişkili] Api.dll kaynak sıralaması — imp#7 ile birlikte ele alınır (aşağıda N-List)

*(structural'ın "ALINE.dll kullanılmıyor" Major'ı **CONTEST** edildi: ALINE referansı kaldırıldığında derleme 52 hatayla çöktü — `Item_Object`'ın temel tipi `MonoBehaviourGizmos` ALINE içinde. Referans zorunlu; bulgu sürüm oyun güncellemesinde kırılma riski notu olarak "Sınırlar"a taşınır.)*

---

## 🟢 Nitler (toplulaştırılmış)

| # | Kaynak | Kısa açıklama | Öneri |
|---|---|---|---|
| N1 | str#2 | README `---` ayraç simetrisi (son 5 bölümde yok) | FIX — tüm H2'lerden önce ayraç |
| N2 | str#3 | `docs\` README ağacında yok; review raporu izleri | FIX — ağaca `docs/ (önemli: review raporları)` ekle |
| N3 | str#4 | Api `RootNamespace` asimetrisi | DECLINE — mod yüzeyi için bilinçli (`using WKModKit;` basitliği), R1 kararına uygun |
| N4 | str#5 | Sandbox üye sıralaması karışık (`_deathFloorNeutralized` ortada; GameApi alanları dağınık) | FIX — alanlar sınıf başına toplanır |
| N5 | str#6 | Api çok-type dosyalar (`Contracts.cs`, `Context.cs`) | DECLINE — R1 karar kapısında onaylanmış düzen korundu (tematik sözleşme dosyaları) |
| N6 | str#7 | `tools\` içinde kalan zip artıkları | FIX — README'ye "zip'lere artık gerek yok, silebilirsin" notu |
| N7 | sty#5 + sty#6 | Test notu ve hile uyarısı tekrarları | FIX — tekilleştir/harmonize; yardım sekmesindeki gevşek kuyruk cümlesi atılır |
| N8 | sty#7 | SandboxMod "tam sandbox modu" vurgusu gereksiz | FIX — "tam" kaldırılır |
| N9 | sty#8 + sty#12 | README L189 şişirme + çift gerekçe; L6 "küçük" | FIX |
| N10 | sty#9 | `ParseF` yorumundaki hata örneği yanlış (tr-TR'de parse başarısız olur → 0 döner, 125 değil) | FIX — yorum gerçek davranışa düzeltilir |
| N11 | sty#10 | Görünürlük log satırındaki sözleşme parantezi kalabalık | FIX — kısaltılır |
| N12 | sty#11 | Mimari diyagram `<ModId>` vs API'de `Id` | FIX — diyagramda `<Id>` |
| N13 | imp#5 | İndirilecek zip dosya adları sabitlenmemiş (`BepInEx_win_x64_5.4.23.6.zip` vb.) | FIX — dosya adları yazılır |
| N14 | imp#6 | Mod derleme çıktı yolu (`bin\Release\MyMod.dll`) yazılmamış | FIX |
| N15 | imp#7 | Api.dll referans kaynaklarının önkoşul sırası sessiz | FIX — "önce depoyu bir kez derle ya da kurulumu çalıştır" cümlesi |
| N16 | imp#8 | install.ps1'de `dotnet` varlık denetimi yok | FIX — ön denetim bloğuna ekle |
| N17 | imp#9 | NoFallDamage havadayken aç-kapa: birikmiş düşüş hasarı toptan gelebilir | FIX — yardım sekmesine uyarı + config açıklamasına not |
| N18 | imp#10 | `_deathFloorNeutralized` sahne geçişinde bayat (örnek değişince yanlış geri açma riski) | FIX — instance kimliğini izle; değiştiyse bayrağı sıfırla |
| N19 | imp#11 | Hotkey'ler oyun dışı durumlarda da config'i değiştiriyor | FIX — yardım sekmesine not |
| N20 | imp#12 + cb#6 | Slider sürüklerken kare başına diske yazma | FIX — `Set` içinde 0.5 sn throttle + kapanışta flush (loader OnDestroy → ctx.Flush) |
| N21 | cb#2 | Patches.cs'te `State.Noclip` kontrolü oyunda zaten var olan guard ile yedekli | FIX — yoruma gerekçe notu |
| N22 | cb#4 | `GiveItemToHand` el doluysa çantaya yönlendirilen eşya için "Ele verildi" diyor | FIX — post-check ile doğru mesaj |
| N23 | cb#7 | AccessTools FieldInfo null denetimi yok (güncelleme sonrası her kare istisna) | FIX — OnLoad'da tek seferlik doğrulama + net hata logu |
| N24 | cb#5 | Death floor bayrak yaşam döngüsü belgelenmemiş | FIX — N18 ile birlikte State.cs yorumuna eklenir |

**CONTEST (kanıtla):** [str#1] ALINE.dll "ölü referans" — kaldırma testi 52 derleme hatası verdi (CS0012: `MonoBehaviourGizmos`); referans zorunludur. Sürüm-notu "Sınırlar"a eklenir: ALINE oyunla geldiği için pratik risk yok.

---

## Uygulama planı (onay sonrası)

1. **Kod:** B1 (Dirilt akışı — önce oyunun tam diriltme yolu decompile'da araştırılır), B2 (loader dedupe/Id denetimi), B4, B5–B6 (UI metinleri), B8 (startingSeed), N4, N18/N24, N20 (throttle+flush), N21, N22, N23.
2. **Loader/Api:** B2, N11, N23 sözleşme notları.
3. **README/install.ps1:** B3, B9, B7, N1, N2, N6, N7, N9, N12–N17, N19(?yardım sekmesinde), N10 (yorum GameApi).
4. **Doğrulama:** derleme (0/0) → kurulum → boot smoke test → rapor güncelleme → Round 3.

---

## Round 2 Çözüm Kaydı (uygulandı)

**Karar:** Kullanıcı "Düzelt ve Round 3" — 32 PROPOSE FIX uygulandı; 3 DECLINE (N3, N5 + config adı zaten belgeli) gerekçeleriyle korundu; str#1 (ALINE) **CONTEST** edildi: referans kaldırma testi 52 derleme hatası verdi (CS0012 — `Item_Object` temel tipi `MonoBehaviourGizmos` ALINE içinde). Referans zorunlu; bağımlılık notu README "Sınırlar"a eklendi.

**Uygulananlar:**
- **B1:** Oyun diriltme akışı decompile'da araştırıldı: `Die()` disk kaydı varsa otomatik `ReviveSequence` (tam kayıt-yükleme akışı) çalıştırıyor; `ENT_Player.Revive()` tek başına statik `dead` bayrağına/korutine dokunamıyor. Düzeltme: `GameApi.RevivePlayer()` artık `CL_GameManager.isDead()` (public static) ile gate'leniyor, oyun akışı aktifken net mesaj dönüyor; menü toast'u bunu gösteriyor.
- **B2:** Loader'a çift-yükleme koruması (assembly adı dedupe) + Id çakışması denetimi (log-error + atla, failures sayacına yazılıyor).
- **B3:** README csproj şablonuna `UnityEngine` façad + `UnityEngine.PhysicsModule` referansları ve "kullanılan her Unity tipi için ilgili modülü ekle" notu.
- **B4:** Menü çizim hatasında `State.MenuOpen = false` (yorum ↔ kod eşleşti; her kare istisna spam'i kapanıyor).
- **B5:** Tüm kullanıcıya bakan metinler "sen" hitabına standardize edildi (README + menü mesajları).
- **B6:** Overlay "HER YERE TUTUN" + "ZEMİN ✕" (menü/tab adlarıyla hizalı).
- **B7:** Üstte belirgin "Test durumu" notu; kullanım bölümündeki mükerrer not kaldırıldı; Sorun giderme tablosu nota bağlandı.
- **B8:** Teleport kayıt/karşılaştırma `WorldLoader.startingSeed`'e (public, koşu boyunca sabit) taşındı; GetRunSeed artık yalnız görüntülemede.
- **B9:** BOM diskte doğrulandı; gereksinim README "Gereksinimler"e yazıldı.
- **Nit kümesi:** N1 (ayraç simetrisi), N2 (docs/ ağaca eklendi), N4 (alan sırası), N6 (zip notu), N7 (yardım notları harmonize; gevşek kuyrak cümlesi atıldı), N8, N9, N10 (ParseF yorumu gerçek davranışa düzeltildi), N11, N12 (`<Id>`), N13 (zip dosya adları), N14 (çıktı yolu), N15 (önkoşul), N16 (`dotnet` denetimi), N17+N19 (yardım sekmesi notları), N18/N24 (death floor instance kimliği izleme + belgeleme), N20 (0.5 sn throttle + loader kapanışında Flush), N21 (yorum), N22 (el dolu → "çantaya verildi" dürüst rapor), N23 (OnLoad'da AccessTools alan doğrulaması).

**Doğrulama:**
- `dotnet build -c Release` → **0 uyarı / 0 hata**.
- `installer\install.ps1` → sorunsuz (yeni ön denetimler dahil); BOM korunuyor (EF BB BF).
- Canlı boot smoke test → "WKModKit: 1 mod yüklendi, 0 hata"; logda Error/Exception yok.
