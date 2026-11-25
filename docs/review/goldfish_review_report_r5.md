# Goldfish Annotated Review Report — WKModKit (Round 5 — Son Tur)

**Target:** WKModKit projesi — R4 düzeltmeleri sonrası haliyle
**Reviewers:** codebase_details (cb), implementation_readiness (imp), structural (str), style (sty)
**Status:** final karar kapısı — bu turdan sonra reviewer döngüsü kapanır

---

## Özet

- 🔴 **Blocker:** 0 · 🟡 **Major:** 6 · 🟢 **Nit:** ~25
- **Kanıt zinciri:** R4'te uygulanan tüm kritik yollar (çift oyun denetimi, Id doğrulama sırası, ikinci Flush, bölünmüş OnUnload, kare-içi Player önbelleği + fake-null güvenliği, overlay önbelleği, throttle, reflection UI gating, doorstop notu, şablon bütünlüğü) imp ve cb tarafından uçtan uca doğrulandı. `Player` önbelleğinin sahne geçişlerinde Unity fake-null semantiğiyle güvenli olduğu kanıtlandı.
- **Dürüst tablo:** R4'te uyguladığım düzeltmelerin **üçünde regresyon/enkasan** tespit edildi (sty M1 çifte yönelme eki; M2/str2/cb3/imp3 — dört reviewer'ın aynı anda yakaladığı yanlış üyeye yapışan XML özeti; str1 banner hizası). Ve cb, R3'teki degraded-mode uygulamasında bıraktığım gerçek bir kod boşluğunu buldu (B1). Döngü tamamen temiz çıkmadı — 2 ardışık temiz tur kuralı bu turda karşılanamadı.

---

## 🟡 Majors

### [R5-B1] [cb#1] `SetInfiniteAmmo`/`SetInfiniteCharge` null FieldInfo guard'ı eksik — gerçek NRE hatası
* R3'te degraded-mode guard'ları yalnız ClimbAnywhere/InfiniteStamina'ya eklenmiş; bu ikisi es geçilmiş. Oyun güncellenip alan adı değişirse + config'te özellik açıksa: `Has*` oyun metodundan false, config true → watchdog **her kare** null FieldInfo'ya SetValue → NRE → ApplyWatchdog gövdesi o satırda durur (noTarget, hız, death-goo sessizce uygulanmaz). README L245'in "güvenli biçimde devre dışı" iddiasıyla çelişir.
* ***Öneri:*** **PROPOSE FIX** — kardeşleriyle aynı `f != null` guard'ı.

### [R5-B2] [cb#2] Eski biçimli (seed'siz) slotlar çapraz-koşu uyarısı vermiyor — README iddiasıyla çelişki
* `t.Seed` boşsa karşılaştırma atlanıyor; parser legacy `slot|pos|rot` satırlarını bilinçli destekliyor. README "farklı koşuda kaydedilen slot uyarı verir" diyor.
* ***Öneri:*** **PROPOSE FIX** — oyunda iken seed'i bilinmeyen slot da "eski/başka koşuya ait olabilir" uyarısı verir; mesaj ve README/help metni buna göre netleştirilir.

### [R5-B3] [sty M1] "Slot 1'e kaydına ışınlandın" — çifte yönelme eki (R4 regresyonu)
* "kaydına" ekini ekleyince `SlotSuffix` düşürülmemiş; "1'e kaydına" dilbilgisi hatası, dört slotta da.
* ***Öneri:*** **PROPOSE FIX** — "Slot N kaydına ışınlandın"; `SlotSuffix` ölü kod olarak silinir.

### [R5-B4] [sty M2 + str#2 + cb#3 + imp#3] `MissingReflectionFields` alanına metodun XML özeti kopyalanmış (R4 regresyonu; 4 reviewer aynı bulgu)
* ***Öneri:*** **PROPOSE FIX** — alana, alanı anlatan tek satırlık özgeçmiş; öz yalnız metodda kalır; "doğrulama" bölüm grubuna `// ---- X ----` ayraç başlığı.

### [R5-B5] [sty M3] Eşya verme toast'ları gözlemlenememiş sonuç iddia ediyor
* "Çantaya verildi / Ele verildi / oyun çantaya yönlendirir" — canlı koşuda hiç doğrulanmadı.
* ***Öneri:*** **DECLINE (gerekçeli)** — yönlendirme koşulu bu turda cb tarafından decompile'da satır numarasıyla yeniden doğrulandı (Inventory.AddItemToHand L1001–1010: `GetHandItem() != null → AddItemToInventoryScreen`); çağrı istisnasız döndüğünde doğrulanmış oyun kod yolu anlatılır. Kanıt sınırları zaten README'deki "Test durumu" notuyla beyan edilmiş; metinler mekanizmayı değil sonucu iddia edecek şekilde hafifçe yumuşatılır (kabul edilebilir orta yol: "El doluydu; çantaya yönlendirildi").

### [R5-B6] [str#1] install.ps1 `Write-Host` durum satırlarında kolon hizası (R4 normalizasyonunun yarım kalması)
* "Oyun        :" (12 kolon) vs "Proje kökü  :" (11) — `# ----` satırlarını eşitlemiştim, bunları değil.
* ***Öneri:*** **PROPOSE FIX** — iki satır 12 kolona hizalanır.

