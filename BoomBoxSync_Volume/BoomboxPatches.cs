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

        public static string[] GetPlaylistBackup()
        {
            string path = RadioPlayerController.GetPlaylistPath();
            if (File.Exists(path))
            {
                return File.ReadAllLines(path);
            }
            return new string[0];
        }

        public static void RestorePlaylist(string[] backupLines)
        {
            if (backupLines == null || backupLines.Length == 0) return;
            string path = RadioPlayerController.GetPlaylistPath();
            File.WriteAllLines(path, backupLines);
            BoomboxLog.Info("[BoomboxSync] Temporärer Sender geladen. Eigene Radio.pls wurde erfolgreich wiederhergestellt.");
        }

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
                                return fileNumber - 1;
                            }
                        }
                    }
                }
            }
            return -1;
        }

        public static int AddUrlToPlaylist(string newUrl)
        {
            string path = RadioPlayerController.GetPlaylistPath();
            if (!File.Exists(path)) return -1;

            List<string> lines = new List<string>(File.ReadAllLines(path));
            int nextIndex = 1;

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

            lines.Add($"File{nextIndex}={newUrl}");
            File.WriteAllLines(path, lines.ToArray());

            return nextIndex - 1;
        }

        public static (Vector3 pos, Quaternion rot, bool inInventory) GetBoomboxTransformAndState(RadioPlayerController instance)
        {
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            bool inInventory = false;

            if (radioToAudio.TryGetValue(instance, out AudioSource audio) && audio != null)
            {
                pos = audio.transform.position;
                rot = audio.transform.rotation;
                inInventory = audio.transform.parent != null;
            }

            return (pos, rot, inInventory);
        }
    }

    [HarmonyPatch(typeof(RadioPlayerController), MethodType.Constructor)]
    [HarmonyPatch(new System.Type[] { typeof(GameObject), typeof(AudioSource) })]
    public static class RadioConstructorPatch
    {
        static void Postfix(RadioPlayerController __instance, GameObject __0, AudioSource __1)
        {
            if (__0 == null) return;
            if (__0.name.StartsWith("GhostBoombox")) return;

            // Multiplayer Registrierung und Komponenten nur laden, wenn MP aktiv ist
            if (Main.isMultiplayerInstalled)
            {
                int id = __0.GetInstanceID();
                RadioTracker.radioToId[__instance] = id;
                BoomboxLog.Info($"[BoomboxSync] Lokales Radio registriert mit ID: {id}");

                if (__0.GetComponent<BoomboxPositionSender>() == null)
                {
                    __0.AddComponent<BoomboxPositionSender>();
                }
            }

            // Lokaler Boost MUSS immer ausgeführt werden (auch im Singleplayer)
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
            if (!Main.enabled || !Main.isMultiplayerInstalled) return;

            if (RadioTracker.radioToId.TryGetValue(__instance, out int boomboxId))
            {
                int currentIndex = __instance.CurrentStationIndex;
                string streamUrl = RadioTracker.GetUrlFromIndex(currentIndex);

                var state = RadioTracker.GetBoomboxTransformAndState(__instance);

                bool networkPlay = true;
                if (state.inInventory && !Main.settings.PlayInInventory) networkPlay = false;

                NetworkSync.SendBoomboxState(boomboxId, networkPlay, streamUrl, currentIndex, state.pos, state.rot, "LocalPlayer", state.inInventory);
            }
        }
    }

    [HarmonyPatch(typeof(RadioPlayerController), "TurnOff")]
    public static class RadioTurnOffPatch
    {
        static void Postfix(RadioPlayerController __instance)
        {
            if (!Main.enabled || !Main.isMultiplayerInstalled) return;

            if (RadioTracker.radioToId.TryGetValue(__instance, out int boomboxId))
            {
                var state = RadioTracker.GetBoomboxTransformAndState(__instance);
                NetworkSync.SendBoomboxState(boomboxId, false, "", 0, state.pos, state.rot, "LocalPlayer", state.inInventory);
            }
        }
    }

    [HarmonyPatch(typeof(RadioPlayerController), "Next")]
    public static class RadioNextPatch
    {
        static void Postfix(RadioPlayerController __instance)
        {
            if (!Main.enabled || !Main.isMultiplayerInstalled) return;

            if (RadioTracker.radioToId.TryGetValue(__instance, out int boomboxId))
            {
                int index = __instance.CurrentStationIndex;
                string streamUrl = RadioTracker.GetUrlFromIndex(index);

                var state = RadioTracker.GetBoomboxTransformAndState(__instance);

                bool networkPlay = true;
                if (state.inInventory && !Main.settings.PlayInInventory) networkPlay = false;

                NetworkSync.SendBoomboxState(boomboxId, networkPlay, streamUrl, index, state.pos, state.rot, "LocalPlayer", state.inInventory);
            }
        }
    }

    [HarmonyPatch(typeof(RadioPlayerController), "Previous")]
    public static class RadioPreviousPatch
    {
        static void Postfix(RadioPlayerController __instance)
        {
            if (!Main.enabled || !Main.isMultiplayerInstalled) return;

            if (RadioTracker.radioToId.TryGetValue(__instance, out int boomboxId))
            {
                int index = __instance.CurrentStationIndex;
                string streamUrl = RadioTracker.GetUrlFromIndex(index);

                var state = RadioTracker.GetBoomboxTransformAndState(__instance);

                bool networkPlay = true;
                if (state.inInventory && !Main.settings.PlayInInventory) networkPlay = false;

                NetworkSync.SendBoomboxState(boomboxId, networkPlay, streamUrl, index, state.pos, state.rot, "LocalPlayer", state.inInventory);
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

        private bool lastInInventory = false;
        private RadioPlayerController controller;

        void Start()
        {
            if (!Main.isMultiplayerInstalled) return;

            instanceId = this.gameObject.GetInstanceID();
            lastPosition = transform.position;
            lastRotation = transform.rotation;

            controller = GetComponent<RadioPlayerController>();
            lastInInventory = (transform.parent != null);

            if (Main.enabled)
            {
                NetworkSync.SendTransformPacket(instanceId, transform.position, transform.rotation);
            }
        }

        void Update()
        {
            if (!Main.isMultiplayerInstalled) return;

            if (Time.time >= nextSendTime)
            {
                nextSendTime = Time.time + sendInterval;

                Vector3 currentPos = transform.position;
                Quaternion currentRot = transform.rotation;

                bool currentInInventory = (transform.parent != null);

                if (currentInInventory != lastInInventory)
                {
                    if (controller != null)
                    {
                        int index = controller.CurrentStationIndex;
                        string url = RadioTracker.GetUrlFromIndex(index);

                        bool actualPlaying = false;
                        if (RadioTracker.radioToAudio.TryGetValue(controller, out AudioSource a) && a != null)
                        {
                            actualPlaying = a.isPlaying;
                        }

                        bool networkPlay = actualPlaying;
                        if (currentInInventory && !Main.settings.PlayInInventory)
                        {
                            networkPlay = false;
                        }

                        NetworkSync.SendBoomboxState(instanceId, networkPlay, url, index, currentPos, currentRot, "LocalPlayer", currentInInventory);
                    }
                    lastInInventory = currentInInventory;
                }

                if (Vector3.Distance(lastPosition, currentPos) > 0.01f ||
                    Quaternion.Angle(lastRotation, currentRot) > 0.1f)
                {
                    NetworkSync.SendTransformPacket(instanceId, currentPos, currentRot);
                    lastPosition = currentPos;
                    lastRotation = currentRot;
                }
            }
        }

        void OnDisable()
        {
            if (!Main.enabled || !Main.isMultiplayerInstalled) return;

            if (controller != null)
            {
                int index = controller.CurrentStationIndex;
                string url = RadioTracker.GetUrlFromIndex(index);

                bool actualPlaying = false;
                if (RadioTracker.radioToAudio.TryGetValue(controller, out AudioSource a) && a != null)
                {
                    actualPlaying = a.isPlaying;
                }

                bool networkPlay = actualPlaying;
                if (!Main.settings.PlayInInventory) networkPlay = false;

                NetworkSync.SendBoomboxState(instanceId, networkPlay, url, index, transform.position, transform.rotation, "LocalPlayer", true);
            }
        }

        void OnDestroy()
        {
            if (Main.enabled && Main.isMultiplayerInstalled) NetworkSync.SendDespawnPacket(instanceId);
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