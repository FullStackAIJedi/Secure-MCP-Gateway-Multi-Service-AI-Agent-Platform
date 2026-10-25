export const queryKeys = {
  auth: {
    me: ["auth", "me"] as const,
  },
  tasks: {
    all: ["tasks"] as const,
    list: () => [...queryKeys.tasks.all, "list"] as const,
    detail: (id: number) => [...queryKeys.tasks.all, "detail", id] as const,
  },
  documents: {
    all: ["documents"] as const,
    search: (keyword: string) =>
      [...queryKeys.documents.all, "search", keyword] as const,
  },
} as const;
