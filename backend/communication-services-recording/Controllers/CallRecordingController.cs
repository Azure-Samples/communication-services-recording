namespace communication_services_recording.Controllers
{
    [Route("api/recording")]
    [ApiController]
    public class CallRecordingController : ControllerBase
    {
        private readonly ICallRecordingService callRecordingService;
        private readonly CallAutomationClient callAutomationClient;
        private static string recordingId = string.Empty;
        private readonly ILogger logger;

        public CallRecordingController(
            ICallRecordingService callRecordingService,
            CallAutomationClient callAutomationClient,
            ILogger<CallRecordingController> logger)
        {
            this.callRecordingService = callRecordingService;
            this.callAutomationClient = callAutomationClient;
            this.logger = logger;
        }

        [HttpPost]
        [Route("initiateRecording")]
        public async Task<IActionResult> Recording(RecordingRequest recordingRequest)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(recordingRequest, nameof(recordingRequest));
                ArgumentException.ThrowIfNullOrEmpty(recordingRequest.ServerCallId);

                var recordingRespone = new RecordingResponse();
                recordingRespone.ServerCallId = recordingRequest.ServerCallId;
                recordingRespone.CallConnectionId = recordingRequest.CallConnectionId;

                // start recording
                var recordingEvent = new Event();
                recordingEvent.Name = "StartRecording";
                recordingEvent.StartTime = DateTime.UtcNow.ToString();

                var recordingResult = await this.callRecordingService.StartRecording(recordingRequest);
                recordingId = recordingResult.RecordingId;
                recordingRespone.RecordingId = recordingResult.RecordingId;
                recordingEvent.EndTime = DateTime.UtcNow.ToString();
                recordingEvent.Response = JsonSerializer.Serialize(recordingResult);
                recordingRespone.Events = new List<Event> { recordingEvent };

                this.logger.LogInformation($"Recording started, recording Id : {recordingResult.RecordingId}");
                this.logger.LogInformation($"Recording state {recordingResult.RecordingState}");

                return Ok(recordingRespone);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("start")]
        public async Task<IActionResult> StartRecording(RecordingRequest recordingRequest)
        {
            ArgumentNullException.ThrowIfNull(recordingRequest, nameof(recordingRequest));
            ArgumentException.ThrowIfNullOrEmpty(recordingRequest.ServerCallId);
            var recordingRespone = new RecordingResponse();
            recordingRespone.ServerCallId = recordingRequest.ServerCallId;
            recordingRespone.CallConnectionId = recordingRequest.CallConnectionId;
            var recordingEvent = new Event();
            recordingEvent.Name = "StartRecording";
            recordingEvent.StartTime = DateTime.UtcNow.ToString();
            var recordingResult = await this.callRecordingService.StartRecording(recordingRequest);
            recordingRespone.RecordingId = recordingResult.RecordingId;
            recordingEvent.EndTime = DateTime.UtcNow.ToString();
            recordingEvent.Response = JsonSerializer.Serialize(recordingResult);
            recordingRespone.Events = new List<Event> { recordingEvent };
            return Ok(recordingRespone);
        }

        [HttpPost]
        [Route("pause")]
        public async Task<IActionResult> PauseRecording(string recordingId)
        {
            await this.callRecordingService.PauseRecording(recordingId);
            return Ok();
        }

        [HttpPost]
        [Route("resume")]
        public async Task<IActionResult> ResumeRecording(string recordingId)
        {
            await this.callRecordingService.ResumeRecording(recordingId);
            return Ok();
        }

        [HttpPost]
        [Route("stop")]
        public async Task<IActionResult> StopRecording(string recordingId)
        {
            await this.callRecordingService.StopRecording(recordingId);
            return Ok();
        }

        [HttpGet]
        [Route("download/path")]
        public async Task<IActionResult> GetRecordingPath(string recordingId)
        {
            var response = await this.callRecordingService.RecordingPath(recordingId);
            return Ok(response);
        }
    }
}
