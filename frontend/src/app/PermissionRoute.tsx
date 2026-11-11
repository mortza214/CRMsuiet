import { Button, Result } from "antd";
import type { ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";

interface PermissionRouteProps {
  permission: string;
  children: ReactNode;
}

export function PermissionRoute({
  permission,
  children,
}: PermissionRouteProps) {
  const navigate = useNavigate();
  const hasPermission = useAuthStore((state) => state.hasPermission);

  if (!hasPermission(permission)) {
    return (
      <Result
        status="403"
        title="دسترسی غیرمجاز"
        subTitle="شما اجازه مشاهده این بخش را ندارید."
        extra={
          <Button type="primary" onClick={() => navigate("/dashboard")}>
            بازگشت به داشبورد
          </Button>
        }
      />
    );
  }

  return <>{children}</>;
}
