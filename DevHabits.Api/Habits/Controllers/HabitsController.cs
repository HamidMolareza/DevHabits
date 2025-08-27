using DevHabits.Api.Habits.Dtos;
using DevHabits.Api.Habits.Entities;
using DevHabits.Api.Habits.Mappings;
using DevHabits.Api.Shared.Database;
using DevHabits.Api.Shared.Libraries.BaseApiControllers;
using DevHabits.Api.Shared.Libraries.Sort;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabits.Api.Habits.Controllers;

[Route("habits")]
public class HabitsController(
    ApplicationDbContext context,
    ISortOptionsProvider<Habit> habitSort
) : BaseApiController {
    // GET: Habits
    [HttpGet]
    public async Task<ActionResult<HabitsCollectionResponse>> GetHabits(
        string? sort,
        CancellationToken cancellationToken) {
        List<HabitResponse> habitDtos = await context.Habits
            .ApplySort(sort, habitSort.GetOptions())
            .Select(HabitQueries.ProjectToDto())
            .ToListAsync(cancellationToken);
        return new HabitsCollectionResponse { Data = habitDtos };
    }

    // GET: Habits/5
    [HttpGet("{id}")]
    public async Task<ActionResult<HabitWithTagsResponse>> GetHabit(string id, CancellationToken cancellationToken) {
        HabitWithTagsResponse? habit = await context.Habits
            .Where(habit => habit.Id == id)
            .Include(habit => habit.HabitTags)
            .ThenInclude(ht => ht.Tag)
            .Select(HabitQueries.ProjectToHabitWithTagsDto())
            .FirstOrDefaultAsync(cancellationToken);

        if (habit == null)
            return NotFoundProblem(resource: "Habit", resourceId: id);

        return habit;
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

        return CreatedAtAction("GetHabit", new { id = habit.Id }, habit);
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
}
