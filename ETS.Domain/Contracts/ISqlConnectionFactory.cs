using System.Data;

namespace ETS.Domain.Contracts
{
    public interface ISqlConnectionFactory
    {
        IDbConnection CreateConnection();
        Task<IEnumerable<T>> DbQueryAsync<T>(IDbConnection dbConn, string sql, object? parameters = null, 
            CommandType commandType = CommandType.StoredProcedure, IDbTransaction? transaction = null);
        Task<(IEnumerable<T1>, IEnumerable<T2>)> DbQueryMultipleResultSetsAsync<T1, T2>(IDbConnection dbConn, 
            string sql, object? parameters = null, CommandType commandType = CommandType.StoredProcedure, 
            IDbTransaction? transaction = null);
    }
}
