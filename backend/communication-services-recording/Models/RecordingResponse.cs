namespace communication_services_recording.Models
{
    public class RecordingResponse
    {
        public string CallConnectionId { get; set; }
        public string ServerCallId { get; set; }
        public string RecordingId { get; set; }
        public Error Error { get; set; }
        public List<Event> Events { get; set; }
    }
    public class Event
    {
        public string Name { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Response { get; set; }

    }

    public class Error
    {
        public string Message { get; set; }
        public string Stacktrace { get; set; }

    }
}
