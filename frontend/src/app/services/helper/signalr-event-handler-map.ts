import { SignalREventType } from "../../models/signalr-event-type.model";
import { PayloadFor } from "./signalr-event-to-payload";

export type SignalREventHandlerMap = {
    [K in SignalREventType]?: (payload: PayloadFor<K>) => void;
};
