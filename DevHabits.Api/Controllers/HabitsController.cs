using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevHabits.Api.Database;
using DevHabits.Api.Dtos.Habits;
using DevHabits.Api.Entities;

namespace DevHabits.Api.Controllers;

[Route("habits")]
[ApiController]
public class HabitsController(ApplicationDbContext context) : ControllerBase {
    // GET: Habits
    [HttpGet]
    public async Task<ActionResult<HabitsCollectionDto>> GetHabits() {
        List<HabitDto> habitDtos = await context.Habits
            .Select(HabitQueries.ProjectToDto())
            .ToListAsync();
        return new HabitsCollectionDto { Data = habitDtos };
    }

    // GET: Habits/5
    [HttpGet("{id}")]
    public async Task<ActionResult<HabitDto>> GetHabit(string id) {
        Habit? habit = await context.Habits.FindAsync(id);

        if (habit == null)
            return NotFound();

        return habit.ToDto();
    }

    // PUT: Habits/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutHabit(string id, UpdateHabitDto habitDto) {
        if (id != habitDto.Id)
            return BadRequest();

        Habit? existingHabit = await context.Habits.FindAsync(id);
        if (existingHabit == null)
            return NotFound();

        context.Attach(existingHabit);

        existingHabit.UpdateFromDto(habitDto);
        await context.SaveChangesAsync();

        return NoContent();
    }

    // POST: Habits
    [HttpPost]
    public async Task<ActionResult<HabitDto>> PostHabit([FromBody] CreateHabitDto habitDto) {
        Habit habit = habitDto.ToEntity();

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        return CreatedAtAction("GetHabit", new { id = habit.Id }, habit);
    }

    // DELETE: Habits/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHabit(string id) {
        var entity = new Habit { Id = id };
        context.Habits.Remove(entity);
        try {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException) {
            if (!await context.Habits.AnyAsync(habit => habit.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }
}
