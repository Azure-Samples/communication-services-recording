namespace communication_services_recording.Controllers
{
    [Route("api/recording")]
    [ApiController]
    public class CallRecordingController : ControllerBase
    {
        private readonly ICallRecordingService callRecordingService;
        private readonly ILogger logger;

        public CallRecordingController(
            ICallRecordingService callRecordingService,
            ILogger<CallRecordingController> logger)
        {
            this.callRecordingService = callRecordingService;
            this.logger = logger;
        }

        [HttpPost]
        [Route("createCall")]
        public async Task<IActionResult> PlaceCall(string targetId)
        {
            // targetId = "8:acs:40b87f1c-e6d1-4772-ba9d-b1360619f38a_0000001b-92b9-2faa-28f4-343a0d00fd74";

            // create call
            var callResult = await this.callRecordingService.CreateCallAsync(targetId);
            var callConnectionId = callResult.CallConnection.CallConnectionId;
            return Ok();
        }

        [HttpPost]
        [Route("record")]
        public async Task<IActionResult> Record(RecordingRequest recordingRequest, string targetId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(recordingRequest, nameof(recordingRequest));
                ArgumentException.ThrowIfNullOrEmpty(recordingRequest.ServerCallId);

                /*TODO get the target id from the client*/
                // string targetId = "8:acs:40b87f1c-e6d1-4772-ba9d-b1360619f38a_0000001b-92b9-2faa-28f4-343a0d00fd74";

                // create call
                var createCallResult = await this.callRecordingService.CreateCallAsync(targetId);
                CallConnection callConnection = createCallResult.CallConnection;
                this.logger.LogInformation($"Call connection Id: {callConnection.CallConnectionId}");

                // We can wait for EventProcessor that related to outbound call here. In this case, we are waiting for CreateCallEventResult
                CreateCallEventResult createCallEventResult = await createCallResult.WaitForEventProcessorAsync();

                // Once EventResult comes back, we can get SuccessResult of CreateCall - which is, CallConnected event.
                CallConnected returnedEvent = createCallEventResult.SuccessResult;
                this.logger.LogInformation($"Call connection id: {returnedEvent.CallConnectionId}Server call Id: {returnedEvent.ServerCallId}");

                // start recording
                var recordingResult = await this.callRecordingService.StartRecording(returnedEvent.ServerCallId);
                this.logger.LogInformation($"Recording started, recording Id : {recordingResult.RecordingId}");

                // play audio file
                var media = callConnection.GetCallMedia();

                /*TODO - Add the audio file path */
                var playSource = new FileSource(new Uri(""));
                PlayResult playResult = await media.PlayToAllAsync(playSource);


                // We can wait for EventProcessor that related to outbound call here. In this case, we are waiting for CreateCallEventResult
                // wait for play to complete
                PlayEventResult playEventResult = await playResult.WaitForEventProcessorAsync();
                // check if the play was completed successfully
                if (playEventResult.IsSuccess)
                {
                    // success play!
                    PlayCompleted playCompleted = playEventResult.SuccessResult;
                }
                else
                {
                    // failed to play the audio.
                    PlayFailed playFailed = playEventResult.FailureResult;
                }

                // stop recording
                await this.callRecordingService.StopRecording(recordingResult.RecordingId);

                // ends the call

                // download recording file

                return Ok();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("start")]
        public async Task<IActionResult> StartRecording(string serverCallId)
        {
            await this.callRecordingService.StartRecording(serverCallId);
            return Ok();
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
        public async Task<IActionResult> StopRecording()
        {
            await this.callRecordingService.StopRecording("");
            return Ok();
        }

    }
}
