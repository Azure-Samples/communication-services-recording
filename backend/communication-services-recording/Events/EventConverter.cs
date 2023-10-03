﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace communication_services_recording.Events
{
    /* 
    * Helper class used to handle communication event models that are in preview, 
    * and not yet part of EventGrid.SystemEvents SDK 
    */
    public sealed class EventConverter
    {
        internal const string CallStartedEventName = "CallStarted";

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public object? Convert(EventGridEvent eventGridEvent)
        {
            var data = eventGridEvent.Data;
            if (data is null) throw new ArgumentNullException($"No data present: {eventGridEvent}");

            return ParseEventType(eventGridEvent.EventType) switch
            {
                CallStartedEventName => data.ToObjectFromJson<CallStartedEvent>(JsonOptions),
                _ => null
            };
        }

        private static string ParseEventType(string eventType)
        {
            var split = eventType.Split("Microsoft.Communication.");
            return split[^1];
        }
    }
}
