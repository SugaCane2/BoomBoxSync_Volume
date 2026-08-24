using System.IO;
using UnityEngine;
using MPAPI;
using MPAPI.Interfaces;
using MPAPI.Interfaces.Packets;

namespace BoomboxSyncMod
{
    public class BoomboxStatePacket : ISerializablePacket
    {
        public int BoomboxId;

        // V2.0 Features:
        public string OwnerName;       // Speichert den Namen des Spielers (UTF-8 sicher!)
        public bool IsInInventory;     // Ist das Radio gerade eingesteckt?

        public bool IsPlaying;
        public string StreamUrl;
        public int StationIndex;
        public Vector3 Position;
        public Quaternion Rotation;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(BoomboxId);
            writer.Write(OwnerName ?? "Unbekannt");
            writer.Write(IsInInventory);
            writer.Write(IsPlaying);
            writer.Write(StreamUrl ?? "");
            writer.Write(StationIndex);
            writer.Write(Position.x); writer.Write(Position.y); writer.Write(Position.z);
            writer.Write(Rotation.x); writer.Write(Rotation.y); writer.Write(Rotation.z); writer.Write(Rotation.w);
        }

        public void Deserialize(BinaryReader reader)
        {
            BoomboxId = reader.ReadInt32();
            OwnerName = reader.ReadString();
            IsInInventory = reader.ReadBoolean();
            IsPlaying = reader.ReadBoolean();
            StreamUrl = reader.ReadString();
            StationIndex = reader.ReadInt32();
            Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }

    public class BoomboxTransformPacket : ISerializablePacket
    {
        public int BoomboxId;
        public Vector3 Position;
        public Quaternion Rotation;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(BoomboxId);
            writer.Write(Position.x); writer.Write(Position.y); writer.Write(Position.z);
            writer.Write(Rotation.x); writer.Write(Rotation.y); writer.Write(Rotation.z); writer.Write(Rotation.w);
        }

        public void Deserialize(BinaryReader reader)
        {
            BoomboxId = reader.ReadInt32();
            Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }

    public class BoomboxDespawnPacket : ISerializablePacket
    {
        public int BoomboxId;

        public void Serialize(BinaryWriter writer) { writer.Write(BoomboxId); }
        public void Deserialize(BinaryReader reader) { BoomboxId = reader.ReadInt32(); }
    }

    public class BoomboxClearAllPacket : ISerializablePacket
    {
        public void Serialize(BinaryWriter writer) { }
        public void Deserialize(BinaryReader reader) { }
    }

    public static class NetworkSync
    {
        public static void Initialize()
        {
            GameObject watcher = new GameObject("BoomboxNetworkWatcher");
            Object.DontDestroyOnLoad(watcher);
            watcher.AddComponent<NetworkRegistrationWatcher>();
        }

        public static void Uninitialize() { }

        public static void SendTransformPacket(int id, Vector3 position, Quaternion rotation)
        {
            if (!MultiplayerAPI.IsMultiplayerLoaded || MultiplayerAPI.Client == null || !MultiplayerAPI.Client.IsConnected) return;

            var packet = new BoomboxTransformPacket { BoomboxId = id, Position = position, Rotation = rotation };
            MultiplayerAPI.Client.SendSerializablePacketToServer(packet, false);
        }

        // ACHTUNG V2.0: Neue optionale Parameter am Ende, damit die alten Dateien erstmal nicht kaputtgehen
        public static void SendBoomboxState(int boomboxId, bool isPlaying, string streamUrl, int stationIndex, Vector3 pos, Quaternion rot, string ownerName = "LocalPlayer", bool isInInventory = false)
        {
            if (!MultiplayerAPI.IsMultiplayerLoaded || MultiplayerAPI.Client == null || !MultiplayerAPI.Client.IsConnected) return;

            var packet = new BoomboxStatePacket
            {
                BoomboxId = boomboxId,
                OwnerName = ownerName,
                IsInInventory = isInInventory,
                IsPlaying = isPlaying,
                StreamUrl = streamUrl,
                StationIndex = stationIndex,
                Position = pos,
                Rotation = rot
            };
            MultiplayerAPI.Client.SendSerializablePacketToServer(packet, true);
        }

        public static void SendDespawnPacket(int boomboxId)
        {
            if (!MultiplayerAPI.IsMultiplayerLoaded || MultiplayerAPI.Client == null || !MultiplayerAPI.Client.IsConnected) return;
            var packet = new BoomboxDespawnPacket { BoomboxId = boomboxId };
            MultiplayerAPI.Client.SendSerializablePacketToServer(packet, true);
        }

