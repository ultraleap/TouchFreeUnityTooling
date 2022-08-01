﻿using System.Net.WebSockets;
using Ultraleap.TouchFree.Library.Connections;

namespace Ultraleap.TouchFree.Library
{
    public interface IClientConnectionManager
    {
        HandPresenceEvent MissedHandPresenceEvent { get; }
        void SendInputAction(InputAction _data);
        void SendHandData(HandFrame _data);
        void AddConnection(IClientConnection _connection);
        void RemoveConnection(WebSocket _socket);
        void SendConfigChangeResponse(ResponseToClient response);
        void SendConfigState(ConfigState currentConfig);
        void SendConfigFile(ConfigState currentConfig);
        void SendQuickSetupConfigFile(ConfigState currentConfig);
        void SendQuickSetupResponse(ResponseToClient response);
        void SendStatusResponse(ResponseToClient response);
        void SendHandDataStreamStateResponse(ResponseToClient response);
        void SendStatus(ServiceStatus currentConfig);
        void SendConfigFileChangeResponse(ResponseToClient response);
        void SendTrackingState(TrackingApiState response);
    }
}
