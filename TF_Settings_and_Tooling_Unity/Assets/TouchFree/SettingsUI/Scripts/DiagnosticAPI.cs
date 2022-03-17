﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

public class DiagnosticAPI : IDisposable
{
    private static string uri = "ws://127.0.0.1:1024/";

    public enum Status { Closed, Connecting, Connected, Expired }
    private Status status = Status.Expired;
    private WebSocket webSocket = null;

    public delegate void MaskingDataDelegate(float _left, float _right, float _top, float _bottom);

    public static event MaskingDataDelegate OnGetMaskingResponse;
    public static event Action<bool> OnMaskingVersionCheck;
    public static event Action<bool> OnGetAnalyticsEnabledResponse;

    public uint connectedDeviceID;
    public bool maskingAllowed = false;

    const string minimumMaskingAPIVerison = "2.1.0";

    ConcurrentQueue<string> newMessages = new ConcurrentQueue<string>();

    public DiagnosticAPI(MonoBehaviour _creatorMonobehaviour)
    {
        Connect();
        _creatorMonobehaviour.StartCoroutine(MessageQueueReader());
    }

    IEnumerator MessageQueueReader()
    {
        while (true)
        {
            if (newMessages.TryDequeue(out var message))
            {
                HandleMessage(message);
            }

            yield return null;
        }
    }

    private void Connect()
    {
        if (status == Status.Connecting || status == Status.Connected)
        {
            return;
        }

        bool requireSetup = status == Status.Expired;
        status = Status.Connecting;

        if (requireSetup)
        {
            if (webSocket != null)
            {
                webSocket.Close();
            }

            webSocket = new WebSocket(uri);
            webSocket.OnMessage += onMessage;
            webSocket.OnOpen += (sender, e) =>
            {
                Debug.Log("DiagnosticAPI open... ");
                status = Status.Connected;
            };
            webSocket.OnError += (sender, e) =>
            {
                Debug.Log("DiagnosticAPI error! " + e.Message + "\n" + e.Exception.ToString());
                status = Status.Expired;
            };
            webSocket.OnClose += (sender, e) =>
            {
                Debug.Log("DiagnosticAPI closed. " + e.Reason);
                status = Status.Closed;
            };
        }

        try
        {
            webSocket.Connect();
        }
        catch (Exception ex)
        {
            Debug.Log("DiagnosticAPI connection exception... " + "\n" + ex.ToString());
            status = Status.Expired;
        }
    }

    private void onMessage(object sender, MessageEventArgs e)
    {
        if (e.IsText)
        {
            newMessages.Enqueue(e.Data);
        }
    }

    void HandleMessage(string _message)
    {
        var response = JsonUtility.FromJson<DiagnosticApiResponse>(_message);

        switch (response.type)
        {
            case "GetImageMask":
                try
                {
                    var maskingResponse = JsonUtility.FromJson<GetImageMaskResponse>(_message);
                    OnGetMaskingResponse?.Invoke(
                        (float)maskingResponse.payload.left,
                        (float)maskingResponse.payload.right,
                        (float)maskingResponse.payload.upper,
                        (float)maskingResponse.payload.lower);
                }
                catch
                {
                    Debug.Log("DiagnosticAPI - Could not parse GetImageMask data: " + _message);
                }
                break;
            case "GetDevices":
                try
                {
                    GetDevicesResponse devicesResponse = JsonUtility.FromJson<GetDevicesResponse>(_message);
                    connectedDeviceID = devicesResponse.payload[0].id;
                    Request("GetImageMask:" + connectedDeviceID);
                }
                catch
                {
                    Debug.Log("DiagnosticAPI - Could not parse GetDevices data: " + _message);
                }
                break;
            case "GetVersion":
                try
                {
                    GetVersionResponse versionResponse = JsonUtility.FromJson<GetVersionResponse>(_message);
                    HandleDiagnosticAPIVersion(versionResponse.payload);
                }
                catch
                {
                    Debug.Log("DiagnosticAPI - Could not parse Version data: " + _message);
                }
                break;
            case "GetAnalyticsEnabled":
                try
                {
                    var data = JsonUtility.FromJson<GetAnalyticsEnabledResponse>(_message);
                    OnGetAnalyticsEnabledResponse?.Invoke(data.payload);
                }
                catch
                {
                    Debug.Log("DiagnosticAPI - Could not parse analytics response: " + _message);
                }
                break;
            case "SetAnalyticsEnabled":
                // No current use for this
                break;
            default:
                Debug.Log("DiagnosticAPI - Could not parse analytics response of type: " + response.type + " with message: " + _message);
                break;
        }
    }

