import { createBrowserRouter, Navigate, RouterProvider } from "react-router-dom";
import { MainLayout } from "@/app/layouts/MainLayout";
import { ProtectedRoute } from "@/app/ProtectedRoute";
import { PermissionRoute } from "@/app/PermissionRoute";
import { RouteError } from "@/app/RouteError";
import { Dashboard } from "@/modules/dashboard/pages/Dashboard";
import { Login } from "@/modules/auth/pages/Login";
import { CustomerDetail } from "@/modules/customers/pages/CustomerDetail";
import { CustomerList } from "@/modules/customers/pages/CustomerList";
import { CreateOrder } from "@/modules/orders/pages/CreateOrder";
import { NewServiceRequest } from "@/modules/orders/pages/NewServiceRequest";
import { OrderDetail } from "@/modules/orders/pages/OrderDetail";
import { OrderList } from "@/modules/orders/pages/OrderList";
import { InvoiceDetail } from "@/modules/billing/pages/InvoiceDetail";
import { InvoiceList } from "@/modules/billing/pages/InvoiceList";
import { TransactionList } from "@/modules/payments/pages/TransactionList";
import { MyWallet } from "@/modules/wallet/pages/MyWallet";
import { CommissionList } from "@/modules/commission/pages/CommissionList";
import { MyCommissions } from "@/modules/commission/pages/MyCommissions";
import { TicketDetail } from "@/modules/tickets/pages/TicketDetail";
import { TicketList } from "@/modules/tickets/pages/TicketList";
import { ReportDashboard } from "@/modules/reports/pages/ReportDashboard";
import { SettingsPage } from "@/modules/settings/pages/SettingsPage";
import { CatalogAddons } from "@/modules/catalog/pages/CatalogAddons";
import { CatalogPrices } from "@/modules/catalog/pages/CatalogPrices";
import { CatalogServices } from "@/modules/catalog/pages/CatalogServices";
import { ServiceEditor } from "@/modules/catalog/pages/ServiceEditor";
import { FormBuilder } from "@/modules/forms/pages/FormBuilder";
import { FormDefinitions } from "@/modules/forms/pages/FormDefinitions";
import { FormSubmissions } from "@/modules/forms/pages/FormSubmissions";
import { WorkflowDefinitions } from "@/modules/workflow/pages/WorkflowDefinitions";
import { WorkflowEditor } from "@/modules/workflow/pages/WorkflowEditor";
import { RequestList } from "@/modules/workflow/pages/RequestList";
import { WorkflowInbox } from "@/modules/workflow/pages/WorkflowInbox";
import { WorkflowInstance } from "@/modules/workflow/pages/WorkflowInstance";
import { PortalProtectedRoute } from "@/modules/portal/ProtectedRoute";
import { PortalLayout } from "@/modules/portal/layouts/PortalLayout";
import { PortalLogin } from "@/modules/portal/pages/PortalLogin";
import { PortalDashboard } from "@/modules/portal/pages/PortalDashboard";
import { PortalOrders } from "@/modules/portal/pages/PortalOrders";
import { PortalInvoices } from "@/modules/portal/pages/PortalInvoices";
import { PortalTickets } from "@/modules/portal/pages/PortalTickets";
import { PortalTicketDetail } from "@/modules/portal/pages/PortalTicketDetail";
import { PortalWallet } from "@/modules/portal/pages/PortalWallet";
import { PortalProfile } from "@/modules/portal/pages/PortalProfile";
import { PortalServices } from "@/modules/portal/pages/PortalServices";

