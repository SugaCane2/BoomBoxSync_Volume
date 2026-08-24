using System.Collections.Generic;
using UnityEngine;
using DV.Radio;
using HarmonyLib;

namespace BoomboxSyncMod
{
    public class VirtualBoomboxState
    {
        public int Id;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsPlaying;
        public string StreamUrl;
        public int StationIndex;
        public string OwnerName;
        public bool IsInInventory;
    }

    public class GhostBoomboxUpdater : MonoBehaviour
    {
        public Vector3 TargetPosition;
        public Quaternion TargetRotation;

        void Update()
        {
            float dist = Vector3.Distance(transform.position, TargetPosition);

            if (dist > 10f)
            {
                transform.position = TargetPosition;
                transform.rotation = TargetRotation;
            }
            else if (dist > 0.001f)
            {
                transform.position = Vector3.Lerp(transform.position, TargetPosition, Time.deltaTime * 15f);
                transform.rotation = Quaternion.Lerp(transform.rotation, TargetRotation, Time.deltaTime * 15f);
            }
        }
    }

    public class BoomboxRadar : MonoBehaviour
    {
        private float timer = 0f;

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= 1.0f)
            {
                timer = 0f;
                GhostBoomboxManager.ProcessRadar();
            }
        }
    }

    public class GhostRadioHolder
    {
        public GameObject GhostObject;
        public RadioPlayerController Controller;
        public GhostBoomboxUpdater Updater;
    }

    public static class GhostBoomboxManager
    {
        public static Dictionary<int, VirtualBoomboxState> virtualBoomboxes = new Dictionary<int, VirtualBoomboxState>();
        private static Dictionary<int, GhostRadioHolder> activeGhostBoomboxes = new Dictionary<int, GhostRadioHolder>();

        public static void InitializeRadar()
        {
            GameObject radar = new GameObject("BoomboxRadarSystem");
            Object.DontDestroyOnLoad(radar);
            radar.AddComponent<BoomboxRadar>();
        }

        public static void UpdateVirtualState(BoomboxStatePacket packet)
        {
            if (!virtualBoomboxes.ContainsKey(packet.BoomboxId))
            {
                virtualBoomboxes[packet.BoomboxId] = new VirtualBoomboxState { Id = packet.BoomboxId };
            }

            var state = virtualBoomboxes[packet.BoomboxId];
            state.Position = packet.Position;
            state.Rotation = packet.Rotation;
            state.IsPlaying = packet.IsPlaying;
            state.StreamUrl = packet.StreamUrl;
            state.StationIndex = packet.StationIndex;
            state.OwnerName = packet.OwnerName;
            state.IsInInventory = packet.IsInInventory;

            ProcessRadar();
        }

        public static void OnTransformReceived(int id, Vector3 position, Quaternion rotation)
        {
            if (!virtualBoomboxes.ContainsKey(id)) return;
            virtualBoomboxes[id].Position = position;
            virtualBoomboxes[id].Rotation = rotation;

            if (activeGhostBoomboxes.TryGetValue(id, out GhostRadioHolder holder))
            {
                holder.Updater.TargetPosition = position;
                holder.Updater.TargetRotation = rotation;
            }
        }

        public static void ProcessRadar()
        {
            if (Camera.main == null) return;
            Vector3 playerPos = Camera.main.transform.position;

            float maxAudioRadius = (Main.settings.MaxVolume / 10f) * 20f;
            float spawnRadius = maxAudioRadius + Main.settings.CullingDistance;

            foreach (var kvp in virtualBoomboxes)
            {
                int id = kvp.Key;
                VirtualBoomboxState state = kvp.Value;

                float distance = Vector3.Distance(playerPos, state.Position);
                bool isMuted = Main.settings.MutedPlayers.Contains(state.OwnerName);

                bool shouldPlay = (distance <= spawnRadius) && state.IsPlaying && !isMuted;

                if (shouldPlay && !activeGhostBoomboxes.ContainsKey(id))
                {
                    SpawnAndPlay(state);
                }
                else if (!shouldPlay && activeGhostBoomboxes.ContainsKey(id))
                {
                    RemoveGhost(id);
                }
            }
        }

        private static void SpawnAndPlay(VirtualBoomboxState state)
        {
            GameObject ghost = new GameObject("GhostBoombox_" + state.Id);
            ghost.transform.position = state.Position;
            ghost.transform.rotation = state.Rotation;

            AudioSource audio = ghost.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.dopplerLevel = 0f;

            foreach (var localAudio in RadioTracker.radioToAudio.Values)
            {
                if (localAudio != null && localAudio.outputAudioMixerGroup != null)
                {
                    audio.outputAudioMixerGroup = localAudio.outputAudioMixerGroup;
                    break;
                }
            }

            float multiplier = (float)Main.settings.MaxVolume / 10f;
            audio.maxDistance = 20f * multiplier;
            audio.minDistance = 2f * multiplier;
            audio.volume = 1f;

            AudioBooster booster = ghost.AddComponent<AudioBooster>();
            booster.VolumeMultiplier = Main.settings.OverdriveBoost;

            GhostBoomboxUpdater updater = ghost.AddComponent<GhostBoomboxUpdater>();
            updater.TargetPosition = state.Position;
            updater.TargetRotation = state.Rotation;

            RadioPlayerController controller = new RadioPlayerController(ghost, audio);

            int localIndex = RadioTracker.GetIndexFromUrl(state.StreamUrl);
            if (localIndex == -1)
            {
                // --- NEU: Backup-Logik für saubere lokale Playlists ---
                string[] originalPlaylist = RadioTracker.GetPlaylistBackup();

                localIndex = RadioTracker.AddUrlToPlaylist(state.StreamUrl);
                if (localIndex != -1)
                {
                    controller.TurnOff();
                    controller = new RadioPlayerController(ghost, audio);

                    RadioTracker.RestorePlaylist(originalPlaylist);
                }
            }

            if (localIndex != -1)
            {
                controller.TurnOff();
                SetControllerIndex(controller, localIndex);
                controller.TurnOn();
            }

            activeGhostBoomboxes[state.Id] = new GhostRadioHolder
            {
                GhostObject = ghost,
                Controller = controller,
                Updater = updater
            };
        }

        private static void SetControllerIndex(RadioPlayerController controller, int index)
        {
            System.Type type = typeof(RadioPlayerController);
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(int) && field.Name.IndexOf("index", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    field.SetValue(controller, index);
                    return;
                }
            }
        }

        public static void RemoveGhost(int id)
        {
            if (activeGhostBoomboxes.TryGetValue(id, out GhostRadioHolder holder))
            {
                if (holder.Controller != null) holder.Controller.TurnOff();
                if (holder.GhostObject != null) Object.Destroy(holder.GhostObject);
                activeGhostBoomboxes.Remove(id);
            }
        }

        public static void ClearAllGhosts()
        {
            foreach (var kvp in activeGhostBoomboxes)
            {
                if (kvp.Value?.GhostObject != null) Object.Destroy(kvp.Value.GhostObject);
            }
            activeGhostBoomboxes.Clear();
            virtualBoomboxes.Clear();
        }

        public static void UpdateAllGhostVolumes(float multiplier, float boost)
        {
            foreach (var kvp in activeGhostBoomboxes)
            {
                if (kvp.Value?.GhostObject != null)
                {
                    AudioSource audio = kvp.Value.GhostObject.GetComponent<AudioSource>();
                    if (audio != null)
                    {
                        audio.maxDistance = 20f * multiplier;
                        audio.minDistance = 2f * multiplier;
                    }
                    AudioBooster b = kvp.Value.GhostObject.GetComponent<AudioBooster>();
                    if (b != null) b.VolumeMultiplier = boost;
                }
            }
        }
    }
}