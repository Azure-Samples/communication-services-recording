# ACS Call Recording Test Tool

Test tool will help to test the recording feature from call automation sdk, with the calling SDK UI. It does support audio and video recording for 1:1, 1:N and group calling.
It has option for both manual and auto record testing

## Features

This project framework provides the following features:

* UI supports both audio and video call for 1:1, 1:N and group call
* You can choose the different recording contraints for your call record
* It does auto record, when we place call with auto record check option, it starts the recording, play some sound for 10 secs, stop the play and disconnect the call.
* Once its disconnected it shows the downloaded recording file path to your local (currently it just download to project path)
* When we uncheck the auto record, then it provide start recording option during the call with the recording contraints
* Provided the api endpoints for the testing the rest api directly with the api version
  ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/3cc34b48-b371-48b9-8e88-a187256fc0ef)

* Api endpoint what call automation version used for the recording
  ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/db2a4afc-cea4-4a8e-905b-2ba7da7e4ea2)


## Getting Started

### Prerequisites

* An Azure account with an active subscription. For details, see [Create an account for free](https://aka.ms/Mech-Azureaccount) 
* Communication service
* NPM
* Node js
* For local run: Install Azure Dev Tunnels CLI. For details, see [Create and host dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)
* [.NET 7](https://dotnet.microsoft.com/download)

### Installation

**Frontend**
1. Get your Azure Communication Services resource connection string from the Azure portal, and put it as the value for connectionString in serverConfig.json file.
2. From the terminal/command prompt, Run:
   * npm install
   * npm run build-local
   * npm run start-local
3. Open provided localhost url from your terminal in the browser

**Backend**
1. Create the devtunnel 
2. Build the project from visual studio and run the project with your dev tunnel

**Communication Service Events Setup**
1. Please add the webhook for the event "RecordingFileStatusUpdated" in your communication service events. make sure your webhook endpoint points to your devtunnel url https://{devtunnelurl}/api/events
### Quickstart
(Add steps to get up and running quickly)

1. git clone [https://github.com/Azure-Samples/communication-services-recording.git](https://github.com/Azure-Samples/communication-services-recording.git)
2. cd communication-services-recording
3. Update the connection string in the frontend/PROJECT/serverConfig.json
4. Follow the frontend and backend installation steps 

## Demo

1. Login using the Login ACS user and initialize SDk
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/aebaca67-cbcc-4485-910e-bbe9c62d3858)

2. Place Call with Record check box enabled for Auto Record, and you can choose recording contraints
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/9e2fc59a-7795-4812-ae22-4c2183df47a0)
3. During the record and once call ended you will see the below screen where it shows the events details and downloaded
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/a0e1fcf2-c9b2-45a2-bb73-9ed9549ce42f)

4. For manual record, uncheck the record check box, and you will be able to see the start recording button
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/b83fe7b6-2578-4e9c-9217-fd99004a9b24)


## Resources

* used webcalling sample as base for the UI https://github.com/Azure-Samples/communication-services-web-calling-tutorial/blob/main/README.md
* Backend used the recording quick starts https://github.com/Azure-Samples/communication-services-web-calling-tutorial/blob/main/README.md
