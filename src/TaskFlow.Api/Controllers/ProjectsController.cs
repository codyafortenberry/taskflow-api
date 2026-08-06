using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Contracts.Common;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
[Produces("application/json")]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    /// <summary>Lists projects with pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProjectResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProjectResponse>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var response = await projectService.ListAsync(
            page < 1 ? 1 : page, Math.Clamp(pageSize, 1, 100), ct);
        return Ok(response);
    }

    /// <summary>Fetches a single project by id.</summary>
    [HttpGet("{id:guid}", Name = nameof(GetProjectById))]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> GetProjectById(Guid id, CancellationToken ct)
    {
        var response = await projectService.GetAsync(id, ct);
        return Ok(response);
    }

    /// <summary>Creates a new project. The caller becomes its owner.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectResponse>> Create(ProjectRequest request, CancellationToken ct)
    {
        var response = await projectService.CreateAsync(request, ct);
        return CreatedAtRoute(nameof(GetProjectById), new { id = response.Id }, response);
    }

    /// <summary>Updates a project. Restricted to the owner or an admin.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> Update(Guid id, ProjectRequest request, CancellationToken ct)
    {
        var response = await projectService.UpdateAsync(id, request, ct);
        return Ok(response);
    }

    /// <summary>Deletes a project and its tasks. Restricted to the owner or an admin.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await projectService.DeleteAsync(id, ct);
        return NoContent();
    }
}
