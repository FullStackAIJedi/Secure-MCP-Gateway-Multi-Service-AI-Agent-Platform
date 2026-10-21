namespace TaskService.Api.Tools
{
    using System.ComponentModel; 
    using Microsoft.EntityFrameworkCore;

    using ModelContextProtocol.Server;
    using TaskService.Api.Data;

    [McpServerToolType]
    public sealed class TaskTools(TaskDbContext db)
    {
        [McpServerTool, Description("Lists all tasks for the current user")]
        public async Task<List<TaskItem>> ListTasks(
            string subjectId,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"[TASKSERVICE] ListTasks subjectId = '{subjectId}'");

            var allTasks = await db.Tasks.AsNoTracking().ToListAsync(cancellationToken);
            Console.WriteLine($"[TASKSERVICE] Total tasks in DB = {allTasks.Count}");
            foreach (var t in allTasks)
            {
                Console.WriteLine($"[TASKSERVICE] Task {t.Id}: Title='{t.Title}', OwnerSubjectId='{t.OwnerSubjectId}'");
            }

            var filtered = allTasks.Where(t => t.OwnerSubjectId == subjectId).ToList();
            Console.WriteLine($"[TASKSERVICE] Filtered tasks = {filtered.Count}");

            return filtered;
        }

        [McpServerTool, Description("Creates a new task for the current user")]
        public async Task<TaskItem> CreateTask(
            string subjectId,
            string title,
            string description,
            CancellationToken cancellationToken)
        {
            var task = new TaskItem
            {
                Title = title,
                Description = description,
                OwnerSubjectId = subjectId
            };
            db.Tasks.Add(task);
            await db.SaveChangesAsync(cancellationToken);
            return task;
        }

        [McpServerTool, Description("Marks a task as complete")]
        public async Task<bool> CompleteTask(
            string subjectId,
            int id,
            CancellationToken cancellationToken)
        {
            var task = await db.Tasks.FirstOrDefaultAsync(
                t => t.Id == id && t.OwnerSubjectId == subjectId, cancellationToken);
            if (task is null) return false;
            task.IsComplete = true;
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        [McpServerTool, Description("Gets a task by identifier")]
        public async Task<TaskItem?> GetTask(
            string subjectId,
            int id,
            CancellationToken cancellationToken)
        {
            return await db.Tasks.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id && t.OwnerSubjectId == subjectId, cancellationToken);
        }

        [McpServerTool, Description("Searches tasks by title keyword")]
        public async Task<List<TaskItem>> SearchTasks(
            string subjectId,
            string keyword,
            CancellationToken cancellationToken)
        {
            return await db.Tasks
                .AsNoTracking()
                .Where(t => t.OwnerSubjectId == subjectId && t.Title.Contains(keyword))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        [McpServerTool, Description("Deletes a task by identifier")]
        public async Task<bool> DeleteTask(
            string subjectId,
            int id,
            CancellationToken cancellationToken)
        {
            var task = await db.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.OwnerSubjectId == subjectId, cancellationToken);
            if (task is null) return false;
            db.Tasks.Remove(task);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}