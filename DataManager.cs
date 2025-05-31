using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CopyWindowSQL
{
    public class DB
    {
        public static Dictionary<string, List<string>> Types = new()
        {
            {"u",  new() {"'U'"}},
            {"p",  new() {"'P'" }},
            {"t",  new() {"'TR'"}},
            {"f",  new() {"'IF'", "'FN'"}},
            {"v",  new() {"'V'"}},
            {"a",  new() {"'U'", "'P'", "'TR'", "'IF'", "'FN'", "'V'"}},
        };
    }

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

        public async Task<IEnumerable<DataModel>> SearchObject(SearchToken searchToken)
        {
            List<string> search_types = new();

            foreach (char type in searchToken.Command)
            {
                if (DB.Types.ContainsKey(type.ToString().ToLower()))
                {
                    search_types.AddRange(DB.Types[type.ToString().ToLower()]);
                }
            }

            if (search_types.Count > 0)
            {
                var filter = string.Join(",", search_types);

                return await sqlService.QueryAsync<DataModel>($"SELECT name as Name, type as Type, object_id as ObjectId FROM sys.objects where type in ({filter}) and Name like '%{searchToken.Text}%' ORDER BY modify_date desc;", commandType: CommandType.Text);
            }

            return Enumerable.Empty<DataModel>();
        }

        public async Task<string> GetObject(DataModel selected)
        {
            if (selected.Type == "U")
            {
                return $"SELECT * \nFROM {selected.Name} \nWHERE = ";
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
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
