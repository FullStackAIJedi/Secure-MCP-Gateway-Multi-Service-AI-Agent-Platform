import { memo } from "react";
import styled, { css } from "styled-components";

interface ButtonProps {
  variant?: "primary" | "secondary";
  size?: "sm" | "md";
  disabled?: boolean;
  onClick?: () => void;
  children: React.ReactNode;
}

interface StyledButtonProps {
  $variant: "primary" | "secondary";
  $size: "sm" | "md";
}

const StyledButton = styled.button<StyledButtonProps>`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 500;
  transition:
    background-color 0.2s,
    opacity 0.2s;

  ${({ $size }) =>
    $size === "sm"
      ? css`
          padding: 4px 12px;
          font-size: 0.875rem;
        `
      : css`
          padding: 8px 20px;
          font-size: 1rem;
        `}

  ${({ $variant }) =>
    $variant === "primary"
      ? css`
          background-color: #3498db;
          color: white;

          &:hover:not(:disabled) {
            background-color: #2980b9;
          }
        `
      : css`
          background-color: #e0e0e0;
          color: #333;

          &:hover:not(:disabled) {
            background-color: #c0c0c0;
          }
        `}

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

export const Button = memo(function Button({
  variant = "primary",
  size = "md",
  disabled = false,
  onClick,
  children,
}: ButtonProps) {
  return (
    <StyledButton
      $variant={variant}
      $size={size}
      disabled={disabled}
      onClick={onClick}
    >
      {children}
    </StyledButton>
  );
});
