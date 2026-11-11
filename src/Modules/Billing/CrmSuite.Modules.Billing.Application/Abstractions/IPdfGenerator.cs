using CrmSuite.Modules.Billing.Application.Dtos;

namespace CrmSuite.Modules.Billing.Application.Abstractions;

public interface IPdfGenerator
{
    byte[] GenerateInvoice(InvoicePdfModel model);

    byte[] GenerateContract(ContractPdfModel model);
}
