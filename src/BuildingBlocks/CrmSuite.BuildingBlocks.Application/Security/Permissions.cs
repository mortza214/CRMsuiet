using System.Reflection;

namespace CrmSuite.BuildingBlocks.Application.Security;

/// <summary>
/// Permission names follow Module.Entity.Action. The catalog is intentionally data-only;
/// authorization decisions live in handlers and policies.
/// </summary>
public static class Permissions
{
    public const string Wildcard = "*";

    public static class Foundation
    {
        public static class AuditLog
        {
            public const string Read = "Foundation.AuditLog.Read";
        }

        public static class Settings
        {
            public const string Read = "Foundation.Settings.Read";
            public const string Manage = "Foundation.Settings.Manage";
        }

        public static class Sequence
        {
            public const string Read = "Foundation.Sequence.Read";
            public const string Manage = "Foundation.Sequence.Manage";
        }
    }

    public static class Identity
    {
        public static class User
        {
            public const string Read = "Identity.User.Read";
            public const string Create = "Identity.User.Create";
            public const string Update = "Identity.User.Update";
            public const string Delete = "Identity.User.Delete";
            public const string ResetPassword = "Identity.User.ResetPassword";
            public const string Lock = "Identity.User.Lock";
        }

        public static class Role
        {
            public const string Read = "Identity.Role.Read";
            public const string Manage = "Identity.Role.Manage";
        }

        public static class Permission
        {
            public const string Read = "Identity.Permission.Read";
        }

        public static class Department
        {
            public const string Read = "Identity.Department.Read";
            public const string Manage = "Identity.Department.Manage";
        }
    }

    public static class Customers
    {
        public static class Customer
        {
            public const string Manage = "Customers.Customer.Manage";
            public const string Read = "Customers.Customer.Read";
            public const string Create = "Customers.Customer.Create";
            public const string Update = "Customers.Customer.Update";
            public const string Delete = "Customers.Customer.Delete";
            public const string AssignOwner = "Customers.Customer.AssignOwner";
            public const string ViewSubordinates = "Customers.Customer.ViewSubordinates";
        }

        public static class Reseller
        {
            public const string Manage = "Customers.Reseller.Manage";
        }

        public static class CustomerGroup
        {
            public const string Read = "Customers.CustomerGroup.Read";
            public const string Manage = "Customers.CustomerGroup.Manage";
        }

        public static class Tag
        {
            public const string Read = "Customers.Tag.Read";
            public const string Manage = "Customers.Tag.Manage";
        }

        public static class Otp
        {
            public const string Send = "Customers.Otp.Send";
        }
    }

    public static class Catalog
    {
        public static class ServiceGroup
        {
            public const string Read = "Catalog.ServiceGroup.Read";
            public const string Manage = "Catalog.ServiceGroup.Manage";
        }

        public static class Service
        {
            public const string Read = "Catalog.Service.Read";
            public const string Manage = "Catalog.Service.Manage";
        }

        public static class ServicePrice
        {
            public const string Read = "Catalog.ServicePrice.Read";
            public const string Manage = "Catalog.ServicePrice.Manage";
        }

        public static class Price
        {
            public const string Read = "Catalog.Price.Read";
            public const string Manage = "Catalog.Price.Manage";
        }

        public static class AddOn
        {
            public const string Read = "Catalog.AddOn.Read";
            public const string Manage = "Catalog.AddOn.Manage";
        }

        public static class DataCenterItem
        {
            public const string Read = "Catalog.DataCenterItem.Read";
            public const string Manage = "Catalog.DataCenterItem.Manage";
        }
    }

    public static class Orders
    {
        public static class Order
        {
            public const string Manage = "Orders.Order.Manage";
            public const string Read = "Orders.Order.Read";
            public const string Create = "Orders.Order.Create";
            public const string Update = "Orders.Order.Update";
            public const string Edit = "Orders.Order.Edit";
            public const string Cancel = "Orders.Order.Cancel";
            public const string PriceOverride = "Orders.Order.PriceOverride";
        }

        public static class Sale
        {
            public const string Read = "Orders.Sale.Read";
            public const string Manage = "Orders.Sale.Manage";
        }
    }

    public static class Billing
    {
        public static class Invoice
        {
            public const string Manage = "Billing.Invoice.Manage";
            public const string Read = "Billing.Invoice.Read";
            public const string Create = "Billing.Invoice.Create";
            public const string Issue = "Billing.Invoice.Issue";
            public const string Cancel = "Billing.Invoice.Cancel";
            public const string DownloadPdf = "Billing.Invoice.DownloadPdf";
        }

        public static class Contract
        {
            public const string Manage = "Billing.Contract.Manage";
            public const string Read = "Billing.Contract.Read";
            public const string Create = "Billing.Contract.Create";
            public const string Issue = "Billing.Contract.Issue";
            public const string DownloadPdf = "Billing.Contract.DownloadPdf";
        }

        public static class Template
        {
            public const string Read = "Billing.Template.Read";
            public const string Manage = "Billing.Template.Manage";
        }
    }

