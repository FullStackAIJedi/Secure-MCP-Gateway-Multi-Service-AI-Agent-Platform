import { useCallback, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import styled from "styled-components";
import { useRegisterMutation } from "../hooks/useAuth";
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

  &:hover {
    text-decoration: underline;
    text-decoration-color: white;
    text-underline-offset: 10px;
  }
`;

export function SignUp() {
  const navigate = useNavigate();
  const registerMutation = useRegisterMutation();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      setValidationError(null);

      if (password.length < 6) {
        setValidationError("Password must be at least 6 characters");
        return;
      }

      if (password !== confirmPassword) {
        setValidationError("Passwords do not match");
        return;
      }

      registerMutation.mutate(
        { email, password },
        { onSuccess: () => navigate("/tasks") },
      );
    },
    [email, password, confirmPassword, registerMutation, navigate],
  );

  const errorMessage =
    validationError ??
    (registerMutation.error ? registerMutation.error.message : null);

  return (
    <Container>
      <Title>Sign Up</Title>

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
          placeholder="Password min 6 chars"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          minLength={6}
          autoComplete="new-password"
        />

        <Input
          type="password"
          placeholder="Confirm password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          required
          autoComplete="new-password"
        />

        {errorMessage && <ErrorText>{errorMessage}</ErrorText>}

        <Button disabled={registerMutation.isPending}>
          {registerMutation.isPending
            ? "Creating account..."
            : "Create account"}
        </Button>
      </Form>

      <Footer>
        Already registered?
        <StyledLink to="/login">Sign in</StyledLink>
      </Footer>
    </Container>
  );
}
