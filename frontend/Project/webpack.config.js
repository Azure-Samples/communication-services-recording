const CommunicationIdentityClient = require("@azure/communication-identity").CommunicationIdentityClient;
const HtmlWebPackPlugin = require("html-webpack-plugin");
const config = require("./serverConfig.json");
const axios = require("axios");
const bodyParser = require('body-parser');

if(!config || !config.connectionString || config.connectionString.indexOf('endpoint=') === -1)
{
    throw new Error("Update `serverConfig.json` with connection string");
}

const communicationIdentityClient = new  CommunicationIdentityClient(config.connectionString);

const PORT = process.env.port || 8081;


const oneSignalRegistrationTokenToAcsUserAccesTokenMap = new Map();
const registerCommunicationUserForOneSignal = async (communicationAccessToken, communicationUserIdentifier) => {
    const oneSignalRegistrationToken = generateGuid();
    await axios({
        url: config.functionAppOneSignalTokenRegistrationUrl,
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json'
        },
        data: JSON.stringify({
            communicationUserId: communicationUserIdentifier.communicationUserId,
            oneSignalRegistrationToken
        })
    }).then((response) => { return response.data });
    oneSignalRegistrationTokenToAcsUserAccesTokenMap.set(oneSignalRegistrationToken, { communicationAccessToken, communicationUserIdentifier });
    return oneSignalRegistrationToken;
}

const generateGuid = function () {
    function s4() {
        return Math.floor((Math.random() + 1) * 0x10000).toString(16).substring(1);
    }
    return `${s4()}${s4()}-${s4()}-${s4()}-${s4()}-${s4()}${s4()}${s4()}`;
}

// Exchanging Azure AD access token of a Teams User for a Communication access token
// https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/manage-teams-identity?pivots=programming-language-javascript


module.exports = {
    devtool: 'inline-source-map',
    mode: 'development',
    entry: "./src/index.js",
    module: {
        rules: [
            {
                test: /\.(js|jsx)$/,
                exclude: /node_modules/,
                use: {
                    loader: "babel-loader"
                }
            },
            {
                test: /\.(ts|tsx)?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
            {
                test: /\.html$/,
                use: [
                    {
                        loader: "html-loader"
                    }
                ]
            },
            {
                test: /\.css$/,
                use: ["style-loader", "css-loader"]
            }
        ]
    },
    plugins: [
        new HtmlWebPackPlugin({
            template: "./public/index.html",
            filename: "./index.html"
        })
    ],
    devServer: {
        open: true,
        port: PORT,
        static:'./public',
        allowedHosts:[
            '.azurewebsites.net'
        ],
        webSocketServer: false,
        setupMiddlewares: (middlewares, devServer) => {
            if (!devServer) {
                throw new Error('webpack-dev-server is not defined');
            }

            devServer.app.use(bodyParser.json());
            devServer.app.post('/getCommunicationUserToken', async (req, res) => {
                try {
                    const communicationUserId = req.body.communicationUserId;
                    let CommunicationUserIdentifier;
                    if (!communicationUserId) {
                        CommunicationUserIdentifier = await communicationIdentityClient.createUser();
                    } else {
                        CommunicationUserIdentifier = { communicationUserId: communicationUserId };
                    }
                    const communicationUserToken = await communicationIdentityClient.getToken(CommunicationUserIdentifier, ["voip"]);
                    let oneSignalRegistrationToken;
                    if (config.functionAppOneSignalTokenRegistrationUrl) {
                        oneSignalRegistrationToken = await registerCommunicationUserForOneSignal(communicationUserToken, CommunicationUserIdentifier);
                    }
                    res.setHeader('Content-Type', 'application/json');
                    res.status(200).json({communicationUserToken, oneSignalRegistrationToken, userId: CommunicationUserIdentifier });
                } catch (e) {
                    console.log('Error setting registration token', e);
                    res.sendStatus(500);
                }
            });
            devServer.app.post('/getCommunicationUserTokenForOneSignalRegistrationToken', async (req, res) => {
                try {
                    const oneSignalRegistrationToken = req.body.oneSignalRegistrationToken;
                    const { communicationUserToken, communicationUserIdentifier } = oneSignalRegistrationTokenToAcsUserAccesTokenMap.get(oneSignalRegistrationToken);
                    res.setHeader('Content-Type', 'application/json');
                    res.status(200).json({ communicationUserToken, userId: communicationUserIdentifier, oneSignalRegistrationToken });
                } catch (e) {
                    console.log('Error setting registration token', e);
                    res.sendStatus(500);
                }
            });
            devServer.app.post('/getOneSignalRegistrationTokenForCommunicationUserToken', async (req, res) => {
                try {
                    const communicationUserToken = {token: req.body.token };
                    const communicationUserIdentifier = { communicationUserId: req.body.communicationUserId };

                    if (!config.functionAppOneSignalTokenRegistrationUrl) {
                        res.setHeader('Content-Type', 'application/json');
                        res.status(200).json({
                            communicationUserToken, userId: communicationUserIdentifier 
                        });
                        return;
                    }

                    let pair = [...oneSignalRegistrationTokenToAcsUserAccesTokenMap.entries()].find((pair) => {
                        return pair[1].token === communicationUserToken.token &&
                            pair[1].communicationUserId === communicationUserIdentifier.communicationUserId;
                    });
                    let oneSignalRegistrationToken;
                    if (pair) {
                        oneSignalRegistrationToken = pair[0];
                    } else {
                        oneSignalRegistrationToken = await registerCommunicationUserForOneSignal(communicationUserToken, communicationUserIdentifier);
                    }
                    res.setHeader('Content-Type', 'application/json');
                    res.status(200).json({
                        communicationUserToken,
                        userId: communicationUserIdentifier,
                        oneSignalRegistrationToken
                    });
                } catch (e) {
                    console.log('Error setting registration token', e);
                    res.sendStatus(500);
                }
            });

            return middlewares;
        }
    }
};
