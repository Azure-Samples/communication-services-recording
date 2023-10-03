using Azure.Messaging;

namespace communication_services_recording.Controllers
{
    [Route("api")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private ICallRecordingService callRecordingService;
        private readonly EventConverter eventConverter;
        private readonly ILogger logger;

        public EventsController(
            ILogger<EventsController> logger,
            ICallRecordingService callRecordingService)
        {
            eventConverter = new EventConverter();
            this.logger = logger;
            this.callRecordingService = callRecordingService;
        }


        /* Route for Azure Communication Service eventgrid webhooks*/
        [HttpPost]
        [Route("events")]
        public async Task<IActionResult> Handle([FromBody] EventGridEvent[] eventGridEvents)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        var responseData = new SubscriptionValidationResponse
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };

                        return Ok(responseData);
                    }
                }

                logger.LogInformation("Received  event: {type}", eventGridEvent.EventType);
                var data = eventConverter.Convert(eventGridEvent);

                switch (data)
                {
                    case null:
                        continue;
                    case CallStartedEvent callStarted:
                        // await this.callRecordingService.StartRecording(callStarted.serverCallId);
                        break;
                }
            }

            return Ok();
        }

        /* Route for Azure Communication Service eventgrid webhooks*/
        [HttpPost]
        [Route("callbacks")]
        public async Task<IActionResult> Handle([FromBody] CloudEvent[] cloudEvents)
        {
            return Ok();
        }
    }
}
