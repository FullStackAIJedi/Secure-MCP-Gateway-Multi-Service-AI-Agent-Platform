import { useCallback, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import styled from "styled-components";
import { useLoginMutation } from "../hooks/useAuth";
import { Button } from "../components/ui/Button";

const Container = styled.div`
  max-width: 400px;
  margin: 80px auto;
  padding: 32px;
  border: 1px solid black;
  border-radius: 8px;
`;

const Title = styled.h1`
  margin: 0 0 24px;
  font-size: 1.5rem;
  color: black;
`;

const Form = styled.form`
  display: flex;
  flex-direction: column;
  gap: 16px;
`;

const Input = styled.input`
  width: 100%;
  padding: 10px;
  border: 1px solid black;
  border-radius: 4px;
  font-size: 1rem;
`;

const ErrorText = styled.p`
  margin: 0;
  color: #e74c3c;
  font-size: 0.875rem;
`;

const Footer = styled.p`
  margin: 16px 0 0;
  text-align: center;
  font-size: 0.875rem;
  color: #666;

  a {
    color: black;
    text-decoration: none;
  }
`;

export function LoginPage() {
  const navigate = useNavigate();
  const loginMutation = useLoginMutation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      loginMutation.mutate(
        { email, password },
        { onSuccess: () => navigate("/tasks") },
      );
    },
    [email, password, loginMutation, navigate],
  );

  return (
    <Container>
      <Title>Sign in</Title>
      <Form onSubmit={handleSubmit}>
        <Input
          type="email"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          autoComplete="email"
        />
        <Input
          type="password"
          placeholder="Password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          autoComplete="current-password"
        />
        {loginMutation.error && (
          <ErrorText>{loginMutation.error.message}</ErrorText>
        )}
        <Button disabled={loginMutation.isPending}>
          {loginMutation.isPending ? "Signing in..." : "Sign in"}
        </Button>
      </Form>
      <Footer>
        Don't have an account? <Link to="/register">Create one</Link>
      </Footer>
    </Container>
  );
}
