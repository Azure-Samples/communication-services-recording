# Call Recording Scenarios Testing Tool

Call recording scenarios tool help to debug/demonstrate how to use the call recording feature from the call automation sdk. We have currently used the call recording in two different scenarios to demonstrate,

1. [incmoing-call-recording](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/incoming-call-recording/README.md) - Receives the incoming call event, and answer the incoming call, starts the recording, and play text to user and allow user to record the message and then it stops the recording until user disconnect the call. Once the recorded file is available for downloading it will be downloaded to project location.
   * [Supported Features](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/incoming-call-recording/README.md#features)
   * [Pre-requisites](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/incoming-call-recording/README.md#prerequisites)
   * [Project Setup](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/incoming-call-recording/README.md#setup-instructions)
   * [Run application locally](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/incoming-call-recording/README.md#running-the-application)
   
3. [web-call-recording](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/web-call-recording/README.md) - this tool will help to test the server side recording with the calling SDK UI. It does support audio and video recording for 1:1, 1:N and group calling. It has option for both manual and auto record testing
   * [Supported Features](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/web-call-recording/README.md#features)
   * [Pre-requisites](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/web-call-recording/README.md#prerequisites)
   * [Project Setup](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/web-call-recording/README.md#setup-instructions)
   * [Run application locally](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/web-call-recording/README.md#setup-instructions)

## Resources

* Used webcalling sample as base for the UI https://github.com/Azure-Samples/communication-services-web-calling-tutorial/blob/main/README.md
* Server side recording we have reffered this documentation https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/voice-video-calling/get-started-call-recording?pivots=programming-language-csharp
