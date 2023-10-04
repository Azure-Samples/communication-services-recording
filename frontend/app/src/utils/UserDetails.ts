import { CommunicationIdentityClient } from '@azure/communication-identity'
import config from '../appsettings.json';


const connectionString = config.acsConnectionString;
export interface User {
    userId: string;
    token: string;
}

export const getUserDetails = async (): Promise<User> => {
    try {
        debugger;
        const identityClient = new CommunicationIdentityClient(connectionString);
        let identityTokenResponse = await identityClient.createUserAndToken(["voip"]);
        const { token, expiresOn, user } = identityTokenResponse;
        console.log(`\nCreated an identity with ID: ${user.communicationUserId}`);
        console.log(`\nIssued an access token with 'voip' scope that expires at ${expiresOn}:`);
        console.log(token);
        const userData: User = {
            userId: user.communicationUserId,
            token: token
        };
        return userData;
    } catch (error) {
       
        throw new Error('Failed at getting userId and token');
    }
};