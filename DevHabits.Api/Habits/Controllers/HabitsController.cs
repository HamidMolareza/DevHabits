using DevHabits.Api.Habits.Dtos;
using DevHabits.Api.Habits.Entities;
using DevHabits.Api.Habits.Mappings;
using DevHabits.Api.Shared.Database;
using DevHabits.Api.Shared.Libraries.BaseApiControllers;
using DevHabits.Api.Shared.Libraries.DataShaping;
using DevHabits.Api.Shared.Libraries.Hateoas;
using DevHabits.Api.Shared.Libraries.Sort;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabits.Api.Habits.Controllers;

[Route("habits")]
public class HabitsController(
    ApplicationDbContext context,
    SortConfigs sortConfigs,
    DataShapeMapping dataShapeMapping,
    LinkService linkService,
    IHateoasService hateoasService
) : BaseApiController {
    // GET: Habits
    [HttpGet]
    public async Task<ActionResult<HabitsCollectionResponse>> GetHabits(
        [FromQuery] GetHabitQueryParams query,
        CancellationToken cancellationToken) {
        List<object> habitDtos = await context.Habits
            .ApplySort(query.Sort, sortConfigs.Get<Habit>())
            .ShapeFields(query.Fields, query.ExcludeFields, dataShapeMapping.Get<Habit, HabitResponse>())
            .ToListAsync(cancellationToken);

        hateoasService.Wrap(HttpContext.Request, habitDtos, habitDto => {
            ((Dictionary<string, object>)habitDto)["Links"] =
                CreateLinksForHabit(((Dictionary<string, object>)habitDto)["Id"].ToString()!).Links;
        });

        var response = new HabitsCollectionResponse { Data = habitDtos };

        hateoasService.Wrap(HttpContext.Request, response, res => {
            res.Links = CreateLinksForHabits(query).Links;
        });

        return response;
    }

    // GET: Habits/5
    [HttpGet("{id}")]
    public async Task<ActionResult<HabitWithTagsResponse>> GetHabit(
        string id,
        string? fields,
        string? excludeFields,
        CancellationToken cancellationToken) {
        object? habit = await context.Habits
            .Where(habit => habit.Id == id)
            .Include(habit => habit.HabitTags)
            .ThenInclude(ht => ht.Tag)
            .ShapeFields(fields, excludeFields, dataShapeMapping.Get<Habit, HabitWithTagsResponse>())
            .FirstOrDefaultAsync(cancellationToken);

        if (habit == null)
            return NotFoundProblem(resource: "Habit", resourceId: id);

        hateoasService.Wrap(HttpContext.Request, (Dictionary<string, object>)habit, h => {
            h["Links"] = CreateLinksForHabit(id, fields).Links;
        });

        return Ok(habit);
    }

    // PUT: Habits/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutHabit(string id, UpdateHabitRequest habitRequest,
        CancellationToken cancellationToken) {
        if (id != habitRequest.Id)
            return BadRequestProblem($"Route id '{id}' does not match body id '{habitRequest.Id}'");

        Habit? existingHabit = await context.Habits.FindAsync([id], cancellationToken: cancellationToken);
        if (existingHabit == null)
            return NotFoundProblem(resource: "Habit", resourceId: id);

        context.Attach(existingHabit);

        existingHabit.UpdateFromDto(habitRequest);
        await context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // POST: Habits
    [HttpPost]
    public async Task<ActionResult<HabitResponse>> PostHabit([FromBody] CreateHabitRequest habitRequest,
        CancellationToken cancellationToken) {
        Habit habit = habitRequest.ToEntity();

        context.Habits.Add(habit);
        await context.SaveChangesAsync(cancellationToken);

        var habitResponse = habit.ToHabitResponse();

        hateoasService.Wrap(HttpContext.Request, habitResponse, h => {
            h.Links = CreateLinksForHabit(h.Id).Links;
        });

        return CreatedAtAction("GetHabit", new { id = habitResponse.Id }, habitResponse);
    }

    // DELETE: Habits/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHabit(string id, CancellationToken cancellationToken) {
        var entity = new Habit { Id = id };
        context.Habits.Remove(entity);
        try {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) {
            if (!await context.Habits.AnyAsync(habit => habit.Id == id, cancellationToken))
                return NotFoundProblem(resource: "Habit", resourceId: id);
            throw;
        }

        return NoContent();
    }

    private LinkCollections CreateLinksForHabit(string id, string? fields = null) {
        LinkCollections linkCollections = new LinkCollections()
            .AddSelf(linkService.CreateGet(nameof(GetHabit), new { id, fields }))
            .AddUpdate(linkService.CreatePut(nameof(PutHabit), new { id }))
            .AddDelete(linkService.CreateDelete(nameof(DeleteHabit), new { id }));
        return linkCollections;
    }

    private LinkCollections CreateLinksForHabits(GetHabitQueryParams query) {
        LinkCollections linkCollections = new LinkCollections()
            .AddSelf(linkService.CreateGet(nameof(GetHabits), new { query.Fields, query.Sort, query.ExcludeFields }))
            .AddCreate(linkService.CreatePost(nameof(PostHabit)));
        return linkCollections;
    }
}
