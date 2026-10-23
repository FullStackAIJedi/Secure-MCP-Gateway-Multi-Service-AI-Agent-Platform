import { useCallback, useRef, useState } from "react";
import styled from "styled-components";
import { useAgentChatMutation } from "../hooks/useAgent";
import stripIds from "../utils/helper";

const Container = styled.div`
  min-width: 800px;
  max-width: 1200px;
  margin: 40px auto;
  padding: 0 16px;
  display: flex;
  flex-direction: column;
  height: 80vh;
`;

const Messages = styled.div`
  flex: 1;
  min-width: 0;
  box-sizing: border-box;
  overflow-y: auto;
  padding: 16px 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
`;

const Bubble = styled.div<{ $role: "user" | "assistant" }>`
  display: block;
  width: fit-content;
  max-width: 80%;
  padding: 12px 10px 12px 16px;
  border-radius: 12px;
  align-self: ${({ $role }) => ($role === "user" ? "flex-end" : "flex-start")};
  background: ${({ $role }) => ($role === "user" ? "black" : "#f0f0f0")};
  color: ${({ $role }) => ($role === "user" ? "#fff" : "#333")};
  white-space: pre-wrap;
  overflow-wrap: anywhere;
  text-align: left;
`;

const ToolsBadge = styled.div`
  font-size: 0.65rem;
  color: #888;
  margin-top: 4px;
  text-align: left;
`;

const ApprovalBox = styled.div`
  border: 2px solid #f34e12;
  border-radius: 12px;
  padding: 16px;
  background: black;
  max-width: 50%;
  text-align: left;
`;

const ApprovalTitle = styled.div`
  font-weight: 900;
  font-size: 1.5rem;
  color: #f34e12;
`;

const ApprovalRow = styled.span`
  font-size: 0.6rem;
  color: #6b6565;
  font-weight: 300;
`;

const ApprovalActions = styled.div`
  display: flex;
  gap: 8px;
  margin-top: 12px;
`;

const ApproveButton = styled.button`
  padding: 8px 16px;
  background: #0b714b;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

const RejectButton = styled.button`
  padding: 8px 16px;
  background: #f34e12;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

const InputRow = styled.div`
  display: flex;
  gap: 8px;
  padding: 16px 0;
`;

const Input = styled.input`
  flex: 1;
  padding: 12px;
  border: 1px solid black;
  border-radius: 10px;
  font-size: 1rem;
`;

