import { AppProviders } from "@/app/providers";
import { AppRouter } from "@/app/router";
import { NotificationProvider } from "@/shared/api/NotificationProvider";

export function App() {
  return (
    <AppProviders>
      <NotificationProvider>
        <AppRouter />
      </NotificationProvider>
    </AppProviders>
  );
}
