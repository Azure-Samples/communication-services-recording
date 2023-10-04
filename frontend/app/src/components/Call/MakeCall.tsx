import { useEffect, useState } from "react";
import { getUserDetails } from "../../utils/UserDetails";
import { CallClient, DeviceManager } from "@azure/communication-calling";
import'../../styles/MakeCall.css'
import { AzureCommunicationTokenCredential, createIdentifierFromRawId } from "@azure/communication-common";
export function MakeCall() {
    const [userId, setUserId] = useState<string>("");
    const [token, setToken] = useState<string>("");
    const [displayUser, setDisplayUser] = useState<string>("");
    const [isLoggedIn, setIsLoggedIn] = useState(false);
    const [callClient, setCallClient] = useState<CallClient>();
    const [identityToCall, setIdentityToCall] = useState<string>('');
    let deviceManager: DeviceManager;

    useEffect(() => {
        
    }, []);

    
    async function placeCall(withVideo: boolean) {
        debugger;
        if (token && callClient) {
            const tokenCredential = new AzureCommunicationTokenCredential(token);
            const callAgent = await callClient.createCallAgent(tokenCredential, { displayName: displayUser });
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
    async function handleLogin() {
        getUserDetails()
            .then((userData) => {
                setUserId(userData.userId);
                setToken(userData.token);
                setIsLoggedIn(true);
            })
            .catch((error) => {
                console.error('Error fetching data:', error);
            });
        
        let client = new CallClient();
        debugger;
        setCallClient(client);
        setDisplayUser("acsUser")
    }

    async function getCallOptions(options: { video: boolean; micMuted: boolean }) {
        debugger;
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
            {!isLoggedIn && <button onClick={handleLogin}>Login Acs user</button>}
            {isLoggedIn && <p>The Identity you've provisioned is: {userId}</p>}
            <div>
                <div className="container">
                    <p>Place a call</p>
                    <p >Enter an identity to make a call to.</p>
                    <input className="txt" disabled value={identityToCall} onChange={handleInputChange }  />
                    <div className="button-group">
                        <button className="btn" disabled onClick={() => placeCall(false)}>Place call</button>
                        <button className="btn" disabled onClick={() => placeCall(true)}>Place call with video</button>
                    </div>
                </div>
            </div>
        </div>
    )
}