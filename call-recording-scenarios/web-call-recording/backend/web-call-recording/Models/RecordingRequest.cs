namespace web_call_recording.Models
{
    public class RecordingRequest
    {
        public string ServerCallId { get; set; }
        public string CallConnectionId { get; set; }
        public string RecordingContent { get; set; }
        public string RecordingChannel { get; set; }

        public string RecordingFormat { get; set; }

        public bool IsRecord { get; set; }
    }
}
