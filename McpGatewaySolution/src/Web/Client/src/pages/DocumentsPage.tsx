import { useCallback, useDeferredValue, useState } from "react";
import styled from "styled-components";
import { useDocumentSearchQuery } from "../hooks/useDocuments";
import type { DocumentSummary } from "../lib/types";

const Container = styled.div`
  max-width: 800px;
  margin: 40px auto;
  padding: 0 16px;
`;

const SearchInput = styled.input`
  box-sizing: border-box;
  width: 100%;
  padding: 10px;
  margin-bottom: 16px;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  font-size: 1rem;
  color: #333;
  background: #fff;

  &:focus {
    outline: 2px solid #3498db;
    outline-offset: 2px;
  }
`;

const ResultList = styled.ul`
  list-style: none;
  padding: 0;
  margin: 0;
`;

const ResultItem = styled.li`
  padding: 16px;
  border-bottom: 1px solid #e0e0e0;
`;

const ResultTitle = styled.h3`
  margin: 0 0 8px;
  font-size: 1.5rem;
  color: #333;
`;

const ResultPreview = styled.p`
  margin: 0;
  color: #666;
  line-height: 1.5;
  overflow-wrap: anywhere;
`;

const EmptyState = styled.p`
  color: #888;
  font-style: italic;
`;

const ErrorMessage = styled.p`
  color: #e74c3c;
`;

interface DocumentItemProps {
  document: DocumentSummary;
}

function DocumentItem({ document }: DocumentItemProps) {
  return (
    <ResultItem>
      <ResultTitle>{document.title}</ResultTitle>
      <ResultPreview>{document.contentPreview}</ResultPreview>
    </ResultItem>
  );
}

export function DocumentsPage() {
  const [keyword, setKeyword] = useState("");

  const deferredKeyword = useDeferredValue(keyword);

  const {
    data: documents,
    isLoading,
    error,
  } = useDocumentSearchQuery(deferredKeyword);

  const handleChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setKeyword(e.target.value);
  }, []);

  const renderResults = () => {
    if (isLoading) {
      return <EmptyState>Searching...</EmptyState>;
    }

    if (error) {
      return <ErrorMessage>Search failed.</ErrorMessage>;
    }

    if (!documents || documents.length === 0) {
      return deferredKeyword.trim().length > 0 ? (
        <EmptyState>No documents found.</EmptyState>
      ) : (
        <EmptyState>Start typing to search documents.</EmptyState>
      );
    }

    return (
      <ResultList>
        {documents.map((doc) => (
          <DocumentItem key={doc.id} document={doc} />
        ))}
      </ResultList>
    );
  };

  return (
    <Container>
      <h1>Docs</h1>

      <SearchInput
        type="search"
        placeholder="Search documents..."
        value={keyword}
        onChange={handleChange}
        aria-label="Search documents"
      />

      {renderResults()}
    </Container>
  );
}
