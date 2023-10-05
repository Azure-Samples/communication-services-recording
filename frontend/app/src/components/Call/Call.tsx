import { PlaceCallData, placeCall } from "../../utils/CallDetails";
import '../../styles/Call.css'
import { Call } from "@azure/communication-calling";
import { useState } from "react";

export interface CallCardProps {
    call: Call;
}
export function CallCard(props: CallCardProps) {
    //async function handleCall() {
    //    const callData: PlaceCallData = {
    //        callerId: "",
    //        targetId: ""
    //    };
    //    const response = await placeCall(callData);
    //}
    return (
        <div>
            <br></br>
            {/*<h2 >{props.call.state !== 'Connected' ? `${props.call.state}...` : `Connected`}</h2>*/}
            <button className="btn" onClick={() => { props.call.hangUp() }}>Hang Up</button>
        </div>
    );
}