import React from "react";
import { CallClient, LocalVideoStream, Features, CallAgentKind, VideoStreamRenderer } from '@azure/communication-calling';
import { AzureCommunicationTokenCredential, createIdentifierFromRawId} from '@azure/communication-common';
import {
    PrimaryButton,
    TextField,
    MessageBar,
    MessageBarType
} from 'office-ui-fabric-react'
import { Icon } from '@fluentui/react/lib/Icon';
import IncomingCallCard from './IncomingCallCard';
import CallCard from '../MakeCall/CallCard';
import Login from './Login';
import MediaConstraint from './MediaConstraint';
import RecordConstraint from './RecordConstraint';
import { setLogLevel, AzureLogger } from '@azure/logger';
import { inflate } from 'pako';
export default class MakeCall extends React.Component {
    constructor(props) {
        super(props);
        this.callClient = null;
        this.callAgent = null;
        this.deviceManager = null;
        this.destinationUserIds = null;
        this.destinationGroup = null;
        this.messageId = null;
        this.callError = null;
        this.logBuffer = [];
        this.videoConstraints = null;
        this.tokenCredential = null;
        this.recordConstraints = null;
        this.logInComponentRef = React.createRef();

        this.state = {
            id: undefined,
            loggedIn: false,
            isCallClientActiveInAnotherTab: false,
            call: undefined,
            incomingCall: undefined,
            showPreCallDiagnostcisResults: false,
            isPreCallDiagnosticsCallInProgress: false,
            selectedCameraDeviceId: null,
            selectedSpeakerDeviceId: null,
            selectedMicrophoneDeviceId: null,
            deviceManagerWarning: null,
            callError: null,
            ufdMessages: [],
            permissions: {
                audio: null,
                video: null
            },
            preCallDiagnosticsResults: {},
            identityMri: undefined,
            recordCallConstraint: null,
            isRecord: true
        };

        setInterval(() => {
            if (this.state.ufdMessages.length > 0) {
                this.setState({ ufdMessages: this.state.ufdMessages.slice(1) });
            }
        }, 10000);
    }

    handleMediaConstraint = (constraints) => {
        if (constraints.video) {
            this.videoConstraints = constraints.video;
        }
    }

    handleRecordConstraint = (constraints) => {
        if (constraints) {
            this.setState({recordCallConstraint : constraints})
        }
    }

    handleCheckboxChange = () => {
        this.setState((prevState) => ({
            isRecord: !prevState.isRecord,
        }));
    };

    handleLogIn = async (userDetails) => {
        if (userDetails) {
            try {
                const tokenCredential = new AzureCommunicationTokenCredential(userDetails.token);
                this.tokenCredential = tokenCredential;
                setLogLevel('verbose');                
                this.callClient = new CallClient({
                    diagnostics: {
                        appName: 'azure-communication-services',
                        appVersion: '1.3.1-beta.1',
                        tags: ["javascript_calling_sdk",
                        `#clientTag:${userDetails.clientTag}`]
                    }
                });

                this.deviceManager = await this.callClient.getDeviceManager();
                const permissions = await this.deviceManager.askDevicePermission({ audio: true, video: true });
                this.setState({permissions: permissions});

                this.setState({ isTeamsUser: userDetails.isTeamsUser});
                this.setState({ identityMri: createIdentifierFromRawId(userDetails.communicationUserId)})
                this.callAgent =  this.state.isTeamsUser ?
                    await this.callClient.createTeamsCallAgent(tokenCredential) :
                    await this.callClient.createCallAgent(tokenCredential, { displayName: userDetails.displayName });

                window.callAgent = this.callAgent;
                window.videoStreamRenderer = VideoStreamRenderer;
                this.callAgent.on('callsUpdated', e => {
                    console.log(`callsUpdated, added=${e.added}, removed=${e.removed}`);

                    e.added.forEach(call => {
                        this.setState({ call: call });

                        const diagnosticChangedListener = (diagnosticInfo) => {
                            const rmsg = `UFD Diagnostic changed:
                            Diagnostic: ${diagnosticInfo.diagnostic}
                            Value: ${diagnosticInfo.value}
                            Value type: ${diagnosticInfo.valueType}`;
                            if (this.state.ufdMessages.length > 0) {
                                this.setState({ ufdMessages: [...this.state.ufdMessages, rmsg] });
                            } else {
                                this.setState({ ufdMessages: [rmsg] });
                            }
                        };

                        call.feature(Features.UserFacingDiagnostics).media.on('diagnosticChanged', diagnosticChangedListener);
                        call.feature(Features.UserFacingDiagnostics).network.on('diagnosticChanged', diagnosticChangedListener);
                    });

                    e.removed.forEach(call => {
                        if (this.state.call && this.state.call === call) {
                            this.displayCallEndReason(this.state.call.callEndReason);
                        }
                    });
                });
                this.callAgent.on('incomingCall', args => {
                    const incomingCall = args.incomingCall;
                    if (this.state.call) {
                        incomingCall.reject();
                        return;
                    }

                    this.setState({ incomingCall: incomingCall });

                    incomingCall.on('callEnded', args => {
                        this.displayCallEndReason(args.callEndReason);
                    });

                });
                this.setState({ loggedIn: true });
                this.logInComponentRef.current.setCallAgent(this.callAgent);
                this.logInComponentRef.current.setCallClient(this.callClient);
            } catch (e) {
                console.error(e);
            }
        }
    }

