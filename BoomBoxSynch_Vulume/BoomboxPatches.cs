using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DV.Radio;

namespace BoomboxSyncMod
{
    public static class RadioTracker
    {
        public static Dictionary<RadioPlayerController, int> radioToId = new Dictionary<RadioPlayerController, int>();
        public static Dictionary<RadioPlayerController, AudioSource> radioToAudio = new Dictionary<RadioPlayerController, AudioSource>();

        public static bool IsLocalRadioId(int id)
        {
            return radioToId.ContainsValue(id);
        }

        public static string GetUrlFromIndex(int index)
        {
            string path = RadioPlayerController.GetPlaylistPath();
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                string searchStr = "File" + (index + 1) + "=";

                foreach (string line in lines)
                {
                    if (line.StartsWith(searchStr))
                    {
                        return line.Substring(searchStr.Length).Trim();
                    }
                }
            }
            return "";
        }

        public static int GetIndexFromUrl(string targetUrl)
        {
            if (string.IsNullOrEmpty(targetUrl)) return -1;

            string path = RadioPlayerController.GetPlaylistPath();
            if (!File.Exists(path)) return -1;

            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                if (line.StartsWith("File", System.StringComparison.OrdinalIgnoreCase))
                {
                    int eqIndex = line.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        string key = line.Substring(0, eqIndex).Trim();
                        string url = line.Substring(eqIndex + 1).Trim();

                        if (url.Equals(targetUrl, System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(key.Substring(4), out int fileNumber))
                            {
                                return fileNumber - 1; // 0-basierter Index für Unity
                            }
                        }
                    }
                }
            }
            return -1;
        }

        // NEU: Fügt fehlende Sender automatisch zur Radio.pls hinzu!
        public static int AddUrlToPlaylist(string newUrl)
        {
            string path = RadioPlayerController.GetPlaylistPath();
            if (!File.Exists(path)) return -1;

            List<string> lines = new List<string>(File.ReadAllLines(path));
            int nextIndex = 1; // Start bei 1, da "File1", "File2", etc.

            // Finde die höchste vergebene Nummer in der Datei
            foreach (string line in lines)
            {
                if (line.StartsWith("File", System.StringComparison.OrdinalIgnoreCase))
                {
                    int eqIndex = line.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        string key = line.Substring(0, eqIndex).Trim();
                        if (int.TryParse(key.Substring(4), out int num))
                        {
                            if (num >= nextIndex) nextIndex = num + 1;
                        }
                    }
                }
            }

            // Neuen Sender an die Liste anhängen
            lines.Add($"File{nextIndex}={newUrl}");
            File.WriteAllLines(path, lines.ToArray());
            BoomboxLog.Info($"[BoomboxSync] Fehlender Sender wurde automatisch zur Radio.pls hinzugefügt: {newUrl} (Index {nextIndex - 1})");

            return nextIndex - 1; // 0-basiert für die Mod zurückgeben
        }
    }

    // --- (Hier folgen deine restlichen Harmony-Patches wie bisher: RadioConstructorPatch, TurnOn, TurnOff, Next, Previous und BoomboxPositionSender) ---
    // [Hinweis: Lass die restlichen Patches exakt so, wie sie in deiner BoomboxPatches.cs aktuell sind!]

    [HarmonyPatch(typeof(RadioPlayerController), MethodType.Constructor)]
    [HarmonyPatch(new System.Type[] { typeof(GameObject), typeof(AudioSource) })]
    public static class RadioConstructorPatch
    {
        static void Postfix(RadioPlayerController __instance, GameObject __0, AudioSource __1)
        {
            if (__0 == null) return;
            if (__0.name.StartsWith("GhostBoombox")) return;

            int id = __0.GetInstanceID();
            RadioTracker.radioToId[__instance] = id;
            BoomboxLog.Info($"[BoomboxSync] Lokales Radio registriert mit ID: {id}");

            if (__0.GetComponent<BoomboxPositionSender>() == null)
            {
                __0.AddComponent<BoomboxPositionSender>();
            }

            if (__1 != null && Main.enabled)
            {
                RadioTracker.radioToAudio[__instance] = __1;

                float multiplier = (float)Main.settings.MaxVolume / 10f;
                __1.spatialBlend = 1f;
                __1.rolloffMode = AudioRolloffMode.Linear;
                __1.dopplerLevel = 0f;
                __1.maxDistance = 20f * multiplier;
                __1.minDistance = 2f * multiplier;

                AudioBooster booster = __1.gameObject.GetComponent<AudioBooster>();
                if (booster == null)
                {
                    booster = __1.gameObject.AddComponent<AudioBooster>();
                }
                booster.VolumeMultiplier = Main.settings.OverdriveBoost;
            }
        }
    }

    [HarmonyPatch(typeof(RadioPlayerController), "TurnOn")]
    public static class RadioTurnOnPatch
    {
        static void Postfix(RadioPlayerController __instance)
        {
            if (!Main.enabled) return;

            if (RadioTracker.radioToId.TryGetValue(__instance, out int boomboxId))
            {
                int currentIndex = __instance.CurrentStationIndex;
                string streamUrl = RadioTracker.GetUrlFromIndex(currentIndex);

                Vector3 pos = Vector3.zero;
                Quaternion rot = Quaternion.identity;

                if (RadioTracker.radioToAudio.TryGetValue(__instance, out AudioSource audio) && audio != null)
                {
                    pos = audio.transform.position;
                    rot = audio.transform.rotation;
                }

                NetworkSync.SendBoomboxState(boomboxId, true, streamUrl, currentIndex, pos, rot);
            }
        }
    }

    [HarmonyPatch(typeof(RadioPlayerController), "TurnOff")]
    public static class RadioTurnOffPatch
    {
        static void Postfix(RadioPlayerController __instance)
        {
            if (!Main.enabled) return;

            if (RadioTracker.radioToId.TryGetValue(__instance, out int boomboxId))
            {
                Vector3 pos = Vector3.zero;
                Quaternion rot = Quaternion.identity;

                if (RadioTracker.radioToAudio.TryGetValue(__instance, out AudioSource audio) && audio != null)
                {
                    pos = audio.transform.position;
                    rot = audio.transform.rotation;
                }

                NetworkSync.SendBoomboxState(boomboxId, false, "", 0, pos, rot);
            }
        }
    }

    [HarmonyPatch(typeof(RadioPlayerController), "Next")]
    public static class RadioNextPatch
    {
        static void Postfix(RadioPlayerController __instance)
        {
            if (!Main.enabled) return;

            if (RadioTracker.radioToId.TryGetValue(__instance, out int boomboxId))
            {
                int index = __instance.CurrentStationIndex;
                string streamUrl = RadioTracker.GetUrlFromIndex(index);

                Vector3 pos = Vector3.zero;
                Quaternion rot = Quaternion.identity;

                if (RadioTracker.radioToAudio.TryGetValue(__instance, out AudioSource audio) && audio != null)
                {
                    pos = audio.transform.position;
                    rot = audio.transform.rotation;
                }

                NetworkSync.SendBoomboxState(boomboxId, true, streamUrl, index, pos, rot);
            }
        }
    }

    [HarmonyPatch(typeof(RadioPlayerController), "Previous")]
    public static class RadioPreviousPatch
    {
        static void Postfix(RadioPlayerController __instance)
        {
            if (!Main.enabled) return;

            if (RadioTracker.radioToId.TryGetValue(__instance, out int boomboxId))
            {
                int index = __instance.CurrentStationIndex;
                string streamUrl = RadioTracker.GetUrlFromIndex(index);

                Vector3 pos = Vector3.zero;
                Quaternion rot = Quaternion.identity;

                if (RadioTracker.radioToAudio.TryGetValue(__instance, out AudioSource audio) && audio != null)
                {
                    pos = audio.transform.position;
                    rot = audio.transform.rotation;
                }

                NetworkSync.SendBoomboxState(boomboxId, true, streamUrl, index, pos, rot);
            }
        }
    }

    public class BoomboxPositionSender : MonoBehaviour
    {
        private int instanceId;
        private float nextSendTime = 0f;
        private float sendInterval = 0.1f;

        private Vector3 lastPosition;
        private Quaternion lastRotation;

        void Start()
        {
            instanceId = this.gameObject.GetInstanceID();
            lastPosition = transform.position;
            lastRotation = transform.rotation;

            // Sofortige Position senden, damit der Geist von Sekunde 1 an am richtigen Ort steht
            if (Main.enabled)
            {
                NetworkSync.SendTransformPacket(instanceId, transform.position, transform.rotation);
            }
        }

        void Update()
        {
            if (Time.time >= nextSendTime)
            {
                nextSendTime = Time.time + sendInterval;

                if (Vector3.Distance(lastPosition, transform.position) > 0.01f ||
                    Quaternion.Angle(lastRotation, transform.rotation) > 0.1f)
                {
                    NetworkSync.SendTransformPacket(instanceId, transform.position, transform.rotation);
                    lastPosition = transform.position;
                    lastRotation = transform.rotation;
                }
            }
        }

        void OnDisable()
        {
            if (Main.enabled) NetworkSync.SendDespawnPacket(instanceId);
        }

        void OnDestroy()
        {
            if (Main.enabled) NetworkSync.SendDespawnPacket(instanceId);
        }
    }

    public class AudioBooster : MonoBehaviour
    {
        public float VolumeMultiplier = 1f;

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (VolumeMultiplier <= 1f) return;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Mathf.Clamp(data[i] * VolumeMultiplier, -1f, 1f);
            }
        }
    }
}