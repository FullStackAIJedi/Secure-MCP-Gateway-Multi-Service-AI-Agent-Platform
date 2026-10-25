namespace TaskService.Api.Data
{
    using Microsoft.EntityFrameworkCore;

    using TaskService.Api.Entities;

    public static class TaskDbSeeder
    {
        public static async Task SeedAsync(TaskDbContext db, CancellationToken cancellationToken = default)
        {
            if (await db.Tasks.AnyAsync(cancellationToken))
                return;

            db.Tasks.AddRange(
                new TaskItem { Title = "Review contract", Description = "Q4 review", OwnerSubjectId = "seed-user-1" },
                new TaskItem { Title = "Buy milk", Description = "From the store", OwnerSubjectId = "seed-user-1" },
                new TaskItem { Title = "Call mom", Description = "Sunday call", OwnerSubjectId = "seed-user-1" }
            );

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
