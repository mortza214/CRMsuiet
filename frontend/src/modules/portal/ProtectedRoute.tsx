import { Navigate, Outlet } from "react-router-dom";
import { usePortalAuth } from "@/modules/portal/store";

export function PortalProtectedRoute() {
  const accessToken = usePortalAuth((state) => state.accessToken);

  if (!accessToken) {
    return <Navigate to="/portal/login" replace />;
  }

  return <Outlet />;
}
