using System.Collections.Generic;
using UnityEngine;
using DV.Radio;
using HarmonyLib;

namespace BoomboxSyncMod
{
    public class GhostBoomboxUpdater : MonoBehaviour
    {
        public Vector3 TargetPosition;
        public Quaternion TargetRotation;

        void Update()
        {
            if (Vector3.Distance(transform.position, TargetPosition) > 0.001f)
            {
                transform.position = Vector3.Lerp(transform.position, TargetPosition, Time.deltaTime * 15f);
                transform.rotation = Quaternion.Lerp(transform.rotation, TargetRotation, Time.deltaTime * 15f);
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
        private static Dictionary<int, GhostRadioHolder> ghostBoomboxes = new Dictionary<int, GhostRadioHolder>();

        public static GhostRadioHolder CreateGhost(int id, Vector3 position, Quaternion rotation)
        {
            GameObject ghost = new GameObject("GhostBoombox_" + id);
            ghost.transform.position = position;
            ghost.transform.rotation = rotation;

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
            updater.TargetPosition = position;
            updater.TargetRotation = rotation;

            RadioPlayerController radioController = new RadioPlayerController(ghost, audio);

            GhostRadioHolder holder = new GhostRadioHolder
            {
                GhostObject = ghost,
                Controller = radioController,
                Updater = updater
            };

            ghostBoomboxes[id] = holder;
            return holder;
        }

        public static void OnTransformReceived(int id, Vector3 position, Quaternion rotation)
        {
            if (!Main.enabled) return;

            if (!ghostBoomboxes.TryGetValue(id, out GhostRadioHolder holder))
            {
                CreateGhost(id, position, rotation);
            }
            else
            {
                holder.Updater.TargetPosition = position;
                holder.Updater.TargetRotation = rotation;
            }
        }

        public static void PlayRadioOnGhost(int id, string streamUrl, int fallbackStationIndex, Vector3 position, Quaternion rotation)
        {
            if (!ghostBoomboxes.TryGetValue(id, out GhostRadioHolder holder))
            {
                holder = CreateGhost(id, position, rotation);
            }

            holder.GhostObject.transform.position = position;
            holder.GhostObject.transform.rotation = rotation;
            holder.Updater.TargetPosition = position;
            holder.Updater.TargetRotation = rotation;

            if (holder.Controller != null && !string.IsNullOrEmpty(streamUrl))
            {
                int localIndex = RadioTracker.GetIndexFromUrl(streamUrl);

                if (localIndex == -1)
                {
                    localIndex = RadioTracker.AddUrlToPlaylist(streamUrl);

                    if (localIndex != -1)
                    {
                        holder.Controller.TurnOff();
                        AudioSource audio = holder.GhostObject.GetComponent<AudioSource>();
                        holder.Controller = new RadioPlayerController(holder.GhostObject, audio);
                    }
                }

                if (localIndex != -1)
                {
                    // FIX: Zuerst zwingend ausschalten, dann Index ändern, dann einschalten!
                    holder.Controller.TurnOff();
                    SetControllerIndex(holder.Controller, localIndex);
                    holder.Controller.TurnOn();
                }
            }
        }

        private static void SetControllerIndex(RadioPlayerController controller, int index)
        {
            System.Type type = typeof(RadioPlayerController);

            // KUGELSICHERE METHODE: Sucht dynamisch nach dem versteckten int-Feld
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(int) && field.Name.IndexOf("index", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    field.SetValue(controller, index);
                    return;
                }
            }
            BoomboxLog.Warning("[BoomboxSync] Konnte internen StationIndex nicht finden!");
        }

        public static void StopRadioOnGhost(int id)
        {
            if (ghostBoomboxes.TryGetValue(id, out GhostRadioHolder holder) && holder.Controller != null)
            {
                holder.Controller.TurnOff();
            }
        }

        public static void RemoveGhost(int id)
        {
            if (ghostBoomboxes.TryGetValue(id, out GhostRadioHolder holder))
            {
                if (holder.GhostObject != null)
                {
                    Object.Destroy(holder.GhostObject);
                }
                ghostBoomboxes.Remove(id);
            }
        }

        public static void UpdateAllGhostVolumes(float multiplier, float boost)
        {
            foreach (var kvp in ghostBoomboxes)
            {
                if (kvp.Value?.GhostObject != null)
                {
                    AudioSource audio = kvp.Value.GhostObject.GetComponent<AudioSource>();
                    if (audio != null)
                    {
                        audio.maxDistance = 20f * multiplier;
                        audio.minDistance = 2f * multiplier;
                    }
                }
            }
        }

        public static void ClearAllGhosts()
        {
            foreach (var kvp in ghostBoomboxes)
            {
                if (kvp.Value?.GhostObject != null)
                {
                    Object.Destroy(kvp.Value.GhostObject);
                }
            }
            ghostBoomboxes.Clear();
        }
    }
}