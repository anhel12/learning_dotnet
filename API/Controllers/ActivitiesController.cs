using Microsoft.AspNetCore.Mvc;
using Domain;
using Application.Activities;

namespace API.Controllers
{
    public class ActivitiesController : BaseApiController
  {
  

    [HttpGet]
    public async Task<ActionResult<List<Activity>>> GetActivities() {
      return await _mediator.Send(new List.Query());
    }

        [HttpGet("{id}")] // activities/id
        public Task<ActionResult<Activity>> GetActivity(Guid id)
        {
            return Ok();
        }
    }
}