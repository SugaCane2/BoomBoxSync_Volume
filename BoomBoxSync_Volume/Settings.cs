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
        // Ausgewählte Sprache
        public ModLanguage SelectedLanguage = ModLanguage.Deutsch;

        public int MaxVolume = 10;
        public float OverdriveBoost = 1f;
        public int CullingDistance = 50;
        public bool EnableDebugLogs = false;
        public bool PlayInInventory = true;

        [HideInInspector]
        public List<string> MutedPlayers = new List<string>();

        // Private Strings für flüssige Texteingaben (werden nicht in der XML gespeichert)
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
            GhostBoomboxManager.UpdateAllGhostVolumes(multiplier, OverdriveBoost);
        }

        public void Draw(UnityModManager.ModEntry modEntry)
        {
            bool isGerman = SelectedLanguage == ModLanguage.Deutsch;

            // Initialisiere die Textfelder einmalig beim Öffnen des Menüs
            if (_maxVolStr == null) _maxVolStr = MaxVolume.ToString();
            if (_boostStr == null) _boostStr = OverdriveBoost.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (_cullingStr == null) _cullingStr = CullingDistance.ToString();

            // --- SPRACHAUSWAHL / LANGUAGE SELECTOR ---
            GUILayout.BeginHorizontal();
            GUILayout.Label(isGerman ? "<b>Sprache / Language:</b>" : "<b>Language / Sprache:</b>", GUILayout.Width(150));

            if (GUILayout.Toggle(SelectedLanguage == ModLanguage.Deutsch, "Deutsch", GUILayout.Width(80)))
            {
                SelectedLanguage = ModLanguage.Deutsch;
            }
            if (GUILayout.Toggle(SelectedLanguage == ModLanguage.English, "English", GUILayout.Width(80)))
            {
                SelectedLanguage = ModLanguage.English;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            // --- EINSTELLUNGEN (Slider + Textfeld kombiniert) ---

            // Max Range (Slider + Text)
            GUILayout.BeginHorizontal();
            GUILayout.Label(isGerman ? "Max. Reichweite (1-100):" : "Max Range (1-100):", GUILayout.Width(250));

            int newMaxVol = (int)GUILayout.HorizontalSlider(MaxVolume, 1f, 100f, GUILayout.Width(200));
            if (newMaxVol != MaxVolume)
            {
                MaxVolume = newMaxVol;
                _maxVolStr = MaxVolume.ToString(); // Textfeld aktualisieren, wenn Slider bewegt wird
                OnChange();
            }

            _maxVolStr = GUILayout.TextField(_maxVolStr, GUILayout.Width(50));
            if (int.TryParse(_maxVolStr, out int parsedMaxVol) && parsedMaxVol != MaxVolume)
            {
                MaxVolume = Mathf.Clamp(parsedMaxVol, 1, 100);
                OnChange(); // Slider aktualisiert sich automatisch mit
            }
            GUILayout.EndHorizontal();

            // Overdrive Boost (Slider + Text)
            GUILayout.BeginHorizontal();
            GUILayout.Label(isGerman ? "Overdrive Boost (0.01-10.0):" : "Overdrive Boost (0.01-10.0):", GUILayout.Width(250));

            float newBoost = GUILayout.HorizontalSlider(OverdriveBoost, 0.01f, 10f, GUILayout.Width(200));
            if (Mathf.Abs(newBoost - OverdriveBoost) > 0.001f)
            {
                OverdriveBoost = Mathf.Round(newBoost * 100f) / 100f; // Auf 2 Nachkommastellen runden
                _boostStr = OverdriveBoost.ToString(System.Globalization.CultureInfo.InvariantCulture);
                OnChange();
            }

            _boostStr = GUILayout.TextField(_boostStr, GUILayout.Width(50));
            string safeBoostStr = _boostStr.Replace(',', '.'); // Akzeptiert Komma und Punkt
            if (float.TryParse(safeBoostStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedBoost))
            {
                if (Mathf.Abs(parsedBoost - OverdriveBoost) > 0.001f)
                {
                    OverdriveBoost = Mathf.Clamp(parsedBoost, 0.01f, 10f);
                    OnChange();
                }
            }
            GUILayout.EndHorizontal();

            // Culling Distance (Slider + Text)
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

            // Toggles (Checkboxes)
            bool newDebug = GUILayout.Toggle(EnableDebugLogs, isGerman
                ? " Debug-Logs in der Konsole anzeigen"
                : " Show Debug Logs in console");
            if (newDebug != EnableDebugLogs) EnableDebugLogs = newDebug;

            bool newInventory = GUILayout.Toggle(PlayInInventory, isGerman
                ? " Radios spielen in der Hand / im Inventar für andere weiter"
                : " Radios continue playing in hand / inventory for others");
            if (newInventory != PlayInInventory) PlayInInventory = newInventory;

            GUILayout.Space(20);

            // --- DIE MUTE-TABELLE / DJ PANEL ---
            GUILayout.Label(isGerman
                ? "<b>🎛️ DJ-Pult (Spieler stummschalten)</b>"
                : "<b>🎛️ DJ Panel (Mute Players)</b>");

            var activeDJs = GhostBoomboxManager.virtualBoomboxes.Values
                .Select(v => v.OwnerName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

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

                        GhostBoomboxManager.ProcessRadar();
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