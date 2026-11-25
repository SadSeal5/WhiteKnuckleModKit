using System;
using HarmonyLib;
using UnityEngine;
using WKModKit;

namespace WKModKit.Sandbox
{
    /// <summary>
    /// WKModKit Sandbox — White Knuckle için sandbox modu.
    /// Kurulum: BepInEx\wkmods\WKModKit.Sandbox\ içine bu DLL + WKModKit.Api.dll atılır.
    /// Menü: F6 (varsayılan; ConfigurationManager'dan değiştirilebilir).
    /// </summary>
    public sealed class SandboxMod : IWKMod, IModUpdateReceiver
    {
        public const string ModId = "wkmodkit.sandbox";
        public const string ModVersion = "1.0.0";

        private IModContext _ctx;
        private Harmony _harmony;
        private GameApi _api;
        private SandboxMenu _menu;

        public WKModInfo Info => new WKModInfo
        {
            Id = ModId,
            Name = "WKModKit Sandbox",
            Version = ModVersion,
            Author = "WKModKit",
            Description = "God mode, sonsuz stamina, uçuş, noclip, zaman ölçeği, eşya ve düşman spawner, ışınlanma noktaları — hepsi oyun içi menüden.",
            LoadPriority = 1000,
        };

        public void OnLoad(IModContext context)
        {
            _ctx = context;
            State.Initialize(context);
            _api = new GameApi(context);
            _api.ValidateGameReflection();
            _menu = new SandboxMenu(context, _api, ModVersion);

            _harmony = context.CreateHarmony();
            _harmony.PatchAll(typeof(Patches).Assembly);

            _api.LoadTeleportsFromDisk();

            _ctx.LogInfo("[sandbox] WKModKit Sandbox yüklendi. Menü aç/kapat: F6 (varsayılan). Notlar: ayarlar BepInEx\\config\\WKModKit." + ModId + ".cfg dosyasında; ConfigurationManager kuruluysa F1 ile de düzenlenebilir.");
        }

        public void OnUnload()
        {
            // Yama geri alma ile oyun durumu sıfırlama birbirinden bağımsız olsun:
            // ResetForExit hata fırlatsa bile Harmony yamaları geri alınır.
            try
            {
                State.ResetForExit(_api);
            }
            catch (Exception e)
            {
                _ctx.LogError("[sandbox] Kapanış sırasında durum sıfırlama hatası: " + e);
            }
            try
            {
                if (_harmony != null) _harmony.UnpatchSelf();
            }
            catch (Exception e)
            {
                _ctx.LogError("[sandbox] Kapanış sırasında Harmony geri alma hatası: " + e);
            }
            _ctx.LogInfo("[sandbox] Sandbox kapatıldı.");
        }

        // ---- Frame event'leri (WKModKit.Loader host'undan) ----

        private float _nextWatchdogErrorLogAt = -1f;

        public void OnModUpdate()
        {
            try
            {
                State.ApplyWatchdog(_api);
            }
            catch (Exception e)
            {
                // Watchdog her karede istisna basarsa log dosyası dolar; hatalar 5 saniyelik
                // pencerelerle örneklendirilir: pencere başına yalnız İLK istisna loglanır —
                // pencere içinde çıkan farklı bir hata bir sonraki pencereye kadar görünmez.
                if (Time.realtimeSinceStartup >= _nextWatchdogErrorLogAt)
                {
                    _nextWatchdogErrorLogAt = Time.realtimeSinceStartup + 5f;
                    _ctx.LogError("[sandbox] Watchdog hatası (her 5 sn'de bir örneklenir): " + e.Message);
                }
            }
        }

        public void OnModLateUpdate() { }
        public void OnModFixedUpdate() { }

        public void OnModGUI()
        {
            try
            {
                if (_menu != null) _menu.Draw();
            }
            catch (Exception e)
            {
                // Menü çizimi hata verirse oyunu kilitlemesin: log'la ve menüyü kapat.
                // İmleç, menü bir sonraki karede hatasız çizilirse Draw içindeki geçiş
                // algısıyla geri yüklenir.
                _ctx.LogError("[sandbox] Menü hatası: " + e);
                State.MenuOpen = false;
            }
        }
    }
}
