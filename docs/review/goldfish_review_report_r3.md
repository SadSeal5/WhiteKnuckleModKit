# Goldfish Annotated Review Report — WKModKit (Round 3)

**Target:** WKModKit projesi — R2 düzeltmeleri sonrası haliyle (`full file`)
**Reviewers:** codebase_details (cb), implementation_readiness (imp), structural (str), style (sty)
**Status:** karar kapışında

---

## Özet

- 🔴 **Blocker:** 1 · 🟡 **Major:** 9 · 🟢 **Nit:** ~37
- Oyun API doğrulaması: **0 derleme-seviyesi imza hatası**; R2'de düzeltilen tüm alanlar (Dirilt gate, dedupe, startingSeed, SetActive, throttle) doğru doğrulandı. ÖNEMLİ METABULGU: R2'de uygulanan N22 düzeltmesi (GiveItemToHand post-check) **ters çalışıyormuş** — cb#1 kanıtladı (bkz. B4): oyunda her iki yolda da `GetHandItem() != null`, dolayısıyla yazdığım kontrol asla "El doluydu" dalına giremiyor. Doğrusu: çağrıdan ÖNCE el doluluğu kontrolü.
- Yakınsama: R3 temiz değil (1 Blocker + 9 Major) → düzeltme sonrası **Round 4** (maks. 5; bir sonraki tur 4.).

---

## 🔴 Blockers

### [R3-D1] [imp#1] Mod csproj şablonu temiz makinede derlenemez (MSB3644)
* **Kapsam:** README L99–158 (şablon) vs Gereksinimler L238–242
* **Açıklama:** .NET SDK, net472 referans assembly'lerini içermez; şablon `Microsoft.NETFramework.ReferenceAssemblies` paketini içermiyor. Repo'nun kendi 3 csproj'u paketle çözüyor, şablona kopyalanmamış.
* ***Öneri:*** **PROPOSE FIX** — şablona aynı PackageReference eklenir.

---

## 🟡 Majors

### [R3-B1] [imp#3] Watchdog `SetTimescaleMult` oyun dışında da her karede yazılıyor
* Menüde/sahne geçişinde timescale mult kalıcılaşır (ana menü yavaşlar) + `RefreshTimescale` → `GetPlayer()` → `GameObject.Find` her kare tetiklenir. Oyun tarafında `CL_GameManager.IsInGame()` mevcut (decomp L1092).
* ***Öneri:*** **PROPOSE FIX** — zaman ölçeği yazısı `api.Player != null` guard'ının içine alınır (oyuncu yokken sahne/ana menü yazılmaz; sahne yüklemesinde sıfırlansa da oyuncu doğunca watchdog tekrar yazar).

### [R3-B2] [imp#4] Reflection alanı kaybolursa hata yarı-uygulanmış durum bırakıyor
* `ValidateGameReflection` yalnız logluyor; `AccessTools.Field` null dönerse `Is*/Set*` her kare NRE basar (spam) ve watchdog gövdesi o satırda durur → sonraki özellikler (noTarget, hız, death floor) sessizce uygulanmaz.
* ***Öneri:*** **PROPOSE FIX** — alan bulunamazsa `Is*` sabit `false` döner, `Set*` sessiz no-op (tekil yükleme uyarısı zaten var); özellik düzgün bozılır, spam yok.

### [R3-B3] [imp#5] Hotkey'larda OS tuş-tekrarı filtresi yok
* F6 basılı tutulunca otomatik tekrar (~0.5 sn sonra) menüyü aç/kapa flap'ı yapar. IMGUI `Event.current.repeat` kontrolü yok.
* ***Öneri:*** **PROPOSE FIX** — `e.repeat` guard'ı eklenir.

### [R3-B4] [cb#1] R2'nin N22 düzeltmesi ters çalışıyor — "El doluydu" dalı ölü kod
* Oyun her iki yolda da ele `handItem` verir; post-check `GetHandItem() == null` hiçbir zaman doğru değil → el dolu yolda çantaya giden eşya "Ele verildi" onayı alır. Doğru koşul: çağrıdan ÖNCE `hand.GetHandItem() != null` (oyunun kendi yönlendirme koşulu).
* ***Öneri:*** **PROPOSE FIX** — ön-check ile yazılır.

