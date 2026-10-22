namespace TaskService.IntegrationTests
{
    using System.Diagnostics.CodeAnalysis; 
    using Microsoft.EntityFrameworkCore;

    using Xunit;
    using FluentAssertions;
 
    using TaskService.Api.Data;
    using TaskService.Api.Tools;

    [SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable",
        Justification = "Fields are disposed in DisposeAsync via IAsyncLifetime")]
    public sealed class TaskToolsTests : IAsyncLifetime
    {
        private TaskDbContext _db = null!;
        private TaskTools _tools = null!;

        public Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<TaskDbContext>()
                .UseInMemoryDatabase(databaseName: $"tasks-{Guid.NewGuid()}")
                .Options;

            _db = new TaskDbContext(options);
            _db.Database.EnsureCreated();
            _tools = new TaskTools(_db);

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _db.DisposeAsync();
        }

        [Fact]
        public async Task CreateTaskStoresTaskWithCorrectOwner()
        {
            var task = await _tools.CreateTask("user-1", "Test Task", "Description", default);

            task.Id.Should().BeGreaterThan(0);
            task.OwnerSubjectId.Should().Be("user-1");
            task.Title.Should().Be("Test Task");
            task.Description.Should().Be("Description");
            task.IsComplete.Should().BeFalse();
        }

        [Fact]
        public async Task CreateTaskWithEmptyDescriptionSucceeds()
        {
            var task = await _tools.CreateTask("user-1", "Title", "", default);

            task.Id.Should().BeGreaterThan(0);
            task.Description.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateTaskAssignsUniqueIds()
        {
            var t1 = await _tools.CreateTask("user-1", "A", "", default);
            var t2 = await _tools.CreateTask("user-1", "B", "", default);

            t1.Id.Should().NotBe(t2.Id);
        }

        [Fact]
        public async Task CreateTaskWithUnicodeTitleWorks()
        {
            var task = await _tools.CreateTask("user-1", "Задача", "Описание", default);

            task.Title.Should().Be("Задача");
            task.Description.Should().Be("Описание");
        }

        [Fact]
        public async Task CreateTaskWithLongDescriptionWorks()
        {
            var desc = new string('x', 2000);
            var task = await _tools.CreateTask("user-1", "Long", desc, default);

            task.Description.Length.Should().Be(2000);
        }

        [Fact]
        public async Task CreateTaskWithSpecialCharactersInTitleWorks()
        {
            var task = await _tools.CreateTask("user-1", "A/B\\C:D*E?F", "", default);

            task.Title.Should().Be("A/B\\C:D*E?F");
        }

        [Fact]
        public async Task CreateTaskSetsCreatedAt()
        {
            var before = DateTimeOffset.UtcNow.AddSeconds(-1);
            var task = await _tools.CreateTask("user-1", "Test", "", default);
            var after = DateTimeOffset.UtcNow.AddSeconds(1);

            task.CreatedAt.Should().BeOnOrAfter(before);
            task.CreatedAt.Should().BeOnOrBefore(after);
        }

        [Fact]
        public async Task CreateTaskWithWhitespaceDescriptionSucceeds()
        {
            var task = await _tools.CreateTask("user-1", "Title", "   ", default);

            task.Description.Should().Be("   ");
        }

        [Fact]
        public async Task CreateTaskWithNewlinesInDescriptionPreservesThem()
        {
            var desc = "Line 1\nLine 2\nLine 3";
            var task = await _tools.CreateTask("user-1", "Multi", desc, default);

            task.Description.Should().Be(desc);
        }

        [Fact]
        public async Task CreateTaskWithEmojiPreservesIt()
        {
            var task = await _tools.CreateTask("user-1", "Emoji", "Hello", default);

            task.Title.Should().Be("Emoji");
            task.Description.Should().Be("Hello");
        }

        [Fact]
        public async Task ListTasksReturnsOnlyUserTasks()
        {
            await _tools.CreateTask("user-1", "Task A", "", default);
            await _tools.CreateTask("user-1", "Task B", "", default);
            await _tools.CreateTask("user-2", "Task C", "", default);

            var user1Tasks = await _tools.ListTasks("user-1", default);
            var user2Tasks = await _tools.ListTasks("user-2", default);

            user1Tasks.Should().HaveCount(2);
            user1Tasks.Should().OnlyContain(t => t.OwnerSubjectId == "user-1");

            user2Tasks.Should().HaveCount(1);
            user2Tasks.Should().OnlyContain(t => t.OwnerSubjectId == "user-2");
        }

        [Fact]
        public async Task ListTasksReturnsEmptyWhenNoTasks()
        {
            var tasks = await _tools.ListTasks("user-1", default);
            tasks.Should().BeEmpty();
        }

        [Fact]
        public async Task ListTasksForUnknownUserReturnsEmpty()
        {
            await _tools.CreateTask("user-1", "Task A", "", default);

            var tasks = await _tools.ListTasks("nonexistent-user", default);
            tasks.Should().BeEmpty();
        }

        [Fact]
        public async Task ListTasksWithManyTasksReturnsAll()
        {
            for (int i = 0; i < 20; i++)
                await _tools.CreateTask("user-1", $"Task {i}", "", default);

            var tasks = await _tools.ListTasks("user-1", default);
            tasks.Should().HaveCount(20);
        }

        [Fact]
        public async Task ListTasksIncludesBothCompleteAndOpenTasks()
        {
            var t1 = await _tools.CreateTask("user-1", "Open", "", default);
            var t2 = await _tools.CreateTask("user-1", "Done", "", default);
            await _tools.CompleteTask("user-1", t2.Id, default);

            var tasks = await _tools.ListTasks("user-1", default);

            tasks.Should().HaveCount(2);
            tasks.Single(t => t.Id == t1.Id).IsComplete.Should().BeFalse();
            tasks.Single(t => t.Id == t2.Id).IsComplete.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteTaskMarksTaskAsComplete()
        {
            var task = await _tools.CreateTask("user-1", "Task A", "", default);

            var result = await _tools.CompleteTask("user-1", task.Id, default);

            result.Should().BeTrue();

            var updated = await _db.Tasks.FindAsync(task.Id);
            updated.Should().NotBeNull();
            updated!.IsComplete.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteTaskOnlyCompletesOwnTasks()
        {
            var task = await _tools.CreateTask("user-1", "Task A", "", default);

            var result = await _tools.CompleteTask("user-2", task.Id, default);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CompleteTaskReturnsFalseForNonexistentId()
        {
            var result = await _tools.CompleteTask("user-1", 99999, default);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CompleteTaskTwiceSucceedsBothTimes()
        {
            var task = await _tools.CreateTask("user-1", "Task A", "", default);

            var first = await _tools.CompleteTask("user-1", task.Id, default);
            var second = await _tools.CompleteTask("user-1", task.Id, default);

            first.Should().BeTrue();
            second.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteTaskDoesNotAffectOtherTasks()
        {
            var t1 = await _tools.CreateTask("user-1", "A", "", default);
            var t2 = await _tools.CreateTask("user-1", "B", "", default);

            await _tools.CompleteTask("user-1", t1.Id, default);

            var fetched2 = await _db.Tasks.FindAsync(t2.Id);
            fetched2!.IsComplete.Should().BeFalse();
        }

        [Fact]
        public async Task CompleteTaskAfterDeleteReturnsFalse()
        {
            var task = await _tools.CreateTask("user-1", "Task A", "", default);
            await _tools.DeleteTask("user-1", task.Id, default);

            var result = await _tools.CompleteTask("user-1", task.Id, default);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteTaskOnlyDeletesOwnTasks()
        {
            var task = await _tools.CreateTask("user-1", "Task A", "", default);

            var result = await _tools.DeleteTask("user-2", task.Id, default);

            result.Should().BeFalse();

            var stillExists = await _db.Tasks.FindAsync(task.Id);
            stillExists.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteTaskRemovesTask()
        {
            var task = await _tools.CreateTask("user-1", "Task A", "", default);

            var result = await _tools.DeleteTask("user-1", task.Id, default);

            result.Should().BeTrue();

            var deleted = await _db.Tasks.FindAsync(task.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteTaskTwiceReturnsFalseSecondTime()
        {
            var task = await _tools.CreateTask("user-1", "Task A", "", default);

            var first = await _tools.DeleteTask("user-1", task.Id, default);
            var second = await _tools.DeleteTask("user-1", task.Id, default);

            first.Should().BeTrue();
            second.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteTaskDoesNotAffectOthers()
        {
            var t1 = await _tools.CreateTask("user-1", "A", "", default);
            var t2 = await _tools.CreateTask("user-1", "B", "", default);

            await _tools.DeleteTask("user-1", t1.Id, default);

            var fetched2 = await _db.Tasks.FindAsync(t2.Id);
            fetched2.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteTaskReturnsFalseForNonexistentId()
        {
            var result = await _tools.DeleteTask("user-1", 99999, default);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteCompletedTaskSucceeds()
        {
            var task = await _tools.CreateTask("user-1", "Task A", "", default);
            await _tools.CompleteTask("user-1", task.Id, default);

            var result = await _tools.DeleteTask("user-1", task.Id, default);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task SearchTasksReturnsMatchingTitles()
        {
            await _tools.CreateTask("user-1", "Buy milk", "", default);
            await _tools.CreateTask("user-1", "Review contract", "", default);
            await _tools.CreateTask("user-1", "Call mom", "", default);

            var results = await _tools.SearchTasks("user-1", "milk", default);

            results.Should().HaveCount(1);
            results[0].Title.Should().Be("Buy milk");
        }

        [Fact]
        public async Task SearchTasksDoesNotReturnOtherUsersTasks()
        {
            await _tools.CreateTask("user-1", "Buy milk", "", default);
            await _tools.CreateTask("user-2", "Buy milk", "", default);

            var results = await _tools.SearchTasks("user-1", "milk", default);

            results.Should().HaveCount(1);
            results.Should().OnlyContain(t => t.OwnerSubjectId == "user-1");
        } 

        [Fact]
        public async Task SearchTasksWithEmptyKeywordReturnsAllUserTasks()
        {
            await _tools.CreateTask("user-1", "A", "", default);
            await _tools.CreateTask("user-1", "B", "", default);

            var results = await _tools.SearchTasks("user-1", "", default);

            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task SearchTasksWithNoMatchesReturnsEmpty()
        {
            await _tools.CreateTask("user-1", "Task A", "", default);

            var results = await _tools.SearchTasks("user-1", "nonexistent", default);
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchTasksMatchesPartialWord()
        {
            await _tools.CreateTask("user-1", "Buy milk", "", default);

            var results = await _tools.SearchTasks("user-1", "mil", default);
            results.Should().HaveCount(1);
        } 

        [Fact]
        public async Task SearchTasksIncludesCompletedTasks()
        {
            var t = await _tools.CreateTask("user-1", "Buy milk", "", default);
            await _tools.CompleteTask("user-1", t.Id, default);

            var results = await _tools.SearchTasks("user-1", "milk", default);
            results.Should().HaveCount(1);
            results[0].IsComplete.Should().BeTrue();
        }

        [Fact]
        public async Task SearchTasksOnEmptyDatabaseReturnsEmpty()
        {
            var results = await _tools.SearchTasks("user-1", "anything", default);
            results.Should().BeEmpty();
        } 

        [Fact]
        public async Task GetTaskReturnsOwnTask()
        {
            var task = await _tools.CreateTask("user-1", "My Task", "Desc", default);

            var result = await _tools.GetTask("user-1", task.Id, default);

            result.Should().NotBeNull();
            result!.Title.Should().Be("My Task");
        }

        [Fact]
        public async Task GetTaskReturnsNullForOtherUsersTask()
        {
            var task = await _tools.CreateTask("user-1", "My Task", "", default);

            var result = await _tools.GetTask("user-2", task.Id, default);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetTaskReturnsNullForNonexistentId()
        {
            var result = await _tools.GetTask("user-1", 99999, default);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetTaskReturnsFullDescription()
        {
            var desc = new string('d', 1500);
            var task = await _tools.CreateTask("user-1", "Long", desc, default);

            var result = await _tools.GetTask("user-1", task.Id, default);

            result!.Description.Should().Be(desc);
        }

        [Fact]
        public async Task GetTaskAfterCompleteReturnsCompletedStatus()
        {
            var task = await _tools.CreateTask("user-1", "Task", "", default);
            await _tools.CompleteTask("user-1", task.Id, default);

            var result = await _tools.GetTask("user-1", task.Id, default);

            result!.IsComplete.Should().BeTrue();
        }

        [Fact]
        public async Task GetTaskAfterDeleteReturnsNull()
        {
            var task = await _tools.CreateTask("user-1", "Task", "", default);
            await _tools.DeleteTask("user-1", task.Id, default);

            var result = await _tools.GetTask("user-1", task.Id, default);
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateThenGetReturnsSameData()
        {
            var created = await _tools.CreateTask("user-1", "Task", "Desc", default);

            var fetched = await _tools.GetTask("user-1", created.Id, default);

            fetched.Should().NotBeNull();
            fetched!.Title.Should().Be(created.Title);
            fetched.Description.Should().Be(created.Description);
            fetched.OwnerSubjectId.Should().Be(created.OwnerSubjectId);
            fetched.IsComplete.Should().Be(created.IsComplete);
        }

        [Fact]
        public async Task CreateCompleteGetReflectsChange()
        {
            var created = await _tools.CreateTask("user-1", "Task", "", default);
            await _tools.CompleteTask("user-1", created.Id, default);

            var fetched = await _tools.GetTask("user-1", created.Id, default);

            fetched!.IsComplete.Should().BeTrue();
        }

        [Fact]
        public async Task CreateDeleteListDoesNotIncludeDeleted()
        {
            var t1 = await _tools.CreateTask("user-1", "A", "", default);
            var t2 = await _tools.CreateTask("user-1", "B", "", default);
            await _tools.DeleteTask("user-1", t1.Id, default);

            var tasks = await _tools.ListTasks("user-1", default);

            tasks.Should().HaveCount(1);
            tasks[0].Id.Should().Be(t2.Id);
        }

        [Fact]
        public async Task FullLifecycleWorks()
        {
            var task = await _tools.CreateTask("user-1", "Lifecycle", "Desc", default);

            var read1 = await _tools.GetTask("user-1", task.Id, default);
            read1!.IsComplete.Should().BeFalse();

            await _tools.CompleteTask("user-1", task.Id, default);

            var read2 = await _tools.GetTask("user-1", task.Id, default);
            read2!.IsComplete.Should().BeTrue();

            await _tools.DeleteTask("user-1", task.Id, default);

            var read3 = await _tools.GetTask("user-1", task.Id, default);
            read3.Should().BeNull();
        }

        [Fact]
        public async Task MultipleUsersDoNotInterfere()
        {
            var u1 = await _tools.CreateTask("user-1", "U1 Task", "", default);
            var u2 = await _tools.CreateTask("user-2", "U2 Task", "", default);

            await _tools.CompleteTask("user-1", u1.Id, default);
            await _tools.DeleteTask("user-2", u2.Id, default);

            var u1Tasks = await _tools.ListTasks("user-1", default);
            var u2Tasks = await _tools.ListTasks("user-2", default);

            u1Tasks.Should().HaveCount(1);
            u1Tasks[0].IsComplete.Should().BeTrue();

            u2Tasks.Should().BeEmpty();
        }

        [Fact]
        public async Task SameUserCannotCompleteOtherUsersTaskById()
        {
            var t = await _tools.CreateTask("user-1", "Task", "", default);

            var result = await _tools.CompleteTask("user-2", t.Id, default);

            result.Should().BeFalse();

            var unchanged = await _db.Tasks.FindAsync(t.Id);
            unchanged!.IsComplete.Should().BeFalse();
        }

        [Fact]
        public async Task CreateManyTasksThenCompleteHalf()
        {
            var tasks = new List<TaskService.Api.Data.TaskItem>();
            for (int i = 0; i < 10; i++)
                tasks.Add(await _tools.CreateTask("user-1", $"Task {i}", "", default));

            foreach (var t in tasks.Take(5))
                await _tools.CompleteTask("user-1", t.Id, default);

            var all = await _tools.ListTasks("user-1", default);

            all.Should().HaveCount(10);
            all.Count(t => t.IsComplete).Should().Be(5);
            all.Count(t => !t.IsComplete).Should().Be(5);
        }

        [Fact]
        public async Task DeleteAllTasksThenListReturnsEmpty()
        {
            var tasks = new List<TaskService.Api.Data.TaskItem>();
            for (int i = 0; i < 5; i++)
                tasks.Add(await _tools.CreateTask("user-1", $"Task {i}", "", default));

            foreach (var t in tasks)
                await _tools.DeleteTask("user-1", t.Id, default);

            var all = await _tools.ListTasks("user-1", default);
            all.Should().BeEmpty();
        }

        [Fact]
        public async Task ConcurrentCreateDoesNotThrow()
        {
            var tasks = Enumerable.Range(0, 10)
                .Select(i => _tools.CreateTask("user-1", $"Task {i}", "", default))
                .ToArray();

            await Task.WhenAll(tasks);

            var all = await _tools.ListTasks("user-1", default);
            all.Should().HaveCount(10);
        }

        [Fact]
        public async Task CreateWithVeryLongTitleWorks()
        {
            var title = new string('t', 200);
            var task = await _tools.CreateTask("user-1", title, "", default);

            task.Title.Should().Be(title);
        }

        [Fact]
        public async Task CreateWithTabInDescriptionPreservesIt()
        {
            var desc = "Col1\tCol2";
            var task = await _tools.CreateTask("user-1", "Tabs", desc, default);

            task.Description.Should().Be(desc);
        }

        [Fact]
        public async Task CreateWithCarriageReturnPreservesIt()
        {
            var desc = "Line1\r\nLine2";
            var task = await _tools.CreateTask("user-1", "CRLF", desc, default);

            task.Description.Should().Be(desc);
        }

        [Fact]
        public async Task GetTaskPreservesUnicodeDescription()
        {
            var task = await _tools.CreateTask("user-1", "Задача", "Описание на кирилица", default);

            var fetched = await _tools.GetTask("user-1", task.Id, default);

            fetched!.Description.Should().Be("Описание на кирилица");
        }

        [Fact]
        public async Task CompleteTaskDoesNotChangeTitleOrDescription()
        {
            var task = await _tools.CreateTask("user-1", "Original", "Original desc", default);
            await _tools.CompleteTask("user-1", task.Id, default);

            var fetched = await _db.Tasks.FindAsync(task.Id);

            fetched!.Title.Should().Be("Original");
            fetched.Description.Should().Be("Original desc");
        }

        [Fact]
        public async Task SearchTasksReturnsSortedByCreatedAtDescending()
        {
            await _tools.CreateTask("user-1", "Match A", "", default);
            await Task.Delay(10);
            await _tools.CreateTask("user-1", "Match B", "", default);

            var results = await _tools.SearchTasks("user-1", "Match", default);

            results.Should().HaveCount(2);
            results[0].Title.Should().Be("Match B");
            results[1].Title.Should().Be("Match A");
        }

        [Fact]
        public async Task SearchTasksAfterDeleteDoesNotFindDeleted()
        {
            var t = await _tools.CreateTask("user-1", "Unique", "", default);
            await _tools.DeleteTask("user-1", t.Id, default);

            var results = await _tools.SearchTasks("user-1", "Unique", default);
            results.Should().BeEmpty();
        }
    }
}