        public static void SendClearAllPacket()
        {
            if (!MultiplayerAPI.IsMultiplayerLoaded || MultiplayerAPI.Client == null || !MultiplayerAPI.Client.IsConnected) return;
            var packet = new BoomboxClearAllPacket();
            MultiplayerAPI.Client.SendSerializablePacketToServer(packet, true);
        }

        public static void OnClientTransformReceived(BoomboxTransformPacket packet)
        {
            if (RadioTracker.IsLocalRadioId(packet.BoomboxId)) return;
            GhostBoomboxManager.OnTransformReceived(packet.BoomboxId, packet.Position, packet.Rotation);
        }

        public static void OnClientStateReceived(BoomboxStatePacket packet)
        {
            if (RadioTracker.IsLocalRadioId(packet.BoomboxId)) return;

            if (packet.IsPlaying)
            {
                // Für den Moment übergeben wir die Daten noch an das alte System, bis wir Schritt 2 gebaut haben
                GhostBoomboxManager.PlayRadioOnGhost(packet.BoomboxId, packet.StreamUrl, packet.StationIndex, packet.Position, packet.Rotation);
            }
            else
            {
                GhostBoomboxManager.StopRadioOnGhost(packet.BoomboxId);
            }
        }

        public static void OnClientDespawnReceived(BoomboxDespawnPacket packet)
        {
            if (RadioTracker.IsLocalRadioId(packet.BoomboxId)) return;
            GhostBoomboxManager.RemoveGhost(packet.BoomboxId);
        }

        public static void OnClientClearAllReceived(BoomboxClearAllPacket packet)
        {
            GhostBoomboxManager.ClearAllGhosts();
        }

        // SERVER-SEITE: Hier injecten wir später den echten Namen!
        public static void OnServerTransformReceived(BoomboxTransformPacket packet, IPlayer sender)
        {
            MultiplayerAPI.Server.SendSerializablePacketToAll(packet, false, false, sender);
            OnClientTransformReceived(packet);
        }

        public static void OnServerStateReceived(BoomboxStatePacket packet, IPlayer sender)
        {
            // V2.0 MAGIE: Der Server überschreibt den OwnerName automatisch mit dem echten Namen des Spielers, der das Paket gesendet hat!
            if (sender != null && !string.IsNullOrEmpty(sender.Username))
            {
                packet.OwnerName = sender.Username;
            }

            MultiplayerAPI.Server.SendSerializablePacketToAll(packet, true, false, sender);
            OnClientStateReceived(packet);
        }

        public static void OnServerDespawnReceived(BoomboxDespawnPacket packet, IPlayer sender)
        {
            MultiplayerAPI.Server.SendSerializablePacketToAll(packet, true, false, sender);
            OnClientDespawnReceived(packet);
        }

        public static void OnServerClearAllReceived(BoomboxClearAllPacket packet, IPlayer sender)
        {
            MultiplayerAPI.Server.SendSerializablePacketToAll(packet, true, false, sender);
            OnClientClearAllReceived(packet);
        }
    }

    public class NetworkRegistrationWatcher : MonoBehaviour
    {
        private bool isRegisteredForSession = false;

        void Update()
        {
            if (!MultiplayerAPI.IsMultiplayerLoaded) return;
            bool isConnected = MultiplayerAPI.Client != null && MultiplayerAPI.Client.IsConnected;

            if (isConnected && !isRegisteredForSession)
            {
                MultiplayerAPI.Client.RegisterSerializablePacket<BoomboxStatePacket>(NetworkSync.OnClientStateReceived);
                MultiplayerAPI.Client.RegisterSerializablePacket<BoomboxTransformPacket>(NetworkSync.OnClientTransformReceived);
                MultiplayerAPI.Client.RegisterSerializablePacket<BoomboxDespawnPacket>(NetworkSync.OnClientDespawnReceived);
                MultiplayerAPI.Client.RegisterSerializablePacket<BoomboxClearAllPacket>(NetworkSync.OnClientClearAllReceived);

                if (MultiplayerAPI.Server != null)
                {
                    MultiplayerAPI.Server.RegisterSerializablePacket<BoomboxStatePacket>(NetworkSync.OnServerStateReceived);
                    MultiplayerAPI.Server.RegisterSerializablePacket<BoomboxTransformPacket>(NetworkSync.OnServerTransformReceived);
                    MultiplayerAPI.Server.RegisterSerializablePacket<BoomboxDespawnPacket>(NetworkSync.OnServerDespawnReceived);
                    MultiplayerAPI.Server.RegisterSerializablePacket<BoomboxClearAllPacket>(NetworkSync.OnServerClearAllReceived);
                }
                isRegisteredForSession = true;
            }
            else if (!isConnected && isRegisteredForSession)
            {
                isRegisteredForSession = false;
            }
        }
    }
}