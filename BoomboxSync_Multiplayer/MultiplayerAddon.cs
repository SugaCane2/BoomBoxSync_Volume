using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BoomboxSyncMod
{
    // Diese Klasse wird per Reflection von der Haupt-Mod gestartet
    public class MultiplayerAddon : IMultiplayerAddon
    {
        public void Initialize()
        {
            NetworkSync.Initialize();
            GhostBoomboxManager.InitializeRadar();
        }

        public void Uninitialize()
        {
            NetworkSync.Uninitialize();
        }

        public void UpdateGhostVolumes(float multiplier, float boost)
        {
            GhostBoomboxManager.UpdateAllGhostVolumes(multiplier, boost);
        }

        public void SendBoomboxState(int id, bool isPlaying, string url, int index, Vector3 pos, Quaternion rot, string owner, bool inInventory)
        {
            NetworkSync.SendBoomboxState(id, isPlaying, url, index, pos, rot, owner, inInventory);
        }

        public void SendTransformPacket(int id, Vector3 pos, Quaternion rot)
        {
            NetworkSync.SendTransformPacket(id, pos, rot);
        }

        public void SendDespawnPacket(int id)
        {
            NetworkSync.SendDespawnPacket(id);
        }

        public void SendClearAllPacket()
        {
            NetworkSync.SendClearAllPacket();
        }

        public void ClearAllGhosts()
        {
            GhostBoomboxManager.ClearAllGhosts();
        }

        public void ProcessRadar()
        {
            GhostBoomboxManager.ProcessRadar();
        }

        public List<string> GetActiveDJs()
        {
            return GhostBoomboxManager.virtualBoomboxes.Values
                .Select(v => v.OwnerName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();
        }
    }
}