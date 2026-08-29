using System.Reflection;
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

        // NEU: Globale Variable, um zu prüfen, ob der Multiplayer da ist
        public static bool isMultiplayerInstalled;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            settings = Settings.Load<Settings>(modEntry);

            // Prüfen, ob der Multiplayer-Mod installiert und aktiv ist
            var mpMod = UnityModManager.FindMod("Multiplayer");
            isMultiplayerInstalled = mpMod != null && mpMod.Active;

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
                }
            }
            else
            {
                harmony.UnpatchAll(modEntry.Info.Id);
                if (isMultiplayerInstalled)
                {
                    NetworkSync.Uninitialize();
                }
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
                    if (booster != null)
                    {
                        booster.VolumeMultiplier = boost;
                    }
                }
            }

            // Geister-Radios nur updaten, wenn MP aktiv ist
            if (isMultiplayerInstalled)
            {
                GhostBoomboxManager.UpdateAllGhostVolumes(multiplier, boost);
            }
        }
    }
}