# WKModKit — White Knuckle Mod Loader + Sandbox

**White Knuckle** (Unity 6000.3.9f1, Mono/x64, Steam) için mod loader altyapısı ve bir sandbox modu.

- **WKModKit.Loader** — BepInEx 5.4.23 üzerinde çalışan, `BepInEx\wkmods\` klasöründeki modları keşfedip yükleyen loader.
- **WKModKit.Api** — Modların derlendiği bağımlılıksız API (arayüzler + config + Harmony fabrikası).
- **WKModKit.Sandbox** — God mode, sonsuz stamina, uçuş, noclip, zaman ölçeği, eşya/düşman spawner, ışınlanma noktaları; hepsi oyun içi menüden (F6).

> ⚠️ Hile içeriğidir; tek oyunculu bir oyunda kendi kopyan üzerinde kullanım içindir. Hileli koşularda oyunun istatistik/ödül kayıtları etkilenebilir.

> 🧪 **Test durumu:** kurulum ve oyun açılışı (BepInEx boot, mod yükleme, 0 hata) canlı doğrulandı. Aşağıdaki oynanış davranış anlatımları kod kaynaklı beklentilerdir; tek bir koşu içinde henüz gözlemlenmemiştir.

---

## Hedefler & kapsam dışı

**Hedefler:**
- White Knuckle (Steam, Mono/x64) için BepInEx 5 tabanlı bir mod loader altyapısı ve referans bir sandbox modu.
- Tek oyunculu oyunda kendi kopyan üzerinde kullanılabilen sandbox araçları: oyun içi menü + hotkey + kalıcı config.
- Küçük, bağımlılıksız bir API: mod yazarı rehberdeki tek csproj şablonuyla derleyip `BepInEx\wkmods\` altına kopyalayabilir.

**Kapsam dışı:**
- Çok oyunculu veya çevrimiçi ortamlarda kullanım (oyun tek oyunculudur).
- IL2CPP sürüm desteği (oyun Mono derlemesidir).
- Mod dağıtım/indirme servisi, sürüm çözümleme veya mod yönetim arayüzleri — modlar DLL kopyalanarak kurulur.
- Mod izolasyonu/sandboxing garantileri (paylaşılan domain; bkz. "Sınırlar & riskler").
- Oynanış özelliklerinin garantili davranışı (bkz. baştaki "Test durumu" notu).

---

## Hızlı başlangıç

```powershell
# 1) Üçüncü parti dağıtımları indir (bir kereye mahsus, tools/ klasörüne).
#
#    BepInEx 5.4.23.6 (win-x64):
#      https://github.com/BepInEx/BepInEx/releases
#      İndirilecek dosya: BepInEx_win_x64_5.4.23.6.zip
#      İndirdiğin zip'in İÇİNDEKİLERİ tools\bepinex klasörüne aç
#      (zip adında ara klasör oluşmasın; doğrudan açılmış dosyalar dursun).
#      Doğrulama: tools\bepinex\BepInEx\core\BepInEx.dll mevcut olmalı.
#
#    BepInEx.ConfigurationManager (BepInEx 5, v19.1):
#      https://github.com/BepInEx/BepInEx.ConfigurationManager/releases
#      İndirilecek dosya: BepInEx.ConfigurationManager_BepInEx5_v19.1.zip
#      Zip'in İÇİNDEKİLERİ tools\configmanager klasörüne aç.
#      Doğrulama: tools\configmanager\BepInEx\plugins\ConfigurationManager\ConfigurationManager.dll mevcut olmalı.
#      (Opsiyonel — yoksa kurulmaz, F1 ayar menüsü çalışmaz.)
#
#    Not: çıkarma işlemi bitince indirdiğin zip dosyalarına artık gerek yok; silebilirsin.

# 2) Kur. Komut herhangi bir çalışma dizininden verilebilir; yolu kendi indirme konumuna uyarla.
#    Steam yolunu otomatik bulur; bulunamazsa -GameDir ile açıkça belirt:
powershell -ExecutionPolicy Bypass -File "C:\yol\WhiteKnuckleModKit\installer\install.ps1" -GameDir "D:\SteamLibrary\steamapps\common\White Knuckle"

