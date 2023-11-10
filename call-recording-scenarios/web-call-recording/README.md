# Web Call Recording Test Tool

The test tool will help to test the recording feature from the call automation SDK, with the calling SDK UI. It supports audio and video recording for 1:1, 1:N, and group calls. It has options for both manual and auto record testing.

## Features

This project framework provides the following features:

* The UI supports both audio and video calls for 1:1, 1:N, and group calls.
* You can choose the different recording constraints for your call record.
* It does auto record when we place a call with the auto record check option. It starts the recording, plays some sound for 10 seconds, stops the play, and disconnects the call.
* Once it is disconnected, it shows the downloaded recording file path to your local (currently it just downloads to the project path).
* When we uncheck the auto record, it provides a start recording option during the call with the recording constraints.
* It provides the API endpoints for testing the REST API directly with the API version.
  ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/3cc34b48-b371-48b9-8e88-a187256fc0ef)

* The API endpoint to show the call automation version used in the project
  ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/db2a4afc-cea4-4a8e-905b-2ba7da7e4ea2)


## Getting Started

### Prerequisites

* An Azure account with an active subscription. For details, see [Create an account for free](https://aka.ms/Mech-Azureaccount) 
* Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource.](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-azp) You'll need to record your resource connection string for this sample.
* NPM
* Node js
* For local run: Install Azure Dev Tunnels CLI. For details, see [Create and host dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)
* [.NET 7](https://dotnet.microsoft.com/download)

## Setup Instructions

### Frontend
1. Get your Azure Communication Services resource connection string from the Azure portal, and put it as the value for connectionString in serverConfig.json file.
2. From the terminal/command prompt, Run:
   
```bash
npm install
npm run build-local
npm run start-local
```

3. Open provided localhost url from your terminal in the browser

### Backend

  #### 1. Setup and host your Azure DevTunnel

[Azure DevTunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) is an Azure service that enables you to share local web services hosted on the internet. Use the commands below to connect your local development environment to the public internet. This creates a tunnel with a persistent endpoint URL and which allows anonymous access. We will then use this endpoint to notify your application of calling events from the ACS Call Automation service.

```bash
devtunnel create --allow-anonymous
devtunnel port create -p 7108
devtunnel host
```

#### 2. Add the required API Keys and endpoints
Open the appsettings.json file to configure the following settings:

    
    - `AcsConnectionString`: Azure Communication Service resource's connection string.
    - `AcsKey`: Azure Communication Service resource key
    - `BaseUrl`:  your dev tunnel endpoint

## Running the application

1. Azure DevTunnel: Ensure your AzureDevTunnel URI is active and points to the correct port of your localhost application
2. Run `dotnet run` to build and run the web-call-recording tool
3. Follow the Setup Instructions
4. Register an EventGrid Webhook for the "Incoming Call" & "Recording File Status Updated" Events that points to your DevTunnel URI. Instructions [here](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/incoming-call-notification).
   Step 1 -> Go to your communication service resource in the Azure portal
   Step 2 -> Left corner you might see the events and click event subsription on the right
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/3e008c23-ba47-4eb7-8bbb-f0df4623801a)

   Step 3 -> Give the Name under the Subscription Details, and provide the system topic name under Topic Details and select "Incoming Call" & "Recording File Status Updated" under Event Types, And select the "Web Hook" from the Endpoint Details section
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/af0045a4-1ca5-4126-98e6-ea96557ec937)

   Step 4 -> Click on Configure an endpoint, provide Subscriber Endpoint to your devtunnel url, and for the events endpoint. ex. https://<devtunnelurl>/api/events. And click on the Confirm Selection and Create
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/673a661a-f36b-4dad-80ea-8a813ad7a17a)

   Step 5 -> once its created you will be able to see under the events section of the communication service
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/88e94e69-0443-466a-ada7-b881e21ff507)




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
* Backend used the recording quick starts https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/voice-video-calling/get-started-call-recording?pivots=programming-language-csharp
