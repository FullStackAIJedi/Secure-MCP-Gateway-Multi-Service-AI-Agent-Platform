import { useMutation } from "@tanstack/react-query";

interface ChatResponse {
  reply: string;
  toolsUsed: string[];
}

export function useAgentChatMutation() {
  return useMutation({
    mutationFn: async (input: { sessionId: string; message: string }) => {
      // Вземи JWT от /api/auth/me (не връща JWT) — затова използвай /agent-token
      const tokenResponse = await fetch("/api/auth/agent-token", {
        method: "POST",
        credentials: "include",
      });
      const { accessToken } = await tokenResponse.json();

      const response = await fetch("/api/agent/chat", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify(input),
      });

      if (!response.ok) throw new Error(`Agent error: ${response.status}`);
      return response.json() as Promise<ChatResponse>;
    },
  });
}
