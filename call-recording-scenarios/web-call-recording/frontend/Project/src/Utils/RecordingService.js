
const BASE_URL = "https://localhost:7108";

export const recordingService = {
    recordCall: async (recordRequest) => {
        try {
            const response = await fetch(`${BASE_URL}/api/recording/initiateRecording`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(recordRequest),
            });

            if (!response.ok) {
                throw new Error('Network response was not ok');
            }

            const data = await response.json();
            console.log('POST request succeeded:', data);
            return data;
        } catch (error) {
            console.error('POST request failed:', error);
        }
    },


    startRecording: async (recordRequest) => {
        try {
            const response = await fetch(`${BASE_URL}/api/recording/start`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(recordRequest),
            });

            if (!response.ok) {
                throw new Error('Network response was not ok');
            }

            const data = await response.json();
            console.log('POST request succeeded:', data);
            return data;
        } catch (error) {
            console.error('POST request failed:', error);
        }

    },

    pauseRecording: async (id) => {
        try {
            const response = await fetch(`${BASE_URL}/api/recording/pause?recordingId=${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            if (response.ok) {
                const data = await response.json();
                return data;
            } else {
                throw new Error('Network response was not ok');
            }
        }
        catch (error) {
            console.error('POST request failed:', error);
        }
    },

    resumeRecording: async (id) => {
        try {
            const response = await fetch(`${BASE_URL}/api/recording/resume?recordingId=${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            if (response.ok) {
                const data = await response.json();
                return data;
            } else {
                throw new Error('Network response was not ok');
            }
        }
        catch (error) {
            console.error('POST request failed:', error);
        }
    },

    stopRecording: async (id) => {
        try {
            const response = await fetch(`${BASE_URL}/api/recording/stop?recordingId=${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            if (response.ok) {
                const data = await response.json();
                return data;
            } else {
                throw new Error('Network response was not ok');
            }
        }
        catch (error) {
            console.error('POST request failed:', error);
        }
    },

    downloadRecording: async (id) => {
        try {
            const response = await fetch(`${BASE_URL}/api/recording/download/path?recordingId=${id}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }

            const data = await response.json();
            console.log('POST request succeeded:', data);
            return data;
        }
        catch (error) {
            console.error('POST request failed:', error);
        }
    }

}