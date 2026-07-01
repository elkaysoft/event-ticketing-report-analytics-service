namespace ETS.Application.Reporting.Queries.GetTransactionReport
{
    public class GetTransactionReportResponse
    {
        public List<TransactionReportAnalytics> Analytics { get; set; }
        public TransactionReportSummary Summary { get; set; }
    }

    public class TransactionReportSummary
    {
        public decimal TotalRevenue { get; set; }
        public int TicketSold { get; set; }
        public decimal RefundedAmount { get; set; } = 0;
        public decimal PayoutIssued { get; set; } = 0;
        public decimal MaximumRevenue { get; set; }
        public decimal MinimumRevenue { get; set; }
    }

    public class TransactionReportAnalytics
    {
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
    }


}
