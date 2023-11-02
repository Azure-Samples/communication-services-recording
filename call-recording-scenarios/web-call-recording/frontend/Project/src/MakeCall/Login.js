import React from "react";
import {
    TextField, PrimaryButton, Checkbox,
    MessageBar, MessageBarType
} from 'office-ui-fabric-react'
import { Features } from "@azure/communication-calling";
import { utils } from "../Utils/Utils";
import { v4 as uuid } from 'uuid';
import OneSignal from "react-onesignal";

export default class Login extends React.Component {
    constructor(props) {
        super(props);
        this.callAgent = undefined;
        this.callClient = undefined;
        this.userDetailsResponse = undefined;
        this.displayName = undefined;
        this.isSafari = /^((?!chrome|android).)*safari/i.test(navigator.userAgent);
        this._callAgentInitPromise = undefined;
        this._callAgentInitPromiseResolve = undefined;
        this.state = {
            isCallClientActiveInAnotherTab: false,
            initializeCallAgent: true,
            environmentInfo: undefined,
            showSpinner: false,
            loginWarningMessage: undefined,
            loginErrorMessage: undefined
        }
    }

    async setupLoginStates() {
        this.setState({
            token: this.userDetailsResponse.communicationUserToken.token
        });
        this.setState({
            communicationUserId: utils.getIdentifierText(this.userDetailsResponse.userId)
        });
            await this.props.onLoggedIn({ 
                communicationUserId: this.userDetailsResponse.userId.communicationUserId,
                token: this.userDetailsResponse.communicationUserToken.token,
                displayName: this.displayName
            });
        
        console.log('Login response: ', this.userDetailsResponse);
        this.setState({ loggedIn: true });
    }

    async logIn() {
        try {
            this.setState({ showSpinner: true });
            if (!this.state.token && !this.state.communicationUserId) {
                this.userDetailsResponse = await utils.getCommunicationUserToken();
            } else if (this.state.token && this.state.communicationUserId) {
                this.userDetailsResponse = await utils.getOneSignalRegistrationTokenForCommunicationUserToken(
                    this.state.token, this.state.communicationUserId
                );
            } else if (!this.state.token && this.state.communicationUserId) {
                this.userDetailsResponse = await utils.getCommunicationUserToken(this.state.communicationUserId);
            }else if (this.state.token && !this.state.communicationUserId) {
                throw new Error('You must specify the associated ACS identity for the provided ACS communication user token');
            }           
            await this.setupLoginStates()
        } catch (error) {
            this.setState({
                loginErrorMessage: error.message
            });
            console.log(error);
        } finally {
            this.setState({ showSpinner: false });
        }
    }

    setCallAgent(callAgent) {
        this.callAgent = callAgent;
        this.callAgent.on('connectionStateChanged', (args) => {
            console.log('Call agent connection state changed from', args.oldValue, 'to', args.newValue);
            this.setState({callAgentConnectionState: args.newValue});
            if(args.reason === 'tokenExpired') {
                this.setState({ loggedIn: false });
                alert('Your token has expired. Please log in again.');
            }
        });
        this.setState({callAgentConnectionState: this.callAgent.connectionState});

        if (!!this._callAgentInitPromiseResolve) {
            this._callAgentInitPromiseResolve();
        }
    }

    async setCallClient(callClient) {
        this.callClient = callClient;
        const environmentInfo = await this.callClient.getEnvironmentInfoInternal();
        this.setState({ environmentInfo });
        const debugInfoFeature = await this.callClient.feature(Features.DebugInfo);
        this.setState({ isCallClientActiveInAnotherTab: debugInfoFeature.isCallClientActiveInAnotherTab });
        debugInfoFeature.on('isCallClientActiveInAnotherTabChanged', () => {
            this.setState({ isCallClientActiveInAnotherTab: debugInfoFeature.isCallClientActiveInAnotherTab });
        });
    }

