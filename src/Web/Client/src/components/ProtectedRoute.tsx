import { Navigate, Outlet } from "react-router-dom";
import { useCurrentUserQuery } from "../hooks/useAuth";

export function ProtectedRoute() {
  const { data: user, isLoading } = useCurrentUserQuery();

  if (isLoading) return <div>Loading...</div>;
  if (!user) return <Navigate to="/login" replace />;

  return <Outlet />;
}
