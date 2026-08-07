using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Contracts.Auth;
using TaskFlow.Api.Contracts.Common;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>Lists users, primarily so clients can populate assignee pickers.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> List(
        [FromQuery] PaginationParameters pagination, CancellationToken ct)
    {
        var response = await userService.ListAsync(pagination.Page, pagination.PageSize, ct);
        return Ok(response);
    }

    /// <summary>Fetches a single user by id.</summary>
    [HttpGet("{id:guid}", Name = nameof(GetUserById))]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUserById(Guid id, CancellationToken ct)
    {
        var response = await userService.GetAsync(id, ct);
        return Ok(response);
    }
}
