using MVVM_Bonus.Services;
using System;
using System.Data.OleDb;

namespace MVVM_Bonus
{
    public class DataBaseService : IDisposable
    {
        OleDbCommand _cmd;
        OleDbConnection _cn;
        OleDbDataAdapter _adapter;
        readonly string _connectionString = Constants.SQL_CONNECTION_STRING;

        public DataBaseService()
        {
            CreateConnection();
        }

        public void CreateConnection()
        {
            _cn = new OleDbConnection(_connectionString);
            _cn.Open();
        }
        public OleDbDataReader GetCommand(string query)
        {
            using (_cmd = new OleDbCommand(query, _cn))
            {
                return _cmd.ExecuteReader();
            }
        }
        public void InsertCommand(string query)
        {
            using (_cmd = new OleDbCommand(query, _cn))
            {
                _cmd.ExecuteNonQuery();
            }
        }
        public void Dispose()
        {
            if (_cn != null)
            {
                if (_cn.State != System.Data.ConnectionState.Closed)
                {
                    _cn.Close();
                }
                _cn.Dispose();
            }
        }
    }
}
