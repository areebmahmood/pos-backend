using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Features.Categories;
using Pos.Domain.Common;

namespace Pos.Api.Controllers;

[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categories) : ApiControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CategoryDto>> List(CancellationToken ct) => categories.ListAsync(ct);

    [HttpGet("{id:int}")]
    public Task<CategoryDto> Get(int id, CancellationToken ct) => categories.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveCategoryRequest request, CancellationToken ct)
    {
        var category = await categories.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<CategoryDto> Update(int id, SaveCategoryRequest request, CancellationToken ct) =>
        categories.UpdateAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await categories.DeleteAsync(id, ct);
        return NoContent();
    }
}
