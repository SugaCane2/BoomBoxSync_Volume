# 📻 BoomboxSyncMod für Unity Mod Manager

**BoomboxSyncMod** ist eine Unity-Mod, die das Multiplayer-Erlebnis in Spielen durch eine präzise und performante Synchronisation von Radio-Streams und Boomboxes auf ein neues Level hebt. Egal ob Web-Streams (wie FFH, TruckersFM oder Simulator Radio) oder individuelle Musik – die Mod sorgt dafür, dass alle Spieler exakt dasselbe zur gleichen Zeit hören.

---

## ✨ Features (Version 2.0)

*   **Vollständige Audio-Synchronisation:** Radios und Boomboxes bewegen sich flüssig mit den Spielern und synchronisieren Streams live im Multiplayer.
*   **Intelligentes Inventar-Verhalten:** Du hast in den Einstellungen die volle Kontrolle darüber, ob dein Radio im Inventar oder in der Hand weiterhin den Ton an Mitspieler überträgt oder stummgeschaltet bleibt. Für dich selbst läuft deine Musik natürlich immer weiter!
*   **🎛️ DJ-Pult (Mute-Tabelle):** In den Mod-Einstellungen findest du eine übersichtliche Liste aller aktiven Spieler-Radios in deiner Welt. Mit einem Klick kannst du störende Radios einzelner Mitspieler komplett stummschalten.
*   **Saubere Playlisten (Auto-Backup):** Fremde Radio-Streams von Mitspielern werden für das Laden im Arbeitsspeicher kurzzeitig genutzt, ohne dauerhaft deine lokale `Radio.pls`-Datei zuzumüllen.
*   **Lautstärke & Overdrive:** Passe die maximale Reichweite individuell an und nutze optional den Overdrive-Boost für mehr Power.
*   **Performance-Optimierung:** Ein intelligentes Radar- und Culling-System kappt Streams im Hintergrund, wenn Spieler zu weit entfernt sind.

---

## ⚙️ Installation

1. Stelle sicher, dass der **Unity Mod Manager (UMM)** für dein Spiel installiert ist.
2. Lade die neueste Version von BoomboxSyncMod herunter.
3. Entpacke den Mod-Ordner in das `Mods`-Verzeichnis deines Spiels oder installiere die ZIP-Datei direkt über den UMM.
4. Starte das Spiel und öffne die UMM-Einstellungen (Strg + F10), um deine Konfiguration anzupassen.

---

## 🛠️ Einstellungen

*   **Max. Lautstärke-Reichweite:** Bestimmt, wie weit der Ton zu hören ist.
*   **Overdrive Boost:** Verstärkt die Lautstärke über das Standard-Limit hinaus (Achtung, kann laut werden!).
*   **Radar-Puffer (Culling Distance):** Definiert den Radius, ab dem entfernte Streams zur Performance-Schonung pausiert werden.
*   **Radio für ANDERE weiterspielen lassen:** Steuert, ob eingesteckte Radios im Inventar für Mitspieler hörbar bleiben.
*   **DJ-Pult:** Aktive Spieler anzeigen und bei Bedarf individuell stummschalten.
