using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SkyLineSQL
{
    public class DataModel
    {
        public string Name { get; set; }

        private string type;

        public string Type
        {
            get { return type; }
            set { type = value.Trim(); }
        }

        public int ObjectId { get; set; }
    }

    public class DataManager : INotifyPropertyChanged
    {
       

        private string currentConnection;

        public string CurrentConnection
        {
            get { return currentConnection; }
            set { currentConnection = value; OnPropertyChanged(); }
        }

        private Dictionary<string, string> Connections = new();
        private IDbConnection sqlService ;

        public DataManager()
        {
            LoadConnections();
        }

        public void LoadConnections()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "DataManager.json");

            string jsonString = File.ReadAllText(filePath);

            Connections = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

            if (Connections.Count > 0)
            {
                CurrentConnection = Connections.ElementAt(0).Key;
                sqlService = new SqlConnection(Connections[CurrentConnection]);
            }
        }

        public void ChangeDatabase()
        {
            int index = Connections.Keys.ToList().IndexOf(CurrentConnection);

            var nextIndex = (index + 1) % Connections.Count;
            
            var newConnection = Connections.ElementAt(nextIndex);
            CurrentConnection = newConnection.Key;
            sqlService = new SqlConnection(Connections[CurrentConnection]);
        }

        public async Task<IEnumerable<DataModel>> SearchObject(List<string> commands, string search)
        {
            if (commands.Count > 0)
            {
                var filter = string.Join(",", commands);

                return await sqlService.QueryAsync<DataModel>($"SELECT name as Name, type as Type, object_id as ObjectId FROM sys.objects where type in ({filter}) and Name like '%{search}%' ORDER BY Len(Name), modify_date desc;", commandType: CommandType.Text);
            }

            return Enumerable.Empty<DataModel>();
        }

        public async Task<IEnumerable<DataModel>> SearchDeepObject(List<string> commands, string search)
        {
            if (commands.Count > 0)
            {
                var filter = string.Join(",", commands);

                var result = await sqlService.QueryAsync<DataModel>($"SELECT name as Name, type as Type, object_id as ObjectId FROM sys.objects where type in ({filter}) and object_definition(object_id) like '%{search}%' ORDER BY Len(Name), modify_date desc;", commandType: CommandType.Text);

                if (commands.Contains("'U'")) // No need to search in table column
                {
                    var sub_result = await sqlService.QueryAsync<DataModel>($"SELECT * FROM (SELECT distinct t.name as Name, type as Type, t.object_id as ObjectId FROM sys.objects t join sys.columns c on c.object_id = t.object_id where type in ('U') and c.name like '%{search}%') AS a ORDER BY LEN(a.Name)", commandType: CommandType.Text);
                    result = result.Union(sub_result);
                }

                return result.OrderBy(x => x.Name.Length);
            }

            return Enumerable.Empty<DataModel>();
        }

        public async Task<IEnumerable<DataModel>> StartProfiler(int second)
        {
            List<DataModel> monitor = new();

            int runInSec = 2;

            for (int i = 0; i < second * runInSec; i++)
            {
                var result = await sqlService.QueryAsync<DataModel>("SELECT OBJECT_NAME(t.objectid, t.dbid) AS Name, 'P' as Type, t.objectid as ObjectId from sys.dm_exec_requests r CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t where r.database_id = 5", commandType: CommandType.Text);

                monitor.AddRange(result);

                await Task.Delay(1000 / runInSec);
            }

            return monitor;
        }

        public async Task<string> GetObject(DataModel selected)
        {
            if (selected.Type == "U")
            {
                return $"SELECT top 100 *\nFROM {selected.Name}\nWHERE IsActive = 1\nORDER BY SortOrder";
            }
            else
            {
                var text = await sqlService.QueryFirstAsync<string>($"SELECT object_definition(object_id) FROM sys.objects where object_id = {selected.ObjectId};", commandType: CommandType.Text);
                text = ButifyText(text, selected.Type);

                return text;
            }
        }

        public string ButifyText(string text, string type)
        {
            StringBuilder str = new StringBuilder();

            if (type == "P")
            {
                if (text.Contains("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Replace("CREATE PROCEDURE", "ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase);
                }
                else if (text.Contains("CREATE PROC", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Replace("CREATE PROC", "ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase);
                }
                else if (text.Contains("ALTER PROC", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Replace("ALTER PROC", "ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase);
                }

                str.Append(text);
                str.Append("\nGO");

                return str.ToString();
            }
            else if (type == "V")
            {
                if (text.Contains("CREATE VIEW", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Replace("CREATE VIEW", "ALTER VIEW", StringComparison.OrdinalIgnoreCase);
                }

                str.Append(text);

                return str.ToString();
            }
            else if (type == "TR")
            {
                if (text.Contains("CREATE TRIGGER", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Replace("CREATE TRIGGER", "ALTER TRIGGER", StringComparison.OrdinalIgnoreCase);
                }

                str.Append(text);

                return str.ToString();
            }

            return text;
        }



        #region Notify

        /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
