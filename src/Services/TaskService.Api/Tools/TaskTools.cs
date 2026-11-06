namespace TaskService.Api.Tools
{
    using System.ComponentModel; 
    using Microsoft.EntityFrameworkCore;

    using ModelContextProtocol.Server;
    using TaskService.Api.Data;
    using TaskService.Api.Entities;

    [McpServerToolType]
    public sealed class TaskTools(TaskDbContext db)
    {
        [McpServerTool, Description("Lists all tasks for the current user")]
        public async Task<List<TaskItem>> ListTasks(
            string subjectId,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"[TASKSERVICE] ListTasks subjectId = '{subjectId}'");

            var filtered = await db.Tasks
                .AsNoTracking()
                .Where(t => t.OwnerSubjectId == subjectId)
                .OrderBy(t => t.Id)
                .ToListAsync(cancellationToken);

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

        [McpServerTool, Description(
            "Updates an existing task identified by its EXACT current title. " +
            "You can change the title, the description, or both. " +
            "You MUST pass 'currentTitle' (the existing title, e.g. 'task 2'). " +
            "Pass 'newTitle' only if you want to rename. " +
            "Pass 'description' only if you want to change the description. " +
            "Example 1 (rename only): currentTitle='task 2', newTitle='My second task'. " +
            "Example 2 (description only): currentTitle='task 2', description='updated text'. " +
            "Example 3 (both): currentTitle='task 2', newTitle='My second task', description='updated text'.")]
        public async Task<string> UpdateTask(
        string subjectId,
        [Description("The EXACT current title of the task to update (e.g. 'task 2'). Required — this is the lookup key. Call list_tasks first if unsure.")]
                string currentTitle,
        [Description("Optional. The new title. Pass null or empty to keep the current title unchanged.")]
                string? newTitle = null,
        [Description("Optional. The new description. Pass null to keep the current description unchanged.")]
                string? description = null,
        CancellationToken cancellationToken = default)
        {
            Console.WriteLine(
                $"[TASKSERVICE] UpdateTask subjectId='{subjectId}', " +
                $"currentTitle='{currentTitle}', newTitle='{newTitle}', description='{description}'");

            if (string.IsNullOrWhiteSpace(currentTitle))
            {
                return "Error: 'currentTitle' is required. Call list_tasks first to see the exact task titles.";
            }

            var hasNewTitle = !string.IsNullOrWhiteSpace(newTitle);
            var hasNewDescription = description is not null;

            if (!hasNewTitle && !hasNewDescription)
            {
                return "Error: nothing to update. Provide 'newTitle' and/or 'description'.";
            }

            var task = await db.Tasks.FirstOrDefaultAsync(
                t => t.OwnerSubjectId == subjectId && t.Title == currentTitle,
                cancellationToken);

            if (task is null)
            {
                var available = await db.Tasks
                    .AsNoTracking()
                    .Where(t => t.OwnerSubjectId == subjectId)
                    .Select(t => t.Title)
                    .ToListAsync(cancellationToken);

                return available.Count == 0
                    ? $"Error: task with title '{currentTitle}' not found. You have no tasks."
                    : $"Error: task with title '{currentTitle}' not found. " +
                      $"Available titles: {string.Join(", ", available.Select(t => $"'{t}'"))}";
            }

            var changes = new List<string>();

            if (hasNewTitle)
            {
                changes.Add($"title '{task.Title}' -> '{newTitle}'");
                task.Title = newTitle!;
            }

            if (hasNewDescription)
            {
                changes.Add($"description -> '{description}'");
                task.Description = description!;
            }

            await db.SaveChangesAsync(cancellationToken);

            return $"Done";
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

        [McpServerTool, Description(
    "Marks a task as complete. Pass either 'id' (preferred) or 'title' (exact current title).")]
        public async Task<string> CompleteTask(
    string subjectId,
    [Description("Task id from list_tasks. Preferred.")] int? id = null,
    [Description("Exact current title, alternative to id.")] string? title = null,
    CancellationToken cancellationToken = default)
        {
            if (id is null && string.IsNullOrWhiteSpace(title))
                return "Error: provide 'id' or 'title'.";

            var task = id is not null
                ? await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.OwnerSubjectId == subjectId, cancellationToken)
                : await db.Tasks.FirstOrDefaultAsync(t => t.Title == title && t.OwnerSubjectId == subjectId, cancellationToken);

            if (task is null) return "Error: task not found.";
            if (task.IsComplete) return $"Task '{task.Title}' is already complete.";
            task.IsComplete = true;
            await db.SaveChangesAsync(cancellationToken);
            return $"Task '{task.Title}' marked complete.";
        }

        [McpServerTool, Description(
            "Deletes a task. Pass either 'id' (preferred) or 'title' (exact current title).")]
        public async Task<string> DeleteTask(
            string subjectId,
            [Description("Task id from list_tasks. Preferred.")] int? id = null,
            [Description("Exact current title, alternative to id.")] string? title = null,
            CancellationToken cancellationToken = default)
        {
            if (id is null && string.IsNullOrWhiteSpace(title))
                return "Error: provide 'id' or 'title'.";

            var task = id is not null
                ? await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.OwnerSubjectId == subjectId, cancellationToken)
                : await db.Tasks.FirstOrDefaultAsync(t => t.Title == title && t.OwnerSubjectId == subjectId, cancellationToken);

            if (task is null) return "Error: task not found.";
            db.Tasks.Remove(task);
            await db.SaveChangesAsync(cancellationToken);
            return $"Task '{task.Title}' deleted.";
        }
    }
}
