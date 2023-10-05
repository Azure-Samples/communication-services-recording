namespace communication_services_recording.Controllers
{
    [Route("api/recording")]
    [ApiController]
    public class CallRecordingController : ControllerBase
    {
        private ICallRecordingService callRecordingService;
        private ICallAutomationService callAutomationService;

        public CallRecordingController(
            ICallRecordingService callRecordingService,
            ICallAutomationService callAutomationService)
        {
            this.callRecordingService = callRecordingService;
            this.callAutomationService = callAutomationService;
        }

        [HttpPost]
        [Route("placecallandrecord")]
        public async Task<IActionResult> PlaceCallAndRecord(string callerId, string targetId)
        {
            // callerId = "8:acs:40b87f1c-e6d1-4772-ba9d-b1360619f38a_0000001b-92b8-4e48-85f4-343a0d00f384";
            // targetId = "8:acs:40b87f1c-e6d1-4772-ba9d-b1360619f38a_0000001b-92b9-2faa-28f4-343a0d00fd74";

            // create call
            var callResult = await this.callAutomationService.CreateCall(callerId, targetId);


            // start recording

            // play audio file

            // stop recording

            // ends the call

            // download recording file

            return Ok();
        }


        [HttpPost]
        [Route("record")]
        public async Task<IActionResult> RecordCall(string serverCallId)
        {
            // start recording
            var recordingResponse = await this.callRecordingService.StartRecording(serverCallId);

            // play audio file
            if (recordingResponse.RecordingId != null)
            {
                await this.callAutomationService.PlayAudio(recordingResponse.RecordingId);
            }

            // stops recoring and ends the call
            await this.callAutomationService.EndCall("callConnectionId", recordingResponse.RecordingId);

            // download recording file

            return Ok();
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
