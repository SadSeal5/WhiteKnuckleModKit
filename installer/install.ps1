#Requires -Version 5.1
<#
.SYNOPSIS
    WKModKit kurulum betiği — White Knuckle için mod loader kurar.

.DESCRIPTION
    0) Ön denetimler: oyun kapalı mı, klasör gerçekten oyun kurulumu mu, BepInEx dağıtımı yerinde mi
    1) Çözümü Release olarak derler (önce derleme: derleme başarısızsa oyun klasörüne dokunulmaz)
    2) BepInEx 5.4.23 (win-x64) dağıtımını tools\bepinex'ten oyun klasörüne kopyalar
    3) ConfigurationManager eklentisini kurar (oyun içi F1 ayar menüsü; opsiyonel)
    4) WKModKit.Loader'ı BepInEx\plugins'e, Sandbox modunu BepInEx\wkmods'a kurar
    5) wkmods ve wkmods-data klasörlerini hazırlar

.PARAMETER GameDir
    White Knuckle kurulum klasörü. Verilmezse Steam'den otomatik bulunur.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File installer\install.ps1 -GameDir "D:\SteamLibrary\steamapps\common\White Knuckle"
#>
[CmdletBinding()]
param(
    [string]$GameDir = ""
)

$ErrorActionPreference = "Stop"

# ---- Yardımcılar -------------------------------------------------------------
function Find-GameDir {
    if ($GameDir) { return $GameDir }
    # Steam kayıt defterinden kurulum köklerini bul
    $roots = @()
    try {
        $steam  = Get-ItemProperty "HKCU:\Software\Valve\Steam" -ErrorAction Stop
        $roots += $steam.SteamPath
    } catch {}
    try {
        $steam64 = Get-ItemProperty "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam" -ErrorAction Stop
        $roots += $steam64.InstallPath
    } catch {}

    $candidates = @()
    foreach ($root in $roots) {
        $vdf = Join-Path $root "steamapps\libraryfolders.vdf"
        if (Test-Path $vdf) {
            $vdfText = Get-Content $vdf -Raw
            foreach ($m in [regex]::Matches($vdfText, '"path"\s+"([^"]+)"')) {
                $candidates += (Join-Path $m.Groups[1].Value.Replace("\\", "\") "steamapps\common\White Knuckle")
            }
        }
        $candidates += (Join-Path $root "steamapps\common\White Knuckle")
    }
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c "White Knuckle_Data")) { return $c }
    }
    throw "White Knuckle kurulumu bulunamadı. -GameDir parametresiyle manuel belirt."
}

$root      = Split-Path -Parent $PSScriptRoot
$toolBep   = Join-Path $root "tools\bepinex"
$toolCfg   = Join-Path $root "tools\configmanager\BepInEx\plugins\ConfigurationManager\ConfigurationManager.dll"