# 3) Oyunu Steam'den başlat, oyun içinde F6
```

Kurulum betiği: önce çözümü derler (`-p:GameDir` ile), sonra BepInEx'i oyun klasörüne kopyalar, Loader'ı `BepInEx\plugins\WKModKit\`'e, Sandbox'ı `BepInEx\wkmods\WKModKit.Sandbox\`'a kurar. Betik oyun açıkken çalışmayı reddeder; önce oyunu kapat (denetim derleme öncesi ve kopyalama öncesi tekrarlanır).

> Not: yeniden kurulumda betik BepInEx dağıtım dosyalarının üzerine yazar; `doorstop_config.ini` dâhil BepInEx düzeyinde yaptığın özelleştirmeler sıfırlanır.

**Kaldırmak için:** oyun klasöründen yalnızca şu dosya ve klasörleri sil: `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `changelog.txt` (BepInEx dağıtımının dosyası) ve `BepInEx\` klasörü. Bunların dışındaki hiçbir dosyaya dokunma — kalan her dosya oyunun kendisinedir.

---

## Sandbox kullanımı

| Tuş (varsayılan) | İşlev |
|---|---|
| **F6** | Sandbox menüsünü aç/kapat |
| **F7** | God mode aç/kapat |
| **F8** | Sonsuz stamina aç/kapat |
| **F9** | Uçuş (fly) aç/kapat |
| **F10** | Noclip aç/kapat |

Menü sekmeleri:
- **Oyuncu** — god mode, sonsuz stamina/mermi/şarj, düşüş hasarı kapama, her yere tutunma, uçuş, noclip, no-target, ölümcül zemin (death goo) kapama, hız çarpanı, zaman ölçeği, iyileş/dirilt/öldür düğmeleri.
- **Eşyalar** — oyun asset veritabanındaki tüm eşyaları arar; çantaya ver / ele ver / öne at.
- **Düşmanlar** — tüm denizen (canavar) prefab'larını listeler; önüne/altına çağır, tümünü öldür.
- **Dünya** — koşu tohumu (seed), 4 slotlu ışınlanma noktası kaydet/yükle. Slotlar mutlak dünya koordinatı saklar: yeni bir koşuda geçersiz kalabilirler (başka koşuda kaydedilen veya tohumu bilinmeyen eski kayıtlar ışınlanırken uyarı verir).
- **Yardım** — kısayollar ve notlar.

Ayarlar `BepInEx\config\WKModKit.wkmodkit.sandbox.cfg` dosyasında saklanır (dosya adı `"WKModKit." + mod Id` kuralıyla üretilir) ve **ConfigurationManager (F1)** üzerinden de düzenlenebilir. Kısayollar config'den değiştirilebilir.

Nasıl çalışır (kısaca): menü/hotkey yalnızca config'e yazar; her karede `watchdog` config'teki istenen durumu oyuna uygular. Bool bayraklar oyun durumuyla karşılaştırılıp yalnız fark varsa yazılır; sayısal ayarlar (zaman ölçeği, hız çarpanı) oyuncu varken her karede yazılır — oyuncu yokken (ana menü/sahne geçişi) dokunulmaz — çünkü oyun bunları sahne yüklemesinde ve ölüm/yeniden doğuşta kendi kendine sıfırlar. ConfigurationManager'dan yapılan değişiklikler de aynı yoldan işler. Düşüş hasarı gibi "negatif" davranışlar tek bir Harmony prefix'i (`ENT_Player.FallingDamage`) ile atlanır.

---

## Mimari

```
Steam oyunu
 └─ BepInEx 5.4.23 (Doorstop: winhttp.dll → BepInEx.Preloader)
     └─ plugins\WKModKit\WKModKit.Loader.dll   ← BepInEx plugin'i
         ├─ wkmods\(*.dll | <Id>\*.dll)          ← WKModKit modları
         ├─ wkmods-data\<Id>\                    ← mod veri klasörleri
         └─ config\WKModKit.<Id>.cfg             ← mod config'leri
