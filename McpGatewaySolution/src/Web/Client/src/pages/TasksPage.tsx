import { useCallback, useState } from "react";
import { useTasksQuery, useCreateTaskMutation } from "../hooks/useTasks";
import { Button } from "../components/ui/Button";
import styled from "styled-components";

const Container = styled.div`
  max-width: 800px;
  margin: 40px auto;
  padding: 0 16px;
`;

const TaskItem = styled.li`
  padding: 12px;
  border-bottom: 1px solid #eee;
`;

export function TasksPage() {
  const { data: tasks, isLoading, error } = useTasksQuery();
  const createTask = useCreateTaskMutation();
  const [newTitle, setNewTitle] = useState("");

  const handleCreate = useCallback(() => {
    if (!newTitle.trim()) return;
    createTask.mutate(
      { title: newTitle, description: "" },
      { onSuccess: () => setNewTitle("") },
    );
  }, [newTitle, createTask]);

  if (isLoading) return <Container>Loading tasks...</Container>;
  if (error) return <Container>Error loading tasks.</Container>;

  return (
    <Container>
      <h1>Tasks</h1>
      <div style={{ marginBottom: 16 }}>
        <input
          value={newTitle}
          onChange={(e) => setNewTitle(e.target.value)}
          placeholder="New task title"
          style={{ marginRight: 8, padding: 8 }}
        />
        <Button onClick={handleCreate} disabled={createTask.isPending}>
          {createTask.isPending ? "Creating..." : "Create"}
        </Button>
      </div>
      <ul>
        {tasks?.map((task) => (
          <TaskItem key={task.id}>
            {task.title} — {task.isComplete ? "Done" : "Open"}
          </TaskItem>
        ))}
      </ul>
    </Container>
  );
}
