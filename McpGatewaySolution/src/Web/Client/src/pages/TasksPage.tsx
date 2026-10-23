import { useCallback, useState } from "react";
import styled from "styled-components";
import {
  useTasksQuery,
  useCreateTaskMutation,
  useCompleteTaskMutation,
} from "../hooks/useTasks";
import { Button } from "../components/ui/Button";

const Container = styled.div`
  max-width: 800px;
  margin: 40px auto;
  padding: 0 16px;
`;

const TaskRow = styled.li`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px;
  border-bottom: 1px solid #eee;
`;

const TaskTitle = styled.span<{ $done: boolean }>`
  text-decoration: ${({ $done }) => ($done ? "line-through" : "none")};
  color: ${({ $done }) => ($done ? "#999" : "#333")};
`;

export function TasksPage() {
  const { data: tasks, isLoading, error } = useTasksQuery();
  const createTask = useCreateTaskMutation();
  const completeTask = useCompleteTaskMutation();
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
          <TaskRow key={task.id}>
            <TaskTitle $done={task.isComplete}>{task.title}</TaskTitle>
            {!task.isComplete && (
              <Button
                size="sm"
                onClick={() => completeTask.mutate(task.id)}
                disabled={completeTask.isPending}
              >
                {completeTask.isPending ? "Completing..." : "Complete"}
              </Button>
            )}
          </TaskRow>
        ))}
      </ul>
    </Container>
  );
}
