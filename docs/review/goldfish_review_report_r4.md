# Goldfish Annotated Review Report — WKModKit (Round 4)

**Target:** WKModKit projesi — R3 düzeltmeleri sonrası haliyle
**Reviewers:** codebase_details (cb), implementation_readiness (imp), structural (str), style (sty)
**Status:** karar kapısında

---

## Özet

- 🔴 **Blocker:** 0 · 🟡 **Major:** 5 (hepsi style) · 🟢 **Nit:** ~36
- **Oyun-API kanıt zinciri tamamen temiz** (cb: 1 Nit): R3'te uygulanan tüm düzeltmeler — watchdog guard'ları, Kill/Revive gate'leri, teleport `startingSeed`, death-goo örnek izleme, el-dolu ön-kontrol, hotkey KeyUp-latch — decompile kaynaklarıyla doğrulandı. imp reviewer ayrıca latch tasarımını "Event.repeat Editor-only; HashSet yaklaşımı doğru olan" diye onayladı.
- **implementation_readiness: 0 Major** — kurulum ve mod-yazma akışları sıfır-karar eşiğini geçti. **structural: 0 Major** — R3'ün tüm yapısal Majör'leri (Hedefler bölümü, ayraçlar, şablon bütünlüğü, sln GUID) temiz doğrulandı.
- Yakınsama: R4 temiz değil (5 Major) → 2 ardışık temiz tur kuralı bu sefer maks. 5 tur sınırında **matematiksel olarak ulaşılabilir değil**. Öneri: R4 düzelt → R5 son tur; R5 sonucundan bağımsız olarak kalanları bilinen-sorun listesiyle kapat (R5'te 0 Blocker/0 Major çıkarsa "temiz-kabul" sayılır).

---

## 🟡 Majors (hepsi style)

### [R4-S1] [sty#1] "Konumu kaydet" sessiz no-op'ta başarı toast'u basıyor
* `GameApi.SaveTeleport` oyuncu yokken sessizce döner; menü koşulsuz "Slot N kaydedildi." der. F6 menüsü oyun dışında açılabildiği için yol erişilebilir.
* ***Öneri:*** **PROPOSE FIX** — SaveTeleport bool döner; toast gate'lenir.

### [R4-S2] [sty#2] "Liste tazelendi." hatay-path'tinde yalan söylüyor
* `RefreshPrefabCaches` hatayı yutup listeyi boş bırakır; toast "tazelendi" derken hemen üstteki uyarı "yüklenmedi" der — iki komşu UI ögesi çelişir.
* ***Öneri:*** **PROPOSE FIX** — RefreshPrefabCaches bool döner; toast gate'li ("Liste tazelendi" / "Veritabanı hazır değil").

### [R4-S3] [sty#3] AimPoint yorumu menüde var olmayan özelliği anlatıyor
* Yorum "(menüden 'buraya spawn' için)" diyor; menü hiçbir çağrıda `atAimPoint: true` geçmiyor — UI açısından ölü yol. Yardım sekmesi asıl davranışı doğru anlatıyor.
* ***Öneri:*** **PROPOSE FIX** — yorum yeniden yazılır: hedef-noktası kipi şu an yalnız API düzeyinde erişilebilir.

### [R4-S4] [sty#4] R3'te yazdığım Id yorumu bozuk cümle içeriyor ("görmekte zorlanan")
* ***Öneri:*** **PROPOSE FIX** — "Boş/yalnızca boşluktan oluşan Id'ler: tip adına düşer" biçiminde.

### [R4-S5] [sty#5] Timescale config betimlemesi dilbilgisi bozuk ("RefreshTimescale'i değeri ...")
* ConfigurationManager F1 panelinde görünen kullanıcı metni.
* ***Öneri:*** **PROPOSE FIX** — "Oyunun RefreshTimescale işlevi değeri her karede sıfırladığı için mod geçerli değeri her karede yeniden uygular."

---

## 🟢 Nitler ve kararlar

| # | Kaynak | Açıklama | Öneri |
|---|---|---|---|
| k1 | cb#1 | GetRunSeed yorumu: 69420 yalnız başlangıç; gMan varken rastgele `fallbackSeed` basılır | FIX — yorum netleştirilir (gate kararı doğru kalır) |
| k2 | sty#6 | L21 "etkilenir" kesin iddia; L9/hesap sekmesi "etkilenebilir" | FIX — "etkilenebilir" |
| k3 | sty#7 | "Oyun açıkken çalıştırılmaz" — özne ve okuyucu kararı belirsiz | FIX — "Betik oyun açıkken çalışmayı reddeder; önce oyunu kapat." |
| k4 | sty#8 | Sorun giderme düzyazısındaki "(bkz. Test durumu notu)" hata ayıklamaya karar katmıyor | FIX — çıkarılır |
| k5 | sty#9 | "ek maliyet getirmez" + "maliyetsizdir" aynı maddede iki kez | FIX — tekleştirilir |
| k6 | sty#10 | "şablonda/şablona dahildir" karışık | FIX — "şablona dahildir" |
| k7 | sty#11 | GameApi "yokunda" → "yoksa" | FIX |
| k8 | sty#12 | "ENT_PLAYER.'x'" bozuk biçim | FIX — "ENT_Player 'x'" |
| k9 | sty#13 | SandboxMod OnLoad log + Context.cs doc: F1'i ConfigurationManager şartı olmadan vaat ediyor | FIX — "kuruluysa" eklenir |
| k10 | sty#14 | "Slot N'e ışınlandın" → hedef konum, slotun kendisi değil | FIX — "Slot N kaydına ışınlandın"; başlık "kaydedilen konuma ışınlan" |
| k11 | sty#15 | Birinci çoğul kalıntıları: WkLoaderPlugin ("almadık/ver"), SandboxMenu ("kilitlemeyiz/döneriz"), install.ps1 ("temizleyelim") | FIX — edilgen çatı |
| k12 | sty#16 | "bilinçli kabul edilir" zarf eksik | FIX — "bilinçli bir tercihtir" |
| k13 | sty#17 | "Veri klasörü :" kolon hizası keyfi | FIX — "Veri klasörü:" |
| k14 | str#1 | İlk H2 ("Hedefler") öncesi ayraç yok — simetri bozuk | FIX — `---` eklenir |
| k15 | str#2 | Ağaç README.md ve .gitignore'u listelemiyor | FIX — eklenir |
| k16 | str#3 | props notu GameManagedDir/Generated props eksik | FIX — not genişletilir |
| k17 | str#4 | docs/ notu gerçek derinliği (docs/review) yansıtmıyor | FIX |
| k18 | str#5 | Şablon `PrivateAssets` ögesi repo csproj'larında kullanılan attribute biçiminden farklı | FIX — attribute biçimi |
| k19 | str#6 | "bkz. csproj'lar" — Api csproj'u netstandard referansı içermiyor | FIX — "bkz. Loader ve Sandbox csproj'ları" |
| k20 | str#7 | Problem Statement yalnız örtük (README's L3) | DECLINE — README spec değil; açılış cümlesi görevi görüyor |
| k21 | str#8 | install.ps1 banner uzunlukları/faz numaraları düzensiz | FIX — banner uzunlukları eşitlenir (faz yeniden numaralandırma DECLINE) |
| k22 | str#9 | Dosya adları "Wk" öneki karışık | DECLINE — yeniden adlandırma akışı bozar (churn) |
| k23 | str#10 | Api dosya=tip paketleme | DECLINE — R3'te onaylı karar (n4) |
| k24 | str#11 | Awake/OnDestroy arası ~250 satya yardımcı | DECLINE — kozmetik; taşıma churn |
| k25 | str#12 | Nested tip yerleşimi dosyalar arası farklı | DECLINE — kozmetik |
| k26 | str#13 | KeyCode/System.Text tam nitelik karışımı | FIX — State'a using UnityEngine; SandboxMenu'ya using System.Text |
| k27 | str#14 | Loader csproj'ta kullanılmayan IMGUIModule referansı | FIX — kaldırılır (derleme doğrular) |
| k28 | imp#1 | OnUnload: ResetForExit fırlatırsa UnpatchSelf atlanır | FIX — UnpatchSelf ayrı try/finally |
| k29 | imp#2 | Watchdog hatası her kare logu spam'ler | FIX — aynı hata 5 sn'de bir loglanır |
| k30 | imp#3 | Latch kenarları: (a) aynı karede ikinci tuş kaybolur; (b) metin kutusuna yazarken harf-kısayol tetiklenir; (c) KeyUp kaybı | FIX — (a) global frame guard'ı kaldır (latch yeterli); (b) TextField'lara kontrol adı + odak denetimi + yardım sekmesi notu; (c) yorum |
| k31 | imp#4 | Overlay her kare StringBuilder çöpü üretir | FIX — değişince yeniden inşa edilen önbellek |
| k32 | imp#5 | Menü açıkken !InGame her IMGUI pass'te GameObject.Find | FIX — InGame kare-başına önbellek (watchdog aramasına dokunulmaz) |
| k33 | imp#6 | Flush, OnUnload'ten önce: mod kapanışta config yazarsa kaybolur | FIX — OnDestroy'ta ikinci Flush |
| k34 | imp#7 | Id path-geçersiz karakter denetimi yok | FIX — GetInvalidFileNameChars denetimi + net hata |
| k35 | imp#8 | Yeniden kurulum doorstop_config.ini özelleştirmesini sıfırlar | FIX — README'ye yeniden-kurulum notu |
| k36 | imp#9 | ConfigurationManager kopya hatası kurulumu yarıda bırakır | FIX — try/catch + uyarı + devam |
| k37 | imp#10 | Reflection-degraded kipte menü/overlay özelliği aktif gösterir | FIX — eksik alan için satır devre dışı etiketi + overlay satırı atlanır |
| k38 | imp#11 | GameDir yoksa anlamsız CS0006; hızlı-hata yok | FIX — Directory.Build.props'a EnsureGameDir Error hedefi |

---

## Uygulama planı (onay sonrası)

1. Kod: S1–S5, k1, k7–k13, k26–k32, k33, k34, k37.
2. README/install.ps1/props/csproj: k2–k6, k14–k19, k21, k27, k35, k36, k38.
3. Doğrulama: **derleme + kurulum betiği düzeyinde** (oyun testi yok) → rapor güncelleme → **Round 5 (son tur)** → R5 sonucu ne olursa olsun kapanış (0 Blocker/0 Major = temiz-kabul; kalırsa bilinen-sorun listesi).

---

## Round 4 Çözüm Kaydı (uygulandı)

**Karar:** Kullanıcı "Düzelt ve R5 son tur" — 5 Major + PROPOSE FIX Nitleri uygulandı; DECLINE'lar: k20 (Problem Statement bölümü — README spesifikasyon değil, açılış cümlesi görevi görüyor), k22 (dosya yeniden adlandırma — churn), k23 (Api paketleme — R3'te onaylı), k24/k25 (kozmetik üye yerleşimi — churn), k21'in faz-yeniden-numaralandırma kısmı (yalnız banner uzunlukları eşitlendi).

