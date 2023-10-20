# ACS Call Recording Test Tool

Test tool will help to test the recording feature from call automation sdk, with the calling SDK UI. It does support audio and video recording for 1:1, 1:N abd group calling.
It has option for both manual and auto record testing

## Features

This project framework provides the following features:

* UI supports both audio and video call for 1:1, 1:N and group call
* You can choose the different recording contraints for your call record
* It does auto record, when we place call with auto record check option, it starts the recording, play some sound for 10 secs, stop the play and disconnect the call.
* Once its disconnected it shows the downloaded recording file path to your local (currently it just download to project path)
* When we uncheck the auto record, then it provide start recording option during the call with the recording contraints

## Getting Started

### Prerequisites

* An Azure account with active subscription
* Communication service
* NPM
* Node js
* Dev tunnel

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

1. git clone [repository clone url]
2. cd [repository name]
3. ...


## Demo

A demo app is included to show how to use the project.

To run the demo, follow these steps:

(Add steps to start up the demo)

1.
2.
3.

## Resources

(Any additional resources or related projects)

- Link to supporting information
- Link to similar sample
- ...
