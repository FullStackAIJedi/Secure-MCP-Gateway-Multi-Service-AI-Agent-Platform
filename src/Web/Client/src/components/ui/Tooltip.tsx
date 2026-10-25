import { useCallback, useState, type ReactNode } from "react";
import styled from "styled-components";

export type TooltipPosition = "top" | "bottom" | "left" | "right";

interface TooltipProps {
  content: ReactNode;
  children: ReactNode;
  position?: TooltipPosition;
  background?: string;
  color?: string;
  fontSize?: string;
  padding?: string;
  borderRadius?: string;
  maxWidth?: string;
  minWidth?: string;
  delay?: number;
  offset?: number;
  disabled?: boolean;
}

const Wrapper = styled.span`
  position: relative;
  display: inline-block;
`;

const TooltipBubble = styled.div<{
  $position: TooltipPosition;
  $visible: boolean;
  $background: string;
  $color: string;
  $fontSize: string;
  $padding: string;
  $borderRadius: string;
  $maxWidth: string;
  $minWidth: string;
  $offset: number;
}>`
  pointer-events: none;
  position: absolute;
  z-index: 1000;
  background: ${({ $background }) => $background};
  color: ${({ $color }) => $color};
  font-size: ${({ $fontSize }) => $fontSize};
  padding: ${({ $padding }) => $padding};
  border-radius: ${({ $borderRadius }) => $borderRadius};
  max-width: ${({ $maxWidth }) => $maxWidth};
  width: max-content;
  white-space: nowrap;
  pointer-events: none;
  opacity: ${({ $visible }) => ($visible ? 1 : 0)};
  transition: opacity 0.15s ease;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);

  ${({ $position, $offset, $background }) => {
    switch ($position) {
      case "top":
        return `
          bottom: calc(100% + ${$offset}px);
          left: 50%;
          transform: translateX(-50%);
          &::after {
            content: "";
            position: absolute;
            top: 100%;
            left: 50%;
            transform: translateX(-50%);
            border: 4px solid transparent;
            border-top-color: ${$background};
          }
        `;
      case "bottom":
        return `
          top: calc(100% + ${$offset}px);
          left: 50%;
          transform: translateX(-50%);
          &::after {
            content: "";
            position: absolute;
            bottom: 100%;
            left: 50%;
            transform: translateX(-50%);
            border: 4px solid transparent;
            border-bottom-color: ${$background};
          }
        `;
      case "left":
        return `
          right: calc(100% + ${$offset}px);
          top: 50%;
          transform: translateY(-50%);
          &::after {
            content: "";
            position: absolute;
            left: 100%;
            top: 50%;
            transform: translateY(-50%);
            border: 4px solid transparent;
            border-left-color: ${$background};
          }
        `;
      case "right":
        return `
          left: calc(100% + ${$offset}px);
          top: 50%;
          transform: translateY(-50%);
          &::after {
            content: "";
            position: absolute;
            right: 100%;
            top: 50%;
            transform: translateY(-50%);
            border: 4px solid transparent;
            border-right-color: ${$background};
          }
        `;
    }
  }}
`;

export function Tooltip({
  content,
  children,
  position = "top",
  background = "#333",
  color = "#fff",
  fontSize = "0.7rem",
  padding = "3px 5px",
  borderRadius = "6px",
  maxWidth = "300px",
  minWidth = "100px",
  delay = 0,
  offset = 8,
  disabled = false,
}: TooltipProps) {
  const [visible, setVisible] = useState(false);
  const [timeoutId, setTimeoutId] = useState<number | null>(null);

  const showTooltip = useCallback(() => {
    if (disabled) return;
    if (delay > 0) {
      const id = window.setTimeout(() => setVisible(true), delay);
      setTimeoutId(id);
    } else {
      setVisible(true);
    }
  }, [delay, disabled]);

  const hideTooltip = useCallback(() => {
    if (timeoutId !== null) {
      window.clearTimeout(timeoutId);
      setTimeoutId(null);
    }
    setVisible(false);
  }, [timeoutId]);

  return (
    <Wrapper onMouseEnter={showTooltip} onMouseLeave={hideTooltip}>
      {children}
      <TooltipBubble
        $position={position}
        $visible={visible}
        $background={background}
        $color={color}
        $fontSize={fontSize}
        $padding={padding}
        $borderRadius={borderRadius}
        $maxWidth={maxWidth}
        $minWidth={minWidth}
        $offset={offset}
      >
        {content}
      </TooltipBubble>
    </Wrapper>
  );
}
