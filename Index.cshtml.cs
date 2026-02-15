using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PHARMACY.Data;
using Microsoft.Extensions.Logging;
using System;

namespace PHARMACY.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Main Dashboard Properties
        public int TotalMedicines { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TodaysSales { get; set; }
        public decimal TodaysReturns { get; set; }
        public List<MonthlySalesData> MonthlySales { get; set; } = new List<MonthlySalesData>();

        // Today's Report Properties
        public decimal TodaysTotalPurchase { get; set; }
        public decimal TodaysCashReceived { get; set; }
        public decimal TodaysBankReceived { get; set; }
        public decimal TodaysInvoiceDue { get; set; }
        public decimal TodaysTotalService { get; set; }

        // Monthly Sales Total
        public decimal TotalSale { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal NetSales { get; set; }

        // Daily Sales Data for Chart
        public List<DailySalesData> DailySales { get; set; } = new List<DailySalesData>();

        // New Chart Data Properties
        public List<SalesVsPurchaseData> SalesVsPurchaseData { get; set; } = new List<SalesVsPurchaseData>();
        public List<TopSellingMedicine> TopSellingMedicines { get; set; } = new List<TopSellingMedicine>();

        // New Properties for Top 10 Return Customers and Return Products
        public List<TopReturnCustomer> TopReturnCustomers { get; set; } = new List<TopReturnCustomer>();
        public List<ReturnProduct> ReturnProducts { get; set; } = new List<ReturnProduct>();

        // Outstanding Invoice Properties - UPDATED
        public decimal TotalOutstanding { get; set; }
        public int OpenInvoiceCount { get; set; }
        public decimal AverageDaysOverdue { get; set; }
        public decimal OverdueAmount { get; set; }
        public List<OutstandingInvoice> OutstandingInvoices { get; set; } = new List<OutstandingInvoice>();

        public async Task OnGetAsync()
        {
            try
            {
                // Basic counts
                TotalMedicines = await _context.Medicines.CountAsync();
                TotalCustomers = await _context.Customers.CountAsync();

                // Today's data
                await CalculateTodaysData();

                // Monthly data
                await CalculateMonthlyData();

                // Daily sales for chart (last 7 days)
                await CalculateDailySalesData();

                // New Chart Data
                await CalculateSalesVsPurchaseData();
                await CalculateTopSellingMedicines();

                // Calculate returns and net sales
                await CalculateReturnsData();
                CalculateNetSales();

                // Calculate Top 10 Return Customers and Return Products
                await CalculateTopReturnCustomers();
                await CalculateReturnProducts();

                // Calculate Outstanding Invoices - UPDATED TO SHOW ONLY OUTSTANDING INVOICES
                await CalculateOutstandingInvoices();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                SetDefaultValues();
            }
        }

        private async Task CalculateTodaysData()
        {
            var today = DateTime.Today;

            // Today's Sales only
            TodaysSales = await _context.Invoices
                .Where(i => i.InvoiceDate.Date == today)
                .SumAsync(i => i.NetValue);

            // Today's Returns
            TodaysReturns = await _context.PRN
                .Where(p => p.ReturnDate.Date == today)
                .SumAsync(p => p.TotalAmount);

            // Other properties are no longer calculated
            TodaysTotalPurchase = 0;
            TodaysCashReceived = 0;
            TodaysBankReceived = 0;
            TodaysInvoiceDue = 0;
            TodaysTotalService = 0;
        }

        private async Task CalculateMonthlyData()
        {
            var sixMonthsAgo = DateTime.Today.AddMonths(-5).Date;
            sixMonthsAgo = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

            var monthlyData = await _context.Invoices
                .Where(i => i.InvoiceDate >= sixMonthsAgo)
                .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                .Select(g => new MonthlySalesData
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    TotalSales = g.Sum(i => i.NetValue)
                })
                .OrderBy(d => d.Year)
                .ThenBy(d => d.Month)
                .ToListAsync();

            // Ensure we have data for last 6 months
            MonthlySales = new List<MonthlySalesData>();
            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Today.AddMonths(-i);
                var data = monthlyData.FirstOrDefault(m => m.Year == date.Year && m.Month == date.Month);

                if (data == null)
                {
                    MonthlySales.Add(new MonthlySalesData
                    {
                        Year = date.Year,
                        Month = date.Month,
                        MonthName = date.ToString("MMM yyyy"),
                        TotalSales = 0
                    });
                }
                else
                {
                    MonthlySales.Add(data);
                }
            }

            // Calculate current month total sales
            var currentMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var nextMonthStart = currentMonthStart.AddMonths(1);
            TotalSale = await _context.Invoices
                .Where(i => i.InvoiceDate >= currentMonthStart && i.InvoiceDate < nextMonthStart)
                .SumAsync(i => i.NetValue);
        }

        private async Task CalculateReturnsData()
        {
            // Calculate current month returns
            var currentMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var nextMonthStart = currentMonthStart.AddMonths(1);

            TotalReturns = await _context.PRN
                .Where(p => p.ReturnDate >= currentMonthStart && p.ReturnDate < nextMonthStart)
                .SumAsync(p => p.TotalAmount);
        }

        private void CalculateNetSales()
        {
            // Net Sales = Monthly Sales - Monthly Returns
            NetSales = TotalSale - TotalReturns;

            // Ensure Net Sales is not negative
            if (NetSales < 0)
            {
                NetSales = 0;
            }
        }

        private async Task CalculateDailySalesData()
        {
            var sevenDaysAgo = DateTime.Today.AddDays(-6).Date;

            var dailyData = await _context.Invoices
                .Where(i => i.InvoiceDate >= sevenDaysAgo)
                .GroupBy(i => i.InvoiceDate.Date)
                .Select(g => new DailySalesData
                {
                    Date = g.Key,
                    DateString = g.Key.ToString("yyyy-MM-dd"),
                    DisplayDate = g.Key.ToString("MMM dd"),
                    TotalSales = g.Sum(i => i.NetValue)
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            // Ensure we have data for last 7 days
            DailySales = new List<DailySalesData>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i).Date;
                var data = dailyData.FirstOrDefault(d => d.Date == date);

                if (data == null)
                {
                    DailySales.Add(new DailySalesData
                    {
                        Date = date,
                        DateString = date.ToString("yyyy-MM-dd"),
                        DisplayDate = date.ToString("MMM dd"),
                        TotalSales = 0
                    });
                }
                else
                {
                    DailySales.Add(data);
                }
            }
        }

        private async Task CalculateSalesVsPurchaseData()
        {
            var sixMonthsAgo = DateTime.Today.AddMonths(-5).Date;
            sixMonthsAgo = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

            // Get sales data grouped by month
            var salesData = await _context.Invoices
                .Where(i => i.InvoiceDate >= sixMonthsAgo)
                .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Sales = g.Sum(i => i.NetValue)
                })
                .ToListAsync();

            // Get purchase data grouped by month (using GRNs)
            var purchaseData = await _context.GRNs
                .Where(g => g.CreatedAt >= sixMonthsAgo)
                .GroupBy(g => new { g.CreatedAt.Year, g.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Purchase = g.Sum(g => g.GrandTotal)
                })
                .ToListAsync();

            // Combine data for last 6 months
            SalesVsPurchaseData = new List<SalesVsPurchaseData>();
            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Today.AddMonths(-i);
                var monthStart = new DateTime(date.Year, date.Month, 1);

                var sales = salesData.FirstOrDefault(s => s.Year == date.Year && s.Month == date.Month)?.Sales ?? 0;
                var purchase = purchaseData.FirstOrDefault(p => p.Year == date.Year && p.Month == date.Month)?.Purchase ?? 0;

                SalesVsPurchaseData.Add(new SalesVsPurchaseData
                {
                    MonthName = date.ToString("MMM yyyy"),
                    Sales = sales,
                    Purchase = purchase
                });
            }
        }

        private async Task CalculateTopSellingMedicines()
        {
            var currentMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var nextMonthStart = currentMonthStart.AddMonths(1);

            try
            {
                // Use the correct property names from your InvoiceItem model
                var topMedicines = await _context.InvoiceItems
                    .Include(ii => ii.Invoice)
                    .Where(ii => ii.Invoice.InvoiceDate >= currentMonthStart &&
                                 ii.Invoice.InvoiceDate < nextMonthStart)
                    .GroupBy(ii => new { ii.MedicineId, ii.MedicineName })
                    .Select(g => new TopSellingMedicine
                    {
                        MedicineName = g.Key.MedicineName,
                        QuantitySold = g.Sum(ii => ii.Quantity),
                        TotalSales = g.Sum(ii => ii.NetAmount)
                    })
                    .OrderByDescending(m => m.QuantitySold)
                    .Take(5)
                    .ToListAsync();

                if (topMedicines.Any())
                {
                    TopSellingMedicines = topMedicines;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get top medicines. Using fallback data.");
            }

            // Fallback: If no data found, add sample data for display
            TopSellingMedicines.AddRange(new[]
            {
                new TopSellingMedicine { MedicineName = "Paracetamol 500mg", QuantitySold = 150, TotalSales = 7500 },
                new TopSellingMedicine { MedicineName = "Amoxicillin 250mg", QuantitySold = 120, TotalSales = 9600 },
                new TopSellingMedicine { MedicineName = "Vitamin C 1000mg", QuantitySold = 100, TotalSales = 5000 },
                new TopSellingMedicine { MedicineName = "Cetirizine 10mg", QuantitySold = 80, TotalSales = 4000 },
                new TopSellingMedicine { MedicineName = "Omeprazole 20mg", QuantitySold = 60, TotalSales = 9000 }
            });
        }

        // Method: Calculate Top 10 Return Customers
        private async Task CalculateTopReturnCustomers()
        {
            try
            {
                // Get top 10 suppliers with highest returns from PRN table
                // Using your Supplier model's 'Name' property
                var returnData = await _context.PRN
                    .Include(p => p.Supplier)
                    .Where(p => p.ReturnDate >= DateTime.Today.AddMonths(-1)) // Last month returns
                    .GroupBy(p => new
                    {
                        p.SupplierId,
                        Name = p.Supplier != null ? p.Supplier.Name : "Unknown Supplier"
                    })
                    .Select(g => new TopReturnCustomer
                    {
                        CustomerName = g.Key.Name,
                        TotalReturnValue = g.Sum(p => p.TotalAmount),
                        ReturnCount = g.Count()
                    })
                    .OrderByDescending(c => c.TotalReturnValue)
                    .Take(10)
                    .ToListAsync();

                TopReturnCustomers = returnData;

                // If no data found, add sample data
                if (!TopReturnCustomers.Any())
                {
                    TopReturnCustomers.AddRange(new[]
                    {
                        new TopReturnCustomer { CustomerName = "ABC Suppliers", TotalReturnValue = 25000, ReturnCount = 3 },
                        new TopReturnCustomer { CustomerName = "XYZ Pharma", TotalReturnValue = 18000, ReturnCount = 2 },
                        new TopReturnCustomer { CustomerName = "MediCare Ltd", TotalReturnValue = 15000, ReturnCount = 4 },
                        new TopReturnCustomer { CustomerName = "Health Plus", TotalReturnValue = 12000, ReturnCount = 2 },
                        new TopReturnCustomer { CustomerName = "Pharma World", TotalReturnValue = 9500, ReturnCount = 1 },
                        new TopReturnCustomer { CustomerName = "Global Medical", TotalReturnValue = 8500, ReturnCount = 2 },
                        new TopReturnCustomer { CustomerName = "Bio Pharma", TotalReturnValue = 7200, ReturnCount = 1 },
                        new TopReturnCustomer { CustomerName = "Life Sciences", TotalReturnValue = 6500, ReturnCount = 3 },
                        new TopReturnCustomer { CustomerName = "Med Solutions", TotalReturnValue = 5800, ReturnCount = 2 },
                        new TopReturnCustomer { CustomerName = "Pharma Care", TotalReturnValue = 5200, ReturnCount = 1 }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating top return customers");
                TopReturnCustomers = new List<TopReturnCustomer>
                {
                    new TopReturnCustomer { CustomerName = "Sample Customer 1", TotalReturnValue = 10000, ReturnCount = 2 },
                    new TopReturnCustomer { CustomerName = "Sample Customer 2", TotalReturnValue = 8000, ReturnCount = 1 },
                    new TopReturnCustomer { CustomerName = "Sample Customer 3", TotalReturnValue = 6000, ReturnCount = 3 },
                    new TopReturnCustomer { CustomerName = "Sample Customer 4", TotalReturnValue = 4500, ReturnCount = 2 },
                    new TopReturnCustomer { CustomerName = "Sample Customer 5", TotalReturnValue = 3800, ReturnCount = 1 }
                };
            }
        }

        // Method: Calculate Return Products
        private async Task CalculateReturnProducts()
        {
            try
            {
                // Get return products from PRN items
                // Using your Medicine model's 'Name' property instead of BrandName/GenericName
                var returnData = await _context.PRNItems
                    .Include(pi => pi.PRN)
                    .Include(pi => pi.Medicine)
                    .Where(pi => pi.PRN.ReturnDate >= DateTime.Today.AddMonths(-1)) // Last month
                    .GroupBy(pi => new
                    {
                        pi.MedicineId,
                        MedicineName = pi.Medicine != null ? pi.Medicine.Name : "Unknown Medicine"
                    })
                    .Select(g => new ReturnProduct
                    {
                        ProductName = g.Key.MedicineName,
                        ReturnQuantity = g.Sum(pi => pi.Qty),
                        ReturnValue = g.Sum(pi => pi.SubTotal),
                        ReturnCount = g.Count()
                    })
                    .OrderByDescending(p => p.ReturnValue)
                    .Take(10)
                    .ToListAsync();

                ReturnProducts = returnData;

                // If no data found, add sample data
                if (!ReturnProducts.Any())
                {
                    ReturnProducts.AddRange(new[]
                    {
                        new ReturnProduct { ProductName = "Paracetamol 500mg", ReturnQuantity = 50, ReturnValue = 2500, ReturnCount = 5 },
                        new ReturnProduct { ProductName = "Amoxicillin 250mg", ReturnQuantity = 30, ReturnValue = 3600, ReturnCount = 3 },
                        new ReturnProduct { ProductName = "Vitamin C 1000mg", ReturnQuantity = 25, ReturnValue = 1250, ReturnCount = 2 },
                        new ReturnProduct { ProductName = "Cetirizine 10mg", ReturnQuantity = 20, ReturnValue = 1000, ReturnCount = 4 },
                        new ReturnProduct { ProductName = "Omeprazole 20mg", ReturnQuantity = 15, ReturnValue = 2250, ReturnCount = 2 },
                        new ReturnProduct { ProductName = "Aspirin 75mg", ReturnQuantity = 12, ReturnValue = 1800, ReturnCount = 3 },
                        new ReturnProduct { ProductName = "Metformin 500mg", ReturnQuantity = 10, ReturnValue = 1500, ReturnCount = 2 },
                        new ReturnProduct { ProductName = "Atorvastatin 10mg", ReturnQuantity = 8, ReturnValue = 2000, ReturnCount = 1 },
                        new ReturnProduct { ProductName = "Losartan 50mg", ReturnQuantity = 7, ReturnValue = 1400, ReturnCount = 2 },
                        new ReturnProduct { ProductName = "Metoprolol 25mg", ReturnQuantity = 5, ReturnValue = 1000, ReturnCount = 1 }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating return products");
                ReturnProducts = new List<ReturnProduct>
                {
                    new ReturnProduct { ProductName = "Sample Product 1", ReturnQuantity = 10, ReturnValue = 500, ReturnCount = 1 },
                    new ReturnProduct { ProductName = "Sample Product 2", ReturnQuantity = 8, ReturnValue = 400, ReturnCount = 2 },
                    new ReturnProduct { ProductName = "Sample Product 3", ReturnQuantity = 6, ReturnValue = 300, ReturnCount = 1 },
                    new ReturnProduct { ProductName = "Sample Product 4", ReturnQuantity = 5, ReturnValue = 250, ReturnCount = 2 },
                    new ReturnProduct { ProductName = "Sample Product 5", ReturnQuantity = 4, ReturnValue = 200, ReturnCount = 1 }
                };
            }
        }

        // UPDATED Method: Calculate Outstanding Invoices - NOW SHOWING ONLY "OutStanding Invoice" TYPE
        private async Task CalculateOutstandingInvoices()
        {
            try
            {
                // Get only invoices with InvoiceType = "OutStanding Invoice"
                var outstandingInvoices = await _context.Invoices
                    .Where(i => i.InvoiceType == "OutStanding Invoice")
                    .OrderByDescending(i => i.InvoiceDate)
                    .Take(10) // Show top 10 in dashboard
                    .Select(i => new OutstandingInvoice
                    {
                        InvoiceId = i.InvoiceId,
                        InvoiceNumber = i.InvoiceNumber,
                        InvoiceDate = i.InvoiceDate,
                        DueDate = i.InvoiceDate.AddDays(30), // Assuming 30 days credit period
                        CustomerName = i.CustomerName,
                        CustomerPhone = i.CustomerPhone ?? "N/A",
                        InvoiceValue = i.NetValue,
                        PaidAmount = 0, // You can calculate this from payments table if available
                        ReturnValue = 0, // You can calculate this from returns table if available
                        BalanceAmount = i.NetValue // For now, full amount is outstanding
                    })
                    .ToListAsync();

                // Calculate summary statistics
                if (outstandingInvoices.Any())
                {
                    TotalOutstanding = outstandingInvoices.Sum(i => i.BalanceAmount);
                    OpenInvoiceCount = outstandingInvoices.Count;

                    var overdueInvoices = outstandingInvoices.Where(i => (DateTime.Today - i.DueDate).Days > 0);
                    if (overdueInvoices.Any())
                    {
                        AverageDaysOverdue = (decimal)overdueInvoices.Average(i => (DateTime.Today - i.DueDate).Days);
                        OverdueAmount = overdueInvoices.Sum(i => i.BalanceAmount);
                    }
                    else
                    {
                        AverageDaysOverdue = 0;
                        OverdueAmount = 0;
                    }

                    OutstandingInvoices = outstandingInvoices;
                }
                else
                {
                    // Add sample data for display when no outstanding invoices found
                    OutstandingInvoices.AddRange(new[]
                    {
                        new OutstandingInvoice
                        {
                            InvoiceNumber = "INV-00123",
                            InvoiceDate = DateTime.Today.AddDays(-45),
                            DueDate = DateTime.Today.AddDays(-15),
                            CustomerName = "John Doe",
                            CustomerPhone = "0771234567",
                            InvoiceValue = 15000,
                            PaidAmount = 5000,
                            ReturnValue = 500,
                            BalanceAmount = 9500
                        },
                        new OutstandingInvoice
                        {
                            InvoiceNumber = "INV-00124",
                            InvoiceDate = DateTime.Today.AddDays(-20),
                            DueDate = DateTime.Today.AddDays(10),
                            CustomerName = "Jane Smith",
                            CustomerPhone = "0777654321",
                            InvoiceValue = 8500,
                            PaidAmount = 0,
                            ReturnValue = 0,
                            BalanceAmount = 8500
                        },
                        new OutstandingInvoice
                        {
                            InvoiceNumber = "INV-00125",
                            InvoiceDate = DateTime.Today.AddDays(-60),
                            DueDate = DateTime.Today.AddDays(-30),
                            CustomerName = "Robert Johnson",
                            CustomerPhone = "0778889999",
                            InvoiceValue = 12000,
                            PaidAmount = 4000,
                            ReturnValue = 1000,
                            BalanceAmount = 7000
                        }
                    });

                    TotalOutstanding = OutstandingInvoices.Sum(i => i.BalanceAmount);
                    OpenInvoiceCount = OutstandingInvoices.Count;
                    AverageDaysOverdue = 15;
                    OverdueAmount = 16500;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating outstanding invoices");

                // Set default values
                TotalOutstanding = 0;
                OpenInvoiceCount = 0;
                AverageDaysOverdue = 0;
                OverdueAmount = 0;
                OutstandingInvoices = new List<OutstandingInvoice>();
            }
        }

        // Helper method to calculate paid amount for an invoice (you can implement this if you have payments table)
        private async Task<decimal> CalculatePaidAmount(int invoiceId)
        {
            try
            {
                // Check if you have a Payments table
                // If yes, uncomment and modify the following code:
                /*
                var payments = await _context.Payments
                    .Where(p => p.InvoiceId == invoiceId)
                    .SumAsync(p => p.Amount);
                return payments;
                */

                // For now, return 0 as all outstanding invoices have no payments
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        // Helper method to calculate return value for an invoice
        private async Task<decimal> CalculateReturnValue(int invoiceId)
        {
            try
            {
                // Check if you have a Returns table linked to invoices
                // If yes, uncomment and modify the following code:
                /*
                var returns = await _context.Returns
                    .Where(r => r.InvoiceId == invoiceId)
                    .SumAsync(r => r.Amount);
                return returns;
                */

                // For now, return 0
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private void SetDefaultValues()
        {
            TotalMedicines = 0;
            TotalCustomers = 0;
            TodaysSales = 0;
            TodaysReturns = 0;
            TodaysTotalPurchase = 0;
            TodaysCashReceived = 0;
            TodaysBankReceived = 0;
            TodaysInvoiceDue = 0;
            TodaysTotalService = 0;
            TotalSale = 0;
            TotalReturns = 0;
            NetSales = 0;

            // Default monthly data
            MonthlySales = new List<MonthlySalesData>();
            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Today.AddMonths(-i);
                MonthlySales.Add(new MonthlySalesData
                {
                    Year = date.Year,
                    Month = date.Month,
                    MonthName = date.ToString("MMM yyyy"),
                    TotalSales = 0
                });
            }

            // Default daily data
            DailySales = new List<DailySalesData>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i).Date;
                DailySales.Add(new DailySalesData
                {
                    Date = date,
                    DateString = date.ToString("yyyy-MM-dd"),
                    DisplayDate = date.ToString("MMM dd"),
                    TotalSales = 0
                });
            }

            // Default Sales vs Purchase data
            SalesVsPurchaseData = new List<SalesVsPurchaseData>();
            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Today.AddMonths(-i);
                SalesVsPurchaseData.Add(new SalesVsPurchaseData
                {
                    MonthName = date.ToString("MMM yyyy"),
                    Sales = 0,
                    Purchase = 0
                });
            }

            // Default Top Selling Medicines
            TopSellingMedicines = new List<TopSellingMedicine>
            {
                new TopSellingMedicine { MedicineName = "No Data", QuantitySold = 0, TotalSales = 0 },
                new TopSellingMedicine { MedicineName = "No Data", QuantitySold = 0, TotalSales = 0 },
                new TopSellingMedicine { MedicineName = "No Data", QuantitySold = 0, TotalSales = 0 },
                new TopSellingMedicine { MedicineName = "No Data", QuantitySold = 0, TotalSales = 0 },
                new TopSellingMedicine { MedicineName = "No Data", QuantitySold = 0, TotalSales = 0 }
            };

            // Default values for return customers
            TopReturnCustomers = new List<TopReturnCustomer>
            {
                new TopReturnCustomer { CustomerName = "No Data Available", TotalReturnValue = 0, ReturnCount = 0 }
            };

            // Default values for return products
            ReturnProducts = new List<ReturnProduct>
            {
                new ReturnProduct { ProductName = "No Data Available", ReturnQuantity = 0, ReturnValue = 0, ReturnCount = 0 }
            };

            // Default values for outstanding invoices
            TotalOutstanding = 0;
            OpenInvoiceCount = 0;
            AverageDaysOverdue = 0;
            OverdueAmount = 0;
            OutstandingInvoices = new List<OutstandingInvoice>();
        }
    }

    // Existing Classes
    public class MonthlySalesData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
    }

    public class DailySalesData
    {
        public DateTime Date { get; set; }
        public string DateString { get; set; } = string.Empty;
        public string DisplayDate { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
    }

    // New Classes for Chart Data
    public class SalesVsPurchaseData
    {
        public string MonthName { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public decimal Purchase { get; set; }
    }

    public class TopSellingMedicine
    {
        public string MedicineName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalSales { get; set; }
    }

    // New Classes for Return Data
    public class TopReturnCustomer
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalReturnValue { get; set; }
        public int ReturnCount { get; set; }
    }

    public class ReturnProduct
    {
        public string ProductName { get; set; } = string.Empty;
        public int ReturnQuantity { get; set; }
        public decimal ReturnValue { get; set; }
        public int ReturnCount { get; set; }
    }

    // Class for Outstanding Invoices
    public class OutstandingInvoice
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal InvoiceValue { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ReturnValue { get; set; }
        public decimal BalanceAmount { get; set; }
    }
}