    render() {
        return (
                    <div className="card">
                        <div className="ms-Grid">
                            <div className="ms-Grid-row">
                                <h2 className="ms-Grid-col ms-lg6 ms-sm6 mb-4">User Identity Provisioning and Calling SDK Initialization</h2>
                            </div>
                            <div className="ms-Grid-row">
                            {
                                this.state.loginWarningMessage &&
                                <MessageBar
                                    className="mb-2"
                                    messageBarType={MessageBarType.warning}
                                    isMultiline={true}
                                    onDismiss={() => { this.setState({ loginWarningMessage: undefined })}}
                                    dismissButtonAriaLabel="Close">
                                    <b>{this.state.loginWarningMessage}</b>
                                </MessageBar>
                            }
                            </div>
                            <div className="ms-Grid-row">
                            {
                                this.state.loginErrorMessage &&
                                <MessageBar
                                    className="mb-2"
                                    messageBarType={MessageBarType.error}
                                    isMultiline={true}
                                    onDismiss={() => { this.setState({ loginErrorMessage: undefined })}}
                                    dismissButtonAriaLabel="Close">
                                    <b>{this.state.loginErrorMessage}</b>
                                </MessageBar>
                            }
                            </div>
                            
                            {
                                this.state.showSpinner &&
                                <div className="justify-content-left mt-4">
                                    <div className="loader inline-block"> </div>
                                    <div className="ml-2 inline-block">Initializing SDK...</div>
                                </div>
                            }
                            
                            {
                                this.state.loggedIn &&
                                    <div>
                                        <br></br>
                                        <div>Congrats! You've provisioned an ACS user identity and initialized the ACS Calling Client Web SDK. You are ready to start making calls!</div>
                                        <div>The Identity you've provisioned is: <span className="identity fontweight-700">{this.state.communicationUserId}</span></div>
                                        <div>Connection status: <span className="identity fontweight-700">{this.state.callAgentConnectionState}</span></div>
                                    </div>  
                            }
                            {
                                !this.state.showSpinner && !this.state.loggedIn &&
                                <div>
                                    <div className="ms-Grid-row">
                                        <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12 ms-xl6 ms-xxl6">
                                            <div className="login-pannel">
                                                <div className="ms-Grid-row">
                                                    <div className="ms-Grid-col">
                                                        <h2>ACS User Identity</h2>
                                                    </div>
                                                </div>
                                                <div className="ms-Grid-row">
                                                    <div className="ms-Grid-col">
                                                        <div>The ACS Identity SDK can be used to create a user access token which authenticates the calling clients. </div>
                                                        <div>The example code shows how to use the ACS Identity SDK from a backend service. A walkthrough of integrating the ACS Identity SDK can be found on <a className="sdk-docs-link" target="_blank" href="https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/access-tokens?pivots=programming-language-javascript">Microsoft Docs</a></div>
                                                    </div>
                                                </div>
                                                <div className="ms-Grid-row">
                                                    <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg9 ms-xl9 ms-xxl9">
                                                        <TextField
                                                                defaultValue={undefined}
                                                                placeholder="Display Name"
                                                                label="Optional - Display name"
                                                                onChange={(e) => { this.displayName = e.target.value }}/>
                                                        <TextField
                                                                defaultValue={this.clientTag}
                                                                label="Optinal - Usage tag for
                                                                this session"
                                                                onChange={(e) => { this.clientTag = e.target.value }}/>
                                                        <TextField
                                                            placeholder="JWT Token"
                                                            label="Optional - ACS token. If no token is entered, then a random one will be generated"
                                                            onChange={(e) => { this.state.token = e.target.value }}/>
                                                        <TextField
                                                                placeholder="8:acs:<ACS Resource ID>_<guid>"
                                                                label="Optional - ACS Identity"
                                                                onChange={(e) => { this.state.communicationUserId = e.target.value }}/>
                                                    </div>
                                                </div>
                                                <div className="ms-Grid-row">
                                                    <div className="ms-Grid-col">
                                                        <PrimaryButton className="primary-button mt-3"
                                                            iconProps={{iconName: 'ReleaseGate', style: {verticalAlign: 'middle', fontSize: 'large'}}}
                                                            label="Provision an user" 
                                                            onClick={() => this.logIn()}>
                                                                Login ACS user and initialize SDK
                                                        </PrimaryButton>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            }
                            {
                                this.state.loggedIn &&
                                <div>
                                    <div className="ms-Grid-row mt-4">
                                        <h3 className="ms-Grid-col ms-sm12 ms-md12 ms-lg12">Environment information</h3>
                                    </div>
                                    <div className="ms-Grid-row ml-1">
                                        <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg3">
                                            <h4>Current environment details</h4>
                                            <div>{`Operating system:   ${this.state.environmentInfo?.environment?.platform}.`}</div>
                                            <div>{`Browser:  ${this.state.environmentInfo?.environment?.browser}.`}</div>
                                            <div>{`Browser's version:  ${this.state.environmentInfo?.environment?.browserVersion}.`}</div>
                                            <div>{`Is the application loaded in many tabs:  ${this.state.isCallClientActiveInAnotherTab}.`}</div>
                                        </div>
                                        <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg9">
                                            <h4>Environment support verification</h4>
                                            <div>{`Operating system supported:  ${this.state.environmentInfo?.isSupportedPlatform}.`}</div>
                                            <div>{`Browser supported:  ${this.state.environmentInfo?.isSupportedBrowser}.`}</div>
                                            <div>{`Browser's version supported:  ${this.state.environmentInfo?.isSupportedBrowserVersion}.`}</div>
                                            <div>{`Current environment supported:  ${this.state.environmentInfo?.isSupportedEnvironment}.`}</div>
                                        </div>
                                    </div>
                                </div>
                            }
                        </div>
                    </div>
        );
    }
}