---

## 🟢 Nitler ve kararlar

| # | Kaynak | Açıklama | Öneri |
|---|---|---|---|
| m1 | imp#1/cb#4 | F6 da odak denetimine takılıyor (işlev tuşu metin girmez) | FIX — ToggleMenu guard'dan muaf (yazarken de menü kapanır); yardım notu güncellenir |
| m2 | imp#2/cb | F8 hotkey toast'u degraded kipte yanıltıcı | FIX — "(oyun güncellendi — uygulanamaz)" eki |
| m3 | imp#4 | `-GameDir "C:\"` → "C:" → kafa karıştırıcı hata | FIX — trim öncesi uzunluk denetimi |
| m4 | imp#5/cb#6 | Throttle yorumu "aynı hata" varsayıyor; pencere içi ikinci farklı hata görünmez | FIX — yorum dürüstleştirilir ("pencere içinde ilk hata") |
| m5 | imp#6 | Şablonda 0Harmony önkoşulu belirtilmemiş (kurulum gerektirir) | FIX — Api yorumuna cümle eklenir |
| m6 | imp#7 | Menü penceresi küçük çözünürlüklerde ekrana sığımayabilir | FIX — Draw'da clamp |
| m7 | cb#4 | Toast'lar yalnız menü açıkken görünür (hotkey bildirimi kapalı menüde görünmez) | FIX — yardım sekmesi notu |
| m8 | cb#5 | "Slot N boş." — oyuncu yokken de aynı mesaj | FIX — "Işınlanılamadı — slot boş ya da oyuncu yok." |
| m9 | sty N1 | İnvariant yorumu açılış cümlesi telegraf üslubu | FIX — yeniden yazım |
| m10 | sty N2 | "bize ait değil" — biz-sesi kalıntısı | FIX — "Sandbox'a ait değil; oyun görebilir." |
| m11 | sty N3 | "terimi gereksizdir" — yanlış adlandırma | FIX — "koşulu gereksizdir" |
| m12 | sty N4 | "Sil" toast'u kalıcılık başarısında da basıyor | FIX — DeleteTeleport bool döner, toast gate'li |
| m13 | sty N5 | "Kurulum tamamlandı!" kısmi kurulumda da basıyor | FIX — bayraklı: sandbox/CM kurulamadıysa "kısmen tamamlandı" |
| m14 | sty N6 | Hedefler API maddesi iç içe cümlede boğulmuş | FIX — sade yazım |
| m15 | sty N7 | README "önüne at" vs menü "Öne at" | FIX — "öne at" |
| m16 | sty N8 | `Author = "sen"` yer tutucu belirsiz | FIX — `Author = "Adın"` |
| m17 | sty N9 | İstatistik uyarısı iki kez (L9 + L23) | FIX — kapsam-dışı maddesinde tekrar kısaltılır |
| m18 | sty N10 | "Seviye-göreli saklama ileride eklenebilir" — spekülatif yol haritası | FIX — çıkarılır |
| m19 | sty N11 | "kadarıyla sınırlıdır" kalıbı kaba | FIX — sade yazım |
| m20 | sty N12 | EnsureGameDir yorumu "özellik hesaplandıktan" → "özellikler hesaplandıktan" | FIX |
| m21 | str#3 | props ağaç notunda `GameDataDir` eksik | FIX — eklenir |
| m22 | str#4 | Contracts.cs/State.cs'te ölü `using System;` | FIX — kaldırılır |
| m23 | str#5 | Overlay alanları sınıf ortasında, dosya düzeniyle çelişiyor | FIX — üstteki alan grubuna taşınır |
| m24 | str#6 | decomp csproj'u Directory.Build.props kapsamına düşer | DECLINE — yeniden üretilebilir tooling çıktısı, gitignore'lı, sln dışı |

---

## Kapanış planı

R5 son turdur; **R6 yapılmayacak.** Onaylanan seçenek: PROPOSE FIX'ler uygulanır → derleme + kurulum doğrulaması → kapanış kaydı (bu rapora yazılır; DECLINE'lar gerekçeleriyle "bilinen kararlar" listesinde).

Bundan sonra R5 Major/Nit'lerin tamamı çözümlenmiş kabul edilerek döngü **kapatılır**: 5 turda bulgu eğrisi 43 → 42 → 47(dedupe ~36) → ~41(dedupe 5M+36n) → 6M+~25n; kod-API kanıt zinciri son turda da 0 imza hatası.

---

## Round 5 Kapanış Kaydı (uygulandı — döngü KAPANDI)

**Karar:** Kullanıcı "Uygula ve kapat" — 6 Major + PROPOSE FIX Nit'leri uygulandı. DECLINE'lar: **R5-B5** (eşya-verme toast'ları: yönlendirme mekanizması cb tarafından decompile'da satır numarasıyla yeniden doğrulandı — Inventory.AddItemToHand L1001–1010; yine de metin "El doluydu; oyun çantaya yönlendirdi" olarak mekanizmaya çekildi, kanıt sınırları README "Test durumu" notunda beyanlı), **m24** (decomp csproj'u Directory.Build.props kapsamı: yeniden üretilebilir tooling çıktısı, gitignore'lı, sln dışı).

