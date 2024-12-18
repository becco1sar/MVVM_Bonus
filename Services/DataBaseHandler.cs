using MVVM_Bonus.Services;
using System;
using System.Data.OleDb;
using System.IO;
using System.Windows;

namespace MVVM_Bonus
{
    public abstract class DataBaseHandler
    {
        static OleDbCommand _cmd;
        private static OleDbConnection _cn;
        static OleDbDataAdapter _adapter;
        
        public static void CreateConnection()
        {
            _cn = new OleDbConnection(Constants.SQL_CONNECTION_STRING);
            _cn.Open();
        }
        public static OleDbDataReader GetCommand(string query)
        {
            _cmd = new OleDbCommand(query, _cn);          
            OleDbDataReader _reader = _cmd.ExecuteReader();
            return _reader;
        }

        public static void InsertCommand(string query)
        {
            _adapter = new OleDbDataAdapter();
            _cmd = new OleDbCommand(query, _cn);
            _adapter.InsertCommand = new OleDbCommand(query, _cn);
            _adapter.InsertCommand.ExecuteNonQuery();
        }
    }
}
