
export const recordingService = {
    recordCall: async (serverCallId, recordingContent, recordingChannel, recordingFormat,isRecordCall) => {
        const recoredRequest = {
            serverCallId: serverCallId,
            recordingContent: recordingContent,
            recordingChannel: recordingChannel,
            recordingFormat: recordingFormat,
            isRecord: isRecordCall
        };

        fetch('https://localhost:7108/api/recording/record', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(recoredRequest),
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