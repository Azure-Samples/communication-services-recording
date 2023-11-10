# Call Recording Scenarios Testing Tool

Call recording scenarios tool help to debug/demonstrate how to use the call recording feature from the call automation sdk. We have currently used the call recording in two different scenarios to demonstrate,

1. [incmoing-call-recording](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/incoming-call-recording/README.md) - Receives the incoming call event, accepts/answers the incoming call, starts the recording, and plays text to the user and allows the user to record the message. Then, it stops recording when the user disconnects the call. Once the recorded file is available for downloading, it will be downloaded to the project location.
   * Supported Features
   * Pre-requisites
   * Project Setup
   * ACS Events Registration
   * Run application locally
   
3. [web-call-recording](https://github.com/Azure-Samples/communication-services-recording/blob/main/call-recording-scenarios/web-call-recording/README.md) - This tool will help to test the server-side recording with the calling SDK UI. It also supports the audio and video recording for 1:1, 1:N, and group calling. It has options for both manual and auto record testing.
   * Supported Features
   * Pre-requisites
   * Project Setup
   * ACS Events Registration
   * Run application locally

## Resources

* Used webcalling sample as base for the UI https://github.com/Azure-Samples/communication-services-web-calling-tutorial/blob/main/README.md
* Server side recording we have reffered this documentation https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/voice-video-calling/get-started-call-recording?pivots=programming-language-csharp
