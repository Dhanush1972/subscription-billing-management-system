using SubscriptionBilling.Application.Common;
using SubscriptionBilling.Application.DTOs.Invoices;

namespace SubscriptionBilling.Application.Interfaces;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetCustomerInvoicesAsync(int userId, InvoiceFilterRequest filter);
    Task<PagedResult<InvoiceDto>> GetAllInvoicesForAdminAsync(InvoiceFilterRequest filter);
    Task<InvoiceDto> GetInvoiceByIdAsync(int invoiceId, int? userId = null);
    Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadInvoicePdfAsync(int invoiceId, int? userId = null);
}