### [R3-B5] [imp#2 + cb#5] Şablonda ALINE.dll referansı yok (README'nin önerdiği desenler CS0012 verir)
* **PROPOSE FIX** — şablona ALINE referansı eklenir ("Oyunla ilgili notlar" sayfasına da tek satır).

### [R3-B6] [sty#1 + sty#2] Hitap kaymaları kaldı: Gereksinimler "makinenizde"; installer/loader mesajları "siz"
* **PROPOSE FIX** — tek tek "sen"e çevrilir (installer çıktısı, WkLoaderPlugin uyarıları, GameApi mesajları).

### [R3-B7] [sty#3] Türkçe ünlü uyumu: "Slot 4'ye" yanlış — "dört" ünsüzle biter ("4'e"), yalnız "iki" buffer alır ("2'ye")
* **PROPOSE FIX** — `SlotSuffix` yorumla birlikte düzeltilir (`d > 'e`, `b > 'ye`).

### [R3-B8] [sty#4] Loader sıralama yorumu "ada göre" diyor; kod `Info.Id`'ye göre sıralıyor
* **PROPOSE FIX** — yorum "Id'ye göre" yapılır.

### [R3-B9] [str#1] Standardın zorunlu "Hedefler & Kapsam Dışı" bölümü yok
* **PROPOSE FIX** — README'ye kısa "Hedefler & kapsam dışı" bölümü (topluluk: tek oyunculu kendi kopyası, Mono; mod izolasyon garantisi yok, IL2CPP yok, mod dağıtım servisi yok, gameplay görsel guaranteesi yok).

---

## 🟢 Nitler (seçilmiş; hepsi raporda işaretli)

| # | Kaynak | Açıklama | Öneri |
|---|---|---|---|
| n1 | str#2/cb#4 | Ağaçta props "(GameDir, BepInExDistDir, net472...)" — net472 props'ta değil | FIX — "(oyun yolu, BepInEx yolu, LangVersion...)" |
| n2 | str#3 | WkHost'ta kullanılmayan `using BepInEx(.Logging)` | FIX — kaldır |
| n3 | str#4 | Api klasör/ad-uzayı farkı ağaçta belirtilmemiş | FIX — ağaca "(ad alanı: WKModKit)" notu |
| n4 | str#5 | `IModConfig` Context.cs içinde | DECLINE — önceki onaylı tematik düzen |
| n5 | str#6 | Sandbox üye sıralaması kozmetiği | FIX — hafif (State/SandboxMenu/GameState alan grupları) |
| n6 | str#7 | sln'de legacy proje-türü GUID | FIX — SDK GUID'ine çevrilir |
| n7 | str#8 + sty#22 | Sorun giderme son satırı uzun; "logu"→"log'u" | FIX — genişletme tablo altına, write |
| n8 | sty#5 | Patches: "yedeklidir"→"gereksiz"; söz kalabalığı | FIX |
| n9 | sty#6/7/8/10/12 | "yazarı/ları", "gerçekten", bedava/ücretsiz, "varolması"+"::", "hazır"/"yaşamaktadır", ALINE "ilave risk taşımaz" kalibrasyonu | FIX — tek yama |
| n10 | sty#11 | "gerçek projede" belirsiz | FIX — "(WKModKit.Sandbox csproj'unda da)" |
| n11 | sty#13 | Timescale config açıklaması özneyi bulandırıyor | FIX — yeniden yazım |
| n12 | sty#14/15/16/20 | "dürüstçe", "varlığini" (typo!), "akış/akışı" tekrarı, birinci çoğul yorum sesi ("saklarız/edelim") | FIX |
| n13 | sty#17 | Overlay "ZEMİN ✕" belirsiz | FIX — "ÖLÜMCÜL ZEMİN ✕" |
| n14 | sty#18/19 | No-op yollarında başarı toast'u; toast paralellikleri ("Önüne çağrıldı", "Çantaya verildi", "(oyun içine gir)" tekleştirme) | FIX — GameApi no-op mesajları döner, menu gate'ler |
| n15 | sty#21 | SandboxMod imleç yorumu koşulsuz söylüyor | FIX — "hatasız çizilirse" |
| n16 | imp#6 | Başarısız modun config/veri klasörü artığı | DOC — Sınırlar'a tek satır |
| n17 | imp#7 | noclip kapamada ViewSway eski durumda kalabilir | DOC (DECLINE kod değişikliği) — Sınırlar'a not |
| n18 | imp#8 | Id beyaz-boşluk geçer | FIX — trim + null/empty fallback |
| n19 | imp#9 | dotnet sürüm denetimi | FIX — `--version` major < 10 → hata |
| n20 | imp#10 | Oyun denetimi yalnız derleme öncesi; kopyalama öncesi tekrar | FIX — 2. adım öncesi tekrar |
| n21 | imp#11 + README/installer | `changelog.txt` sahiplik varsayımı | FIX — "(BepInEx dağıtımının dosyası)" notu |
| n22 | cb#2 | Öldür butonu isDead/reviving no-op'unda "Öldün." diyor | FIX — KillPlayer da durum döner |
| n23 | cb#3 | GetRunSeed fallback 69420 gösteriliyor | FIX — oyun içi değilse "-" |
| n24 | cb#6 | Spawner'ların parent'lama farkı belgesiz | DOC — kod yorumuna |
| n25 | cb#7 | AimPoint trigger collider'lara takılıyor | FIX — QueryTriggerInteraction.Ignore |

