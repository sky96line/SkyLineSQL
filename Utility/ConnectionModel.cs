using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyLineSQL.Utility
{
    public class Root
    {
        public List<ConnectionModel> Data { get; set; }
    }

    public class ConnectionModel
    {
        public string ProjectName { get; set; }
        public string Environment { get; set; }
        public string ThemeColor { get; set; } 
        public string ConnectionString { get; set; }
    }
}
