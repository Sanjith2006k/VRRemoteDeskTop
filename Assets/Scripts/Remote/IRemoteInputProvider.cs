using System;
using UnityEngine;

namespace VRRemoteDesktop.Remote
{
    /// <summary>
    /// Contract for remote input abstraction layer.
    /// Handles transmitting keyboard, mouse, and touch events to a remote host.
    /// </summary>
    public interface IRemoteInputProvider
    {
        bool IsConnected { get; }
        int CurrentLatencyMs { get; }
        long PacketsSent { get; }

        event Action<int> OnLatencyUpdated;
        event Action<string> OnTextReceived;

        void SendTextInput(string text);
        void SendKeyDown(KeyCode key);
        void SendKeyUp(KeyCode key);
        void SendMouseMove(Vector2 delta);
        void SendMouseButton(int button, bool isDown);
        void SendScroll(Vector2 scrollDelta);
    }
}
