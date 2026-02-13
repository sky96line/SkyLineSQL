using System.Collections.Generic;

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

        public List<string> ExternalDB { get; set; }
    }
}
