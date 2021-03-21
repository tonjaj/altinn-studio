using Altinn.Platform.Events.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrgWebPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsHookController : ControllerBase
    {
        public async Task<ActionResult> Post([FromBody] CloudEvent cloudEvent)
        {

            return Ok();
        }
    }
}
