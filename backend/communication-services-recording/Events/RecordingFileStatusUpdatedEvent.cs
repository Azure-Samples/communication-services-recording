namespace communication_services_recording.Events
{
    public class RecordingFileStatusUpdatedEvent
    {
        public RecordingStorageInfo recordingStorageInfo { get; set; }
        public string recordingStartTime { get; set; }
    }

    public class RecordingChunk
    {
        public string documentId { get; set; }
        public int index { get; set; }
        public string contentLocation { get; set; }

        public string metadataLocation { get; set; }

        public string deleteLocation { get; set; }

        public string endReason { get; set; }
    }

    public class RecordingStorageInfo
    {
        public RecordingChunk[] recordingChunks { get; set; }
    }
}
