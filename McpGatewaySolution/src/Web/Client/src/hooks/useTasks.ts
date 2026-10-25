import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "../lib/apiClient";
import { queryKeys } from "../lib/queryKeys";
import type { TaskItem } from "../lib/types";
import toast from "react-hot-toast";

export function useTasksQuery() {
  return useQuery<TaskItem[]>({
    queryKey: queryKeys.tasks.list(),
    queryFn: () => apiFetch<TaskItem[]>("/api/v1/tasks"),
    staleTime: 30_000,
  });
}

export function useCreateTaskMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: { title: string; description: string }) =>
      apiFetch<TaskItem>("/api/v1/tasks", {
        method: "POST",
        body: JSON.stringify(input),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all });
      toast.success("Task created");
    },
  });
}

export function useCompleteTaskMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) =>
      apiFetch<{ success: boolean }>(`/api/v1/tasks/${id}/complete`, {
        method: "PATCH",
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all });
      toast.success("Task created");
    },
  });
}
