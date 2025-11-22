using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using SkyLineSQL.Utility;
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

    public class ParameterModel
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public int MaxLength { get; set; }

        //get min length



        public string GetMaxLength()
        {
            if (DataType.Equals("nvarchar"))
            {
                if (MaxLength > 0) return $"({MaxLength / 2})";
                else if (MaxLength == -1) return $"(MAX)";
            }
            else if (DataType.Equals("datetime") || DataType.Equals("uniqueidentifier") || DataType.Equals("bit"))
            {
                return "";
            }

            return "";
        }


        public override string ToString()
        {
            if (DataType.Equals("nvarchar"))
            {
                if (MaxLength > 0) return $"{Name} {DataType} ({MaxLength / 2})";
                else if (MaxLength == -1) return $"{Name} {DataType} (MAX)";
            }
            return $"{Name} {DataType}";
        }
    }

    public class DataManager : INotifyPropertyChanged
    {
        private ConnectionModel currentConnection;

        public ConnectionModel CurrentConnection
        {
            get { return currentConnection; }
            set { currentConnection = value; OnPropertyChanged(); }
        }

        private List<ConnectionModel> Connections = new();
        private IDbConnection sqlService;

        public DataManager()
        {
            LoadConnections();
        }



        public void LoadConnections()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "DataManager.json");

            string jsonString = File.ReadAllText(filePath);

            var root = JsonConvert.DeserializeObject<Root>(jsonString);
            Connections = root.Data;

            if (Connections.Count > 0)
            {
                CurrentConnection = Connections.ElementAt(0);
                sqlService = new SqlConnection(CurrentConnection.ConnectionString);
            }
        }

        public void ChangeDatabase(int offset)
        {
            //var currentConn = Connections.FirstOrDefault(x => x.ConnectionString == CurrentConnection.ConnectionString);
            int index = Connections.IndexOf(CurrentConnection);

            //var nextIndex = (index + offset) % Connections.Count;
            if (index < 0)
                index = 0;

            // Proper cyclic modulo that works for negative numbers
            int nextIndex = ((index + offset) % Connections.Count + Connections.Count) % Connections.Count;


            var newConnection = Connections[nextIndex];
            CurrentConnection = newConnection;
            sqlService = new SqlConnection(CurrentConnection.ConnectionString);
        }

        public IDbConnection GetProfilerConnection()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(sqlService.ConnectionString);
            var masterConnString = $"Data Source={builder.DataSource};Initial Catalog=master;User Id={builder.UserID};Password={builder.Password};TrustServerCertificate=True;Application Name=SkyLineSQL";

            return new SqlConnection(masterConnString);
        }

        public async Task<IEnumerable<DataModel>> SearchObject(List<string> commands, string search)
        {
            try
            {
                if (commands.Count > 0)
                {
                    var filter = string.Join(",", commands);

                    return await sqlService.QueryAsync<DataModel>($"SELECT name as Name, type as Type, object_id as ObjectId FROM sys.objects where type in ({filter}) and Name like '%{search}%' ORDER BY Len(Name), modify_date desc;", commandType: CommandType.Text);
                }
            }
            catch (Exception)
            {
                // TODO: Need to handle exception
            }

            return Enumerable.Empty<DataModel>();
        }

        public async Task<IEnumerable<DataModel>> SearchDeepObject(List<string> commands, string search)
        {
            try
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
            }
            catch (Exception)
            {
                // TODO: Need to handle exception
            }


            return Enumerable.Empty<DataModel>();
        }

        public async Task<string> GetObject(DataModel selected, List<string> conditions)
        {
            if (selected.Type == "U")
            {
                //var cols = await sqlService.QueryAsync<string>($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{selected.Name}' ORDER BY ORDINAL_POSITION desc");
                var cols = await GetColumns(selected);

                var result = new List<string>();
                result.Add($"SELECT top 100 *\nFROM {selected.Name} WHERE 1=1");

                if (cols.Contains("IsActive"))
                {
                    result.Add($"\nAND IsActive = 1");
                }

                foreach (var condition in conditions)
                {
                    result.Add($"\nAND {condition} = ''");
                }

                if (cols.Contains("SortOrder"))
                {
                    result.Add($"\nORDER BY SortOrder");
                }
                else
                {
                    result.Add($"\nORDER BY 1 DESC");
                }

                return string.Join("", result);
                //    return $"SELECT top 100 *\nFROM {selected.Name}\nWHERE IsActive = 1\nORDER BY SortOrder";
            }
            else
            {
                var text = await sqlService.QueryFirstAsync<string>($"SELECT object_definition(object_id) FROM sys.objects where object_id = {selected.ObjectId};", commandType: CommandType.Text);
                text = ButifyText(text, selected.Type);

                return text.Trim();
            }
        }

        public async Task<IEnumerable<string>> GetColumns(DataModel selected)
        {
            if (selected.Type == "U")
            {
                return await sqlService.QueryAsync<string>($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{selected.Name}' ORDER BY ORDINAL_POSITION");
            }

            return Enumerable.Empty<string>();
        }


        public async Task<IEnumerable<ParameterModel>> GetParameterDefination(DataModel selected)
        {
            var parameterSQL = "";
            if (selected.Type == "P" || selected.Type == "FN" || selected.Type == "IF")
            {
                parameterSQL = $"SELECT p.name AS Name, t.name AS DataType, p.max_length as MaxLength FROM sys.parameters p JOIN sys.types t ON p.user_type_id = t.user_type_id WHERE p.object_id = {selected.ObjectId}";
            }
            if (selected.Type == "U" || selected.Type == "V")
            {
                parameterSQL = $"SELECT c.name AS Name, t.name AS DataType, c.max_length as MaxLength FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id WHERE c.object_id = {selected.ObjectId}";
            }

            return await sqlService.QueryAsync<ParameterModel>(parameterSQL, commandType: CommandType.Text);
            //return string.Join('\n', parameters);
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
