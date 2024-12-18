using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVM_Bonus.Services
{
    public static class Constants
    {
        public static string SQL_CONNECTION_STRING = @$"provider = Microsoft.ACE.OLEDB.12.0; Data Source = O:\Sécurisation\Département Affichage\Controle Adshel 2m²\PRIME DE QUALITE\BETA 2.0\WorkerBonus.accdb";
        public static string MAIN_MENU_VIEW = "MainMenuView";
        public static string MESSENGER_TEAMLEADER_IDENTIFICATION = "SendTeamLeader";
        public static string SQL_GET_TEAMLEADER_QUERY = "SELECT * FROM Teamleaders WHERE username";
        public static string SQL_GET_WORKER_QUERY = "SELECT * FROM Workers WHERE Teamleader_id ";
        public static string ACTIVE_ROWS = "Yes";
        public static string SQL_NAME_COLUMN_NAME = "Name";
        public static string SQL_ID_COLUMN_NAME = "ID";
        public static string SQL_LANGUAGE_COLUMN_NAME = "Language";
        public static string SQL_ROLE_COLUMN_NAME = "Role";
        public static string SQL_ACTIVE_COLUMN_NAME = "Active";
        public static string SQL_CONTRACT_ID_COLUMN_NAME = "Contract_id";
        public static string SQL_BV_ID_COLUMN_NAME = "BV_Id";
    }
}