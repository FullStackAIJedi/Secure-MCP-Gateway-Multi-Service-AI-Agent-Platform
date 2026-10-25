namespace TaskService.Api.Entities
{
    public sealed class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string OwnerSubjectId { get; set; } = string.Empty;
    }
}
