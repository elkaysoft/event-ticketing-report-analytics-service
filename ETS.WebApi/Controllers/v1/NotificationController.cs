using ETS.Application.Helpers;
using ETS.Application.TicketNotification.Queries.GetPagedNotification;
using ETS.Domain.Contracts;
using ETS.Domain.Errors;
using ETS.WebApi.DTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ETS.WebApi.Controllers.v1
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : AuthControllerBase<NotificationController>
    {
        public NotificationController(ILogger<NotificationController> logger,
            IConfiguration config, 
            IUserContext userService, 
            ISender mediator) : base(logger, config, userService, mediator)
        {
        }

        [HttpGet("email-logs")]
        [ProducesResponseType(typeof(PaginatedList<GetPagedNotificatonDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllActiveEvents([FromQuery] NotificationLogFilter request)
        {
            var query = new GetPagedNotificationQuery(request.SearchText,
                request.Status,
                request.SortField);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

    }
}
