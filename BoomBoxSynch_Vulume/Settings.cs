using UnityModManagerNet;
using UnityEngine;

namespace BoomboxSyncMod
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Max. Lautstärke-Reichweite (Standard: 10)", Min = 1, Max = 100)]
        public int MaxVolume = 10;

        [Draw("Overdrive Boost (Standard: 1.0, Achtung: Laut!)", Min = 0.01, Max = 10.00)]
        public float OverdriveBoost = 1f;

        [Draw("Debug-Logs in der Konsole anzeigen")]
        public bool EnableDebugLogs = true;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            // FIX: Limits an die UI-Werte angepasst!
            MaxVolume = Mathf.Clamp(MaxVolume, 1, 100);
            OverdriveBoost = Mathf.Clamp(OverdriveBoost, 0.01f, 10f);

            Save(this, modEntry);
        }

        public void OnChange()
        {
            // FIX: Limits an die UI-Werte angepasst!
            MaxVolume = Mathf.Clamp(MaxVolume, 1, 100);
            OverdriveBoost = Mathf.Clamp(OverdriveBoost, 0.01f, 10f);

            float multiplier = (float)MaxVolume / 10f;
            GhostBoomboxManager.UpdateAllGhostVolumes(multiplier, OverdriveBoost);
        }

        public void Draw(UnityModManager.ModEntry modEntry)
        {
            Settings self = this;
            UnityModManager.UI.DrawFields(ref self, modEntry, DrawFieldMask.Any, OnChange);

            GUILayout.Space(20);

            if (GUILayout.Button("Bugfix: Alle Geister-Radios (weltweit) löschen", GUILayout.Width(350)))
            {
                GhostBoomboxManager.ClearAllGhosts();
                NetworkSync.SendClearAllPacket();
            }
        }
    }

    // Angepasster Logger für das UMM-Fenster
    public static class BoomboxLog
    {
        public static void Info(string message)
        {
            if (Main.settings != null && Main.settings.EnableDebugLogs && Main.mod != null)
                Main.mod.Logger.Log(message);
        }
        public static void Warning(string message)
        {
            if (Main.settings != null && Main.settings.EnableDebugLogs && Main.mod != null)
                Main.mod.Logger.Warning(message);
        }
        public static void Error(string message)
        {
            if (Main.mod != null) Main.mod.Logger.Error(message);
        }
    }
}