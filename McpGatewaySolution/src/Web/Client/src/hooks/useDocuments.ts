import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "../lib/apiClient";
import { queryKeys } from "../lib/queryKeys";
import type { DocumentSummary } from "../lib/types";

export function useDocumentSearchQuery(keyword: string) {
  return useQuery<DocumentSummary[]>({
    queryKey: queryKeys.documents.search(keyword),
    queryFn: () =>
      apiFetch<DocumentSummary[]>(
        `/api/documents/search?keyword=${encodeURIComponent(keyword)}`,
      ),
    enabled: keyword.trim().length > 0,
    staleTime: 60_000,
  });
}
