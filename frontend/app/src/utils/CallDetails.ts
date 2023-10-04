import config from '../appsettings.json';
const BASE_URL = config.baseUrl;

export interface PlaceCallData {
    callerId: string;
    targetId: string;
}

export const placeCall = async (placeCallData: PlaceCallData) => {
    try {
        debugger;
        const response = await fetch(`${BASE_URL}/api/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(placeCallData)
        });
        if (response.ok) {
            return response.json();
        } else {
            console.error('failed to place call.');
        }
    } catch (error) {
        console.error('An error occurred:', error);
    }
};