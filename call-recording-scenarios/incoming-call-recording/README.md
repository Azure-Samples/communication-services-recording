# Incoming Call Recording

Receives the incoming call event, and answer the incoming call, starts the recording, and play text to user and allow user to record the message and then it stops the recording until user disconnect the call. Once the recorded file is available for downloading it will be downloaded to project location

## Features

This project framework provides the following features:

* [TODO]

## Getting Started

### Prerequisites

* An Azure account with an active subscription. For details, see [Create an account for free](https://aka.ms/Mech-Azureaccount) 
* Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource.](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-azp) You'll need to record your resource connection string for this sample.
* For local run: Install Azure Dev Tunnels CLI. For details, see [Create and host dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)
* [.NET 7](https://dotnet.microsoft.com/download)
* [Cognitive Service ](https://learn.microsoft.com/en-us/azure/search/search-create-service-portal)

## Setup Instructions

Before running this sample, you'll need to setup the resources above with the following configuration updates:

##### 1. Setup and host your Azure DevTunnel

[Azure DevTunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) is an Azure service that enables you to share local web services hosted on the internet. Use the commands below to connect your local development environment to the public internet. This creates a tunnel with a persistent endpoint URL and which allows anonymous access. We will then use this endpoint to notify your application of calling events from the ACS Call Automation service.

```bash
devtunnel create --allow-anonymous
devtunnel port create -p 7051
devtunnel host
```

##### 2. Add a Managed Identity to the ACS Resource that connects to the Cognitive Services resource
Follow the instructions in this [documentation](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/azure-communication-services-azure-cognitive-services-integration).

##### 3. Add the required API Keys and endpoints
Open the appsettings.json file to configure the following settings:

    
    - `AcsConnectionString`: Azure Communication Service resource's connection string.
    - `CognitiveServiceEndpoint`: The Cognitive Services endpoint
    - `BaseUrl`:  your dev tunnel endpoint

## Running the application

1. Azure DevTunnel: Ensure your AzureDevTunnel URI is active and points to the correct port of your localhost application
2. Run `dotnet run` to build and run the incoming-call-recording tool
3. Register an EventGrid Webhook for the IncomingCall Event that points to your DevTunnel URI. Instructions [here](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/incoming-call-notification).
   Step 1 -> Go to your communication service resource in the Azure portal
   Step 2 -> Left corner you might see the events and click event subsription on the right
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/3e008c23-ba47-4eb7-8bbb-f0df4623801a)

   Step 3 -> Give the Name under the Subscription Details, and provide the system topic name under Topic Details and select "Incoming Call" & "Recording File Status Updated" under Event Types, And select the "Web Hook" from the Endpoint Details section
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/af0045a4-1ca5-4126-98e6-ea96557ec937)

   Step 4 -> Click on Configure an endpoint, provide Subscriber Endpoint to your devtunnel url, and for the events endpoint. ex. https://<devtunnelurl>/api/events. And click on the Confirm Selection and Create
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/673a661a-f36b-4dad-80ea-8a813ad7a17a)

   Step 5 -> once its created you will be able to see under the events section of the communication service
   ![image](https://github.com/Azure-Samples/communication-services-recording/assets/146493756/88e94e69-0443-466a-ada7-b881e21ff507)



   

