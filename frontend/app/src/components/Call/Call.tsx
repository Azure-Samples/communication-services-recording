import { PlaceCallData, placeCall } from "../../utils/CallDetails";
import '../../styles/Call.css'
export function Call() {
    async function handleCall() {
        const callData: PlaceCallData = {
            callerId: "",
            targetId: ""
        };
        const response = await placeCall(callData);
    }
    return (
        <div>

        </div>
    );
}