const authConfig = {
    auth: {
        clientId: '',
        authority: 'https://login.microsoftonline.com/tenantId'
    }
};
// Add here scopes for id token to be used at MS Identity Platform endpoints.
const authScopes = {
    popUpLogin: [
        
    ],
    m365Login: [
        
    ]
};

module.exports = { authConfig, authScopes }