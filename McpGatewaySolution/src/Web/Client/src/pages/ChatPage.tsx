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
  height: calc(100vh - 100px);
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
  max-width: 100%;
  padding: 12px 16px;
  border-radius: 12px;
  align-self: ${({ $role }) => ($role === "user" ? "flex-end" : "flex-start")};
  background: ${({ $role }) => ($role === "user" ? "black" : "#f0f0f0")};
  color: ${({ $role }) => ($role === "user" ? "#fff" : "#333")};
  white-space: pre-wrap;
  text-align: left;
`;

const ToolsBadge = styled.div`
  font-size: 0.65rem;
  color: #888;
  margin-top: 4px;
  text-align: left;
`;

const InputRow = styled.div`
  display: flex;
  gap: 8px;
  padding: 16px 0;
  border-top: 1px solid black;
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

const Header = styled.div`
  color: orange;
  font-size: 3rem;
  font-weight: 700;
  margin-bottom: 1.8rem;
`;

interface Message {
  role: "user" | "assistant";
  content: string;
  tools?: string[];
}

export function ChatPage() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
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
          setMessages((prev) => [
            ...prev,
            { role: "assistant", content: data.reply, tools: data.toolsUsed },
          ]);
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

  return (
    <Container>
      <Header>Chat</Header>
      <Messages>
        {messages.map((msg, i) => (
          <div key={i}>
            <Bubble $role={msg.role}>{stripIds(msg.content)}</Bubble>
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
