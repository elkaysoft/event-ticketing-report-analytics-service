using ETS.Application.Helpers;
using ETS.Application.Reporting.Queries.GetTransactionReport;
using ETS.Application.TicketNotification.Queries.GetPagedNotification;
using ETS.Domain.Contracts;
using ETS.WebApi.DTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ETS.WebApi.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ReportController : AuthControllerBase<ReportController>
    {
        public ReportController(ILogger<ReportController> logger,
            IConfiguration config,
            IUserContext userService, 
            ISender mediator) : base(logger, config, userService, mediator)
        {
        }

        [HttpGet("ticket-analytics")]
        [ProducesResponseType(typeof(GetTransactionReportResponse), StatusCodes.Status200OK)]        
        public async Task<IActionResult> GetTransactionReport([FromQuery] ReportRequest request)
        {
            var query = new GetTransactionReportQuery(request.StartDate, request.EndDate);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