**Uygulananlar:**
- **S1:** `SaveTeleport` bool döner; toast gate'li ("Oyuncu yok — konum kaydedilmedi.").
- **S2:** `RefreshPrefabCaches` bool döner; iki "Listeyi tazele" toast'u gate'li.
- **S3:** AimPoint ölü-yol yorumu dürüstleştirildi: kip menüde kullanılmıyor, API düzeyinde erişilebilir.
- **S4:** Id trim yorumu düzeltildi ("Boş/yalnızca boşluktan oluşan Id'ler").
- **S5:** Timescale config betimlemesi dilbilgisi düzeltildi ("Oyunun RefreshTimescale işlevi değeri...").
- **k1:** GetRunSeed yorumu: `fallbackSeed` mekaniği anlatıldı (69420 → gMan doğunca rastgele); gate kararının gerekçesi sağlamlaştı.
- **k30:** Latch kenarları — global `_lastHotkeyFrame` kaldırıldı (latch zaten IMGUI çoklu-pass'i kapsıyor; farklı tuşlar artık aynı karede engellenmiyor), arama kutularına `GUI.SetNextControlName("itemSearch"/"denSearch")` + `GUI.GetNameOfFocusedControl()` odak denetimi (yazım sırasında kısayol tetiklenmez, e.Use() karakter yutmaz), KeyUp kaybı belgelendi, yardım sekmesine not.
- **k31:** Overlay metni artık snapshot karşılaştırmalı önbellek — değer değişmedikçe kare başına çöp üretimi yok.
- **k32:** `GameApi.Player` kare-içi önbellek (menünün `!InGame` kontrolü IMGUI başına Find yapmıyor artık; watchdog akışı dokunulmadı).
- **k37:** `ValidateGameReflection` eksik alanları `MissingReflectionFields` set'ine yazar; menü satırı "(oyun güncellendi — kullanılamıyor)" etiketiyle devre dışı, overlay satırı basılmaz — degraded kip artık UI'da dürüst.
- **k28:** OnUnload: durum sıfırlama ve Harmony geri alma ayrı try bloklarında (ResetForExit hata verse de UnpatchSelf çalışır).
- **k29:** Watchdog hata logu 5 sn throttle.
- **k33:** OnDestroy'ta OnUnload döngüsünden sonra ikinci Flush (mod kapanışta config yazarsa kaybolmaz).
- **k34:** Id `Path.GetInvalidFileNameChars()` denetimi + net hata mesajı.
- **k36:** ConfigurationManager kopyası try/catch — opsiyonel adım hatası kurulumu yarıda bırakmaz.
- **k38:** Directory.Build.props'a `EnsureGameDir` Error hedefi — yanlış GameDir'de CS0006 yerine tek net Türkçe mesaj (canlı tetikle doğrulandı: `error : Oyun kurulumu bulunamadı: 'C:\bozuk_yol'. ...`).
- **k27:** Loader csproj'tan kullanılmayan IMGUIModule referansı kaldırıldı.
- **k11/k13/k12/k2–k6/k7–k10/k14–k19/k35/k21:** dil/chatı/belge düzeltmeleri (edilgen çatı, kolon hizası, ağaca README+.gitignore, docs/review derinliği, PrivateAssets attribute biçimi, "etkilenebilir" kalibrasyonu, yeniden-kurulum doorstop notu, banner uzunlukları).

**Doğrulama:**
- `dotnet build -c Release` → **0 uyarı / 0 hata**.
- `EnsureGameDir` hedefi sahte GameDir ile tetiklendi → tek net hata mesajı (beklenen davranış).
- `installer\install.ps1` → sorunsuz; BOM korunuyor (EF BB BF).
- Oyun testi yapılmadı (kullanıcı talebi); doğrulama derleme + kurulum düzeyinde.
