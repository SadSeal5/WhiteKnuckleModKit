using HarmonyLib;

namespace WKModKit.Sandbox
{
    /// <summary>
    /// Sandbox'un Harmony yamaları. Yalnızca "negatif" davranışları (hasar, öldürme) engeller;
    /// diğer her şey State.ApplyWatchdog tarafından doğrudan API çağrılarıyla uygulanır.
    /// </summary>
    internal static class Patches
    {
        /// <summary>
        /// ENT_Player.FallingDamage() (private) — düşüş hasarı ve ölümcül düşüş öldürmesi burada işlenir.
        /// Prefix false dönerse orijinal tamamen atlanır: düşüş hiçbir şey yapmaz.
        /// Not: !State.Noclip koşulu gereksizdir (oyunun kendi FallingDamage'i zaten
        /// noclip'te erken çıkar); bilinçli olarak ekstra koruma katmanı olarak bırakıldı.
        /// </summary>
        [HarmonyPatch(typeof(ENT_Player), "FallingDamage")]
        [HarmonyPrefix]
        private static bool FallingDamage_Prefix()
        {
            return !State.NoFallDamage && !State.Noclip;
        }
    }
}
