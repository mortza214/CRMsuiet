import { Button, Result } from "antd";
import { isRouteErrorResponse, useNavigate, useRouteError } from "react-router-dom";

export function RouteError() {
  const error = useRouteError();
  const navigate = useNavigate();

  const message = isRouteErrorResponse(error)
    ? `${error.status} — ${error.statusText}`
    : "مشکلی در نمایش این صفحه رخ داد.";

  return (
    <Result
      status="error"
      title="خطا"
      subTitle={message}
      extra={
        <Button type="primary" onClick={() => navigate("/")}>
          بازگشت به صفحه اصلی
        </Button>
      }
    />
  );
}
