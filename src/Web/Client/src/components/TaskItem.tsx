import styled from "styled-components";
import { Button } from "../components/ui/Button";
import type { TaskItem } from "../../src/lib/types";

interface TaskItemProps {
  task: TaskItem;
  onComplete: (taskId: number) => void;
  isPending: boolean;
}

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

export function TaskItem({ task, onComplete, isPending }: TaskItemProps) {
  return (
    <TaskRow>
      <TaskTitle $done={task.isComplete}>{task.title}</TaskTitle>

      {!task.isComplete && (
        <Button
          size="sm"
          onClick={() => onComplete(task.id)}
          disabled={isPending}
        >
          {isPending ? "Completing..." : "Complete"}
        </Button>
      )}
    </TaskRow>
  );
}
