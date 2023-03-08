﻿using Ultraleap.TouchFree.Library.Configuration;
using Ultraleap.TouchFree.Library.Connections.DiagnosticApi;

namespace Ultraleap.TouchFree.Library.Connections.MessageQueues
{
    public class ServiceStatusQueueHandler : MessageQueueHandler
    {
        private readonly IConfigManager configManager;
        private readonly IHandManager handManager;
        private readonly ITrackingDiagnosticApi trackingApi;

        public override ActionCode[] HandledActionCodes => new[] { ActionCode.REQUEST_SERVICE_STATUS };

        protected override string whatThisHandlerDoes => "Service state request";

        protected override ActionCode failureActionCode => ActionCode.SERVICE_STATUS_RESPONSE;

        public ServiceStatusQueueHandler(IUpdateBehaviour _updateBehaviour, IClientConnectionManager _clientMgr, IConfigManager _configManager, IHandManager _handManager, ITrackingDiagnosticApi _trackingApi) : base(_updateBehaviour, _clientMgr)
        {
            configManager = _configManager;
            handManager = _handManager;
            trackingApi = _trackingApi;
        }

        protected override void Handle(IncomingRequestWithId request)
        {
            void handleDeviceInfoResponse()
            {
                var currentConfig = new ServiceStatus(
                    request.RequestId,
                    handManager.ConnectionManager.TrackingServiceState,
                    configManager.ErrorLoadingConfigFiles ? ConfigurationState.ERRORED : ConfigurationState.LOADED,
                    VersionManager.Version,
                    trackingApi.trackingServiceVersion,
                    trackingApi.connectedDeviceSerial,
                    trackingApi.connectedDeviceFirmware);

                clientMgr.SendResponse(currentConfig, ActionCode.SERVICE_STATUS);
            };

            trackingApi.OnTrackingDeviceInfoResponse += handleDeviceInfoResponse;

            // RequestGetDeviceInfo will return false if there is no currently connected camera. This ensures we send
            // a response in cases there's no camera. If there is a camera, we wait for the request for its Device
            // information to resolve and then transmit that.
            if (!(trackingApi.RequestGetDeviceInfo()))
            {
                handleDeviceInfoResponse();
                trackingApi.OnTrackingDeviceInfoResponse -= handleDeviceInfoResponse;
            }
        }
    }
}