```

- **Loader**, `Awake` ile klasörleri hazırlar, `wkmods` altındaki tüm DLL'leri (üst düzey + her alt klasörün üst düzey DLL'leri) yükler, `IWKMod` uygulayan public somut sınıfları bulur, `LoadPriority`'ye göre sıralar ve `OnLoad(ctx)` ile başlatır. `AssemblyResolve` fallback'i sayesinde mod DLL'inin yanındaki bağımlılıklar (`WKModKit.Api` vb.) otomatik çözülür.
- **Host MonoBehaviour** (`WKModKit.Host`), `IModUpdateReceiver` uygulayan modlara her karede `Update/LateUpdate/FixedUpdate/OnGUI` event'i pompalar; her çağrı ayrı try/catch içindedir, istisnalar yakalanır ve loglanır.
- **Config**, BepInEx `ConfigFile` üzerine ince bir sarmalayıcıdır: dosya tabanlı, ConfigurationManager ile entegre, `WKModKit.<Id>.cfg` adıyla `BepInEx\config`'e yazılır.

### Oyunla ilgili notlar (mod yazarlarına)

White Knuckle Mono tabanlı olduğu için doğrudan oyun tiplerine derleme yapılabilir (`Assembly-CSharp.dll` referansı). Önemli noktalar:

- Oyuncu: `ENT_Player.GetPlayer()` — içinde `SetGodMode()`, `fly`, `noclip`, `hands[i].SetGripStrength()` (stamina), `Teleport()`, perk/buff API'leri mevcut.
- Eşya/düşman veritabanı: `CL_AssetManager.GetFullCombinedAssetDatabase()` → `itemPrefabs`, `denizenPrefabs`, `perkAssets`...
- Envantere eşya verme için oyunun kendi yolu: `Inventory.instance.AddItemToInventoryScreen(pos, prefab.itemData.GetClone(), ...)`.
- Oyun tiplerinde ALINE: `Item_Object`, `Denizen`, `GameEntity` gibi tipler ALINE.dll içindeki `MonoBehaviourGizmos`'dan türediği için bu tiplere dokunan modlar csproj'a `ALINE.dll` referansı eklemek zorundadır (yoksa CS0012); şablona dahildir.
- Zaman: `Time.timeScale`'i **doğrudan** yazma — `CL_GameManager.RefreshTimescale()` her karede ezer; `CL_GameManager.SetTimescaleMult(x)` kullan.
- Unity 6 yüzünden tüm hedefler `net472`'dir ve oyunun `netstandard.dll` façadı referans olarak eklenmelidir (bkz. Loader ve Sandbox csproj'ları).

---

## Kendi modunu yazma

1. Yeni bir sınıf kütüphanesi projesi aç (`.NET Framework 4.7.2`). Kopyalanabilir minimal csproj — yolları kendi kurulumuna göre değiştir:

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net472</TargetFramework>
       <LangVersion>7.3</LangVersion>
       <AssemblyName>MyMod</AssemblyName>
       <RootNamespace>MyMod</RootNamespace>
       <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
     </PropertyGroup>
     <ItemGroup>
       <!-- net472 hedefi için referans assembly paketi: .NET SDK, .NET Framework referans
            assembly'lerini içermez; bu paket olmadan temiz makinede derleme MSB3644 hatası
            verir ("reference assemblies for .NETFramework,Version=v4.7.2 were not found").
            İlk derlemede NuGet'ten indirilir (internet gerekir); repo projeleri de
            aynı paketi kullanır. -->
       <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
       <!-- WKModKit.Api.dll'nin var olması için depoyu bir kez derlemiş (bkz. Gereksinimler)
            ya da install.ps1'i çalıştırmış olman gerekir. Referans kaynaklarından biri:
            src\WKModKit.Api\bin\Release\WKModKit.Api.dll ya da
            <oyun>\BepInEx\plugins\WKModKit\WKModKit.Api.dll
            (0Harmony referansı da oyun tarafını gösterir: BepInEx\core\0Harmony.dll
            yalnız kurulumdan sonra orada bulunur.) -->
       <Reference Include="WKModKit.Api">
         <HintPath>C:\yol\WhiteKnuckleModKit\src\WKModKit.Api\bin\Release\WKModKit.Api.dll</HintPath>
         <Private>false</Private>
       </Reference>
       <Reference Include="0Harmony">
         <HintPath>D:\SteamLibrary\steamapps\common\White Knuckle\BepInEx\core\0Harmony.dll</HintPath>
         <Private>false</Private>
       </Reference>
       <Reference Include="Assembly-CSharp">
         <HintPath>D:\SteamLibrary\steamapps\common\White Knuckle\White Knuckle_Data\Managed\Assembly-CSharp.dll</HintPath>
         <Private>false</Private>
       </Reference>
       <!-- Item_Object/Denizen/GameEntity gibi oyun tipleri ALINE.dll içindeki
            MonoBehaviourGizmos'dan türediği için (CS0012 önlemi) oyun tiplerine
            dokunan her modda bu referans şart; şablona dahildir: -->
       <Reference Include="ALINE">
         <HintPath>D:\SteamLibrary\steamapps\common\White Knuckle\White Knuckle_Data\Managed\ALINE.dll</HintPath>
         <Private>false</Private>
       </Reference>
       <!-- Unity 6 assemblies netstandard 2.1'e bağlı; oyunun kendi façadı şart (CS0012 önlemi): -->
       <Reference Include="netstandard">
         <HintPath>D:\SteamLibrary\steamapps\common\White Knuckle\White Knuckle_Data\Managed\netstandard.dll</HintPath>
         <Private>false</Private>
       </Reference>
       <Reference Include="UnityEngine.CoreModule">
         <HintPath>D:\SteamLibrary\steamapps\common\White Knuckle\White Knuckle_Data\Managed\UnityEngine.CoreModule.dll</HintPath>
         <Private>false</Private>
       </Reference>
       <Reference Include="UnityEngine.IMGUIModule">
         <HintPath>D:\SteamLibrary\steamapps\common\White Knuckle\White Knuckle_Data\Managed\UnityEngine.IMGUIModule.dll</HintPath>
         <Private>false</Private>
       </Reference>
       <!-- UnityEngine.dll façadı tip yönlendirmeleriyle modül tiplerini çözer
            (WKModKit.Sandbox csproj'unda da kullanılır); buna rağmen kullandığın
            her Unity tipi için ilgili modül DLL'ini eklemen gerekebilir:
            Physics.Raycast için PhysicsModule, GUI yazı tipleri için
            TextRenderingModule vb. (tam liste: WKModKit.Sandbox csproj). -->
       <Reference Include="UnityEngine">
         <HintPath>D:\SteamLibrary\steamapps\common\White Knuckle\White Knuckle_Data\Managed\UnityEngine.dll</HintPath>
         <Private>false</Private>
       </Reference>
       <Reference Include="UnityEngine.PhysicsModule">
         <HintPath>D:\SteamLibrary\steamapps\common\White Knuckle\White Knuckle_Data\Managed\UnityEngine.PhysicsModule.dll</HintPath>
         <Private>false</Private>
       </Reference>
     </ItemGroup>
   </Project>
   ```

   Modu `dotnet build -c Release` ile derle (mutlak HintPath'ler kullandığın için `-p:GameDir` gerekmez); çıktı `bin\Release\MyMod.dll` olarak oluşur.

2. `IWKMod` uygula (gerekirse `IModUpdateReceiver` de). Mod sınıfı **public**, somut ve parametresiz kuruculu olmalı:

   ```csharp
   using WKModKit;

   public class MyMod : IWKMod, IModUpdateReceiver
   {
       public WKModInfo Info => new WKModInfo
       {
           Id = "mymod", Name = "My Mod", Version = "1.0.0",
           Author = "Adın", Description = "Örnek mod", LoadPriority = 500,
       };

       private IModContext _ctx;
       private HarmonyLib.Harmony _harmony;

       public void OnLoad(IModContext context)
       {
           _ctx = context;
           _harmony = context.CreateHarmony();   // kimlik: "WKModKit.mymod"
           _harmony.PatchAll(typeof(MyMod).Assembly);
           bool flag = context.Config.Bind("General", "Enabled", true, "Mod açık mı?");
       }

       public void OnModUpdate() { /* her kare */ }
       public void OnModGUI()     { /* IMGUI çizimi */ }
       public void OnModLateUpdate() { }
       public void OnModFixedUpdate() { }

       // OnLoad hata fırlatırsa loader temizlik için OnUnload'u çağırır:
       // Harmony yamalarını burada geri alın.
       public void OnUnload() { _harmony?.UnpatchSelf(); }
   }
   ```

3. Derlenmiş `MyMod.dll` dosyasını oyundaki `BepInEx\wkmods\MyMod\` klasörüne kopyala. `WKModKit.Api.dll`'yi yanına kopyalamak opsiyoneldir (loader `AssemblyResolve` ile kurulu kopyadan çözer); tek başına dağıtmak istiyorsan kopyala.

Detaylı oyun API notları için `WKModKit.Sandbox` projesine (`GameApi.cs`, `State.cs`) bak — her çağrı oyunun gerçek metot çağrı yollarıyla örneklenmiştir.

---

## Tasarım alternatifleri

- **BepInEx 5.4.23 (win-x64) yerine BepInEx 6:** BepInEx 6, Mono oyunlarda kullanılabilir olsa da hâlâ Beta sürecinde ve eklenti ekosistemi 5.x üzerine kurulu. Oyun Mono/x64 olduğundan 5.4.23'in stabil Doorstop + preloader zinciri daha düşük risk taşıdı. Ret gerekçesi: istikrar ve ekosistem olgunluğu.
- **BepInEx yerine sıfırdan kendi loader:** Doorstop proxy'si, preloader, config sistemi, konsol ve Harmony bootstrap'ini yeniden icat etmek anlamına gelirdi; kazanç yalnızca bağımlılık azaltma olurdu. Ret gerekçesi: maliyet/fayda oranı.
- **Doğrudan `Assembly-CSharp` referansı yerine salt-reflection API:** Mono build olduğundan derleme zamanı tip güvenliği ek maliyet getirmez; reflection her erişimde çalışma zamanı hata riski ve yüksek bakım maliyeti eklerdi. Ret gerekçesi: IL2CPP olsaydı reflection zorunlu olurdu.
- **AppDomain bazlı mod izolasyonu yerine paylaşılan domain:** Unity'nin Mono runtime'ında AppDomain izolasyonu ve Harmony patch'lerinin domain sınırlarından geçişi kırılgandır; tek domain + event pompasında try/catch ve OnLoad başarısızlığında temizlik çağrısı (bkz. `IWKMod` sözleşmesi) yeterli göründü. Ret gerekçesi: karmaşıklık.

---

## Sınırlar & riskler

- **Oyun güncellemeleri:** Assembly-CSharp değişirse modlar `MissingMethodException`/`TypeLoadException` verebilir; çözüm: projeyi yeniden derle (`installer\install.ps1`). Sandbox, eriştiği private alanları yüklemede doğrulayıp bulunamayan özellikleri güvenli biçimde devre dışı bırakır.
- **Test kapsamı:** bkz. baştaki "Test durumu" notu.
- **Işınlanma slotları:** mutlak dünya koordinatı saklanır; oyun ~10000 m'de dünyayı yeniden merkezlediği ve sahne geçişlerinde seviyeleri yeniden ürettiği için yeni koşuda slot geçersiz olabilir (farklı koşu uyarısı mevcut).
- **Death goo kapatma:** oyunun `DEN_DeathFloor.SetActive` API'si kullanılır; kapatma yalnızca modun durdurduğu zeminde geri açar (oyunun kendisi kapatmışsa dokunmaz).
- **Config dosya adı:** `WKModKit.wkmodkit.sandbox.cfg` kalıbı `"WKModKit." + Id` kuralından gelir ve bilinçli bir tercihtir; Id değişikliği mevcut kullanıcı configlerini bozacağı için planlanmıyor.
- **Mod izolasyonu:** paylaşılan domainde bir modun OnLoad'ı yarıda kalırsa loader OnUnload temizliği çağırır; ancak bir mod sonsuz döngüye girerse oyunu dondurabilir (try/catch bunu engellemez).
- **Başarısız mod artıkları:** OnLoad'i başarısız olan bir modun config dosyası (`WKModKit.<Id>.cfg`) ve veri klasörü diskte kalabilir (loader, OnLoad'dan önce context oluşturduğu için); zararsız artıktır, silinebilir.
- **Noclip geçişi:** sandbox oyunun `noclip` alanını doğrudan yazdığı için oyunun kendi `Noclip()` işleyicisindeki el ViewSway sıfırlaması çalışmaz; oyun tarafından ayarlanmış özel sway parametreleri bir sonraki oyun-içi durum geçişine kadar kalabilir (kozmetik).
- **ALINE bağımlılığı:** Sandbox, `Item_Object` tiplerine derleme zamanında erişebilmek için oyunla gelen `ALINE.dll`'yi referans verir (temel tip `MonoBehaviourGizmos` ALINE içinde tanımlıdır — referans kaldırılırsa CS0012). Oyunla birlikte dağıtıldığı için güncelleme kırılma riski Assembly-CSharp referansıyla aynı düzeydedir.

---

## Proje düzeni

```
README.md
.gitignore
WKModKit.sln
Directory.Build.props        → ortak derleme ayarları (GameDir, GameDataDir, GameManagedDir, BepInExDistDir, LangVersion ve MSBuild ayarları; TargetFramework net472 csproj'ların kendisindedir)
src/WKModKit.Api             → mod sözleşmeleri (Contracts.cs, Context.cs; ad alanı: WKModKit)
src/WKModKit.Loader          → BepInEx plugin'i + mod keşfi + host + config uygulaması
src/WKModKit.Sandbox         → sandbox modu (menü, hotkey, Harmony, spawn, teleport)
installer/install.ps1        → derle + kur
docs/review/                 → EGM review raporları (kalite denetim çıktıları)
tools/                       → (git ignore) BepInEx & ConfigurationManager dağıtımları
decomp/                      → (git ignore) ilspycmd ile decompile edilen Assembly-CSharp
```

---

## Gereksinimler

- .NET SDK 10+ (`dotnet build`)
- White Knuckle (Steam, Mono sürümü — IL2CPP değil)
- Kaynak kodundan derleme sırasında oyun kurulu olmalı (Assembly-CSharp referansları için). Varsayılan oyun yolu `Directory.Build.props` içindeki `GameDir` değeridir (depodaki değer yazarın makinesine aittir): kendi makinen ya bu değeri düzenle ya da her derlemede `-p:GameDir=...` geçir.
- `installer\install.ps1` UTF-8 **BOM** ile kaydedilmiş olmalı (PowerShell 5.1, BOM'suz UTF-8'i ANSI okur ve Türkçe mesajlar bozulur). Betiği düzenlersen kodlamayı koru.

---

## Sorun giderme

| Belirti | Çözüm |
|---|---|
| Oyun açılıyor ama menü yok | `BepInEx\LogOutput.log`'a bak; "WKModKit: N mod yüklendi" satırı olmalı. Yoksa `BepInEx\plugins\WKModKit\WKModKit.Loader.dll` kurulu mu? |
| Steam oyunu doğrudan başlatmayı reddediyor | Oyunu Steam üzerinden başlat (DRM). |
| Bir mod güncelleme sonrası `MissingMethodException`/`TypeLoadException` veriyor | Oyun güncellenmiştir; projeyi yeniden derle (`installer\install.ps1`). |
| Menü açılıyor ama tıklama çalışmıyor | Oyun penceresi odağında olmalı; menü açıkken mod imleci serbest bırakır (kapatınca önceki değerine döndürür). |

Menü çizimi hata verirse menü kendini kapatır ve `BepInEx\LogOutput.log`'da `[sandbox] Menü hatası` satırı görünür — log'u incele.
