import { useCallback, useState, type ReactNode } from "react";
import styled from "styled-components";
import { Tooltip, type TooltipPosition } from "./Tooltip";

interface CopyableTextProps {
  text: string;
  displayText?: ReactNode;
  copyLabel?: string;
  copiedLabel?: string;
  position?: TooltipPosition;
  tooltipBackground?: string;
  copiedBackground?: string;
  fontSize?: string;
  color?: string;
  hoverColor?: string;
  copyTimeout?: number;
}

const ClickableText = styled.span<{
  $fontSize: string;
  $color: string;
  $hoverColor: string;
}>`
  font-size: ${({ $fontSize }) => $fontSize};
  color: ${({ $color }) => $color};
  cursor: pointer;
  user-select: none;
  transition: color 0.15s;

  &:hover {
    color: ${({ $hoverColor }) => $hoverColor};
    text-decoration: underline;
  }
`;

export function CopyableText({
  text,
  displayText,
  copyLabel = "Click to copy",
  copiedLabel = "Copied!",
  position = "top",
  tooltipBackground = "#f35212",
  copiedBackground = "#27ae60",
  fontSize = "0.5rem",
  color = "#3498db",
  hoverColor = "#2980b9",
  copyTimeout = 2000,
}: CopyableTextProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(text);
    } catch {
      const textarea = document.createElement("textarea");
      textarea.value = text;
      document.body.appendChild(textarea);
      textarea.select();
      document.execCommand("copy");
      document.body.removeChild(textarea);
    }

    setCopied(true);
    setTimeout(() => setCopied(false), copyTimeout);
  }, [text, copyTimeout]);

  return (
    <Tooltip
      content={copied ? copiedLabel : copyLabel}
      position={position}
      background={copied ? copiedBackground : tooltipBackground}
    >
      <ClickableText
        $fontSize={fontSize}
        $color={color}
        $hoverColor={hoverColor}
        onClick={handleCopy}
      >
        {displayText ?? text}
      </ClickableText>
    </Tooltip>
  );
}
