import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "../lib/apiClient";
import { queryKeys } from "../lib/queryKeys";
import type { AuthUser, LoginRequest, RegisterRequest } from "../lib/types";
import toast from "react-hot-toast";

export function useCurrentUserQuery() {
  return useQuery<AuthUser | null>({
    queryKey: queryKeys.auth.me,
    queryFn: async () => {
      try {
        return await apiFetch<AuthUser>("/api/v1/auth/me");
      } catch {
        return null;
      }
    },
    staleTime: 5 * 60 * 1000,
    retry: false,
  });
}

export function useLoginMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (credentials: LoginRequest) =>
      apiFetch<{ user: AuthUser }>("/api/v1/auth/login", {
        method: "POST",
        body: JSON.stringify(credentials),
      }),
    onSuccess: (data) => {
      queryClient.setQueryData(queryKeys.auth.me, data.user);
      toast.success("Successfully Signed In");
    },
  });
}

export function useRegisterMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (credentials: RegisterRequest) =>
      apiFetch<{ user: AuthUser }>("/api/v1/auth/register", {
        method: "POST",
        body: JSON.stringify(credentials),
      }),
    onSuccess: (data) => {
      queryClient.setQueryData(queryKeys.auth.me, data.user);
      toast.success("Successfully Sign Up");
    },
  });
}

export function useLogoutMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async () => {
      try {
        const csrfToken = document.cookie.match(
          /(?:^|;\s*)XSRF-TOKEN=([^;]*)/,
        )?.[1];
        const res = await fetch(
          `${import.meta.env.VITE_API_URL ?? "http://localhost:5000"}/api/v1/auth/logout`,
          {
            method: "POST",
            credentials: "include",
            headers: {
              "Content-Type": "application/json",
              ...(csrfToken
                ? { "X-XSRF-TOKEN": decodeURIComponent(csrfToken) }
                : {}),
            },
          },
        );
        if (!res.ok) {
          console.warn("Logout API returned:", res.status);
        }
      } catch (err) {
        console.warn("Logout API failed:", err);
      }
    },
    onSuccess: () => {
      queryClient.clear();
      window.location.href = "/login";
    },
    onError: () => {
      queryClient.clear();
      window.location.href = "/login";
    },
  });
}
