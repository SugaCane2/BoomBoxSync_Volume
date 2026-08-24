using UnityModManagerNet;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BoomboxSyncMod
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Max. Lautstärke-Reichweite (Standard: 10)", Min = 1, Max = 100)]
        public int MaxVolume = 10;

        [Draw("Overdrive Boost (Standard: 1.0, Achtung: Laut!)", Min = 0.01, Max = 10.00)]
        public float OverdriveBoost = 1f;

        [Draw("Radar-Puffer (Meter, bis der Stream im Hintergrund gekappt wird). <b>Empfohlener Standard: 50</b>", Min = 10, Max = 500)]
        public int CullingDistance = 50;

        [Draw("Debug-Logs in der Konsole anzeigen")]
        public bool EnableDebugLogs = true;

        // --- NEU: Das Feature für das Inventar/Hand ---
        [Draw("Radios spielen in der Hand / im Inventar weiter")]
        public bool PlayInInventory = true;

        // V2.0 Feature: Speicher für stummgeschaltete Spieler
        public List<string> MutedPlayers = new List<string>();

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            MaxVolume = Mathf.Clamp(MaxVolume, 1, 100);
            OverdriveBoost = Mathf.Clamp(OverdriveBoost, 0.01f, 10f);
            CullingDistance = Mathf.Clamp(CullingDistance, 10, 500);

            Save(this, modEntry);
        }

        public void OnChange()
        {
            MaxVolume = Mathf.Clamp(MaxVolume, 1, 100);
            OverdriveBoost = Mathf.Clamp(OverdriveBoost, 0.01f, 10f);
            CullingDistance = Mathf.Clamp(CullingDistance, 10, 500);

            float multiplier = (float)MaxVolume / 10f;
            GhostBoomboxManager.UpdateAllGhostVolumes(multiplier, OverdriveBoost);
        }

        public void Draw(UnityModManager.ModEntry modEntry)
        {
            Settings self = this;
            UnityModManager.UI.DrawFields(ref self, modEntry, DrawFieldMask.Any, OnChange);

            GUILayout.Space(20);

            // --- DIE MUTE-TABELLE ---
            GUILayout.Label("<b>🎛️ DJ-Pult (Spieler stummschalten)</b>");

            var activeDJs = GhostBoomboxManager.virtualBoomboxes.Values
                .Select(v => v.OwnerName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            if (activeDJs.Count == 0)
            {
                GUILayout.Label("<i>Aktuell sind keine fremden Radios in der Welt bekannt.</i>");
            }
            else
            {
                foreach (string djName in activeDJs)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(djName, GUILayout.Width(200));

                    bool isMuted = MutedPlayers.Contains(djName);
                    bool newMuted = GUILayout.Toggle(isMuted, isMuted ? " 🔇 Stummgeschaltet" : " 🔊 Hörbar");

                    if (newMuted != isMuted)
                    {
                        if (newMuted) MutedPlayers.Add(djName);
                        else MutedPlayers.Remove(djName);

                        GhostBoomboxManager.ProcessRadar();
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Bugfix: Alle Geister-Radios (weltweit) löschen", GUILayout.Width(350)))
            {
                GhostBoomboxManager.ClearAllGhosts();
                NetworkSync.SendClearAllPacket();
            }
        }
    }

    public static class BoomboxLog
    {
        public static void Info(string message) { if (Main.settings != null && Main.settings.EnableDebugLogs && Main.mod != null) Main.mod.Logger.Log(message); }
        public static void Warning(string message) { if (Main.settings != null && Main.settings.EnableDebugLogs && Main.mod != null) Main.mod.Logger.Warning(message); }
        public static void Error(string message) { if (Main.mod != null) Main.mod.Logger.Error(message); }
    }
}