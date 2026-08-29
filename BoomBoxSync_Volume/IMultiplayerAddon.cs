using UnityEngine;
using System.Collections.Generic;

namespace BoomboxSyncMod
{
    public interface IMultiplayerAddon
    {
        void Initialize();
        void Uninitialize();
        void UpdateGhostVolumes(float multiplier, float boost);
        void SendBoomboxState(int id, bool isPlaying, string url, int index, Vector3 pos, Quaternion rot, string owner, bool inInventory);
        void SendTransformPacket(int id, Vector3 pos, Quaternion rot);
        void SendDespawnPacket(int id);
        void SendClearAllPacket();
        void ClearAllGhosts();
        void ProcessRadar();
        List<string> GetActiveDJs();
    }
}