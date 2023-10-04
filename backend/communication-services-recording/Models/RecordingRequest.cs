namespace communication_services_recording.Models
{
    public class RecordingRequest
    {
        public string ServerCallId { get; set; }
        public string RecordingContent { get; set; }
        public string RecordingChannel { get; set; }

        public string RecordingFormat { get; set; }
    }
}
