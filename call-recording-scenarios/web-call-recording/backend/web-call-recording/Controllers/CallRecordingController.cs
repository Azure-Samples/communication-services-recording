namespace web_call_recording.Controllers
{
    [Route("api/recording")]
    [ApiController]
    public class CallRecordingController : ControllerBase
    {
        private readonly ICallRecordingService callRecordingService;
        private readonly CallAutomationClient callAutomationClient;
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
            string startTime = DateTime.UtcNow.ToString();
            var recordingResult = await this.callRecordingService.StartRecording(recordingRequest);
            string endTime = DateTime.UtcNow.ToString();
            string response = JsonSerializer.Serialize(recordingResult);

            this.logger.LogInformation($"Recording started, recording Id : {recordingResult.RecordingId}");
            this.logger.LogInformation($"Recording state {recordingResult.RecordingState}");
            var recordingResponse = this.getRecordingResponse(recordingRequest.ServerCallId,
                 recordingRequest.CallConnectionId, recordingResult.RecordingId);
            recordingResponse.Events = new List<Event>() { this.getEvent("StartRecording", startTime, endTime, response) };
            return Ok(recordingResponse);
        }

        [HttpPost]
        [Route("pause")]
        public async Task<IActionResult> PauseRecording(string recordingId)
        {
            var events = new List<Event>();
            string startTime = DateTime.UtcNow.ToString();
            var response = await this.callRecordingService.PauseRecording(recordingId);
            string endTime = DateTime.UtcNow.ToString();
            events.Add(this.getEvent("PauseRecording", startTime, endTime, response.ToString()));
            if (response != null && response.Status == 202)
            {
                string stateCallStartTime = DateTime.UtcNow.ToString();
                var state = await callAutomationClient.GetCallRecording().GetStateAsync(recordingId);
                string stateCallEndTime = DateTime.UtcNow.ToString();
                events.Add(this.getEvent("GetState", stateCallStartTime, stateCallEndTime, JsonSerializer.Serialize(state)));
                logger.LogInformation($"Recording has been paused and {state?.Value.RecordingState} state");
            }

            return Ok(events);
        }

        [HttpPost]
        [Route("resume")]
        public async Task<IActionResult> ResumeRecording(string recordingId)
        {
            var events = new List<Event>();
            string startTime = DateTime.UtcNow.ToString();
            var response =  await this.callRecordingService.ResumeRecording(recordingId);
            string endTime = DateTime.UtcNow.ToString();
            events.Add(this.getEvent("ResumeRecording", startTime, endTime, response.ToString()));
            if (response != null && response.Status == 202)
            {
                string stateCallStartTime = DateTime.UtcNow.ToString();
                var state = await callAutomationClient.GetCallRecording().GetStateAsync(recordingId);
                string stateCallEndTime = DateTime.UtcNow.ToString();
                events.Add(this.getEvent("GetState", stateCallStartTime, stateCallEndTime, JsonSerializer.Serialize(state)));
                logger.LogInformation($"Recording has been resumed and {state?.Value.RecordingState} state");
            }

            return Ok(events);
        }

        [HttpPost]
        [Route("stop")]
        public async Task<IActionResult> StopRecording(string recordingId)
        {
            var events = new List<Event>();
            string startTime = DateTime.UtcNow.ToString();
            var response = await this.callRecordingService.StopRecording(recordingId);
            string endTime = DateTime.UtcNow.ToString();
            events.Add(this.getEvent("StopRecording", startTime, endTime, response.ToString()));
            return Ok(events);
        }

        [HttpGet]
        [Route("download/path")]
        public async Task<IActionResult> GetRecordingPath(string recordingId)
        {
            var response = await this.callRecordingService.RecordingPath(recordingId);
            return Ok(response);
        }

        private RecordingResponse getRecordingResponse(string serverCallId, string clientCallId, string recordingId)
        {
            return new RecordingResponse()
            {
                ServerCallId = serverCallId,
                CallConnectionId = clientCallId,
                RecordingId = recordingId
            };
        }

        private Event getEvent(string eventName, string startTime, string endTime, string result)
        {
            return new Event()
            {
                Name = eventName,
                StartTime = startTime,
                EndTime = endTime,
                Response = result
            };
        }
    }
}
