using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Contracts.Common;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/tasks")]
[Authorize]
[Produces("application/json")]
public sealed class TasksController(ITaskService taskService) : ControllerBase
{
    /// <summary>Lists tasks with filtering, search, sorting and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TaskResponse>>> List(
        [FromQuery] TaskQueryParameters query, CancellationToken ct)
    {
        var response = await taskService.ListAsync(query, ct);
        return Ok(response);
    }

    /// <summary>Fetches a single task by id.</summary>
    [HttpGet("{id:guid}", Name = nameof(GetTaskById))]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetTaskById(Guid id, CancellationToken ct)
    {
        var response = await taskService.GetAsync(id, ct);
        return Ok(response);
    }

    /// <summary>Creates a new task within a project.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var response = await taskService.CreateAsync(request, ct);
        return CreatedAtRoute(nameof(GetTaskById), new { id = response.Id }, response);
    }

    /// <summary>Replaces the mutable fields of a task.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(Guid id, UpdateTaskRequest request, CancellationToken ct)
    {
        var response = await taskService.UpdateAsync(id, request, ct);
        return Ok(response);
    }

    /// <summary>Transitions a task to a new status — the primary board interaction.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateStatus(
        Guid id, UpdateTaskStatusRequest request, CancellationToken ct)
    {
        var response = await taskService.UpdateStatusAsync(id, request.Status, ct);
        return Ok(response);
    }

    /// <summary>Deletes a task. Restricted to the creator or an admin.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await taskService.DeleteAsync(id, ct);
        return NoContent();
    }
}
