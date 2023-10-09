export const recordingService = {
    recordCall: async (recordRequest) => {
        fetch('https://localhost:7108/api/recording/record', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(recordRequest),
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                console.log('POST request succeeded:', data);
            })
            .catch(error => {
                console.error('POST request failed:', error);
            });
    },

    startRecording: async (id) => {
        fetch(`https://localhost:7108/api/recording/start?serverCallId=${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                console.log('POST request succeeded:', data);
            })
            .catch(error => {
                console.error('POST request failed:', error);
            });
    },

    pauseRecording: async (id) => {
        fetch(`https://localhost:7108/api/recording/pause?recordingId=${id}`,{
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                console.log('POST request succeeded:', data);
            })
            .catch(error => {
                console.error('POST request failed:', error);
            });
    },

    resumeRecording: async (id) => {
        
        fetch(`https://localhost:7108/api/recording/resume?recordingId=${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                console.log('POST request succeeded:', data);
            })
            .catch(error => {
                console.error('POST request failed:', error);
            });
    },

    stopRecording: async (id) => {
        
        fetch(`https://localhost:7108/api/recording/stop?recordingId=${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                console.log('POST request succeeded:', data);
            })
            .catch(error => {
                console.error('POST request failed:', error);
            });
    }
}