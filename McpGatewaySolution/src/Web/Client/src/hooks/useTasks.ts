// import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
// import { apiFetch } from "../lib/apiClient";
// import { queryKeys } from "../lib/queryKeys";
// import type { TaskItem } from "../lib/types";

// export function useTasksQuery() {
//   return useQuery<TaskItem[]>({
//     queryKey: queryKeys.tasks.list(),
//     queryFn: () => apiFetch<TaskItem[]>("/api/tasks"),
//     staleTime: 30_000,
//   });
// }

// export function useCreateTaskMutation() {
//   const queryClient = useQueryClient();

//   return useMutation({
//     mutationFn: (input: { title: string; description: string }) =>
//       apiFetch<TaskItem>("/api/tasks", {
//         method: "POST",
//         body: JSON.stringify(input),
//       }),
//     onSuccess: () => {
//       queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all });
//     },
//   });
// }

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "../lib/apiClient";
import { queryKeys } from "../lib/queryKeys";
import type { TaskItem } from "../lib/types";

export function useTasksQuery() {
  return useQuery<TaskItem[]>({
    queryKey: queryKeys.tasks.list(),
    queryFn: () => apiFetch<TaskItem[]>("/api/tasks"),
    staleTime: 30_000,
  });
}

export function useCreateTaskMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: { title: string; description: string }) =>
      apiFetch<TaskItem>("/api/tasks", {
        method: "POST",
        body: JSON.stringify(input),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all });
    },
  });
}

export function useCompleteTaskMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) =>
      apiFetch<{ success: boolean }>(`/api/tasks/${id}/complete`, {
        method: "PATCH",
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all });
    },
  });
}
