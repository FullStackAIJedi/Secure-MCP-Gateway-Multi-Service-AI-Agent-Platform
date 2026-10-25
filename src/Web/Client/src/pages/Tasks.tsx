import { useCallback, useState } from "react";
import styled from "styled-components";
import {
  useTasksQuery,
  useCreateTaskMutation,
  useCompleteTaskMutation,
} from "../hooks/useTasks";
import { Button } from "../components/ui/Button";
import { TaskItem } from "../components/TaskItem";

const Container = styled.div`
  max-width: 800px;
  margin: 40px auto;
  padding: 0 16px;
`;

const CreateTaskContainer = styled.div`
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 16px;
`;

const TaskInput = styled.input`
  flex: 1;
  min-width: 0;
  height: 44px;
  box-sizing: border-box;
  padding: 0 12px;
  border: 1px solid #ccc;
  border-radius: 10px;
  font-size: 16px;
`;

const TaskList = styled.ul`
  padding: 0;
`;

export function Tasks() {
  const { data: tasks, isLoading, error } = useTasksQuery();
  const createTask = useCreateTaskMutation();
  const completeTask = useCompleteTaskMutation();
  const [newTitle, setNewTitle] = useState("");

  const handleCreate = useCallback(() => {
    if (!newTitle.trim()) return;

    createTask.mutate(
      { title: newTitle, description: "" },
      {
        onSuccess: () => setNewTitle(""),
      },
    );
  }, [newTitle, createTask]);

  if (isLoading) {
    return <Container>Loading tasks...</Container>;
  }

  if (error) {
    return <Container>Error loading tasks.</Container>;
  }

  return (
    <Container>
      <h1>Tasks</h1>

      <CreateTaskContainer>
        <TaskInput
          value={newTitle}
          onChange={(event) => setNewTitle(event.target.value)}
          placeholder="New task title"
        />

        <Button
          size="md"
          onClick={handleCreate}
          disabled={createTask.isPending}
        >
          {createTask.isPending ? "Creating..." : "Create"}
        </Button>
      </CreateTaskContainer>

      {tasks && tasks.length > 0 ? (
        <TaskList>
          {tasks.map((task) => (
            <TaskItem
              key={task.id}
              task={task}
              onComplete={(id) => completeTask.mutate(id)}
              isPending={completeTask.isPending}
            />
          ))}
        </TaskList>
      ) : (
        <p>No tasks found.</p>
      )}
    </Container>
  );
}
