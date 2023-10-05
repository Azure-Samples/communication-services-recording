import { useEffect, useState } from "react";
import { Call, CallAgent, CallClient, CallEndReason, DeviceManager, IncomingCall } from "@azure/communication-calling";
import'../../styles/MakeCall.css'
import { createIdentifierFromRawId } from "@azure/communication-common";
import { Login } from "../User/Login";
import { IncomingCallCard } from "./IncomingCall";
import { CallCard } from "./Call";
export function MakeCall() {
    const [userId, setUserId] = useState<string>("");
    const [token, setToken] = useState<string>("");
    const [displayUser, setDisplayUser] = useState<string>("");
    const [isLoggedIn, setIsLoggedIn] = useState(false);
    const [callClient, setCallClient] = useState<CallClient>();
    const [callAgent, setCallAgent] = useState<CallAgent>();
    const [identityToCall, setIdentityToCall] = useState<string>('');
    const [incomingCall, setIncomingCall] = useState<IncomingCall>();
    const [call, setCall] = useState<Call>();
    const [serverCallId, setServerCallId] = useState<string>('');
    const [displayCallEndReason, setDisplayCallEndReason] = useState<CallEndReason>();
    let deviceManager: DeviceManager;

    useEffect(() => {
        
    }, []);

    
    async function placeCall(withVideo: boolean) {
        
        if (callAgent) {
            let identitiesToCall: any[] = [];
            const userIdsArray = identityToCall.split(',');

            userIdsArray.forEach((userId: any, index) => {
                if (userId) {
                    userId = userId.trim();
                    if (userId === '8:echo123') {
                        userId = { id: userId };
                    }
                    else {
                        userId = createIdentifierFromRawId(userId);
                    }
                    if (!identitiesToCall.find(id => { return id === userId })) {
                        identitiesToCall.push(userId);
                    }
                }
            });
            const callOptions = await getCallOptions({ video: withVideo, micMuted: false });
            callAgent.startCall(identitiesToCall, callOptions);
        }
    }

    function handleInputChange(event: React.ChangeEvent<HTMLInputElement>) {
        setIdentityToCall(event.target.value)
    }
    async function handleLogin(userId: string, token: string, displayName: string, callClient: CallClient, callAgent: CallAgent) {
        
        setUserId(userId);
        setToken(token);
        setDisplayUser(displayName);
        setCallClient(callClient);
        setCallAgent(callAgent);
        setIsLoggedIn(true);

        callAgent.on('callsUpdated', e => {
            console.log(`callsUpdated, added=${e.added}, removed=${e.removed}`);

            e.added.forEach(call => {
                
                setCall(call);
                setServerCallId(call.id);
                console.log("SERVER CALL ID " + call.id);
            });
            e.removed.forEach(call => {
                if (call) {
                }
            });
        });

        callAgent.on('incomingCall', args => {
            const incomingCall = args.incomingCall;
            setIncomingCall(incomingCall);
            if (call) {
                incomingCall.reject();
                return;
            }

            incomingCall.on('callEnded', args => {
                setDisplayCallEndReason(args.callEndReason);
            });

        });
    }

    async function getCallOptions(options: { video: boolean; micMuted: boolean }) {
        
        if (callClient !== undefined) {
            deviceManager = await callClient.getDeviceManager();
        }
        let callOptions = {
            videoOptions: {
                localVideoStreams: undefined
            },
            audioOptions: {
                muted: !!options.micMuted
            }
        };

        let cameraWarning = undefined;
        let speakerWarning = undefined;
        let microphoneWarning = undefined;

        const permissions = await deviceManager.askDevicePermission({ audio: true, video: true });

        const cameras = await deviceManager.getCameras();
        const cameraDevice = cameras[0];
        if (cameraDevice && cameraDevice?.id !== 'camera:') {
            //this.setState({
            //    selectedCameraDeviceId: cameraDevice?.id,
            //    cameraDeviceOptions: cameras.map(camera => { return { key: camera.id, text: camera.name } })
            //});
        }
        if (!!options.video) {
            try {
                if (!cameraDevice || cameraDevice?.id === 'camera:') {
                    throw new Error('No camera devices found.');
                } else if (cameraDevice) {
                    //callOptions.videoOptions = { localVideoStreams: [new LocalVideoStream(cameraDevice)] };
                    //if (this.videoConstraints) {
                    //    callOptions.videoOptions.constraints = this.videoConstraints;
                    //}
                }
            } catch (e) {
                /*cameraWarning = e.message;*/
            }
        }

        try {
            const speakers = await deviceManager.getSpeakers();
            const speakerDevice = speakers[0];
            if (!speakerDevice || speakerDevice.id === 'speaker:') {
                throw new Error('No speaker devices found.');
            } else if (speakerDevice) {
                //this.setState({
                //    selectedSpeakerDeviceId: speakerDevice.id,
                //    speakerDeviceOptions: speakers.map(speaker => { return { key: speaker.id, text: speaker.name } })
                //});
                await deviceManager.selectSpeaker(speakerDevice);
            }
        } catch (e) {
            /*speakerWarning = e.message;*/
        }

        try {
            const microphones = await deviceManager.getMicrophones();
            const microphoneDevice = microphones[0];
            if (!microphoneDevice || microphoneDevice.id === 'microphone:') {
                throw new Error('No microphone devices found.');
            } else {
                //this.setState({
                //    selectedMicrophoneDeviceId: microphoneDevice.id,
                //    microphoneDeviceOptions: microphones.map(microphone => { return { key: microphone.id, text: microphone.name } })
                //});
                await deviceManager.selectMicrophone(microphoneDevice);
            }
        } catch (e) {
            /*microphoneWarning = e.message;*/
        }

        //if (cameraWarning || speakerWarning || microphoneWarning) {
        //    this.setState({
        //        deviceManagerWarning:
        //            `${cameraWarning ? cameraWarning + ' ' : ''}
        //            ${speakerWarning ? speakerWarning + ' ' : ''}
        //            ${microphoneWarning ? microphoneWarning + ' ' : ''}`
        //    });
        //}

        return callOptions;
    }


    return (
        <div>
            <h3>User Identity Provisioning and Calling SDK Initialization</h3>
            {!isLoggedIn && <Login onLoggedIn={handleLogin} />}
            {
                isLoggedIn &&
                <div>
                    <br></br>
                        <div className="login-info">Congrats! You've provisioned an ACS user identity and initialized the ACS Calling Client Web SDK. You are ready to start making calls!</div>
                        <div className="login-info">The Identity you've provisioned is: <span className="identity">{userId}</span></div>
                </div>  
            }
            <hr></hr>
            <div>{!incomingCall && !call &&
                <div className="container">
                    <p className="info">Place a call</p>
                    <p className="text-info">Enter an identity to make a call to.</p>
                    <input className="txt" disabled={!isLoggedIn} value={identityToCall} onChange={handleInputChange} />
                    <div className="button-group">
                        <button className="btn" disabled={!isLoggedIn} onClick={() => placeCall(false)}>Place call</button>
                        {/*<button className="btn" disabled onClick={() => placeCall(true)}>Place call with video</button>*/}
                    </div>
                </div>
            }
                {call && <CallCard call={call} />}
                {incomingCall && !call && <IncomingCallCard incomingCall={incomingCall} onReject={() => { setIncomingCall(undefined) }} />}
            </div>
        </div>
    )
}