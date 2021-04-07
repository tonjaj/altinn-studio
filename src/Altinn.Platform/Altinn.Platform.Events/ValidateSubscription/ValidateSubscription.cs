using System;
using System.Text.Json;
using Altinn.Platform.Events.Models;
using Altinn.Platform.Events.ValidateSubscription.Service.Interface;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ValidateSubscription
{
    public class ValidateSubscription
    {
        private readonly IEventsWebHook _eventsWebHookService;

        public ValidateSubscription(IEventsWebHook eventsWebHookService)
        {
            _eventsWebHookService = eventsWebHookService;
        }

        [FunctionName("ValidateSubscription")]
        public async Task<void Run([QueueTrigger("myqueue-items", Connection = "")]string myQueueItem, ILogger log)
        {
            Subscription subscription = JsonSerializer.Deserialize<Subscription>(myQueueItem);
            CloudEvent cloudEvent = CreateTestEvent(subscription);
            HttpStatusCode httpStatusCode = await _eventsWebHookService.PushEvent(cloudEvent);


            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }

        private static CloudEvent CreateTestEvent(Subscription subscription)
        {
            CloudEvent cloudEvent = new CloudEvent();
            cloudEvent.Id = subscription.Id.ToString();
            cloudEvent.Source = new Uri("https://platform.altinn.no/events/subscription/" + subscription.Id);
            cloudEvent.Type = "altinn.platform.events.subscriptionvalidation";
            return cloudEvent;
        }
    }
}