const SendButton = styled.button`
  padding: 12px 24px;
  background: black;
  color: white;
  border: none;
  border-radius: 10px;
  cursor: pointer;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

interface Message {
  role: "user" | "assistant";
  content: string;
  tools?: string[];
  requiresApproval?: boolean;
  toolName?: string;
  arguments?: string;
  approved?: boolean;
}

interface ApprovalPayload {
  requiresApproval: boolean;
  toolName: string;
  arguments: string;
}

function tryParseApproval(reply: string): ApprovalPayload | null {
  try {
    const obj = JSON.parse(reply);
    if (obj && obj.requiresApproval === true) {
      return obj as ApprovalPayload;
    }
  } catch {}
  return null;
}

export function ChatPage() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [approvingIndex, setApprovingIndex] = useState<number | null>(null);
  const sessionId = useRef(`session-${Date.now()}`).current;
  const chatMutation = useAgentChatMutation();

  const handleSend = useCallback(() => {
    const text = input.trim();
    if (!text || chatMutation.isPending) return;

    setMessages((prev) => [...prev, { role: "user", content: text }]);
    setInput("");

    chatMutation.mutate(
      { sessionId, message: text },
      {
        onSuccess: (data) => {
          const approval = tryParseApproval(data.reply);

          if (approval) {
            setMessages((prev) => [
              ...prev,
              {
                role: "assistant",
                content: `Approval required for ${approval.toolName}`,
                requiresApproval: true,
                toolName: approval.toolName,
                arguments: approval.arguments,
              },
            ]);
          } else {
            setMessages((prev) => [
              ...prev,
              {
                role: "assistant",
                content: data.reply,
                tools: data.toolsUsed,
              },
            ]);
          }
        },
        onError: (error) => {
          setMessages((prev) => [
            ...prev,
            { role: "assistant", content: `Error: ${error.message}` },
          ]);
        },
      },
    );
  }, [input, sessionId, chatMutation]);

  const handleApprove = useCallback(
    async (index: number) => {
      const msg = messages[index];
      if (!msg.toolName || !msg.arguments) return;

      setApprovingIndex(index);
      try {
        const tokenResponse = await fetch("/api/auth/agent-token", {
          method: "POST",
          credentials: "include",
        });

        if (!tokenResponse.ok) {
          throw new Error(`Failed to get agent token: ${tokenResponse.status}`);
        }

        const { accessToken } = (await tokenResponse.json()) as {
          accessToken: string;
        };

        const agentUrl =
          import.meta.env.VITE_AGENT_URL ?? "http://localhost:5003";
        const response = await fetch(`${agentUrl}/api/agent/approve`, {
          method: "POST",
          credentials: "include",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${accessToken}`,
          },
          body: JSON.stringify({
            toolName: msg.toolName,
            arguments: msg.arguments,
          }),
        });

        if (!response.ok) {
          const errorText = await response.text();
          throw new Error(`API error: ${response.status} - ${errorText}`);
        }

        const result = (await response.json()) as { result: string };

        setMessages((prev) => {
          const updated = [...prev];
          updated[index] = { ...msg, requiresApproval: false, approved: true };
          updated.push({
            role: "assistant",
            content: result.result || "Done",
          });
          return updated;
        });
      } catch (err) {
        setMessages((prev) => {
          const updated = [...prev];
          updated[index] = { ...msg, requiresApproval: false, approved: false };
          updated.push({
            role: "assistant",
            content: `Approval failed: ${(err as Error).message}`,
          });
          return updated;
        });
      } finally {
        setApprovingIndex(null);
      }
    },
    [messages],
  );

  const handleReject = useCallback((index: number) => {
    setMessages((prev) => {
      const updated = [...prev];
      updated[index] = {
        ...updated[index],
        requiresApproval: false,
        approved: false,
      };
      updated.push({ role: "assistant", content: "Cancelled." });
      return updated;
    });
  }, []);

  return (
    <Container>
      <Messages>
        {messages.map((msg, i) => (
          <div key={i}>
            {msg.requiresApproval ? (
              <ApprovalBox>
                <ApprovalTitle>Approval Required</ApprovalTitle>
                <ApprovalRow>
                  <strong>Tool:</strong> {msg.toolName}
                </ApprovalRow>
                <ApprovalActions>
                  <ApproveButton
                    onClick={() => handleApprove(i)}
                    disabled={approvingIndex === i}
                  >
                    {approvingIndex === i ? "Approving..." : "Approve"}
                  </ApproveButton>
                  <RejectButton
                    onClick={() => handleReject(i)}
                    disabled={approvingIndex === i}
                  >
                    Reject
                  </RejectButton>
                </ApprovalActions>
              </ApprovalBox>
            ) : (
              <Bubble $role={msg.role}>{stripIds(msg.content)}</Bubble>
            )}

            {msg.tools && msg.tools.length > 0 && (
              <ToolsBadge>Tools used: {msg.tools.join(", ")}</ToolsBadge>
            )}
          </div>
        ))}

        {chatMutation.isPending && (
          <Bubble $role="assistant">Thinking...</Bubble>
        )}
      </Messages>

      <InputRow>
        <Input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleSend()}
          placeholder="Ask the agent..."
          disabled={chatMutation.isPending}
        />
        <SendButton
          onClick={handleSend}
          disabled={chatMutation.isPending || !input.trim()}
        >
          Send
        </SendButton>
      </InputRow>
    </Container>
  );
}
