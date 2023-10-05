import { IncomingCall } from "@azure/communication-calling";
import '../../styles/MakeCall.css'
export interface IncomingCallProps{
    incomingCall: IncomingCall;
    onReject: () => void;
}
export function IncomingCallCard(props: IncomingCallProps) {
    return (
        <div className="button-group">
            <button className="btn" onClick={() => props.incomingCall.accept()}>Accept</button>
            <button className="btn" onClick={() => { props.incomingCall.reject(); props.onReject(); }}>Reject</button>
        </div>
    )
}
