import styled from "styled-components";
import type { DocumentSummary } from "../lib/types";

const ResultItem = styled.li`
  padding: 16px;
  border-bottom: 1px solid #666;
`;

const ResultTitle = styled.h3`
  margin: 0 0 8px;
  font-size: 1.5rem;
  color: white;
`;

const ResultPreview = styled.p`
  margin: 0;
  color: white;
  line-height: 1.5;
  overflow-wrap: anywhere;
`;

interface DocumentItemProps {
  document: DocumentSummary;
}

export function DocumentItem({ document }: DocumentItemProps) {
  console.log("document", document);
  return (
    <ResultItem>
      <ResultTitle> {document.title}</ResultTitle>
      <ResultPreview> {document.content}</ResultPreview>
    </ResultItem>
  );
}
