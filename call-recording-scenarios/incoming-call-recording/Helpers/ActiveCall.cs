using Azure.Communication.CallAutomation;
using System.Diagnostics;

namespace incoming_call_recording.Helpers
{
    public record ActiveCall
    {
        public CallConnection? CallConnection { get; set; }

        public CallConnectionProperties? CallConnectionProperties { get; set; }

        public string? CallId { get; set; }

        public string? SubscriptionId { get; set; }

        public string? RecordingId { get; set; }

        public string? CallerId { get; set; }

        public Stream? Stream { get; set; }

        public Stopwatch CallConnectedTimer { get; set; }

        public Stopwatch StartRecordingWithAnswerTimer { get; set; }

        public Stopwatch StartRecordingEventTimer { get; set; }

        public Stopwatch StartRecordingTimer { get; set; }

        public Stopwatch StopRecordingTimer { get; set; }

        public Stopwatch StopRecordingEventTimer { get; set; }

    }
}
