import {
    ConfigState,
    ConfigStateCallback,
    HandPresenceState,
    TouchFreeRequestCallback,
    ResponseCallback,
    ServiceStatus,
    ServiceStatusCallback,
    TouchFreeRequest,
    WebSocketResponse,
} from './TouchFreeServiceTypes';
import {
    BitmaskFlags,
    ConvertInputAction,
    InputType,
    TouchFreeInputAction,
    WebsocketInputAction,
} from '../TouchFreeToolingTypes';
import { InputActionManager } from '../Plugins/InputActionManager';
import { ConnectionManager } from './ConnectionManager';
import { HandDataManager } from 'TouchFree/Plugins/HandDataManager';

// Class: MessageReceiver
// Handles the receiving of messages from the Service in an ordered manner.
// Distributes the results of the messages to the respective managers.
export class MessageReceiver {
    // Group: Variables

    // Variable: callbackClearTimer
    // The amount of time between checks of <responseCallbacks> to eliminate expired
    // <ResponseCallbacks>. Used in <ClearUnresponsiveCallbacks>.
    callbackClearTimer: number = 300;

    // Variable: update Rate
    // How many times per second to process <WebSocketResponse> & <TouchFreeInputActions>
    updateRate: number = 60;

    // Calculated on construction for use in setting the update interval
    private updateDuration: number;

    // Variable: actionCullToCount
    // How many non-essential <TouchFreeInputActions> should the <actionQueue> be trimmed *to* per
    // frame. This is used to ensure the Tooling can keep up with the Events sent over the
    // WebSocket.
    actionCullToCount: number = 2;

    // Variable: actionQueue
    // A queue of <TouchFreeInputActions> that have been received from the Service.
    actionQueue: Array<WebsocketInputAction> = [];

    // Variable: handDataQueue
    // A queue of <TouchFreeInputActions> that have been received from the Service.
    handDataQueue: Array<any> = [];

    // Variable: responseQueue
    // A queue of <WebSocketResponses> that have been received from the Service.
    responseQueue: Array<WebSocketResponse> = [];

    // Variable: responseCallbacks
    // A dictionary of unique request IDs and <ResponseCallbacks> that represent requests that are awaiting response from the Service.
    responseCallbacks: { [id: string]: ResponseCallback; } = {};

    // Variable: configStateQueue
    // A queue of <ConfigState> that have been received from the Service.
    configStateQueue: Array<ConfigState> = [];

    // Variable: configStateCallbacks
    // A dictionary of unique request IDs and <ConfigStateCallback> that represent requests that are awaiting response from the Service.
    configStateCallbacks: { [id: string]: ConfigStateCallback; } = {};

    // Variable: serviceStatusQueue
    // A queue of <ServiceStatus> that have been received from the Service.
    serviceStatusQueue: Array<ServiceStatus> = [];

    // Variable: serviceStatusCallbacks
    // A dictionary of unique request IDs and <ServiceStatusCallback> that represent requests that are awaiting response from the Service.
    serviceStatusCallbacks: { [id: string]: ServiceStatusCallback; } = {};

    lastStateUpdate: HandPresenceState;

    // Variable: callbackClearInterval
    // Stores the reference number for the interal running <ClearUnresponsiveCallbacks>, allowing
    // it to be cleared.
    private callbackClearInterval: number;

    // Variable: updateInterval
    // Stores the reference number for the interval running <Update>, allowing it to be cleared.
    private updateInterval: number;

    // Used to ensure UP events are sent at the correct position relative to the previous
    // MOVE event.
    // This is required due to the culling of events from the actionQueue in CheckForAction.
    lastKnownCursorPosition: Array<number> = [0, 0];

    // Group: Functions

    // Function: constructor
    // Starts the two regular intervals managed for this (running <ClearUnresponsiveCallbacks> on an
    // interval of <callbackClearTimer> and <Update> on an interval of updateDuration
    constructor() {
        this.lastStateUpdate = HandPresenceState.PROCESSED;
        this.updateDuration = (1 / this.updateRate) * 1000;

        this.callbackClearInterval = setInterval(
            this.ClearUnresponsivePromises as TimerHandler,
            this.callbackClearTimer);

        this.updateInterval = setInterval(
            this.Update.bind(this) as TimerHandler,
            this.updateDuration);
    }

    // Function: Update
    // Update function. Checks all queues for messages to handle. Run on an interval
    // started during the constructor
    Update(): void {
        this.CheckForResponse();
        this.CheckForConfigState();
        this.CheckForServiceStatus();
        this.CheckForAction();
        this.CheckForHandData();
    }

    // Function: CheckForResponse
    // Used to check the <responseQueue> for a <WebSocketResponse>. Sends it to Sends it to <HandleCallbackList> with
    // the <responseCallbacks> dictionary if there is one.
    CheckForResponse(): void {
        let response: WebSocketResponse | undefined = this.responseQueue.shift();

        if (response !== undefined) {
            const handledResponse = MessageReceiver.HandleCallbackList(response, this.responseCallbacks);

            if (!handledResponse) {
                console.log("Received a WebSocketResponse that did not match a callback." +
                    "This is the content of the response: \n Response ID: " + response.requestID +
                    "\n Status: " + response.status + "\n Message: " + response.message +
                    "\n Original request - " + response.originalRequest);
            } else {
                // This is logged to aid users in debugging
                console.log(response.message);
            }
        }
    }

