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

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            settings = Settings.Load<Settings>(modEntry);

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
                NetworkSync.Initialize();
            }
            else
            {
                harmony.UnpatchAll(modEntry.Info.Id);
                NetworkSync.Uninitialize();
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
            float boost = settings.OverdriveBoost; // Hier ist kein (float) mehr nötig, da es bereits eine Kommazahl ist

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

            GhostBoomboxManager.UpdateAllGhostVolumes(multiplier, boost);
        }
    }
}