using DevHabits.Api.Database;
using DevHabits.Api.Dtos.Tags;
using DevHabits.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabits.Api.Controllers;

[Route("tags")]
[ApiController]
public class TagsController(ApplicationDbContext context) : ControllerBase {
    // GET: Tags
    [HttpGet]
    public async Task<ActionResult<TagsCollectionDto>> GetTags() {
        List<TagDto> tagDtos = await context.Tags
            .Select(TagQueries.ProjectToDto())
            .ToListAsync();
        return new TagsCollectionDto { Data = tagDtos };
    }

    // GET: Tags/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag(string id) {
        Tag? tag = await context.Tags.FindAsync(id);

        if (tag == null)
            return NotFound();

        return tag.ToDto();
    }

    // PUT: Tags/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTag(string id, UpdateTagDto tagDto) {
        if (id != tagDto.Id)
            return BadRequest();

        Tag? existingTag = await context.Tags.FindAsync(id);
        if (existingTag == null)
            return NotFound();

        context.Attach(existingTag);
        existingTag.UpdateFromDto(tagDto);
        await context.SaveChangesAsync();

        return NoContent();
    }

    // POST: Tags
    [HttpPost]
    public async Task<ActionResult<TagDto>> PostTag(CreateTagDto tagDto) {
        Tag tag = tagDto.ToEntity();

        if (await context.Tags.AnyAsync(t => EF.Functions.Like(t.Name, tag.Name))) {
            return Conflict(new { message = "A tag with the same name already exists." });
        }

        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        return CreatedAtAction("GetTag", new { id = tag.Id }, tag);
    }

    // DELETE: Tags/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTag(string id) {
        var entity = new Tag { Id = id };
        context.Tags.Remove(entity);
        try {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException) {
            if (!await context.Tags.AnyAsync(t => t.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }
}
