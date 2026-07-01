using ETS.Domain.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ETS.WebApi.Controllers.v1
{
    [ApiController]
    public abstract class AuthControllerBase<T> : ControllerBase where T : AuthControllerBase<T>
    {
        /// <summary>
        /// Gets the logger instance for logging operations.
        /// </summary>
        protected readonly ILogger<T> _logger;

        /// <summary>
        /// Gets the IConfiguration instance
        /// </summary>
        protected readonly IConfiguration _config;

        /// <summary>
        /// Gets the UserContext instance
        /// </summary>
        protected readonly IUserContext _userService;

        protected ISender _mediator;

        protected IActionResult OkEmptyResult() => Ok();

        protected AuthControllerBase(ILogger<T> logger,
            IConfiguration config,
            IUserContext userService,
            ISender mediator)
        {
            _logger = logger;
            _config = config;
            _userService = userService;
            _mediator = mediator;
        }

    }
}