    displayCallEndReason = (callEndReason) => {
        if (callEndReason.code !== 0 || callEndReason.subCode !== 0) {
            this.setState({ callError: `Call end reason: code: ${callEndReason.code}, subcode: ${callEndReason.subCode}` });
        }
        
        this.setState({ call: null, incomingCall: null });
    }

    placeCall = async (withVideo) => {
        if (withVideo) {
            const videoRecordConstraints = { recordingContent: "video", recordingChannel: "mixed", recordingFormat: "mp4" };
            this.setState({ recordCallConstraint: videoRecordConstraints })
        }
        try {
            let identitiesToCall = [];
            const userIdsArray = this.destinationUserIds.value.split(',');

            userIdsArray.forEach((userId, index) => {
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

            const callOptions = await this.getCallOptions({video: withVideo, micMuted: false});
            this.callAgent.startCall(identitiesToCall, callOptions);

        } catch (e) {
            console.error('Failed to place a call', e);
            this.setState({ callError: 'Failed to place a call: ' + e });
        }
    };

    joinGroup = async (withVideo) => {
        try {
            const callOptions = await this.getCallOptions({video: withVideo, micMuted: false});
            this.callAgent.join({ groupId: this.destinationGroup.value }, callOptions);
        } catch (e) {
            console.error('Failed to join a call', e);
            this.setState({ callError: 'Failed to join a call: ' + e });
        }
    };
   
    async getCallOptions(options) {
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

        // On iOS, device permissions are lost after a little while, so re-ask for permissions
        const permissions = await this.deviceManager.askDevicePermission({ audio: true, video: true });
        this.setState({permissions: permissions});

        const cameras = await this.deviceManager.getCameras();
        const cameraDevice = cameras[0];
        if (cameraDevice && cameraDevice?.id !== 'camera:') {
            this.setState({
                selectedCameraDeviceId: cameraDevice?.id,
                cameraDeviceOptions: cameras.map(camera => { return { key: camera.id, text: camera.name } })
            });
        }
        if (!!options.video) {
            try {
                if (!cameraDevice || cameraDevice?.id === 'camera:') {
                    throw new Error('No camera devices found.');
                } else if (cameraDevice) {
                    callOptions.videoOptions = { localVideoStreams: [new LocalVideoStream(cameraDevice)] };
                    if (this.videoConstraints) {
                        callOptions.videoOptions.constraints = this.videoConstraints;
                    }
                }
            } catch (e) {
                cameraWarning = e.message;
            }
        }

        try {
            const speakers = await this.deviceManager.getSpeakers();
            const speakerDevice = speakers[0];
            if (!speakerDevice || speakerDevice.id === 'speaker:') {
                throw new Error('No speaker devices found.');
            } else if (speakerDevice) {
                this.setState({
                    selectedSpeakerDeviceId: speakerDevice.id,
                    speakerDeviceOptions: speakers.map(speaker => { return { key: speaker.id, text: speaker.name } })
                });
                await this.deviceManager.selectSpeaker(speakerDevice);
            }
        } catch (e) {
            speakerWarning = e.message;
        }

        try {
            const microphones = await this.deviceManager.getMicrophones();
            const microphoneDevice = microphones[0];
            if (!microphoneDevice || microphoneDevice.id === 'microphone:') {
                throw new Error('No microphone devices found.');
            } else {
                this.setState({
                    selectedMicrophoneDeviceId: microphoneDevice.id,
                    microphoneDeviceOptions: microphones.map(microphone => { return { key: microphone.id, text: microphone.name } })
                });
                await this.deviceManager.selectMicrophone(microphoneDevice);
            }
        } catch (e) {
            microphoneWarning = e.message;
        }

        if (cameraWarning || speakerWarning || microphoneWarning) {
            this.setState({
                deviceManagerWarning:
                    `${cameraWarning ? cameraWarning + ' ' : ''}
                    ${speakerWarning ? speakerWarning + ' ' : ''}
                    ${microphoneWarning ? microphoneWarning + ' ' : ''}`
            });
        }

        return callOptions;
    }
    async runPreCallDiagnostics() {
        try {
            this.setState({
                showPreCallDiagnostcisResults: false,
                isPreCallDiagnosticsCall: true,
                preCallDiagnosticsResults: {}
            });
            const preCallDiagnosticsResult = await this.callClient.feature(Features.PreCallDiagnostics).startTest(this.tokenCredential);

            const deviceAccess =  await preCallDiagnosticsResult.deviceAccess;
            this.setState({preCallDiagnosticsResults: {...this.state.preCallDiagnosticsResults, deviceAccess}});

            const deviceEnumeration = await preCallDiagnosticsResult.deviceEnumeration;
            this.setState({preCallDiagnosticsResults: {...this.state.preCallDiagnosticsResults, deviceEnumeration}});

            const inCallDiagnostics =  await preCallDiagnosticsResult.inCallDiagnostics;
            this.setState({preCallDiagnosticsResults: {...this.state.preCallDiagnosticsResults, inCallDiagnostics}});

            const browserSupport =  await preCallDiagnosticsResult.browserSupport;
            this.setState({preCallDiagnosticsResults: {...this.state.preCallDiagnosticsResults, browserSupport}});

            this.setState({
                showPreCallDiagnostcisResults: true,
                isPreCallDiagnosticsCall: false
            });

        } catch {
            throw new Error("Can't run Pre Call Diagnostics test. Please try again...");
        }
    }
    render() {
        // TODO: Create section component. Couldnt use the ExampleCard compoenent from uifabric because it is buggy,
        //       when toggling their show/hide code functionality, videos dissapear from DOM.

        return (
            <div>
                <Login onLoggedIn={this.handleLogIn} ref={this.logInComponentRef}/>
                <div className="card">
                    <div className="ms-Grid">
                        <div className="ms-Grid-row">
                            <div className="ms-Grid-col ms-lg6 ms-sm6 mb-4">
                                <h2>Placing and receiving calls</h2>
                                <div>{`Permissions audio: ${this.state.permissions.audio} video: ${this.state.permissions.video}`}</div>
                            </div>
                        </div>
                        <div className="ms-Grid-row">
                            <div className="ms-Grid-col mb-2">Having provisioned an ACS Identity and initialized the SDK from the section above, you are now ready to place calls, join group calls, and receiving calls.</div>
                        </div>
                        {
                            this.state.callError &&
                            <div>
                                <MessageBar
                                    messageBarType={MessageBarType.error}
                                    isMultiline={false}
                                    onDismiss={() => { this.setState({ callError: undefined }) }}
                                    dismissButtonAriaLabel="Close">
                                    <b>{this.state.callError}</b>
                                </MessageBar>

                            </div>
                        }
                        {
                            this.state.deviceManagerWarning &&
                            <MessageBar
                                messageBarType={MessageBarType.warning}
                                isMultiline={false}
                                onDismiss={() => { this.setState({ deviceManagerWarning: undefined }) }}
                                dismissButtonAriaLabel="Close">
                                <b>{this.state.deviceManagerWarning}</b>
                            </MessageBar>
                        }
                        {
                            this.state.ufdMessages.length > 0 &&
                            <MessageBar
                                messageBarType={MessageBarType.warning}
                                isMultiline={true}
                                onDismiss={() => { this.setState({ ufdMessages: [] }) }}
                                dismissButtonAriaLabel="Close">
                                {this.state.ufdMessages.map((msg, index) => <li key={index}>{msg}</li>)}
                            </MessageBar>
                        }
                        {
                            !this.state.incomingCall && !this.state.call &&
                            <div>
                                    <div>
                                        <h2>Record</h2> <input type="checkbox" checked ={this.state.isRecord} onChange={this.handleCheckboxChange} />
                                    </div>
                                
                                <div className="ms-Grid-row mt-3">
                                    <div className="call-input-panel mb-5 ms-Grid-col ms-sm12 ms-md12 ms-lg12 ms-xl6 ms-xxl3">
                                        <div className="ms-Grid-row">
                                            <div className="ms-Grid-col">
                                                <h2 className="mb-0">Place a call</h2>
                                            </div>
                                        </div>
                                        <div className="ms-Grid-row">
                                            <div className="md-Grid-col ml-2 mt-0 ms-sm11 ms-md11 ms-lg9 ms-xl9 ms-xxl11">
                                                <TextField
                                                    className="mt-0"
                                                    disabled={this.state.call || !this.state.loggedIn}
                                                    label={`Enter an Identity to make a call to. You can specify multiple Identities to call by using \",\" separated values."`}
                                                    componentRef={(val) => this.destinationUserIds = val} />
                                            </div>
                                        </div>
                                        <PrimaryButton
                                            className="primary-button"
                                            iconProps={{ iconName: 'Phone', style: { verticalAlign: 'middle', fontSize: 'large' } }}
                                            text="Place call"
                                            disabled={this.state.call || !this.state.loggedIn}
                                            onClick={() => this.placeCall(false)}>
                                        </PrimaryButton>
                                        <PrimaryButton
                                            className="primary-button"
                                            iconProps={{ iconName: 'Video', style: { verticalAlign: 'middle', fontSize: 'large' } }}
                                            text="Place call with video"
                                            disabled={this.state.call || !this.state.loggedIn}
                                            onClick={() => this.placeCall(true)}>
                                        </PrimaryButton>
                                        </div>
                                    <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12 ms-xl1 ms-xxl1">
                                    </div>
                                   
                                    <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12 ms-xl1 ms-xxl1">
                                    </div>
                                    <div className="call-input-panel mb-5 ms-Grid-col ms-sm12 ms-md12 ms-lg12 ms-xl6 ms-xxl3">
                                        <div>
                                            <h2 className="mb-0">Join a group call</h2>
                                            <div className="ms-Grid-row">
                                                <div className="ms-sm11 ms-md11 ms-lg9 ms-xl9 ms-xxl11">
                                                    <TextField
                                                        className="mb-3 mt-0"
                                                        disabled={this.state.call || !this.state.loggedIn}
                                                        label="Group Id"
                                                        placeholder="29228d3e-040e-4656-a70e-890ab4e173e5"
                                                        defaultValue="29228d3e-040e-4656-a70e-890ab4e173e5"
                                                        componentRef={(val) => this.destinationGroup = val} />
                                                </div>
                                            </div>
                                            <PrimaryButton
                                                className="primary-button"
                                                iconProps={{ iconName: 'Group', style: { verticalAlign: 'middle', fontSize: 'large' } }}
                                                text="Join group call"
                                                disabled={this.state.call || !this.state.loggedIn}
                                                onClick={() => this.joinGroup(false)}>
                                            </PrimaryButton>
                                            <PrimaryButton
                                                className="primary-button"
                                                iconProps={{ iconName: 'Video', style: { verticalAlign: 'middle', fontSize: 'large' } }}
                                                text="Join group call with video"
                                                disabled={this.state.call || !this.state.loggedIn}
                                                onClick={() => this.joinGroup(true)}>
                                            </PrimaryButton>
                                        </div>
                                    </div>
                                </div>
                                <div className="ms-Grid-row mt-3">
                                    <div className="call-input-panel mb-5 ms-Grid-col ms-sm12 ms-lg12 ms-xl12 ms-xxl4">
                                        <h3 className="mb-1">Video Send Constraints</h3>
                                        <MediaConstraint
                                            onChange={this.handleMediaConstraint}
                                            disabled={this.state.call || !this.state.loggedIn}
                                        />
                                    </div>
                                    </div>

                                    <div className="ms-Grid-row mt-3">
                                        <div className="call-input-panel mb-5 ms-Grid-col ms-sm12 ms-lg12 ms-xl12 ms-xxl4">
                                            <h3 className="mb-1">Record Constraints</h3>
                                            <RecordConstraint
                                                onChange={this.handleRecordConstraint}
                                                disabled={this.state.call || !this.state.loggedIn}
                                            />
                                        </div>
                                    </div>
                            </div>

                        }
                        {
                            this.state.call && this.state.isPreCallDiagnosticsCallInProgress &&
                            <div>
                                Pre Call Diagnostics call in progress...
                            </div>
                        }
                        {
                            this.state.call && !this.state.isPreCallDiagnosticsCallInProgress &&
                            <CallCard
                                call={this.state.call}
                                deviceManager={this.deviceManager}
                                selectedCameraDeviceId={this.state.selectedCameraDeviceId}
                                cameraDeviceOptions={this.state.cameraDeviceOptions}
                                speakerDeviceOptions={this.state.speakerDeviceOptions}
                                microphoneDeviceOptions={this.state.microphoneDeviceOptions}
                                identityMri={this.state.identityMri}
                                onShowCameraNotFoundWarning={(show) => { this.setState({ showCameraNotFoundWarning: show }) }}
                                onShowSpeakerNotFoundWarning={(show) => { this.setState({ showSpeakerNotFoundWarning: show }) }}
                                onShowMicrophoneNotFoundWarning={(show) => { this.setState({ showMicrophoneNotFoundWarning: show }) }}
                                recordCallConstraint={this.state.recordCallConstraint}
                                isRecord={this.state.isRecord}
                            />
                        }
                        {
                            this.state.incomingCall && !this.state.call &&
                            <IncomingCallCard
                                incomingCall={this.state.incomingCall}
                                acceptCallMicrophoneUnmutedVideoOff={async () => await this.getCallOptions({ video: false, micMuted: false })}
                                acceptCallMicrophoneUnmutedVideoOn={async () => await this.getCallOptions({ video: true, micMuted: false })}
                                acceptCallMicrophoneMutedVideoOn={async () => await this.getCallOptions({ video: true, micMuted: true })}
                                acceptCallMicrophoneMutedVideoOff={async () => await this.getCallOptions({ video: false, micMuted: true })}
                                onReject={() => { this.setState({ incomingCall: undefined }) }} />
                        }
                    </div>
                </div>
                <div className="card">
                    <div className="ms-Grid">
                        <div className="ms-Grid-row">
                            <h2 className="ms-Grid-col ms-lg6 ms-sm6 mb-4">Pre Call Diagnostics</h2>
                            <div className="ms-Grid-col ms-lg6 text-right">
                                <PrimaryButton
                                    className="secondary-button"
                                    iconProps={{ iconName: 'TestPlan', style: { verticalAlign: 'middle', fontSize: 'large' } }}
                                    text={`Run Pre Call Diagnostics`}
                                    disabled={this.state.call || !this.state.loggedIn}
                                    onClick={() => this.runPreCallDiagnostics()}>
                                </PrimaryButton>
                            </div>
                        </div>
                        {
                            this.state.call && this.state.isPreCallDiagnosticsCallInProgress &&
                            <div>
                                Pre Call Diagnostics call in progress...
                                <div className="custom-row">
                                    <div className="ringing-loader mb-4"></div>
                                </div>
                            </div>
                        }
                        {
                            this.state.showPreCallDiagnostcisResults &&
                            <div>
                                {
                                    <div className="pre-call-grid-container">
                                        {
                                            this.state.preCallDiagnosticsResults.deviceAccess &&
                                            <div className="pre-call-grid">
                                                <span>Device Permission: </span>
                                                <div  >
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Audio: </div>
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.deviceAccess.audio.toString()}</div>
                                                </div>
                                                <div >
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Video: </div>
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.deviceAccess.video.toString()}</div>
                                                </div>
                                            </div>
                                        }
                                        {
                                            this.state.preCallDiagnosticsResults.deviceEnumeration &&
                                            <div className="pre-call-grid">
                                                <span>Device Access: </span>
                                                <div >
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Microphone: </div>
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.deviceEnumeration.microphone}</div>
                                                </div>
                                                <div >
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Camera: </div>
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.deviceEnumeration.camera}</div>
                                                </div>
                                                <div >
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Speaker: </div>
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.deviceEnumeration.speaker}</div>
                                                </div>
                                            </div>
                                        }
                                        {
                                            this.state.preCallDiagnosticsResults.browserSupport &&
                                            <div className="pre-call-grid">
                                                <span>Browser Support: </span>
                                                <div >
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">OS: </div>
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.browserSupport.os}</div>
                                                </div>
                                                <div >
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Browser: </div>
                                                    <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.browserSupport.browser}</div>
                                                </div>
                                            </div>
                                        }
                                        {
                                            this.state.preCallDiagnosticsResults.inCallDiagnostics &&
                                            <div className="pre-call-grid">
                                                <span>Call Diagnostics: </span>
                                                <div className="pre-call-grid">
                                                    <div >
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Call Connected: </div>
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.inCallDiagnostics.connected.toString()}</div>
                                                    </div>
                                                    <div >
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">BandWidth: </div>
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.inCallDiagnostics.bandWidth}</div>
                                                    </div>

                                                    <div >
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Audio Jitter: </div>
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.inCallDiagnostics.diagnostics.audio.jitter}</div>
                                                    </div>
                                                    <div >
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Audio PacketLoss: </div>
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.inCallDiagnostics.diagnostics.audio.packetLoss}</div>
                                                    </div>
                                                    <div >
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Audio Rtt: </div>
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.inCallDiagnostics.diagnostics.audio.rtt}</div>
                                                    </div>

                                                    <div >
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Video Jitter: </div>
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.inCallDiagnostics.diagnostics.video.jitter}</div>
                                                    </div>
                                                    <div >
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Video PacketLoss: </div>
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.inCallDiagnostics.diagnostics.video.packetLoss}</div>
                                                    </div>
                                                    <div >
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">Video Rtt: </div>
                                                        <div className="ms-Grid-col ms-u-sm2 pre-call-grid-panel">{this.state.preCallDiagnosticsResults.inCallDiagnostics.diagnostics.video.rtt}</div>
                                                    </div>
                                                </div>
                                            </div>
                                        }
                                    </div>
                                }
                            </div>
                        }
                        {
                            this.state.showPreCallDiagnosticsSampleCode &&
                            <pre>
                                <code style={{ color: '#b3b0ad' }}>
                                    {
                                        preCallDiagnosticsSampleCode
                                    }
                                </code>
                            </pre>
                        }
                    </div>
                </div>
                <div className="card">
                    <div className="ms-Grid">
                        <div className="ms-Grid-row">
                            <h2 className="ms-Grid-col ms-lg6 ms-sm6 mb-4">Video, Screen sharing, and local video preview</h2>
                        </div>
                        
                        <h3>
                            Video - try it out.
                        </h3>
                        <div>
                            From your current call, toggle your video on and off by clicking on the <Icon className="icon-text-xlarge" iconName="Video" /> icon.
                            When you start your video, remote participants can see your video by receiving a stream and rendering it in an HTML element.
                        </div>
                        <br></br>
                        <h3>
                            Screen sharing - try it out.
                        </h3>
                        <div>
                            From your current call, toggle your screen sharing on and off by clicking on the <Icon className="icon-text-xlarge" iconName="TVMonitor" /> icon.
                            When you start sharing your screen, remote participants can see your screen by receiving a stream and rendering it in an HTML element.
                        </div>
                    </div>
                </div>
                <div className="card">
                    <div className="ms-Grid">
                        <div className="ms-Grid-row">
                            <h2 className="ms-Grid-col ms-lg6 ms-sm6 mb-4">Mute / Unmute</h2>
                        </div>
                        <h3>
                            Try it out.
                        </h3>
                        <div>
                            From your current call, toggle your microphone on and off by clicking on the <Icon className="icon-text-xlarge" iconName="Microphone" /> icon.
                            When you mute or unmute your microphone, remote participants can receive an event about wether your micrphone is muted or unmuted.
                        </div>
                    </div>
                </div>
                <div className="card">
                    <div className="ms-Grid">
                        <div className="ms-Grid-row">
                            <h2 className="ms-Grid-col ms-lg6 ms-sm6 mb-4">Hold / Unhold</h2>                           
                        </div>
                        <h3>
                            Try it out.
                        </h3>
                        <div>
                            From your current call, toggle hold call and unhold call on by clicking on the <Icon className="icon-text-xlarge" iconName="Play" /> icon.
                            When you hold or unhold the call, remote participants can receive other participant state changed events. Also, the call state changes.
                        </div>
                    </div>
                </div>
                <div className="card">
                    <div className="ms-Grid">
                        <div className="ms-Grid-row">
                            <h2 className="ms-Grid-col ms-lg6 ms-sm6 mb-4">Device Manager</h2>
                        </div>
                        <h3>
                            Try it out.
                        </h3>
                        <div>
                            From your current call, click on the <Icon className="icon-text-xlarge" iconName="Settings" /> icon to open up the settings panel.
                            The DeviceManager is used to select the devices (camera, microphone, and speakers) to use across the call stack and to preview your camera.
                        </div>
                    </div>
                </div>
            </div>
        );
    }
}
