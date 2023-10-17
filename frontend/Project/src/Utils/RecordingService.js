export const recordingService = {
    recordCall: async (recordRequest) => {
        try {
            const response = await fetch('https://localhost:7108/api/recording/initiateRecording', {
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
            const response = await fetch('https://localhost:7108/api/recording/start', {
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
            const response = await fetch(`https://localhost:7108/api/recording/pause?recordingId=${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            if (response.ok) {
                return response.ok;
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
            const response = await fetch(`https://localhost:7108/api/recording/resume?recordingId=${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            if (response.ok) {
                return response.ok;
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
            const response = await fetch(`https://localhost:7108/api/recording/stop?recordingId=${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            if (response.ok) {
                return response.ok;
            } else {
                throw new Error('Network response was not ok');
            }
        }
        catch (error) {
            console.error('POST request failed:', error);
        }
    },

    downloadRecording: async () => {
        try {
            // const response = await fetch(`https://localhost:7108/api/recording/`, {
            //     method: 'POST',
            //     headers: {
            //         'Content-Type': 'application/json',
            //     }
            // });
            // if (!response.ok) {
            //     throw new Error('Network response was not ok');
            // }

            // const data = await response.json();
            // console.log('POST request succeeded:', data);
            // return data;
            return 'D:\\download\\recording';
        }
        catch (error) {
            console.error('POST request failed:', error);
        }
    }
    
}