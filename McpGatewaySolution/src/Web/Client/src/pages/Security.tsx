import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import styled from "styled-components";
import { apiFetch } from "../lib/apiClient";
import { CopyableText } from "../components/ui/CopyableText";

interface ToolInvocationRecord {
  subjectId: string;
  toolName: string;
  downstreamService: string;
  outcome: string;
  timestamp: string;
  durationMs: number;
  traceId?: string;
  spanId?: string;
  serviceName?: string;
  environment?: string;
  userEmail?: string;
  toolScope?: string;
  errorMessage?: string;
}

interface PagedAuditResult {
  items: ToolInvocationRecord[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

const Container = styled.div`
  width: 100%;
  max-width: 100%;
  margin: 0px auto;
  padding: 0 24px 80px;
  position: relative;
  min-height: calc(100vh - 120px);
  box-sizing: border-box;
`;

const Title = styled.h1`
  color: #f39c12;
  font-size: 2rem;
  font-weight: 700;
  margin-bottom: 24px;
`;

const StatsRow = styled.div`
  display: flex;
  gap: 16px;
  margin-bottom: 24px;
  justify-content: center;
  flex-wrap: wrap;
`;

const StatCard = styled.div<{ $color: string }>`
  padding: 12px 20px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  min-width: 120px;
`;

const StatValue = styled.div`
  font-size: 1.5rem;
  font-weight: 700;
  color: #333;
`;

const StatLabel = styled.div`
  font-size: 0.75rem;
  color: #888;
  margin-top: 2px;
`;

const Filters = styled.div`
  display: flex;
  gap: 12px;
  margin-bottom: 24px;
  flex-wrap: wrap;
  justify-content: center;
  align-items: center;
`;

const FilterInput = styled.input`
  padding: 8px 12px;
  border: 1px solid #ccc;
  border-radius: 8px;
  font-size: 0.9rem;
  min-width: 200px;
  flex: 1 1 200px;
  max-width: 320px;
`;

const FilterSelect = styled.select`
  padding: 8px 12px;
  border: 1px solid #ccc;
  border-radius: 8px;
  font-size: 0.9rem;
  min-width: 160px;
  flex: 0 1 160px;
`;

const ClearButton = styled.button`
  padding: 8px 16px;
  background: transparent;
  border: 1px solid #f39c12;
  color: #f39c12;
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.9rem;
  flex: 0 0 auto;

  &:hover {
    background: #f39c12;
    color: white;
  }
`;

const TableWrapper = styled.div`
  width: 100%;
  overflow-x: auto;
  margin-top: 2rem;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  -webkit-overflow-scrolling: touch;
`;

const Table = styled.table`
  width: 100%;
  min-width: 0;
  border-collapse: collapse;
  font-size: 0.8rem;
  table-layout: fixed;
`;

const Th = styled.th`
  background: #333;
  color: white;
  padding: 10px 6px;
  text-align: left;
  font-size: 0.75rem;
  font-weight: 600;
  white-space: normal;
  overflow-wrap: anywhere;
  position: sticky;
  top: 0;
  z-index: 1;

  &:nth-child(1) {
    width: 11%;
  }
  &:nth-child(2) {
    width: 6%;
  }
  &:nth-child(3) {
    width: 10%;
  }
  &:nth-child(4) {
    width: 10%;
  }
  &:nth-child(5) {
    width: 6%;
  }
  &:nth-child(6) {
    width: 12%;
  }
  &:nth-child(7) {
    width: 10%;
  }
  &:nth-child(8) {
    width: 7%;
  }
  &:nth-child(9) {
    width: 5%;
  }
  &:nth-child(10) {
    width: 14%;
  }
  &:nth-child(11) {
    width: 6%;
  }
  &:nth-child(12) {
    width: 6%;
  }
`;

const Td = styled.td`
  padding: 10px 4px;
  color: #aea4a4;
  white-space: normal;
  overflow-wrap: anywhere;
  word-break: break-word;
  text-align: left;
  vertical-align: top;
`;

const TruncatedText = styled.span`
  display: inline-block;
  max-width: 180px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  vertical-align: middle;
`;

const Badge = styled.span<{ $outcome: string }>`
  padding: 3px 8px;
  border-radius: 6px;
  font-size: 0.7rem;
  font-weight: 600;
  background: ${({ $outcome }) =>
    $outcome === "success"
      ? "#d4edda"
      : $outcome === "denied"
        ? "#f8d7da"
        : $outcome === "error"
          ? "#fff3cd"
          : "#e2e3e5"};
  color: ${({ $outcome }) =>
    $outcome === "success"
      ? "#155724"
      : $outcome === "denied"
        ? "#721c24"
        : $outcome === "error"
          ? "#856404"
          : "#383d41"};
`;

const ScopeBadge = styled.span<{ $scope: string }>`
  padding: 3px 8px;
  border-radius: 6px;
  font-size: 0.7rem;
  font-weight: 600;
  background: ${({ $scope }) =>
    $scope === "Read"
      ? "#d1ecf1"
      : $scope === "Write"
        ? "#fff3cd"
        : $scope === "Admin"
          ? "#f8d7da"
          : "#e2e3e5"};
  color: ${({ $scope }) =>
    $scope === "Read"
      ? "#0c5460"
      : $scope === "Write"
        ? "#856404"
        : $scope === "Admin"
          ? "#721c24"
          : "#383d41"};
`;

const EnvBadge = styled.span<{ $env: string }>`
  padding: 3px 8px;
  border-radius: 6px;
  font-size: 0.7rem;
  font-weight: 600;
  background: ${({ $env }) =>
    $env === "Development"
      ? "#d1ecf1"
      : $env === "Production"
        ? "#d4edda"
        : "#e2e3e5"};
  color: ${({ $env }) =>
    $env === "Development"
      ? "#0c5460"
      : $env === "Production"
        ? "#155724"
        : "#383d41"};
`;

const ErrorText = styled.span`
  display: inline-block;
  max-width: 180px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  vertical-align: middle;
  color: #e74c3c;
  font-size: 0.75rem;
  font-weight: 500;
`;

const Pagination = styled.div`
  position: static;
  transform: none;
  display: flex;
  justify-content: center;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  width: 100%;
  max-width: 100%;
  padding: 12px 16px;
  box-sizing: border-box;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  z-index: auto;
  overflow-x: visible;
`;

const PagerInfo = styled.div`
  position: static;
  transform: none;
  width: fit-content;
  max-width: 100%;
  margin: 1px auto 0px;
  text-align: center;
  font-size: 0.8rem;
  color: #888;
  padding: 4px 12px;
  border-radius: 999px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
`;

const PageButton = styled.button<{ $active?: boolean }>`
  width: 36px;
  height: 36px;
  border-radius: 50%;
  border: none;
  cursor: pointer;
  font-weight: 600;
  font-size: 0.85rem;
  flex: 0 0 auto;
  background: ${({ $active }) => ($active ? "#f39c12" : "#f0f0f0")};
  color: ${({ $active }) => ($active ? "white" : "#333")};
  transition: all 0.2s;

  &:hover:not(:disabled) {
    background: ${({ $active }) => ($active ? "#e67e22" : "#e0e0e0")};
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  @media (max-width: 600px) {
    width: 32px;
    height: 32px;
    font-size: 0.75rem;
  }
`;

const EmptyState = styled.p`
  text-align: center;
  color: #888;
  padding: 40px;
  font-style: italic;
`;

const Loading = styled.p`
  text-align: center;
  color: #888;
  padding: 40px;
`;

function getPageNumbers(current: number, total: number): number[] {
  const pages: number[] = [];
  const maxVisible = 10;

  if (total <= maxVisible) {
    for (let i = 1; i <= total; i++) pages.push(i);
    return pages;
  }

  pages.push(1);

  const start = Math.max(2, current - 2);
  const end = Math.min(total - 1, current + 2);

  if (start > 2) pages.push(-1);

  for (let i = start; i <= end; i++) pages.push(i);

  if (end < total - 1) pages.push(-2);

  pages.push(total);
  return pages;
}

export function Security() {
  const [page, setPage] = useState(1);
  const [outcome, setOutcome] = useState("");
  const [toolName, setToolName] = useState("");
  const [search, setSearch] = useState("");

  const queryParams = new URLSearchParams({
    page: String(page),
    pageSize: "12",
  });
  if (outcome) queryParams.set("outcome", outcome);
  if (toolName) queryParams.set("toolName", toolName);
  if (search) queryParams.set("search", search);

  const { data, isLoading } = useQuery<PagedAuditResult>({
    queryKey: ["security", "audit-logs", page, outcome, toolName, search],
    queryFn: () =>
      apiFetch<PagedAuditResult>(`/api/v1/security/audit-logs?${queryParams}`),
    placeholderData: keepPreviousData,
    refetchInterval: 10000,
  });

  if (isLoading && !data)
    return (
      <Container>
        <Loading>Loading...</Loading>
      </Container>
    );

  const logs = data?.items ?? [];
  const totalPages = data?.totalPages ?? 0;
  const totalCount = data?.totalCount ?? 0;

  const successCount = logs.filter((l) => l.outcome === "success").length;
  const deniedCount = logs.filter((l) => l.outcome === "denied").length;
  const errorCount = logs.filter((l) => l.outcome === "error").length;

  const clearFilters = () => {
    setOutcome("");
    setToolName("");
    setSearch("");
    setPage(1);
  };

  return (
    <Container>
      <Title>Security Audit Logs</Title>

      <StatsRow>
        <StatCard $color="#27ae60">
          <StatValue>{successCount}</StatValue>
          <StatLabel>Successful</StatLabel>
        </StatCard>
        <StatCard $color="#e74c3c">
          <StatValue>{deniedCount}</StatValue>
          <StatLabel>Denied</StatLabel>
        </StatCard>
        <StatCard $color="#f39c12">
          <StatValue>{errorCount}</StatValue>
          <StatLabel>Errors</StatLabel>
        </StatCard>
        <StatCard $color="#3498db">
          <StatValue>{totalCount}</StatValue>
          <StatLabel>Total</StatLabel>
        </StatCard>
      </StatsRow>

      <Filters>
        <FilterInput
          placeholder="Search (tool, email, trace, span)..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
        <FilterInput
          placeholder="Tool name..."
          value={toolName}
          onChange={(e) => {
            setToolName(e.target.value);
            setPage(1);
          }}
        />
        <FilterSelect
          value={outcome}
          onChange={(e) => {
            setOutcome(e.target.value);
            setPage(1);
          }}
        >
          <option value="">All outcomes</option>
          <option value="success">Success</option>
          <option value="denied">Denied</option>
          <option value="error">Error</option>
        </FilterSelect>
        <ClearButton onClick={clearFilters}>Clear</ClearButton>
      </Filters>
      {totalPages > 1 && (
        <>
          <PagerInfo>
            Page {page} of {totalPages} ({totalCount} total)
          </PagerInfo>
          <Pagination>
            <PageButton
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
            >
              ‹
            </PageButton>

            {getPageNumbers(page, totalPages).map((p, i) =>
              p < 0 ? (
                <PageButton key={`ellipsis-${i}`} disabled>
                  ...
                </PageButton>
              ) : (
                <PageButton
                  key={p}
                  $active={p === page}
                  onClick={() => setPage(p)}
                >
                  {p}
                </PageButton>
              ),
            )}

            <PageButton
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
            >
              ›
            </PageButton>
          </Pagination>
        </>
      )}
      {logs.length === 0 ? (
        <EmptyState>No audit logs found.</EmptyState>
      ) : (
        <TableWrapper>
          <Table>
            <thead>
              <tr>
                <Th>Time</Th>
                <Th>User</Th>
                <Th>Email</Th>
                <Th>Tool</Th>
                <Th>Scope</Th>
                <Th>Service</Th>
                <Th>Env</Th>
                <Th>Outcome</Th>
                <Th>Duration</Th>
                <Th>Error</Th>
                <Th>Trace ID</Th>
                <Th>Span ID</Th>
              </tr>
            </thead>
            <tbody>
              {logs.map((log, i) => (
                <tr key={i}>
                  <Td>{new Date(log.timestamp).toLocaleString()}</Td>
                  <Td>
                    {log.spanId ? (
                      <CopyableText
                        text={log.spanId}
                        displayText={`${log.subjectId.slice(0, 10)}...`}
                        copyLabel="Click to copy User Id"
                        copiedLabel="Copied!"
                        fontSize="0.7rem"
                        color="#edac33"
                        hoverColor="#e98f29"
                      />
                    ) : (
                      "—"
                    )}
                  </Td>
                  <Td>
                    {log.userEmail ? (
                      <CopyableText
                        text={log.userEmail}
                        copyLabel="Click to copy email"
                        copiedLabel="Copied!"
                        fontSize="0.8rem"
                        color="#333"
                        hoverColor="#f39c12"
                      />
                    ) : (
                      "—"
                    )}
                  </Td>
                  <Td>
                    <TruncatedText title={log.toolName}>
                      {log.toolName}
                    </TruncatedText>
                  </Td>
                  <Td>
                    {log.toolScope ? (
                      <ScopeBadge $scope={log.toolScope}>
                        {log.toolScope}
                      </ScopeBadge>
                    ) : (
                      "—"
                    )}
                  </Td>
                  <Td>
                    <TruncatedText title={log.downstreamService}>
                      {log.downstreamService}
                    </TruncatedText>
                  </Td>
                  <Td>
                    {log.environment ? (
                      <EnvBadge $env={log.environment}>
                        {log.environment}
                      </EnvBadge>
                    ) : (
                      "—"
                    )}
                  </Td>
                  <Td>
                    <Badge $outcome={log.outcome}>{log.outcome}</Badge>
                  </Td>
                  <Td>{log.durationMs}ms</Td>
                  <Td>
                    {log.errorMessage ? (
                      <CopyableText
                        text={log.errorMessage}
                        displayText={
                          <ErrorText>
                            {log.errorMessage.slice(0, 40)}
                            {log.errorMessage.length > 40 ? "..." : ""}
                          </ErrorText>
                        }
                        copyLabel="Click to copy error"
                        copiedLabel="Copied!"
                        fontSize="0.75rem"
                        color="#e74c3c"
                        hoverColor="#c0392b"
                      />
                    ) : (
                      "—"
                    )}
                  </Td>
                  <Td>
                    {log.traceId ? (
                      <CopyableText
                        text={log.traceId}
                        displayText={`${log.traceId.slice(0, 10)}...`}
                        copyLabel="Click to copy trace ID"
                        copiedLabel="Copied!"
                        fontSize="0.7rem"
                        color="#3498db"
                        hoverColor="#2980b9"
                      />
                    ) : (
                      "—"
                    )}
                  </Td>
                  <Td>
                    {log.spanId ? (
                      <CopyableText
                        text={log.spanId}
                        displayText={`${log.spanId.slice(0, 10)}...`}
                        copyLabel="Click to copy span ID"
                        copiedLabel="Copied!"
                        fontSize="0.7rem"
                        color="#9b59b6"
                        hoverColor="#8e44ad"
                      />
                    ) : (
                      "—"
                    )}
                  </Td>
                </tr>
              ))}
            </tbody>
          </Table>
        </TableWrapper>
      )}
    </Container>
  );
}