**Uygulananlar:**
- **R5-B1:** `SetInfiniteAmmo`/`SetInfiniteCharge`'a kardeşleriyle aynı `f != null` guard'ı + tüm setter grubuna açıklayıcı yorum (watchdog NRE zinciri kapandı).
- **R5-B2:** `LoadTeleport`: koşu tohumu bilinmeyen (eski biçimli `slot|pos|rot`) kayıtlar da oyun içindeyken `staleSeed=true` verir; README + yardım sekmesi metni "başka koşuda kaydedilen veya tohumu bilinmeyen eski kayıtlar" olarak netleştirildi.
- **R5-B3 (regresyon):** "Slot N kaydına ışınlandın" — `SlotSuffix` yardımcısı ölü kod olarak silindi; çifte yönelme eki giderildi.
- **R5-B4 (4 reviewer'ın ortak bulgusu, regresyon):** `MissingReflectionFields` alanının üstündeki yanlış XML özeti kaldırıldı (alan /* yorumuyla belgelendi), özet yalnız `ValidateGameReflection`'da, bölüm `// ---- Oyun alan doğrulama ----` ayracıyla ayrıldı.
- **R5-B6 (yarım kalan normalizasyon):** install.ps1 `Write-Host` durum satırları 10 kolon etiket genişliğine hizalandı ("Oyun :" / "Proje kökü :").
- **m1:** Menü tuşu (F6) arama-kutusu odak denetiminden muaf — yazarken de menü açılıp kapanır; yardım sekmesinde not.
- **m2:** F8 hotkey toast'una degraded kip eki: "(oyun güncellendi — oyuna uygulanamaz)".
- **m3:** install.ps1: trim sonrası sürücü-kökü denetimi → net hata mesajı.
- **m4:** Watchdog throttle yorumu dürüstleştirildi: "pencere başına yalnız ilk istisna loglanır; pencere içindeki farklı hata bir sonraki pencereye kadar görünmez".
- **m5:** csproj şablonunda 0Harmony önkoşul notu (BepInEx\core yalnız kurulumdan sonra orada).
- **m5-B5 yumuşatma:** "El doluydu; çantaya verildi" → "El doluydu; oyun çantaya yönlendirdi" (iddia mekanizmaya çekildi).
- **m6:** Menü penceresi `Draw()` içinde ekran sınırlarına clamp'lenir (küçük çözünürlükte toast/uyarı satırı kaybolmaz).
- **m7:** Yardım sekmesi: toast bildirimleri yalnız menü açıkken görünür notu.
- **m8:** "Slot N boş." → "Işınlanılamadı — slot boş ya da oyuncu yok."
- **m9:** İnvariant okuma yorumu düz cümleye yeniden yazıldı.
- **m10/m11:** "Sandbox'a ait değil; oyunun tuşu olabilir" + "!State.Noclip koşulu gereksizdir".
- **m12:** `DeleteTeleport` bool döner (SaveTeleportsToDisk bool'a çevrildi; SaveTeleport da artık diske yazma sonucunu bildirir); "Silindi"/"Silinemedi — disk yazma hatası" toast'ları gate'li.
- **m13:** install.ps1 `$partial` bayrağı: sandbox/CM kurulamadıysa son bant "Kurulum KISMEN tamamlandı!" olarak basılır.
- **m14–m19, m21:** README düzeltmeleri (API maddesi sade yazım, istatistik tekrarı kısaltıldı, "öne at", `Author = "Adın"`, spekülatif yol haritası cümlesi çıkarıldı, "kadarıyla sınırlıdır" kalıbı, ağaç notuna GameDataDir).
- **m20:** EnsureGameDir yorumunda çoğul düzeltmesi.
- **m22:** Contracts.cs ve State.cs'ten ölü `using System;` kaldırıldı.
- **m23:** Overlay önbellek alanları üstteki alan grubuna taşındı.

**Doğrulama:**
- `dotnet build -c Release` → **0 uyarı / 0 hata** (Düzenleme sonrası; ölü using kaldırımı derleme ile kanıtlandı).
- `installer\install.ps1` → sorunsuz; `$partial=false` ile "Kurulum tamamlandı!" bandı; BOM korunuyor (EF BB BF).
- Oyun testi yapılmadı (kullanıcı talebi); doğrulama derleme + kurulum düzeyinde kapanış.

**Döngü özeti (5 tur):** Bulgu eğrisi 43 → ~42 → ~36 → 5M+36n → 6M+~25n (R4/R5 Majör'lerinin önemli kısmı önceki tur düzenlemelerinin regresyonlarıydı). Oyun-API imza zinciri 5 tur boyunca **0 hata**; tüm oyun-API iddiaları decompile kaynaklarıyla satır satır doğrulandı. Kalan bilinen kararlar: yukarıdaki DECLINE maddeleri.