    public static class Payments
    {
        public static class Payment
        {
            public const string Manage = "Payments.Payment.Manage";
            public const string Read = "Payments.Payment.Read";
            public const string Create = "Payments.Payment.Create";
            public const string Verify = "Payments.Payment.Verify";
            public const string Reconcile = "Payments.Payment.Reconcile";
        }

        public static class ManualPayment
        {
            public const string Create = "Payments.ManualPayment.Create";
        }

        public static class Receipt
        {
            public const string Read = "Payments.Receipt.Read";
            public const string Upload = "Payments.Receipt.Upload";
        }
    }

    public static class Wallet
    {
        public static class CustomerWallet
        {
            public const string Read = "Wallet.CustomerWallet.Read";
            public const string Adjust = "Wallet.CustomerWallet.Adjust";
        }

        public static class CommissionWallet
        {
            public const string Read = "Wallet.CommissionWallet.Read";
        }

        public static class Withdrawal
        {
            public const string Request = "Wallet.Withdrawal.Request";
            public const string Approve = "Wallet.Withdrawal.Approve";
        }
    }

    public static class Commission
    {
        public static class Rule
        {
            public const string Read = "Commission.Rule.Read";
            public const string Manage = "Commission.Rule.Manage";
        }

        public static class Entry
        {
            public const string Read = "Commission.Entry.Read";
            public const string Approve = "Commission.Entry.Approve";
        }

        public static class Schedule
        {
            public const string Read = "Commission.Schedule.Read";
        }
    }

    public static class Tickets
    {
        public static class Ticket
        {
            public const string Read = "Tickets.Ticket.Read";
            public const string Create = "Tickets.Ticket.Create";
            public const string Assign = "Tickets.Ticket.Assign";
            public const string Reply = "Tickets.Ticket.Reply";
            public const string Close = "Tickets.Ticket.Close";
        }
    }

    public static class Forms
    {
        public static class Form
        {
            public const string Read = "Forms.Form.Read";
            public const string Manage = "Forms.Form.Manage";
        }

        public static class Submission
        {
            public const string Read = "Forms.Submission.Read";
            public const string Create = "Forms.Submission.Create";
            public const string Review = "Forms.Submission.Review";
        }
    }

    public static class Notifications
    {
        public static class Notification
        {
            public const string Read = "Notifications.Notification.Read";
            public const string Send = "Notifications.Notification.Send";
        }

        public static class Template
        {
            public const string Manage = "Notifications.Template.Manage";
        }
    }

    public static class Reports
    {
        public static class Financial
        {
            public const string Read = "Reports.Financial.Read";
        }

        public static class CustomerPayments
        {
            public const string Read = "Reports.CustomerPayments.Read";
        }

        public static class Debtors
        {
            public const string Read = "Reports.Debtors.Read";
        }

        public static class Commissions
        {
            public const string Read = "Reports.Commissions.Read";
        }

        public static class Sales
        {
            public const string Read = "Reports.Sales.Read";
        }
    }

    public static class Settings
    {
        public static class Company
        {
            public const string Read = "Settings.Company.Read";
            public const string Manage = "Settings.Company.Manage";
        }

        public static class Tax
        {
            public const string Read = "Settings.Tax.Read";
            public const string Manage = "Settings.Tax.Manage";
        }

        public static class Gateway
        {
            public const string Read = "Settings.Gateway.Read";
            public const string Manage = "Settings.Gateway.Manage";
        }

        public static class Commission
        {
            public const string Read = "Settings.Commission.Read";
            public const string Manage = "Settings.Commission.Manage";
        }

        public static class Template
        {
            public const string Read = "Settings.Template.Read";
            public const string Manage = "Settings.Template.Manage";
        }

        public static class Sms
        {
            public const string Read = "Settings.Sms.Read";
            public const string Manage = "Settings.Sms.Manage";
        }
    }

    public static class Workflow
    {
        public static class Definition
        {
            public const string Read = "Workflow.Definition.Read";
            public const string Manage = "Workflow.Definition.Manage";
        }

        public static class Instance
        {
            public const string Read = "Workflow.Instance.Read";
            public const string Start = "Workflow.Instance.Start";
            public const string Execute = "Workflow.Instance.Execute";
            public const string Cancel = "Workflow.Instance.Cancel";
        }

        public static class Task
        {
            public const string Read = "Workflow.Task.Read";
            public const string Reassign = "Workflow.Task.Reassign";
        }
    }

    /// <summary>Every declared permission, used by the dynamic policy provider.</summary>
    public static IReadOnlyCollection<string> All { get; } = typeof(Permissions)
        .GetNestedTypes(BindingFlags.Public)
        .SelectMany(type => type.GetNestedTypes(BindingFlags.Public))
        .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .Where(value => value != Wildcard)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();

    private static readonly HashSet<string> AllSet = new(All, StringComparer.Ordinal);

    public static bool IsDefined(string permission) => AllSet.Contains(permission);
}
