# Call recording scenarios 

Call recording scenarios tool help to debug/demonstrate how to use the call recording feature from the call automation sdk. We have currently used the call recording in two different scenarios to demonstrate,

1. incmoing-call-recording - When acs receives the incoming call event, will answers the incoming call and starts the recording, and stops the recording until user disconnect the call. And downloads the recording in project location whenever the recording is available
2. web-call-recording - this tool will help to test the server side recording with the calling SDK UI. It does support audio and video recording for 1:1, 1:N and group calling. It has option for both manual and auto record testing

Please follow respective projects readme.md files to know on prerequists and set up instructions. 

## Resources

* Used webcalling sample as base for the UI https://github.com/Azure-Samples/communication-services-web-calling-tutorial/blob/main/README.md
* Server side recording we have reffered this documentation https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/voice-video-calling/get-started-call-recording?pivots=programming-language-csharp
