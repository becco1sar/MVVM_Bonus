using System;
using System.Data;
using System.Data.OleDb;
using MVVM_Bonus.Services;

namespace MVVM_Bonus
{
    public class DataBaseService
    {
        private readonly string _connectionString;

        public DataBaseService(string connectionString = null)
        {
            _connectionString = connectionString ?? Constants.SQL_CONNECTION_STRING;
        }

        private OleDbConnection CreateConnection()
        {
            var cn = new OleDbConnection(_connectionString);
            cn.Open();
            return cn;
        }

        /// <summary>
        /// Executes a SELECT query safely with parameters and returns a DataTable.
        /// Connection is closed immediately after reading data.
        /// </summary>
        public DataTable ExecuteQuery(string query, params object[] parameters)
        {
            using (var cn = CreateConnection())
            using (var cmd = new OleDbCommand(query, cn))
            {
                AddParameters(cmd, parameters);
                using (var adapter = new OleDbDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        /// <summary>
        /// Executes an INSERT, UPDATE, or DELETE query securely with parameters.
        /// </summary>
        public int ExecuteNonQuery(string query, params object[] parameters)
        {
            using (var cn = CreateConnection())
            using (var cmd = new OleDbCommand(query, cn))
            {
                AddParameters(cmd, parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes a scalar query returning a single value.
        /// </summary>
        public object ExecuteScalar(string query, params object[] parameters)
        {
            using (var cn = CreateConnection())
            using (var cmd = new OleDbCommand(query, cn))
            {
                AddParameters(cmd, parameters);
                return cmd.ExecuteScalar();
            }
        }

        private static void AddParameters(OleDbCommand cmd, object[] parameters)
        {
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue("?", param ?? DBNull.Value);
                }
            }
        }
    }
}
