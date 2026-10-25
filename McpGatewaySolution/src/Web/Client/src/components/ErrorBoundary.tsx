import { Component, type ReactNode, type ErrorInfo } from "react";
import styled from "styled-components";

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

const Container = styled.div`
  max-width: 600px;
  margin: 80px auto;
  padding: 32px;
  background: #fff5f5;
  border: 1px solid #feb2b2;
  border-radius: 12px;
  text-align: center;
`;

const Title = styled.h1`
  color: #c53030;
  font-size: 1.5rem;
  margin-bottom: 16px;
`;

const Message = styled.p`
  color: #742a2a;
  font-size: 0.9rem;
`;

const ReloadButton = styled.button`
  margin-top: 24px;
  padding: 10px 24px;
  background: #c53030;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;

  &:hover {
    background: #9b2c2c;
  }
`;

export class ErrorBoundary extends Component<Props, State> {
  public state: State = { hasError: false };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("ErrorBoundary caught:", error, errorInfo);
  }

  public render() {
    if (this.state.hasError) {
      return (
        <Container>
          <Title>Something went wrong</Title>
          <Message>{this.state.error?.message ?? "Unknown error"}</Message>
          <ReloadButton onClick={() => window.location.reload()}>
            Reload page
          </ReloadButton>
        </Container>
      );
    }

    return this.props.children;
  }
}
