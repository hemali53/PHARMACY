using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using PHARMACY.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PHARMACY.Pages.Billing
{
    public class OutstandingReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OutstandingReportModel> _logger;

        public OutstandingReportModel(ApplicationDbContext context, ILogger<OutstandingReportModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public int? CustomerFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DueDateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DueDateTo { get; set; }

        // Report data
        public List<OutstandingInvoiceDetail> OutstandingInvoices { get; set; } = new List<OutstandingInvoiceDetail>();
        public List<CustomerSummary> Customers { get; set; } = new List<CustomerSummary>();
        public decimal TotalOutstanding { get; set; }
        public int TotalInvoices { get; set; }
        public int OverdueCount { get; set; }
        public List<TopCustomerBalance> TopCustomers { get; set; } = new List<TopCustomerBalance>();

        public async Task OnGetAsync()
        {
            try
            {
                // Load customers for filter dropdown
                await LoadCustomers();

                // Calculate outstanding invoices
                await CalculateOutstandingInvoices();

                // Apply Due Date filters after calculating due dates
                ApplyDueDateFilters();

                // Calculate summary statistics
                CalculateSummaryStatistics();

                // Get top customers
                await CalculateTopCustomers();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading outstanding report");
                SetDefaultValues();
            }
        }

        private async Task LoadCustomers()
        {
            // Load only customers with OutStanding Invoice type
            Customers = await _context.Invoices
                .Where(i => i.InvoiceType == "OutStanding Invoice")
                .Select(i => new CustomerSummary
                {
                    CustomerId = i.CustomerId ?? 0,
                    CustomerName = i.CustomerName
                })
                .Distinct()
                .OrderBy(c => c.CustomerName)
                .ToListAsync();
        }

        private async Task CalculateOutstandingInvoices()
        {
            // Start with all outstanding invoices
            var query = _context.Invoices
                .Where(i => i.InvoiceType == "OutStanding Invoice");

            // Apply Customer filter if provided
            if (CustomerFilter.HasValue && CustomerFilter > 0)
            {
                query = query.Where(i => i.CustomerId == CustomerFilter);
            }

            // Apply Invoice Date From filter if provided
            if (FromDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate >= FromDate.Value);
            }

            // Apply Invoice Date To filter if provided
            if (ToDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate <= ToDate.Value);
            }

            // If no date filters are provided, show last 6 months by default
            if (!FromDate.HasValue && !ToDate.HasValue)
            {
                var defaultFromDate = DateTime.Today.AddMonths(-6);
                query = query.Where(i => i.InvoiceDate >= defaultFromDate);
            }

            // Get invoices
            var invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            // Convert to OutstandingInvoiceDetail
            foreach (var invoice in invoices)
            {
                // Calculate due date (assuming 30 days credit)
                var dueDate = invoice.InvoiceDate.AddDays(30);

                // Get payments for this invoice
                var paidAmount = await GetPaidAmountForInvoice(invoice.InvoiceId);

                // Get returns for this invoice
                var returnValue = await GetReturnValueForInvoice(invoice.InvoiceId);

                // Calculate net amount and balance
                var netAmount = invoice.NetValue - returnValue;
                var balanceAmount = netAmount - paidAmount;

                // Only add if there's a balance
                if (balanceAmount > 0)
                {
                    var detail = new OutstandingInvoiceDetail
                    {
                        InvoiceId = invoice.InvoiceId,
                        InvoiceNumber = invoice.InvoiceNumber,
                        InvoiceType = invoice.InvoiceType,
                        InvoiceDate = invoice.InvoiceDate,
                        DueDate = dueDate,
                        CustomerName = invoice.CustomerName,
                        CustomerPhone = invoice.CustomerPhone ?? "N/A",
                        CustomerAddress = invoice.CustomerAddress ?? string.Empty,
                        InvoiceValue = invoice.NetValue,
                        PaidAmount = paidAmount,
                        ReturnValue = returnValue,
                        NetAmount = netAmount,
                        BalanceAmount = balanceAmount
                    };

                    OutstandingInvoices.Add(detail);
                }
            }
        }

        private void ApplyDueDateFilters()
        {
            // Apply Due Date From filter
            if (DueDateFrom.HasValue)
            {
                OutstandingInvoices = OutstandingInvoices
                    .Where(i => i.DueDate >= DueDateFrom.Value)
                    .ToList();
            }

            // Apply Due Date To filter
            if (DueDateTo.HasValue)
            {
                OutstandingInvoices = OutstandingInvoices
                    .Where(i => i.DueDate <= DueDateTo.Value)
                    .ToList();
            }

            // Sort by balance amount (highest first)
            OutstandingInvoices = OutstandingInvoices
                .OrderByDescending(i => i.BalanceAmount)
                .ThenByDescending(i => i.DueDate)
                .ToList();
        }

        private async Task<decimal> GetPaidAmountForInvoice(int invoiceId)
        {
            try
            {
                // Check if you have a Payments table in your context
                // Uncomment and adjust based on your actual Payments model
                /*
                if (_context.Payments != null)
                {
                    var payments = await _context.Payments
                        .Where(p => p.InvoiceId == invoiceId && p.PaymentStatus == "Completed")
                        .SumAsync(p => p.Amount);
                    return payments;
                }
                */
                return 0m;
            }
            catch (Exception)
            {
                return 0m;
            }
        }

        private async Task<decimal> GetReturnValueForInvoice(int invoiceId)
        {
            try
            {
                // Check if you have a Returns table in your context
                // Uncomment and adjust based on your actual Returns model
                /*
                if (_context.Returns != null)
                {
                    var returns = await _context.Returns
                        .Where(r => r.InvoiceId == invoiceId && r.ReturnStatus == "Approved")
                        .SumAsync(r => r.Amount);
                    return returns;
                }
                */
                return 0m;
            }
            catch (Exception)
            {
                return 0m;
            }
        }

        private void CalculateSummaryStatistics()
        {
            TotalInvoices = OutstandingInvoices.Count;
            TotalOutstanding = OutstandingInvoices.Sum(i => i.BalanceAmount);
            OverdueCount = OutstandingInvoices.Count(i => i.DueDate < DateTime.Today);
        }

        private async Task CalculateTopCustomers()
        {
            // Group by customer and calculate totals
            var customerGroups = OutstandingInvoices
                .GroupBy(i => new { i.CustomerName })
                .Select(g => new TopCustomerBalance
                {
                    CustomerName = g.Key.CustomerName,
                    InvoiceCount = g.Count(),
                    BalanceAmount = g.Sum(i => i.BalanceAmount)
                })
                .OrderByDescending(c => c.BalanceAmount)
                .Take(5)
                .ToList();

            TopCustomers = customerGroups;
        }

        private void SetDefaultValues()
        {
            OutstandingInvoices = new List<OutstandingInvoiceDetail>();
            Customers = new List<CustomerSummary>();
            TotalOutstanding = 0;
            TotalInvoices = 0;
            OverdueCount = 0;
            TopCustomers = new List<TopCustomerBalance>();
        }
    }

    // Supporting classes
    public class OutstandingInvoiceDetail
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string InvoiceType { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public decimal InvoiceValue { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ReturnValue { get; set; }
        public decimal NetAmount { get; set; }
        public decimal BalanceAmount { get; set; }
    }

    public class CustomerSummary
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
    }

    public class TopCustomerBalance
    {
        public string CustomerName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal BalanceAmount { get; set; }
    }
}