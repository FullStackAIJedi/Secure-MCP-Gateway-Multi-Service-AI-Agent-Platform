export interface TaskItem {
  id: number;
  title: string;
  description: string;
  isComplete: boolean;
  createdAt: string;
}

export interface DocumentSummary {
  id: number;
  title: string;
  content: string;
}

export interface AuthUser {
  subjectId: string;
  email: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  user: AuthUser;
}

export interface RegisterRequest {
  email: string;
  password: string;
}
