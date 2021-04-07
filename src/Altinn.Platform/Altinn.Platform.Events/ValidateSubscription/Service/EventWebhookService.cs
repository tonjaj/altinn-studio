using Altinn.Platform.Events.Models;
using Altinn.Platform.Events.ValidateSubscription.Service.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Altinn.Platform.Events.ValidateSubscription.Service
{
    public class EventWebhookService: IEventsWebHook
    {
        private readonly HttpClient _client;
        private readonly ILogger<EventWebhookService> _logger;

        public EventWebhookService(
            HttpClient httpClient,
            ILogger<EventWebhookService> logger)
        {
            httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("ApiPushEventsEndpoint"));
            _client = httpClient;
            _logger = logger;
        }

        public async Task<System.Net.HttpStatusCode> PushEvent(CloudEvent cloudEvent, Uri targetUri)
        {
            string serializedCloudEvent = JsonSerializer.Serialize(cloudEvent);

            HttpResponseMessage response = await _client.PostAsync(targetUri.Host,        
                  new StringContent(serializedCloudEvent, Encoding.UTF8, "application/json"));
            return response.StatusCode;
        }
    }
}