$GameDir   = Find-GameDir
# Sondaki ters eğik çizgiyi at: -p:GameDir="...\\" biçimi dotnet komut satırı
# alıntılamasını bozabildiği için temizlenir.
$GameDir   = $GameDir.TrimEnd('\')
if ($GameDir.Length -lt 3) {
    # Sürücü kökü ("C:\") trim sonrası "C:" kalır; Join-Path/Managed denetimi bu durumda
    # kafa karıştırıcı hata verir. Aynı net mesajı burada ver.
    throw "Geçersiz oyun klasörü: '$GameDir' — sürücü kökü olamaz; White Knuckle'ın kurulum klasörünü ver (ör. ...\steamapps\common\White Knuckle)."
}
$gameBepInEx = Join-Path $GameDir "BepInEx"

Write-Host "== WKModKit kurulumu ==" -ForegroundColor Cyan
Write-Host "Oyun       : $GameDir"
Write-Host "Proje kökü : $root"

# Kısmi kurulum bayrağı: opsiyonel/yerinde olmayan bileşen atlanırsa son bandırma değişir.
$partial = $false

# ---- Ön denetimler -----------------------------------------------------------
$running = Get-Process -Name "White Knuckle" -ErrorAction SilentlyContinue
if ($running) {
    throw "White Knuckle şu an çalışıyor. Önce oyunu kapat (winhttp.dll oyun açıkken kopyalanamaz)."
}

if (-not (Test-Path (Join-Path $GameDir "White Knuckle_Data\Managed\Assembly-CSharp.dll"))) {
    throw "Bu klasör White Knuckle kurulumu gibi görünmüyor: $GameDir"
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet bulunamadı. .NET SDK 10+ kurulu olmalı (README 'Gereksinimler')."
}
$dnVersion = (& dotnet --version) -join ""
try { $dnMajor = [int]::Parse(($dnVersion -split '\.')[0]) } catch { $dnMajor = -1 }
if ($dnMajor -ge 0 -and $dnMajor -lt 10) {
    throw "dotnet sürümü yetersiz ($dnVersion); .NET SDK 10+ gerekiyor (README 'Gereksinimler')."
}
$bepCoreDll = Join-Path $toolBep "BepInEx\core\BepInEx.dll"
if (-not (Test-Path $bepCoreDll)) {
    throw "BepInEx dağıtımı eksik: $toolBep — tools klasörü .gitignore'dadır; indirme ve çıkarma talimatı için README 'Hızlı başlangıç' bölümüne bak."
}

# ---- 1) Çözümü derle (kopyalamadan önce) -------------------------------------
Write-Host "`n[1/5] Çözüm derleniyor..." -ForegroundColor Yellow
dotnet build (Join-Path $root "WKModKit.sln") -c Release -p:GameDir="$GameDir"
if ($LASTEXITCODE -ne 0) { throw "Derleme başarısız; oyun klasörüne hiçbir şey kopyalanmadı." }

# ---- 2) BepInEx dağıtımını kopyala -------------------------------------------
Write-Host "[2/5] BepInEx kopyalanıyor..." -ForegroundColor Yellow
# Derleme uzun sürebilir; oyun derleme sırasında açılmış olabilir — kopyalamadan
# hemen önce tekrar denetle ki ham IOException yerine anlaşılır bir mesaj verilsin.
$runningNow = Get-Process -Name "White Knuckle" -ErrorAction SilentlyContinue
if ($runningNow) {
    throw "White Knuckle derleme sırasında açıldı. Oyunu kapat ve betiği yeniden çalıştır."
}
Copy-Item -Path (Join-Path $toolBep "*") -Destination $GameDir -Recurse -Force

# ---- 3) ConfigurationManager kopyala -----------------------------------------
Write-Host "[3/5] ConfigurationManager kopyalanıyor..." -ForegroundColor Yellow
if (Test-Path $toolCfg) {
    $cmDir = Join-Path $gameBepInEx "plugins\ConfigurationManager"
    New-Item -ItemType Directory -Force -Path $cmDir | Out-Null
    try {
        Copy-Item $toolCfg $cmDir -Force
    } catch {
        # Opsiyonel eklenti: kopya hatası kurulumu yarıda bırakmasın.
        $partial = $true
        Write-Warning "ConfigurationManager kopyalanamadı ($($_.Exception.Message)); F1 ayar menüsü kurulamadı. Kuruluma devam ediliyor."
    }
} else {
    $partial = $true
    Write-Warning "ConfigurationManager bulunamadı ($toolCfg); F1 ayar menüsü kurulmadı (opsiyonel eklenti)."
}

# ---- 4) Kurulum dosyalarını kopyala ------------------------------------------
Write-Host "[4/5] WKModKit kuruluyor..." -ForegroundColor Yellow

# Loader (BepInEx plugin)
$plugDir = Join-Path $gameBepInEx "plugins\WKModKit"
New-Item -ItemType Directory -Force -Path $plugDir | Out-Null
Copy-Item (Join-Path $root "src\WKModKit.Loader\bin\Release\WKModKit.Loader.dll") $plugDir -Force
Copy-Item (Join-Path $root "src\WKModKit.Api\bin\Release\WKModKit.Api.dll") $plugDir -Force

# Sandbox (WKModKit modu)
$repoSandbox = Join-Path $root "src\WKModKit.Sandbox\bin\Release\WKModKit.Sandbox.dll"
if (Test-Path $repoSandbox) {
    $sandboxDir = Join-Path $gameBepInEx "wkmods\WKModKit.Sandbox"
    New-Item -ItemType Directory -Force -Path $sandboxDir | Out-Null
    Copy-Item $repoSandbox $sandboxDir -Force
    # Sandbox'ın Api bağımlılığı: Loader AssemblyResolve ile plugin klasöründen çözer;
    # yine de mod klasörüne de koyalım (tek başına dağıtım kolaylığı).
    Copy-Item (Join-Path $root "src\WKModKit.Api\bin\Release\WKModKit.Api.dll") $sandboxDir -Force
} else {
    $partial = $true
    Write-Warning "WKModKit.Sandbox.dll bulunamadı; sandbox modu kurulmadı."
}

# ---- 5) Klasörleri hazırla ---------------------------------------------------
Write-Host "[5/5] wkmods/wkmods-data klasörleri hazırlanıyor..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path (Join-Path $gameBepInEx "wkmods") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $gameBepInEx "wkmods-data") | Out-Null

# Kaldırma: oyun klasöründen yalnızca şunları sil:
#   winhttp.dll, doorstop_config.ini, .doorstop_version, changelog.txt ve BepInEx\ klasörü.
#   (changelog.txt BepInEx dağıtımının dosyasıdır; oyunun kendi dosyaları değildir.)
# Bunların dışındaki HİÇBİR dosyaya dokunma; kalan her dosya oyunun kendisinedir.

if ($partial) {
    Write-Host "`nKurulum KISMEN tamamlandı!" -ForegroundColor Yellow
    Write-Host "  - Yukarıdaki uyarılara bak: eksik bileşen F1 ayar menüsü (ConfigurationManager) ve/veya sandbox modu olabilir."
} else {
    Write-Host "`nKurulum tamamlandı!" -ForegroundColor Green
}
Write-Host "  - Oyunu Steam'den başlat. Konsol penceresi açılırsa BepInEx çalışıyor demektir."
Write-Host "  - Oyun içinde: WKModKit günlükleri BepInEx\LogOutput.log dosyasındadır."
Write-Host "  - Ayar menüsü : ConfigurationManager kuruluysa F1."
Write-Host "  - Kaldırmak için: winhttp.dll, doorstop_config.ini, .doorstop_version, changelog.txt dosyalarını ve BepInEx\ klasörünü sil (başka hiçbir dosyaya dokunma)."
