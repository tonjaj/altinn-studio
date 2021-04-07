using Altinn.Platform.Events.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Altinn.Platform.Events.ValidateSubscription.Service.Interface
{
    public interface IEventsWebHook
    {
        public int PushEvent(CloudEvent cloudEvent, Uri targetUri);
    }
}
