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

<<<<<<< HEAD
<<<<<<< HEAD
        public static bool isMultiplayerInstalled;

        // Die Brücke zum Addon
        public static IMultiplayerAddon MP;

=======
        // NEU: Globale Variable, um zu prüfen, ob der Multiplayer da ist
        public static bool isMultiplayerInstalled;

>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
=======
        // NEU: Globale Variable, um zu prüfen, ob der Multiplayer da ist
        public static bool isMultiplayerInstalled;

>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            settings = Settings.Load<Settings>(modEntry);

<<<<<<< HEAD
<<<<<<< HEAD
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

=======
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
            // Prüfen, ob der Multiplayer-Mod installiert und aktiv ist
            var mpMod = UnityModManager.FindMod("Multiplayer");
            isMultiplayerInstalled = mpMod != null && mpMod.Active;

<<<<<<< HEAD
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
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

<<<<<<< HEAD
<<<<<<< HEAD
                if (isMultiplayerInstalled && MP != null)
                {
                    MP.Initialize();
                    BoomboxLog.Info("[BoomboxSync] Sync-Features aktiviert.");
=======
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
                // Multiplayer-Systeme nur starten, wenn die Mod da ist
                if (isMultiplayerInstalled)
                {
                    NetworkSync.Initialize();
                    GhostBoomboxManager.InitializeRadar();
                    BoomboxLog.Info("[BoomboxSync] Multiplayer erkannt! Sync-Features aktiviert.");
                }
                else
                {
                    BoomboxLog.Info("[BoomboxSync] Kein Multiplayer erkannt. Mod läuft im Singleplayer-Modus (nur Volume Boost).");
<<<<<<< HEAD
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
                }
            }
            else
            {
                harmony.UnpatchAll(modEntry.Info.Id);
<<<<<<< HEAD
<<<<<<< HEAD
                if (isMultiplayerInstalled && MP != null) MP.Uninitialize();
=======
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
                if (isMultiplayerInstalled)
                {
                    NetworkSync.Uninitialize();
                }
<<<<<<< HEAD
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
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

<<<<<<< HEAD
<<<<<<< HEAD
            // Weitergabe an das Addon
            if (isMultiplayerInstalled && MP != null)
            {
                MP.UpdateGhostVolumes(multiplier, boost);
=======
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
            // Geister-Radios nur updaten, wenn MP aktiv ist
            if (isMultiplayerInstalled)
            {
                GhostBoomboxManager.UpdateAllGhostVolumes(multiplier, boost);
<<<<<<< HEAD
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
            }
        }
    }
}