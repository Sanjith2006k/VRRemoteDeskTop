using System;
using UnityEngine;

namespace VRRemoteDesktop.Networking
{
    public enum NetworkMessageType : byte
    {
        Handshake = 0x01,
        HandshakeAck = 0x02,
        HeartbeatPing = 0x03,
        HeartbeatPong = 0x04,
        MouseMove = 0x10,
        MouseButton = 0x11,
        MouseScroll = 0x12,
        KeyDown = 0x13,
        KeyUp = 0x14,
        TextInput = 0x15,
        SetQuality = 0x20,
        FrameData = 0x30,
        Disconnect = 0xFF
    }

    [Serializable]
    public struct NetworkHandshake
    {
        public string ClientVersion;
        public string ClientName;
        public string AuthToken;
        public string RequestedPreset;
    }

    [Serializable]
    public struct NetworkInputPayload
    {
        public NetworkMessageType Type;
        public long Timestamp;
        public float X;
        public float Y;
        public int Button;
        public int KeyCode;
        public string Text;
    }
}
