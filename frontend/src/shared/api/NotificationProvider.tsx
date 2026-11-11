import { useEffect, type PropsWithChildren } from "react";
import { App } from "antd";
import { setApiErrorHandler } from "@/shared/api/client";

export function NotificationProvider({ children }: PropsWithChildren) {
  const { message } = App.useApp();

  useEffect(() => {
    setApiErrorHandler((text, errors) => {
      const details = errors?.filter(Boolean).slice(0, 2).join("، ");
      message.error(details ? `${text} (${details})` : text);
    });

    return () => setApiErrorHandler(null);
  }, [message]);

  return children;
}
