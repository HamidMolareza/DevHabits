using DevHabits.Api.Database;
using DevHabits.Api.Dtos.Tags;
using DevHabits.Api.Entities;
using DevHabits.Api.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabits.Api.Controllers;

[Route("tags")]
public class TagsController(ApplicationDbContext context) : BaseApiController {
    // GET: Tags
    [HttpGet]
    public async Task<ActionResult<TagsCollectionResponse>> GetTags() {
        List<TagResponse> tagDtos = await context.Tags
            .Select(TagQueries.ProjectToDto())
            .ToListAsync();
        return new TagsCollectionResponse { Data = tagDtos };
    }

    // GET: Tags/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TagResponse>> GetTag(string id) {
        TagResponse? tag = await context.Tags
            .Where(tag => tag.Id == id)
            .Select(TagQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        if (tag == null)
            return NotFoundProblem(resource: "Tag", resourceId: id);

        return Ok(tag);
    }

    // PUT: Tags/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTag(string id, UpdateTagRequest tagRequest) {
        if (id != tagRequest.Id)
            return BadRequest($"Route id '{id}' does not match body id '{tagRequest.Id}'");

        Tag? existingTag = await context.Tags.FindAsync(id);
        if (existingTag == null)
            return NotFoundProblem(resource: "Tag", resourceId: id);

        context.Attach(existingTag);
        existingTag.UpdateFromDto(tagRequest);
        await context.SaveChangesAsync();

        return NoContent();
    }

    // POST: Tags
    [HttpPost]
    public async Task<ActionResult<TagResponse>> PostTag(CreateTagRequest tagRequest,
        CreateTagRequestValidator validator) {
        await validator.ValidateAndThrowAsync(tagRequest);

        Tag tag = tagRequest.ToEntity();

        if (await context.Tags.AnyAsync(t => EF.Functions.Like(t.Name, tag.Name))) {
            return ConflictProblem(
                detail: $"A tag with the name '{tag.Name}' already exists.");
        }

        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        return CreatedAtAction("GetTag", new { id = tag.Id }, tag);
    }

    // DELETE: Tags/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTag(string id) {
        var entity = new Tag { Id = id, Name = string.Empty };
        context.Tags.Remove(entity);
        try {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException) {
            if (!await context.Tags.AnyAsync(t => t.Id == id))
                return NotFoundProblem(resource: "Tag", resourceId: id);
            throw;
        }

        return NoContent();
    }
}