const router = createBrowserRouter([
  {
    path: "/login",
    element: <Login />,
    errorElement: <RouteError />,
  },
  {
    path: "/portal/login",
    element: <PortalLogin />,
    errorElement: <RouteError />,
  },
  {
    path: "/portal",
    element: <PortalProtectedRoute />,
    errorElement: <RouteError />,
    children: [
      {
        path: "",
        element: <PortalLayout />,
        children: [
          { index: true, element: <Navigate to="/portal/dashboard" replace /> },
          { path: "dashboard", element: <PortalDashboard /> },
          { path: "orders", element: <PortalOrders /> },
          { path: "services", element: <PortalServices /> },
          { path: "invoices", element: <PortalInvoices /> },
          { path: "tickets", element: <PortalTickets /> },
          { path: "tickets/:id", element: <PortalTicketDetail /> },
          { path: "wallet", element: <PortalWallet /> },
          { path: "profile", element: <PortalProfile /> },
        ],
      },
    ],
  },
  {
    path: "/",
    element: <ProtectedRoute />,
    errorElement: <RouteError />,
    children: [
      {
        path: "",
        element: <MainLayout />,
        children: [
          { index: true, element: <Navigate to="/dashboard" replace /> },
          { path: "dashboard", element: <Dashboard /> },
          { path: "customers", element: <CustomerList /> },
          { path: "customers/:id", element: <CustomerDetail /> },
          {
            path: "customers/:id/new-service",
            element: (
              <PermissionRoute permission="Orders.Order.Create">
                <NewServiceRequest />
              </PermissionRoute>
            ),
          },
          { path: "orders", element: <OrderList /> },
          { path: "orders/create", element: <CreateOrder /> },
          { path: "orders/:id", element: <OrderDetail /> },
          { path: "sales", element: <Navigate to="/orders" replace /> },
          { path: "invoices", element: <InvoiceList /> },
          { path: "invoices/:id", element: <InvoiceDetail /> },
          { path: "payments", element: <TransactionList /> },
          { path: "wallet", element: <MyWallet /> },
          {
            path: "commissions",
            element: (
              <PermissionRoute permission="Commission.Entry.Approve">
                <CommissionList />
              </PermissionRoute>
            ),
          },
          {
            path: "my-commissions",
            element: (
              <PermissionRoute permission="Commission.Entry.Read">
                <MyCommissions />
              </PermissionRoute>
            ),
          },
          { path: "tickets", element: <TicketList /> },
          { path: "tickets/:id", element: <TicketDetail /> },
          {
            path: "reports",
            element: (
              <PermissionRoute permission="Reports.Financial.Read">
                <ReportDashboard />
              </PermissionRoute>
            ),
          },
          {
            path: "settings",
            element: (
              <PermissionRoute permission="Settings.Company.Manage">
                <SettingsPage />
              </PermissionRoute>
            ),
          },
          {
            path: "catalog/services",
            element: (
              <PermissionRoute permission="Catalog.Service.Read">
                <CatalogServices />
              </PermissionRoute>
            ),
          },
          {
            path: "catalog/services/:id",
            element: (
              <PermissionRoute permission="Catalog.Service.Read">
                <ServiceEditor />
              </PermissionRoute>
            ),
          },
          {
            path: "catalog/prices",
            element: (
              <PermissionRoute permission="Catalog.Price.Read">
                <CatalogPrices />
              </PermissionRoute>
            ),
          },
          {
            path: "catalog/addons",
            element: (
              <PermissionRoute permission="Catalog.AddOn.Read">
                <CatalogAddons />
              </PermissionRoute>
            ),
          },
          {
            path: "forms/templates",
            element: (
              <PermissionRoute permission="Forms.Form.Read">
                <FormDefinitions />
              </PermissionRoute>
            ),
          },
          {
            path: "forms/templates/:id",
            element: (
              <PermissionRoute permission="Forms.Form.Read">
                <FormBuilder />
              </PermissionRoute>
            ),
          },
          {
            path: "forms/submissions",
            element: (
              <PermissionRoute permission="Forms.Submission.Read">
                <FormSubmissions />
              </PermissionRoute>
            ),
          },
          {
            path: "workflow/definitions",
            element: (
              <PermissionRoute permission="Workflow.Definition.Read">
                <WorkflowDefinitions />
              </PermissionRoute>
            ),
          },
          {
            path: "workflow/definitions/:id",
            element: (
              <PermissionRoute permission="Workflow.Definition.Read">
                <WorkflowEditor />
              </PermissionRoute>
            ),
          },
          {
            path: "requests",
            element: (
              <PermissionRoute permission="Orders.Order.Read">
                <RequestList />
              </PermissionRoute>
            ),
          },
          {
            path: "workflow/inbox",
            element: (
              <PermissionRoute permission="Workflow.Task.Read">
                <WorkflowInbox />
              </PermissionRoute>
            ),
          },
          {
            path: "workflow/instances/:id",
            element: (
              <PermissionRoute permission="Workflow.Instance.Read">
                <WorkflowInstance />
              </PermissionRoute>
            ),
          },
        ],
      },
    ],
  },
], {
  future: {
    v7_relativeSplatPath: true,
  },
});

export function AppRouter() {
  return (
    <RouterProvider
      router={router}
      future={{ v7_startTransition: true }}
    />
  );
}
