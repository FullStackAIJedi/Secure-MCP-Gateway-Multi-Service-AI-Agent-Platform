import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  BrowserRouter,
  Routes,
  Route,
  Navigate,
  Outlet,
} from "react-router-dom";
import { NavBar } from "./components/NavBar";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { SignUp } from "./pages/SignUp";
import { Tasks } from "./pages/Tasks";
import { Documents } from "./pages/Documents";
import { Chat } from "./pages/Chat";
import { SignIn } from "./pages/SignIn";
import { Security } from "./pages/Security";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { Toaster } from "react-hot-toast";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

function AppLayout() {
  return (
    <>
      <NavBar />
      <Outlet />
    </>
  );
}

export default function App() {
  return (
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <Toaster position="top-right" />
          <Routes>
            <Route path="/login" element={<SignIn />} />
            <Route path="/register" element={<SignUp />} />

            <Route element={<ProtectedRoute />}>
              <Route element={<AppLayout />}>
                <Route path="/tasks" element={<Tasks />} />
                <Route path="/documents" element={<Documents />} />
                <Route path="/chat" element={<Chat />} />
                <Route path="/security" element={<Security />} />
              </Route>
            </Route>

            <Route path="*" element={<Navigate to="/tasks" replace />} />
          </Routes>
        </BrowserRouter>
      </QueryClientProvider>
    </ErrorBoundary>
  );
}
