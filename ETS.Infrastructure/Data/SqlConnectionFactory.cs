using Dapper;
using ETS.Domain.Contracts;
using Microsoft.Data.SqlClient;
using System.Data;
using static Dapper.SqlMapper;

namespace ETS.Infrastructure.Data
{
    public sealed class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();

            return conn;
        }

        public async Task<IEnumerable<T>> DbQueryAsync<T>(IDbConnection dbConn, string sql, object? parameters = null, CommandType commandType = CommandType.StoredProcedure, IDbTransaction? transaction = null)
        {
            return parameters == null ?
                await dbConn.QueryAsync<T>(sql, commandType, transaction: transaction) :
                await dbConn.QueryAsync<T>(sql, parameters, commandType: commandType, transaction: transaction);
        }

        public async Task<(IEnumerable<T1>, IEnumerable<T2>)> DbQueryMultipleResultSetsAsync<T1, T2>(IDbConnection dbConn, string sql, object? parameters = null, CommandType commandType = CommandType.StoredProcedure, IDbTransaction? transaction = null)
        {
            await using GridReader results = parameters == null
                ? await dbConn.QueryMultipleAsync(sql, commandType, transaction: transaction)
                : await dbConn.QueryMultipleAsync(sql, parameters, commandType: commandType, transaction: transaction);

            var resultSet1 = await results.ReadAsync<T1>();
            var resultSet2 = await results.ReadAsync<T2>();

            return (resultSet1, resultSet2);
        }
    }
}