    public void HandleDiagnosticAPIVersion(string _version)
    {
        Version curVersion = new Version(_version);
        Version minVersion = new Version(minimumMaskingAPIVerison);

        if (curVersion.CompareTo(minVersion) >= 0)
        {
            // Version allows masking
            maskingAllowed = true;
        }
        else
        {
            // Version does not allow masking
            maskingAllowed = false;
        }

        OnMaskingVersionCheck?.Invoke(maskingAllowed);
    }

    public void Request(object payload)
    {
        if (status == Status.Connected)
        {
            webSocket.Send(JsonUtility.ToJson(payload, true));
        }
        else
        {
            Connect();
        }
    }

    public void SetMasking(float _left, float _right, float _top, float _bottom)
    {
        Request(new SetImageMaskRequest()
        {
            payload = new ImageMaskData()
            {
                device_id = connectedDeviceID,
                left = _left,
                right = _right,
                upper = _top,
                lower = _bottom
            }
        });
    }

    public void GetAnalyticsMode()
    {
        Request(new GetAnalyticsEnabledRequest());
    }

    public void GetImageMask()
    {
        Request(new GetImageMaskRequest() { payload = new DeviceIdPayload { device_id = connectedDeviceID } });
    }

    public void SetAnalyticsMode(bool enabled)
    {
        Request(new SetAnalyticsEnabledRequest() { payload = enabled });
    }

    public void GetDevices()
    {
        Request(new GetDevicesRequest());
    }

    public void GetVersion()
    {
        Request(new GetVersionRequest());
    }

    void IDisposable.Dispose()
    {
        status = Status.Expired;
        webSocket.Close();
    }

    [Serializable]
    class DiagnosticApiRequest
    {
        public DiagnosticApiRequest() { }
        protected DiagnosticApiRequest(string _type)
        {
            type = _type;
        }
        public string type;
    }

    [Serializable]
    class DiagnosticApiResponse
    {
        public DiagnosticApiResponse() { }
        protected DiagnosticApiResponse(string _type)
        {
            type = _type;
        }
        public string type;
        public int? status;
    }

    [Serializable]
    class SetImageMaskRequest : DiagnosticApiRequest
    {
        public SetImageMaskRequest() : base("SetImageMask") { }
        public ImageMaskData payload;
    }

    [Serializable]
    class SetImageMaskResponse : DiagnosticApiResponse
    {
        public SetImageMaskResponse() : base("SetImageMask") { }
        public ImageMaskData payload;
    }

    [Serializable]
    class GetImageMaskRequest : DiagnosticApiRequest
    {
        public GetImageMaskRequest() : base("GetImageMask") { }
        public DeviceIdPayload payload;
    }

    [Serializable]
    class GetImageMaskResponse : DiagnosticApiResponse
    {
        public GetImageMaskResponse() : base("GetImageMask") { }
        public ImageMaskData payload;
    }

    [Serializable]
    class GetVersionRequest : DiagnosticApiRequest
    {
        public GetVersionRequest() : base("GetVersion") { }
    }

    [Serializable]
    class GetVersionResponse : DiagnosticApiResponse
    {
        public GetVersionResponse() : base("GetVersion") { }
        public string payload;
    }

    [Serializable]
    class GetAnalyticsEnabledRequest : DiagnosticApiRequest
    {
        public GetAnalyticsEnabledRequest() : base("GetAnalyticsEnabled") { }
    }

    [Serializable]
    class GetAnalyticsEnabledResponse : DiagnosticApiResponse
    {
        public GetAnalyticsEnabledResponse() : base("GetAnalyticsEnabled") { }
        public bool payload;
    }

    [Serializable]
    class SetAnalyticsEnabledRequest : DiagnosticApiRequest
    {
        public SetAnalyticsEnabledRequest() : base("SetAnalyticsEnabled") { }
        public bool payload;
    }

    [Serializable]
    class GetDevicesRequest : DiagnosticApiRequest
    {
        public GetDevicesRequest() : base("GetDevices") { }
    }

    [Serializable]
    class GetDevicesResponse : DiagnosticApiResponse
    {
        public GetDevicesResponse() : base("GetDevices") { }
        public DiagnosticDevice[] payload;
    }

    [Serializable]
    struct DeviceIdPayload
    {
        public uint device_id;
    }

    [Serializable]
    struct ImageMaskData
    {
        public double lower;
        public double upper;
        public double right;
        public double left;
        public uint device_id;
    }

    [Serializable]
    struct DiagnosticDevice
    {
        public uint id;
        public string type;
        public uint clients;
        public bool streaming;
    }
}