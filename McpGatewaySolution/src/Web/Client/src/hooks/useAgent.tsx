import { useMutation } from "@tanstack/react-query";

interface ChatResponse {
  reply: string;
  toolsUsed: string[];
}

export function useAgentChatMutation() {
  return useMutation({
    mutationFn: async (input: { sessionId: string; message: string }) => {
      const tokenResponse = await fetch("/api/v1/auth/agent-token", {
        method: "POST",
        credentials: "include",
      });

      if (!tokenResponse.ok) {
        const errorText = await tokenResponse.text();
        throw new Error(
          `Failed to get agent token: ${tokenResponse.status} - ${errorText || "Empty response"}`,
        );
      }

      const tokenText = await tokenResponse.text();
      if (!tokenText) {
        throw new Error("Agent token response is empty");
      }

      const { accessToken } = JSON.parse(tokenText) as { accessToken: string };

      if (!accessToken) {
        throw new Error("Agent token response missing accessToken");
      }

      const response = await fetch("/api/v1/agent/chat", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify(input),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(
          `Agent error: ${response.status} - ${errorText || "Empty response"}`,
        );
      }

      const text = await response.text();
      if (!text) {
        throw new Error("Agent returned empty response");
      }

      return JSON.parse(text) as ChatResponse;
    },
  });
}
