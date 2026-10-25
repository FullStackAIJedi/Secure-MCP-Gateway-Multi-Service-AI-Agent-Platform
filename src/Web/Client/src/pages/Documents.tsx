import { useCallback, useDeferredValue, useState } from "react";
import styled from "styled-components";
import { useDocumentSearchQuery } from "../hooks/useDocuments";
import { DocumentItem } from "../components/DocumentItem";

const Container = styled.div`
  max-width: 800px;
  margin: 40px auto;
  padding: 0 16px;
`;

const SearchContainer = styled.div`
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 16px;
`;

const SearchInput = styled.input`
  flex: 1;
  min-width: 0;
  height: 44px;
  box-sizing: border-box;
  padding: 0 12px;
  border: 1px solid #ccc;
  border-radius: 10px;
  font-size: 16px;
`;

const DocumentList = styled.ul`
  padding: 0;
  list-style: none;
`;

export function Documents() {
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

  if (isLoading) {
    return <Container>Loading documents...</Container>;
  }

  if (error) {
    return <Container>Error loading documents.</Container>;
  }

  return (
    <Container>
      <h1>Docs</h1>

      <SearchContainer>
        <SearchInput
          type="search"
          placeholder="Search documents..."
          value={keyword}
          onChange={handleChange}
          aria-label="Search documents"
        />
      </SearchContainer>

      {documents && documents.length > 0 ? (
        <DocumentList>
          {documents.map((doc) => (
            <DocumentItem key={doc.id} document={doc} />
          ))}
        </DocumentList>
      ) : deferredKeyword.trim().length > 0 ? (
        <p>No documents found.</p>
      ) : (
        <p>Start typing to search documents.</p>
      )}
    </Container>
  );
}
