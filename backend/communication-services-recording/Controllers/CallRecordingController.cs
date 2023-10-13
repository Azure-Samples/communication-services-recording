using communication_services_recording.Models;
using System.Diagnostics;

namespace communication_services_recording.Controllers
{
    [Route("api/recording")]
    [ApiController]
    public class CallRecordingController : ControllerBase
    {
        private readonly ICallRecordingService callRecordingService;
        private readonly CallAutomationClient callAutomationClient;
        private static string recordingId = "";
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
        [Route("recording")]
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
                if (!string.IsNullOrWhiteSpace(recordingId))
                {
                    return Ok("recording already in progress");
                }

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
        [Route("record")]
        public async Task<IActionResult> Record(RecordingRequest recordingRequest)
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
                                
                // play audio file
                var callConnection = this.callAutomationClient.GetCallConnection(recordingRequest.CallConnectionId);
                var media = callConnection.GetCallMedia();
                var playSource = new FileSource(new Uri("https://voiceage.com/wbsamples/in_mono/Chorus.wav"));

                var playEvent = new Event();
                playEvent.Name = "PlayToAll";
                playEvent.StartTime = DateTime.UtcNow.ToString();
                PlayResult playResult = await media.PlayToAllAsync(playSource);

                // We can wait for EventProcessor that related to outbound call here. In this case, we are waiting for PlayToAllAsync
                // wait for play to complete
                PlayEventResult playEventResult = await playResult.WaitForEventProcessorAsync();
                playEvent.EndTime = DateTime.UtcNow.ToString();
                playEvent.Response = JsonSerializer.Serialize(playEventResult);
                this.logger.LogInformation($"Play completed successful: {playEventResult.IsSuccess}");
                recordingRespone.Events.Add(playEvent);

                // stop recording
                var recordingStopEvent = new Event();
                recordingStopEvent.Name = "StopRecording";
                recordingStopEvent.StartTime = DateTime.UtcNow.ToString();
                var response = await this.callRecordingService.StopRecording(recordingResult.RecordingId);
                recordingStopEvent.EndTime = DateTime.UtcNow.ToString();
                recordingStopEvent.Response = JsonSerializer.Serialize(response);
                recordingRespone.Events.Add(recordingStopEvent);


                // ends the call
                var hangUpCallEvent = new Event();
                hangUpCallEvent.Name = "HangUp";
                hangUpCallEvent.StartTime = DateTime.UtcNow.ToString();
                var endCallResponse = await callConnection.HangUpAsync(true);
                hangUpCallEvent.EndTime = DateTime.UtcNow.ToString();
                hangUpCallEvent.Response = JsonSerializer.Serialize(response);
                recordingRespone.Events.Add(hangUpCallEvent);

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

            await this.callRecordingService.StartRecording(recordingRequest);
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