---

## Uygulama planı (onay sonrası)

1. Kod: R3-B1..B4 + n2/n5/n12/n13/n14/n18/n22..n25 (yukarıda).
2. README: R3-D1, R3-B5, R3-B9, n1/n3/n7/n9/n10/n11/n16/n17/n21.
3. install.ps1: R3-B6 (hitap), n19/n20/n21 (+BOM korunur).
4. Doğrulama: **derleme + kurulum betiği düzeyinde** (kullanıcı talebiyle oyun testi yapılmayacak) → rapor güncelleme → Round 4.

---

## Round 3 Çözüm Kaydı (uygulandı)

**Karar:** Kullanıcı "Düzelt ve Round 4" — 1 Blocker + 9 Major + ~25 Nit (PROPOSE FIX'ler) uygulandı; DECLINE'lar: n4 (IModConfig'in Context.cs'te kalması — önceki onaylı tematik düzen), n17'nin kod değişikliği kısmı (noclip ViewSway yeniden yönlendirmesi yerine Sınırlar'a belge notu; minimum dokunuş ilkesi).

**Uygulananlar:**
- **R3-D1:** README csproj şablonuna `Microsoft.NETFramework.ReferenceAssemblies` 1.0.3 PackageReference (temiz makinede MSB3644 önlemi; NuGet gerekçe yorumuyla).
- **R3-B1:** Watchdog `SetTimescaleMult` yazısı oyuncu-var guard'ının içine alındı (ana menüde çarpan kalıcılaşmaz, boşa `GameObject.Find` kalktı); README/Sorumlar betimi güncellendi.
- **R3-B2:** Reflection alanı kaybolursa `Is*` sabit `false` döner, `Set*` sessiz no-op — tanımlı bozulma modu, saat başına log spam yok; README "Sınırlar"da belgelendi.
- **R3-B3:** Hotkey tuş-tekrarı: Unity **runtime** `Event`'i `repeat` özelliği taşımıyor (derleme hatası CS1061 ile kanıtlandı — reviewer önerisi Editor-only) → KeyUp-durumlu `HashSet<KeyCode>` latch ile çözüldü: basılı tutulan tuş KeyUp gelene kadar yeni tetikleme üretmez.
- **R3-B4:** R2'nin ters çalışan N22 düzeltmesi kanıtla düzeltildi: el doluluğu `AddItemToHand` çağrısından **önce** kontrol edilir (oyunun kendi yönlendirme koşulu olan `GetHandItem() != null`).
- **R3-B5:** Şablona ALINE.dll referansı + "Oyunla ilgili notlar"a ALINE/CS0012 maddesi.
- **R3-B6:** Kalan tüm "siz" hitabı "sen"e çevrildi (install.ps1 çıktı ve hata mesajları, WkLoaderPlugin uyarısı, GameApi metni, README Gereksinimler "makinen").
- **R3-B7:** `SlotSuffix`: yalnız 2 → "-ye"; 4 → "-e" (yorumla birlikte — ünlü uyumu kuralı doğru yazıldı).
- **R3-B8:** Loader sıralama yorumu "Id'ye göre" (kodla eşleşti).
- **R3-B9:** README'ye "Hedefler & kapsam dışı" bölümü (5 kapsam-dışı maddesiyle).
- **Nitler:** n1 (props ağaç notu düzeltildi), n2 (WkHost using temizliği), n3 (Api ağaç notu), n5 (State/SandboxMenu/GameApi üye sırası hafif düzeltme), n6 (sln SDK GUID), n7 (Sorun giderme son satırı tablo altına + "log'u"), n8 (Patches "gereksiz" + öz yorum), n9 ("yazarı ya da yazarları", "gerçekten" çıkarıldı, "bedava/ücretsiz" tekledi, "var olma", "mevcut", ALINE "tanımlıdır"+risk kalibrasyonu, "çözüm: yeniden derle"), n10 (façad yorumu Sandbox'a referansla), n11 (Timescale config betimlemesi özne düzeltildi), n12 ("dürüstçe" nötr, "varlığını", "akış" tekrarı, birinci çoğul yorum sesi → edilgen), n13 (overlay "ÖLÜMCÜL ZEMİN ✕"), n14 (HealFull/KillPlayer mesaj döner, menü toast'u gate'li; KillPlayer `p.dead`/`IsReviving()` kontrolleri decomp L3071 kanıtıyla), n15 (imleç yorumu koşullandırıldı), n16 (Sınırlar'a başarısız mod artığı notu), n17 (Sınırlar'a ViewSway notu), n18 (Id trim + boşluk fallback), n19 (dotnet sürüm denetimi — parse edilemezse sessiz geç), n20 (kopyalama öncesi oyun-durum yeniden denetimi), n21 (changelog.txt BepInEx sahiplik notu — README + installer yorumu), n22 (KillPlayer durum döner), n23 (GetRunSeed oyun dışında "-" — 69420 fallback'i artık gösterilmez), n24 (spawn parent'lama farkı doc yorumu), n25 (AimPoint `QueryTriggerInteraction.Ignore`).

**Kanıt notları:**
- `Event.current.repeat` Unity runtime'da yok (CS1061: 'Event' bir 'repeat' tanımı içermiyor) — imp#5'in önerdiği API Editor-only; latch deseni ile eşdeğer koruma sağlandı. Bir sonraki turda reviewer'a bu bilgnotu iletilecek.
- `CL_GameManager.gMan` public static (decomp L96) — KillPlayer'ın IsReviving gate'i doğrulandı. `IsReviving()` public instance (decomp L1029).

**Doğrulama:**
- `dotnet build -c Release` → **0 uyarı / 0 hata** (ara denemede CS1061 yakalandı ve düzeltildi).
- `installer\install.ps1` → sorunsuz (yeni dotnet sürüm denetimi + kopyalama öncesi oyun denetimi dahil); BOM korunuyor (EF BB BF).
- ~~Boot smoke test~~ → **kullanıcı talebiyle atlandı**; doğrulama derleme + kurulum düzeyinde kısıtlandı. Kurulum, oyunu başlatmadan oyun klasörüne kurulumu tamamladı (süreç denetimi: oyun kapalı).
