import { useCallback, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import styled from "styled-components";
import { useLoginMutation } from "../hooks/useAuth";
import { Button } from "../components/ui/Button";

const Container = styled.div`
  max-width: 300px;
  margin: 150px auto;
`;

const Title = styled.h1`
  margin: 0 0 24px;
  font-size: 1.5rem;
  color: white;
`;

const Form = styled.form`
  display: flex;
  flex-direction: column;
  gap: 16px;
`;

const Input = styled.input`
  padding: 10px;
  border: 1px solid black;
  border-radius: 9px;
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
  color: white;
`;

const StyledLink = styled(Link)`
  color: white;
  font-weight: 500;
  padding-left: 7px;
  text-decoration: none;
  transition: color 0.2s ease;

  &:hover {
    text-decoration: underline;
    text-decoration-color: white;
    text-underline-offset: 10px;
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
        First time here? <StyledLink to="/register">Sign Up</StyledLink>
      </Footer>
    </Container>
  );
}
