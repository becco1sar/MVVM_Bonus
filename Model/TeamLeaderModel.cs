using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVM_Bonus.Services;

namespace MVVM_Bonus
{
    public class TeamLeaderModel
    {
        public string Name { get; set; }        
        public int Id { get; set; }
        public string Role { get; set; }
    }
}
