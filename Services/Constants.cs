using System;
using System.IO;

namespace MVVM_Bonus.Services
{
    public static class Constants
    {
        private static string GetDatabaseFilePath()
        {
            string defaultPath = @"O:\Sécurisation\Département Affichage\Controle Adshel 2m²\PRIME DE QUALITE\BETA 2.0\WorkerBonus.accdb";
            if (File.Exists(defaultPath))
                return defaultPath;

            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WorkerBonus.accdb");
            if (File.Exists(localPath))
                return localPath;

            return defaultPath;
        }

        public static string SQL_CONNECTION_STRING => $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={GetDatabaseFilePath()};";

        public const string MAIN_MENU_VIEW = "MainMenuView";
        public const string HR_DASHBOARD_VIEW = "HrDashboardView";
        public const string STAFF_MANAGEMENT_VIEW = "StaffManagementView";
        public const string BONUS_CONFIG_VIEW = "BonusConfigView";
        public const string MESSENGER_TEAMLEADER_IDENTIFICATION = "SendTeamLeader";

        public const string SQL_GET_TEAMLEADER_QUERY = "SELECT * FROM Teamleaders WHERE username = ?";
        public const string SQL_GET_WORKER_QUERY = "SELECT * FROM Workers WHERE Teamleader_id = ? AND Active = ?";

        public const string ACTIVE_ROWS = "Yes";
        public const string SQL_NAME_COLUMN_NAME = "Name";
        public const string SQL_ID_COLUMN_NAME = "ID";
        public const string SQL_LANGUAGE_COLUMN_NAME = "Language";
        public const string SQL_ROLE_COLUMN_NAME = "Role";
        public const string SQL_ACTIVE_COLUMN_NAME = "Active";
        public const string SQL_CONTRACT_ID_COLUMN_NAME = "Contract_id";
        public const string SQL_BV_ID_COLUMN_NAME = "BV_Id";
    }
}