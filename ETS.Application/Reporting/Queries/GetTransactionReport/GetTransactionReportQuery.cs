using Dapper;
using ETS.Domain.Contracts;
using MediatR;

namespace ETS.Application.Reporting.Queries.GetTransactionReport
{
    public record GetTransactionReportQuery(DateTime StartDate, DateTime EndDate) 
        : IRequest<GetTransactionReportResponse>;

    public class GetTransactionReportQueryHandler(ISqlConnectionFactory _sqlConnectionFactory) 
        : IRequestHandler<GetTransactionReportQuery, GetTransactionReportResponse>
    {
        public async Task<GetTransactionReportResponse> Handle(GetTransactionReportQuery request, CancellationToken cancellationToken)
        {
            DynamicParameters parameters = new();
            parameters.Add("@StartDate", request.StartDate);
            parameters.Add("@EndDate", request.EndDate);

            using var connection = _sqlConnectionFactory.CreateConnection();

            var (summary, analytics) = await _sqlConnectionFactory
                .DbQueryMultipleResultSetsAsync<TransactionReportSummary, TransactionReportAnalytics>(connection,
                "sproc_GetOrderItemsReport", parameters);

            return new GetTransactionReportResponse
            {
                Summary = summary.FirstOrDefault() ?? new TransactionReportSummary(),
                Analytics = analytics.ToList() ?? new List<TransactionReportAnalytics>()
            };
        }
    }

}
