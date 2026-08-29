using UnityModManagerNet;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BoomboxSyncMod
{
    public enum ModLanguage
    {
        Deutsch,
        English
    }

    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public ModLanguage SelectedLanguage = ModLanguage.Deutsch;

        public int MaxVolume = 10;
        public float OverdriveBoost = 1f;
        public int CullingDistance = 50;
        public bool EnableDebugLogs = false;
        public bool PlayInInventory = true;

        [HideInInspector]
        public List<string> MutedPlayers = new List<string>();

        private string _maxVolStr;
        private string _boostStr;
        private string _cullingStr;

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

<<<<<<< HEAD
<<<<<<< HEAD
            if (Main.isMultiplayerInstalled)
            {
                // Geändert zu Main.MP
                Main.MP.UpdateGhostVolumes(multiplier, OverdriveBoost);
=======
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
            // Geister-Radios existieren nur, wenn der Multiplayer aktiv ist
            if (Main.isMultiplayerInstalled)
            {
                GhostBoomboxManager.UpdateAllGhostVolumes(multiplier, OverdriveBoost);
<<<<<<< HEAD
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
            }
        }

        public void Draw(UnityModManager.ModEntry modEntry)
        {
            bool isGerman = SelectedLanguage == ModLanguage.Deutsch;

            if (_maxVolStr == null) _maxVolStr = MaxVolume.ToString();
            if (_boostStr == null) _boostStr = OverdriveBoost.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (_cullingStr == null) _cullingStr = CullingDistance.ToString();

            // --- SPRACHAUSWAHL ---
            GUILayout.BeginHorizontal();
            GUILayout.Label(isGerman ? "<b>Sprache / Language:</b>" : "<b>Language / Sprache:</b>", GUILayout.Width(150));

            if (GUILayout.Toggle(SelectedLanguage == ModLanguage.Deutsch, "Deutsch", GUILayout.Width(80))) SelectedLanguage = ModLanguage.Deutsch;
            if (GUILayout.Toggle(SelectedLanguage == ModLanguage.English, "English", GUILayout.Width(80))) SelectedLanguage = ModLanguage.English;

            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            // --- ALLGEMEINE EINSTELLUNGEN (Immer sichtbar) ---

            // Max Range
            GUILayout.BeginHorizontal();
            GUILayout.Label(isGerman ? "Max. Reichweite (1-100):" : "Max Range (1-100):", GUILayout.Width(250));

            int newMaxVol = (int)GUILayout.HorizontalSlider(MaxVolume, 1f, 100f, GUILayout.Width(200));
            if (newMaxVol != MaxVolume)
            {
                MaxVolume = newMaxVol;
                _maxVolStr = MaxVolume.ToString();
                OnChange();
            }

            _maxVolStr = GUILayout.TextField(_maxVolStr, GUILayout.Width(50));
            if (int.TryParse(_maxVolStr, out int parsedMaxVol) && parsedMaxVol != MaxVolume)
            {
                MaxVolume = Mathf.Clamp(parsedMaxVol, 1, 100);
                OnChange();
            }
            GUILayout.EndHorizontal();

            // Overdrive Boost
            GUILayout.BeginHorizontal();
            GUILayout.Label(isGerman ? "Overdrive Boost (0.01-10.0):" : "Overdrive Boost (0.01-10.0):", GUILayout.Width(250));

            float newBoost = GUILayout.HorizontalSlider(OverdriveBoost, 0.01f, 10f, GUILayout.Width(200));
            if (Mathf.Abs(newBoost - OverdriveBoost) > 0.001f)
            {
                OverdriveBoost = Mathf.Round(newBoost * 100f) / 100f;
                _boostStr = OverdriveBoost.ToString(System.Globalization.CultureInfo.InvariantCulture);
                OnChange();
            }

            _boostStr = GUILayout.TextField(_boostStr, GUILayout.Width(50));
            string safeBoostStr = _boostStr.Replace(',', '.');
            if (float.TryParse(safeBoostStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedBoost))
            {
                if (Mathf.Abs(parsedBoost - OverdriveBoost) > 0.001f)
                {
                    OverdriveBoost = Mathf.Clamp(parsedBoost, 0.01f, 10f);
                    OnChange();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            bool newDebug = GUILayout.Toggle(EnableDebugLogs, isGerman ? " Debug-Logs in der Konsole anzeigen" : " Show Debug Logs in console");
            if (newDebug != EnableDebugLogs) EnableDebugLogs = newDebug;

            // --- MULTIPLAYER EINSTELLUNGEN (Nur sichtbar, wenn MP installiert ist) ---
            if (Main.isMultiplayerInstalled)
            {
                GUILayout.Space(15);
                GUILayout.Label(isGerman ? "<b>Multiplayer Sync Einstellungen:</b>" : "<b>Multiplayer Sync Settings:</b>");
                GUILayout.Space(5);

                // Culling Distance
                GUILayout.BeginHorizontal();
                GUILayout.Label(isGerman ? "Radar-Puffer in Metern (10-500):" : "Radar Buffer in Meters (10-500):", GUILayout.Width(250));

                int newCulling = (int)GUILayout.HorizontalSlider(CullingDistance, 10f, 500f, GUILayout.Width(200));
                if (newCulling != CullingDistance)
                {
                    CullingDistance = newCulling;
                    _cullingStr = CullingDistance.ToString();
                    OnChange();
                }

                _cullingStr = GUILayout.TextField(_cullingStr, GUILayout.Width(50));
                if (int.TryParse(_cullingStr, out int parsedCulling) && parsedCulling != CullingDistance)
                {
                    CullingDistance = Mathf.Clamp(parsedCulling, 10, 500);
                    OnChange();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                bool newInventory = GUILayout.Toggle(PlayInInventory, isGerman
                    ? " Radios spielen in der Hand / im Inventar für andere weiter"
                    : " Radios continue playing in hand / inventory for others");
                if (newInventory != PlayInInventory) PlayInInventory = newInventory;

                GUILayout.Space(20);

                // --- DJ PANEL ---
                GUILayout.Label(isGerman
                    ? "<b>🎛️ DJ-Pult (Spieler stummschalten)</b>"
                    : "<b>🎛️ DJ Panel (Mute Players)</b>");

<<<<<<< HEAD
<<<<<<< HEAD
                // Geändert zu Main.MP
                var activeDJs = Main.MP.GetActiveDJs();
=======
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
                var activeDJs = GhostBoomboxManager.virtualBoomboxes.Values
                    .Select(v => v.OwnerName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .ToList();
<<<<<<< HEAD
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
=======
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c

                if (activeDJs.Count == 0)
                {
                    GUILayout.Label(isGerman
                        ? "<i>Aktuell sind keine fremden Radios in der Welt bekannt.</i>"
                        : "<i>No foreign radios currently detected in world.</i>");
                }
                else
                {
                    foreach (string djName in activeDJs)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(djName, GUILayout.Width(200));

                        bool isMuted = MutedPlayers.Contains(djName);
                        string buttonText = isGerman
                            ? (isMuted ? " 🔇 Stummgeschaltet" : " 🔊 Hörbar")
                            : (isMuted ? " 🔇 Muted" : " 🔊 Audible");

                        bool newMuted = GUILayout.Toggle(isMuted, buttonText);

                        if (newMuted != isMuted)
                        {
                            if (newMuted) MutedPlayers.Add(djName);
                            else MutedPlayers.Remove(djName);

<<<<<<< HEAD
<<<<<<< HEAD
                            // Geändert zu Main.MP
                            Main.MP.ProcessRadar();
=======
                            GhostBoomboxManager.ProcessRadar();
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
=======
                            GhostBoomboxManager.ProcessRadar();
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(20);

                string bugfixBtnText = isGerman
                    ? "Bugfix: Alle Geister-Radios (weltweit) löschen"
                    : "Bugfix: Clear all ghost radios (world)";

                if (GUILayout.Button(bugfixBtnText, GUILayout.Width(350)))
                {
<<<<<<< HEAD
<<<<<<< HEAD
                    // Geändert zu Main.MP
                    Main.MP.ClearAllGhosts();
                    Main.MP.SendClearAllPacket();
=======
                    GhostBoomboxManager.ClearAllGhosts();
                    NetworkSync.SendClearAllPacket();
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
=======
                    GhostBoomboxManager.ClearAllGhosts();
                    NetworkSync.SendClearAllPacket();
>>>>>>> 0b4a913f71c00fc4d97f19b1ef99babb1f1bbe6c
                }
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