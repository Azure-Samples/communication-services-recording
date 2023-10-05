import { useEffect, useState } from "react";
import { CallAgent, CallClient } from "@azure/communication-calling";
import { getUserDetails } from "../../utils/UserDetails";
import { AzureCommunicationTokenCredential } from "@azure/communication-common";
import '../../styles/MakeCall.css'

export interface LoginProps {
    onLoggedIn: (userId: string, token: string, displayName: string, callClient: CallClient, callAgent: CallAgent) => void;
}
export function Login(props: LoginProps) {
    const [userId, setUserId] = useState<string>("");
    const [token, setToken] = useState<string>("");
    const [displayUser, setDisplayUser] = useState<string>("");
    const [isLoggedIn, setIsLoggedIn] = useState(false);
    const [callClient, setCallClient] = useState<CallClient>();
    const [callAgent, setcallAgent] = useState<CallAgent>();

    useEffect(() => {
        getUserDetails()
            .then((userData) => {
                setUserId(userData.userId);
                setToken(userData.token);
            })
            .catch((error) => {
                console.error('Error fetching data:', error);
            });
    }, []);
    async function logIn() {
        try {

            const client = new CallClient();
            setCallClient(client);
            const tokenCredential = new AzureCommunicationTokenCredential(token);
            const callAgent = await client.createCallAgent(tokenCredential, { displayName: displayUser });
            setcallAgent(callAgent);
            props.onLoggedIn(userId, token, displayUser, client, callAgent);
            setIsLoggedIn(true);
            
        } catch (e){
            
        }
    }

    return (
        <div>
            {!isLoggedIn && <button className="btn" onClick={() => logIn()}>Login Acs user and initialize SDK </button>}
        </div>
    );
}