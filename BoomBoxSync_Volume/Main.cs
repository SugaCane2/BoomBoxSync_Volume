using System.Reflection;
using System.IO;
using System;
using UnityModManagerNet;
using HarmonyLib;

namespace BoomboxSyncMod
{
    public static class Main
    {
        public static bool enabled;
        public static Settings settings;
        public static UnityModManager.ModEntry mod;
        public static Harmony harmony;

        public static bool isMultiplayerInstalled;
        public static IMultiplayerAddon MP;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            settings = Settings.Load<Settings>(modEntry);

            var mpMod = UnityModManager.FindMod("Multiplayer");
            isMultiplayerInstalled = mpMod != null && mpMod.Active;

            // Addon dynamisch laden, wenn MP installiert ist
            if (isMultiplayerInstalled)
            {
                string addonPath = Path.Combine(modEntry.Path, "BoomboxSync_Multiplayer.dll");
                if (File.Exists(addonPath))
                {
                    try
                    {
                        Assembly addonAssembly = Assembly.LoadFrom(addonPath);
                        Type addonType = addonAssembly.GetType("BoomboxSyncMod.MultiplayerAddon");
                        MP = (IMultiplayerAddon)Activator.CreateInstance(addonType);
                        BoomboxLog.Info("[BoomboxSync] Multiplayer-Addon erfolgreich geladen!");
                    }
                    catch (Exception ex)
                    {
                        BoomboxLog.Error($"[BoomboxSync] Fehler beim Laden des Addons: {ex}");
                        isMultiplayerInstalled = false; // Fallback auf Singleplayer
                    }
                }
                else
                {
                    BoomboxLog.Warning("[BoomboxSync] BoomboxSync_Multiplayer.dll fehlt im Mod-Ordner! Modus: Singleplayer.");
                    isMultiplayerInstalled = false;
                }
            }

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            harmony = new Harmony(modEntry.Info.Id);

            if (enabled)
            {
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                if (isMultiplayerInstalled && MP != null)
                {
                    MP.Initialize();
                    BoomboxLog.Info("[BoomboxSync] Sync-Features aktiviert.");
                }
                else
                {
                    BoomboxLog.Info("[BoomboxSync] Mod läuft im Singleplayer-Modus (nur Volume Boost).");
                }
            }
            else
            {
                harmony.UnpatchAll(modEntry.Info.Id);
                if (isMultiplayerInstalled && MP != null) MP.Uninitialize();
            }
            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
            ApplyLiveSettings();
        }

        public static void ApplyLiveSettings()
        {
            float multiplier = (float)settings.MaxVolume / 10f;
            float boost = settings.OverdriveBoost;

            foreach (var audio in RadioTracker.radioToAudio.Values)
            {
                if (audio != null)
                {
                    audio.maxDistance = 20f * multiplier;
                    audio.minDistance = 2f * multiplier;

                    AudioBooster booster = audio.gameObject.GetComponent<AudioBooster>();
                    if (booster != null) booster.VolumeMultiplier = boost;
                }
            }

            if (isMultiplayerInstalled && MP != null)
            {
                MP.UpdateGhostVolumes(multiplier, boost);
            }
        }
    }
}