using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MVVM_Bonus
{
    public abstract class DataBaseHandler
    {
        static OleDbCommand _cmd;
        private static OleDbConnection _cn;
        static OleDbDataAdapter _adapter;
        static OleDbDataReader _reader;
        static string currentDir = Directory.GetCurrentDirectory().Remove(2);
        static string _connectionString = @$"provider = Microsoft.ACE.OLEDB.12.0; Data Source = O:\Sécurisation\Département Affichage\Controle Adshel 2m²\PRIME DE QUALITE\BETA 2.0\WorkerBonus.accdb";


        public static OleDbDataReader GetCommand(string query)
        {
            if(_cn == null || _cn.State != System.Data.ConnectionState.Open)
            {
                try
                {
                    _cn = new OleDbConnection(_connectionString);
                    _cn.Open();

                }
                catch(Exception e)
                {
                    MessageBox.Show(e.ToString());
                    
                }               
            }
            try
            {
                _cmd = new OleDbCommand(query, _cn);
                _reader = _cmd.ExecuteReader();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());

            }



            return _reader;

        }

        public static void InsertCommand(string query)
        {
            if (_cn == null || _cn.State != System.Data.ConnectionState.Open)
            {
                try
                {
                    _cn = new OleDbConnection(_connectionString);
                    _cn.Open();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }

            }
            _adapter = new OleDbDataAdapter();
            _cmd = new OleDbCommand(query, _cn);
            _adapter.InsertCommand = new OleDbCommand(query, _cn);
            _adapter.InsertCommand.ExecuteNonQuery();

        }
    }
}
