namespace TaskService.Api.Tools
{
    using System.ComponentModel;

    using Microsoft.EntityFrameworkCore;

    using ModelContextProtocol.Server;
    using TaskService.Api.Data;

    [McpServerToolType]
    public sealed class TaskTools(TaskDbContext db)
    {
        [McpServerTool, Description("Lists all tasks")]
        public async Task<List<TaskItem>> ListTasks(CancellationToken cancellationToken)
        {
            return await db.Tasks
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        [McpServerTool, Description("Gets a task by identifier")]
        public async Task<TaskItem?> GetTask(int id, CancellationToken cancellationToken)
        {
            return await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        [McpServerTool, Description("Creates a new task")]
        public async Task<TaskItem> CreateTask(string title, string description, CancellationToken cancellationToken)
        {
            var task = new TaskItem { Title = title, Description = description };
            db.Tasks.Add(task);
            await db.SaveChangesAsync(cancellationToken);
            return task;
        }

        [McpServerTool, Description("Marks a task as complete")]
        public async Task<bool> CompleteTask(int id, CancellationToken cancellationToken)
        {
            var task = await db.Tasks.FindAsync([id], cancellationToken);
            if (task is null) return false;
            task.IsComplete = true;
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}