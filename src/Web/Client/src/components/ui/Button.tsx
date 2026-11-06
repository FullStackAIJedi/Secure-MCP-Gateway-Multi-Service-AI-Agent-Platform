import { memo } from "react";
import styled, { css } from "styled-components";

interface ButtonProps {
  variant?: "primary" | "secondary";
  size?: "sm" | "md" | "lg" | string;
  backgroundColor?: string;
  color?: string;
  border?: string;
  hoverBackgroundColor?: string;
  hoverColor?: string;
  disabled?: boolean;
  onClick?: () => void;
  children: React.ReactNode;
}

interface StyledButtonProps {
  $variant: "primary" | "secondary";
  $size?: string;
  $backgroundColor?: string;
  $color?: string;
  $border?: string;
  $hoverBackgroundColor?: string;
  $hoverColor?: string;
}

const StyledButton = styled.button<StyledButtonProps>`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: none;
  cursor: pointer;
  font-weight: 500;
  transition:
    background-color 0.2s,
    color 0.2s,
    border-color 0.2s,
    opacity 0.2s;

  ${({ $size }) => {
    switch ($size) {
      case "sm":
        return css`
          height: 34px;
          padding: 4px 12px;
          font-size: 0.775rem;
          border-radius: 7px;
        `;
      case "md":
        return css`
          height: 44px;
          padding: 0 12px;
          font-size: 1rem;
          border-radius: 10px;
        `;
      case "lg":
        return css`
          height: 55px;
          box-sizing: border-box;
          padding: 0 24px;
          font-size: 1.125rem;
          border-radius: 12px;
        `;
      default:
        return css`
          height: 60px;
          box-sizing: border-box;
          padding: 0 20px;
          font-size: 1rem;
          border-radius: 10px;
        `;
    }
  }}

  ${({ $backgroundColor, $hoverBackgroundColor }) =>
    $backgroundColor &&
    css`
      background-color: ${$backgroundColor};

      &:hover:not(:disabled) {
        background-color: ${$hoverBackgroundColor || $backgroundColor};
      }
    `}

  ${({ $color, $hoverColor }) =>
    $color &&
    css`
      color: ${$color};

      &:hover:not(:disabled) {
        ${$hoverColor && `color: ${$hoverColor};`}
      }
    `}

  ${({ $border }) =>
    $border &&
    css`
      border: ${$border};
    `}

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

export const Button = memo(function Button({
  variant = "primary",
  size = "md",
  backgroundColor,
  color,
  border,
  hoverBackgroundColor,
  hoverColor,
  disabled = false,
  onClick,
  children,
}: ButtonProps) {
  return (
    <StyledButton
      $variant={variant}
      $size={size}
      $backgroundColor={backgroundColor}
      $color={color}
      $border={border}
      $hoverBackgroundColor={hoverBackgroundColor}
      $hoverColor={hoverColor}
      disabled={disabled}
      onClick={onClick}
    >
      {children}
    </StyledButton>
  );
});