    // Function: CheckForConfigState
    // Used to check the <configStateQueue> for a <ConfigState>. Sends it to <HandleCallbackList> with
    // the <configStateCallbacks> dictionary if there is one.
    CheckForConfigState(): void {
        let configState: ConfigState | undefined = this.configStateQueue.shift();

        if (configState !== undefined) {
            MessageReceiver.HandleCallbackList(configState, this.configStateCallbacks);
        }
    }

    // Function: HandleCallbackList
    // Checks the dictionary of <callbacks> for a matching request ID. If there is a
    // match, calls the callback action in the matching <TouchFreeRequestCallback>.
    // Returns true if it was able to find a callback, returns false if not
    private static HandleCallbackList<T extends TouchFreeRequest>(callbackResult: T, callbacks: {[id: string]: TouchFreeRequestCallback<T>}) : boolean {
        if (callbacks !== undefined) {
            for (let key in callbacks) {
                if (key === callbackResult.requestID) {
                    callbacks[key].callback(callbackResult);
                    delete callbacks[key];
                    return true;
                }
            };
        }

        return false;
    }

    // Function: CheckForServiceStatus
    // Used to check the <serviceStatusQueue> for a <ServiceStatus>. Sends it to <HandleCallbackList> with
    // the <serviceStatusCallbacks> dictionary if there is one.
    CheckForServiceStatus(): void {
        let serviceStatus: ServiceStatus | undefined = this.serviceStatusQueue.shift();

        if (serviceStatus !== undefined) {
            MessageReceiver.HandleCallbackList(serviceStatus, this.serviceStatusCallbacks);
        }
    }

    // Function: CheckForAction
    // Checks <actionQueue> for valid <TouchFreeInputActions>. If there are too many in the queue,
    // clears out non-essential <TouchFreeInputActions> down to the number specified by
    // <actionCullToCount>. If any remain, sends the oldest <TouchFreeInputAction> to
    // <InputActionManager> to handle the action.
    // UP <InputType>s have their positions set to the last known position to ensure
    // input events trigger correctly.
    CheckForAction(): void {
        while (this.actionQueue.length > this.actionCullToCount) {
            if (this.actionQueue[0] !== undefined) {
                // Stop shrinking the queue if we have a 'key' input event
                if (this.actionQueue[0].InteractionFlags & BitmaskFlags.MOVE ||
                    this.actionQueue[0].InteractionFlags & BitmaskFlags.NONE) {
                    // We want to ignore non-move results
                    this.actionQueue.shift();
                } else {
                    break;
                }
            }
        }

        let action: WebsocketInputAction | undefined = this.actionQueue.shift();

        if (action !== undefined) {

            // Parse newly received messages & distribute them
            let converted: TouchFreeInputAction = ConvertInputAction(action);

            //Cache or use the lastKnownCursorPosition. Copy the array to ensure it is not a reference
            if (converted.InputType !== InputType.UP) {
                this.lastKnownCursorPosition = Array.from(converted.CursorPosition);
            }
            else {
                converted.CursorPosition = Array.from(this.lastKnownCursorPosition);
            }

            // Wrapping the function in a timeout of 0 seconds allows the dispatch to be asynchronous
            setTimeout(() => {
                InputActionManager.HandleInputAction(converted);
            });
        }

        if (this.lastStateUpdate !== HandPresenceState.PROCESSED) {
            ConnectionManager.HandleHandPresenceEvent(this.lastStateUpdate);
            this.lastStateUpdate = HandPresenceState.PROCESSED;
        }
    }
    
    // Function: CheckForAction
    // Checks <actionQueue> for valid <TouchFreeInputActions>. If there are too many in the queue,
    // clears out non-essential <TouchFreeInputActions> down to the number specified by
    // <actionCullToCount>. If any remain, sends the oldest <TouchFreeInputAction> to
    // <InputActionManager> to handle the action.
    // UP <InputType>s have their positions set to the last known position to ensure
    // input events trigger correctly.
    CheckForHandData(): void {
        while (this.handDataQueue.length > this.actionCullToCount) {
            if (this.handDataQueue[0] !== undefined) {
                this.handDataQueue.shift();
            }
        }

        let action: any | undefined = this.handDataQueue.shift();

        if (action !== undefined) {
            // Wrapping the function in a timeout of 0 seconds allows the dispatch to be asynchronous
            setTimeout(() => {
                HandDataManager.HandleInputAction(action);
            });
        }
    }

    // Function: ClearUnresponsiveCallbacks
    // Waits for <callbackClearTimer> seconds and clears all <ResponseCallbacks> that are
    // expired from <responseCallbacks>.
    ClearUnresponsivePromises(): void {
        let lastClearTime: number = Date.now();

        MessageReceiver.ClearUnresponsiveItems(lastClearTime, this.responseCallbacks);
        MessageReceiver.ClearUnresponsiveItems(lastClearTime, this.configStateCallbacks);
        MessageReceiver.ClearUnresponsiveItems(lastClearTime, this.serviceStatusCallbacks);
    }

    private static ClearUnresponsiveItems<T>(lastClearTime: number, callbacks: {[id: string]: TouchFreeRequestCallback<T>}) {
        if (callbacks !== undefined) {
            for (let key in callbacks) {
                if (callbacks[key].timestamp < lastClearTime) {
                    delete callbacks[key];
                } else {
                    break;
                }
            };
        }
    }
}