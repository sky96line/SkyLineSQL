using SkyLineSQL.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SkyLineSQL
{
    enum ProfilingStateEnum
    {
        psStopped,
        psProfiling,
        psPaused
    }

    public class SearchToken
    {
        public SearchToken()
        {
            Command = "";
            Text = "";
        }

        private string command;

        public string Command
        {
            get { return command.ToLower(); }
            set { command = value.ToLower(); }
        }

        public string Text { get; set; }
    }

    public class PopupText
    {
        public string Heading { get; set; }
        public string Text { get; set; }
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private Visibility workInProgress = Visibility.Hidden;

        public Visibility WorkInProgress
        {
            get { return workInProgress; }
            set { workInProgress = value; OnPropertyChanged(); }
        }

        #region Popup

        private bool isPopupOpen;

        public bool IsPopupOpen
        {
            get { return isPopupOpen; }
            set { isPopupOpen = value; OnPropertyChanged(); }
        }

        private bool inTab;
        public bool InTab
        {
            get { return inTab; }
            set { inTab = value; OnPropertyChanged(); }
        }

        private List<DataModel> tab_cols = new();





        private string popupHeading;

        public string PopupHeading
        {
            get { return popupHeading; }
            set { popupHeading = value; OnPropertyChanged(); }
        }

        private string themeColor;

        public string ThemeColor
        {
            get { return themeColor; }
            set { themeColor = value; OnPropertyChanged(); OnPropertyChanged("ThemeColorDim"); }
        }

        public string ThemeColorDim
        {
            get { return themeColor + "FF"; }
        }

        #endregion


        private string textBox = "";

        public string TextBox
        {
            get { return textBox; }
            set { textBox = value; OnPropertyChanged(); GenerateSearchToken(); }
        }

        private void GenerateSearchToken()
        {
            if (this.textBox.Contains(" "))
            {
                var parts = this.textBox.Split(" ");
                var cmd = parts.First();
                var txt = string.Join(" ", parts[1..]);

                SearchToken.Command = cmd;
                SearchToken.Text = txt;
            }
            else
            {
                SearchToken.Command = "a";
                SearchToken.Text = this.textBox;
            }
        }


        public ObservableCollection<DataModel> DatabaseObjects { get; set; }

        public ObservableCollection<KPV> ColumnsOfObject { get; set; }

        private Dictionary<string, int> usage = new();

        public List<string> Conditions { get; set; }


        private string previewText;

        public string PreviewText
        {
            get { return previewText; }
            set { previewText = value; OnPropertyChanged(); }
        }


        private ObservableCollection<object> tableData;

        public ObservableCollection<object> TableData
        {
            get { return tableData; }
            set { tableData = value; OnPropertyChanged(); }
        }

        private int selectedIndex;

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; OnPropertyChanged(); }
        }


        ProfilerWindowViewModel profilerVM;
        public DataManager DM { get; set; }
        public SearchToken SearchToken { get; set; }

        private CancellationTokenSource? cst;

        public ICommand ChangeDatabaseCommand { get; }
        public ICommand SearchDatabaseCommand { get; }
        public ICommand ReloadDatabaseCommand { get; }

        public ICommand NavigationUpCommand { get; }
        public ICommand NavigationDownCommand { get; }
        public ICommand SelectionCommand { get; }
        public ICommand SelectionNameCommand { get; }
        public ICommand PopupCommand { get; }

        public ICommand ChangePreviewModeCommand { get; }
        public ICommand TabClickCommand { get; }



        public ICommand HideWindowCommand { get; }
        public ICommand ExitCommand { get; }

        string preview_mode = "External"; // "Normal" | "External" | "Table"

        public MainWindowViewModel()
        {
            profilerVM = new();


            DatabaseObjects = new();

            ColumnsOfObject = new();
            Conditions = new();

            DM = new();
            ThemeColor = DM.CurrentConnection.ThemeColor;

            SearchToken = new();

            ChangeDatabaseCommand = new RelayCommand(ExecuteChangeDatabaseCommand);
            SearchDatabaseCommand = new RelayCommandAsync(ExecuteSearchDatabaseCommand, CanExecuteSearchDatabaseCommand);
            ReloadDatabaseCommand = new RelayCommand(ExecuteReloadDatabaseCommand);

            NavigationUpCommand = new RelayCommandAsync(ExecuteNavigationUpCommand, CanExecuteNavigationUpCommand);
            NavigationDownCommand = new RelayCommandAsync(ExecuteNavigationDownCommand, CanExecuteNavigationDownCommand);
            SelectionCommand = new RelayCommandAsync(ExecuteSelectionCommand, CanExecuteSelectionCommand);
            SelectionNameCommand = new RelayCommandAsync(ExecuteSelectionNameCommand, CanExecuteSelectionNameCommand);
            PopupCommand = new RelayCommandAsync(ExecutePopupCommand, CanExecutePopupCommand);

            ChangePreviewModeCommand = new RelayCommandAsync(ExecuteChangePreviewModeCommand, CanExecuteChangePreviewModeCommand);
            TabClickCommand = new RelayCommandAsync(ExecuteTabClickCommandCommand, CanExecuteTabClickCommandCommand);

            HideWindowCommand = new RelayCommand(ExecuteHideWindowCommand);
            ExitCommand = new RelayCommand(ExecuteExitCommand);
        }

        private bool CanExecuteTabClickCommandCommand(object arg)
        {
            return DatabaseObjects.Count > 0 && SelectedIndex > -1 && SelectedIndex < DatabaseObjects.Count;
        }

        private async Task ExecuteTabClickCommandCommand(object arg)
        {
            cst?.Cancel();              // cancel previous search
            cst = new CancellationTokenSource();
            var token = cst.Token;

            InTab = false;
            var item = DatabaseObjects[SelectedIndex];

            tab_cols.Clear();
            DatabaseObjects.Clear();
            ColumnsOfObject.Clear();

            var cols = await DM.GetColumns(item, token);
            foreach (var col in cols)
            {
                DataModel dm = new()
                {
                    Name = col,
                    Type = Constant.Column,
                    ObjectId = 0
                };
                tab_cols.Add(dm);
                DatabaseObjects.Add(dm);
            }

            if (DatabaseObjects.Count > 0)
            {
                InTab = true;
                TextBox = "";
                SelectedIndex = 0;
            }
        }


        private async Task GenerateColumns(CancellationToken token)
        {
            Conditions.Clear();

            int i = 1;
            ColumnsOfObject.Clear();
            var cols = await DM.GetColumns(DatabaseObjects[SelectedIndex], token);
            foreach (var col in cols.Take(10))
            {
                ColumnsOfObject.Add(new(i, col));
                i++;
            }
        }


        private async Task GetPreviewText(CancellationToken token)
        {
            if (IsPopupOpen)
            {
                if (preview_mode.Equals("Normal"))
                {
                    PreviewText = await DM.GetPreviewText(DatabaseObjects[SelectedIndex], token);
                }
                else if (preview_mode.Equals("External"))
                {
                    PreviewText = await DM.PreviewExternalDBObject(DatabaseObjects[SelectedIndex], token);
                }
                else if (preview_mode.Equals("Table"))
                {
                    PreviewText = await DM.PreviewTableObject(DatabaseObjects[SelectedIndex], "Jobs", token);
                }
            }
        }

        private async Task GetPreviewGrid(CancellationToken token)
        {
            if (IsPopupOpen)
                TableData = new(await DM.GetPreviewGrid(DatabaseObjects[SelectedIndex], token));
        }



        private void ExecuteChangeDatabaseCommand(object param)
        {
            string key = param as string;
            if (key.Equals("P"))
                DM.ChangeDatabase(1);
            else if (key.Equals("O"))
                DM.ChangeDatabase(-1);

            DatabaseObjects.Clear();
            Conditions.Clear();
            ColumnsOfObject.Clear();
            ThemeColor = DM.CurrentConnection.ThemeColor;
        }


        private bool CanExecuteSearchDatabaseCommand(object param)
        {
            return (SearchToken.Text.Length >= 3 && SearchToken.Command.Length > 0) || InTab;
        }

        private async Task ExecuteSearchDatabaseCommand(object key)
        {
            if (InTab)
            {
                var cols = tab_cols.Where(x => x.Name.Contains(TextBox, StringComparison.OrdinalIgnoreCase)).ToList();
                DatabaseObjects.Clear();

                foreach (var col in cols)
                {
                    DatabaseObjects.Add(col);
                }

                SelectedIndex = 0;
            }
            else
            {
                cst?.Cancel();              // cancel previous search
                cst = new CancellationTokenSource();
                var token = cst.Token;

                try
                {
                    //char keyChar = (char)key;
                    //if (char.IsDigit(keyChar))
                    //{
                    //    int k = int.Parse(key.ToString());
                    //    var c = ColumnsOfObject.FirstOrDefault(x => x.Key == k);

                    //    if (c != null)
                    //    {
                    //        Conditions.Add(c.Value);
                    //        return;
                    //    }
                    //}

                    // debounce delay (tweak as needed)
                    await Task.Delay(300, token);

                    WorkInProgress = Visibility.Visible;
                    DatabaseObjects.Clear();
                    Conditions.Clear();

                    Dictionary<string, List<string>> SQlCommands = new()
                {
                        {"u",  new() { Constant.UserTable}},
                        {"p",  new() { Constant.Procedure }},
                        {"t",  new() { Constant.Trigger}},
                        {"f",  new() { Constant.FunctionIF, Constant.FunctionFN}},
                        {"v",  new() { Constant.View}},
                        {"a",  new() { Constant.UserTable, Constant.Procedure, Constant.Trigger, Constant.FunctionIF, Constant.FunctionFN, Constant.View}},
                };

                    List<string> filters = new();
                    bool deepSearch = false;

                    foreach (var cmdStr in SearchToken.Command)
                    {
                        var cmd = cmdStr.ToString().ToLower();
                        if (SQlCommands.ContainsKey(cmd))
                        {
                            filters.AddRange(SQlCommands[cmd]);
                        }
                        else if (cmd.Equals(Constant.DeepSearch))
                        {
                            deepSearch = true;
                        }
                    }

                    if (deepSearch && filters.Count == 0)
                    {
                        filters.AddRange(SQlCommands[Constant.AllSearch.ToLower()]);
                    }

                    if (deepSearch)
                    {
                        foreach (var item in await DM.SearchDeepObject(filters, SearchToken.Text, token))
                        {
                            DatabaseObjects.Add(item);
                        }
                    }
                    else
                    {
                        foreach (var item in await DM.SearchObject(filters, SearchToken.Text, token))
                        {
                            if (usage.ContainsKey(item.Name))
                            {
                                int count = usage[item.Name];
                                item.UsageCount = count;
                            }

                            if (!item.Star)
                            {
                                DatabaseObjects.Add(item);
                            }
                            else
                            {
                                var index = DatabaseObjects
                                           .TakeWhile(x => x.Star && x.UsageCount >= item.UsageCount)
                                           .Count();
                                DatabaseObjects.Insert(index, item);
                            }
                        }
                    }

                    if (DatabaseObjects.Count > 0)
                    {
                        SelectedIndex = 0;

                        GetPreviewText(token);
                        //GetPreviewGrid(token);
                        GenerateColumns(token);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            WorkInProgress = Visibility.Hidden;
        }


        private void ExecuteReloadDatabaseCommand(object param)
        {
            DM.LoadConnections();
        }



        private bool CanExecuteNavigationUpCommand(object param)
        {
            var index = (int)param;
            return index > 0;
        }
        private async Task ExecuteNavigationUpCommand(object param)
        {
            cst?.Cancel();              // cancel previous search
            cst = new CancellationTokenSource();
            var token = cst.Token;

            var index = (int)param;
            SelectedIndex = index - 1;
            //IsPopupOpen = false;

            GetPreviewText(token);
            //GetPreviewGrid(token);
            GenerateColumns(token);
        }

        private bool CanExecuteNavigationDownCommand(object param)
        {
            var index = (int)param;
            return index < DatabaseObjects.Count;
        }
        private async Task ExecuteNavigationDownCommand(object param)
        {
            cst?.Cancel();              // cancel previous search
            cst = new CancellationTokenSource();
            var token = cst.Token;

            var index = (int)param;
            SelectedIndex = index + 1;
            //IsPopupOpen = false;

            GetPreviewText(token);
            //GetPreviewGrid(token);
            GenerateColumns(token);
        }


        private bool CanExecuteSelectionCommand(object param)
        {
            return (SelectedIndex > -1 && SelectedIndex < DatabaseObjects.Count) || SearchToken.Command.Equals("prof") || SearchToken.Command.Equals("rpt");
        }

        public class MetaData
        {
            public string SPName;
            public string TableName;
            public string ColumnName;
        }
        private async Task ExecuteSelectionCommand(object param)
        {
            cst?.Cancel();              // cancel previous search
            cst = new CancellationTokenSource();
            var token = cst.Token;

            MainWindow window = param as MainWindow;
            if (window is not null)
            {
                window.Hide();
            }

            if (SearchToken.Text.Equals("prof"))
            {
                profilerVM.SetDataManager(DM);
                ProfilerWindow profilerWindow = new($"{DM.CurrentConnection} - SQL Profiler", profilerVM);
                profilerWindow.Show();
            }
            else if (SearchToken.Text.Equals("rpt"))
            {
                ColumnsOfObject.Clear();
                ColumnsOfObject.Add(new(0, "Generating Report..."));


                List<MetaData> list_res = new List<MetaData>();
                var result = await DM.SearchObject(new List<string>() { Constant.FunctionFN, Constant.FunctionIF }, "", token);
                int count = 1;
                foreach (var res in result)
                {
                    ColumnsOfObject.Add(new(1, $"{count++} / {result.Count()}"));
                    var str = await DM.GenerateReport(res, token);
                    list_res.AddRange(str);

                    ColumnsOfObject.Remove(ColumnsOfObject[1]);
                }

                await File.AppendAllLinesAsync("./output.txt", list_res.Select(x => $"{x.SPName},{x.TableName},{x.ColumnName}"));

                foreach (var item in list_res.GroupBy(x => x.SPName))
                {
                    var str = $"{item.Key} -> {item.Select(x => x.TableName).Distinct().Count()} Tables";
                    await File.AppendAllLinesAsync("./output_table_summery.txt", new[] { str });
                }

                foreach (var item in list_res.GroupBy(x => x.SPName))
                {
                    var str = $"{item.Key} -> {item.Select(x => $"{x.TableName}.{x.ColumnName}").Distinct().Count()} Columns";
                    await File.AppendAllLinesAsync("./output_column_summery.txt", new[] { str });
                }

                foreach (var item in list_res.GroupBy(x => x.SPName))
                {
                    var str = $"{item.Key} -> {item.Where(x => x.TableName.StartsWith("ExternalDB")).Select(x => x.TableName).Distinct().Count()} Tables";
                    await File.AppendAllLinesAsync("./output_external_table_summery.txt", new[] { str });
                }
                foreach (var item in list_res.GroupBy(x => x.SPName))
                {
                    var str = $"{item.Key} -> {item.Where(x => x.TableName.StartsWith("ExternalDB")).Select(x => $"{x.TableName}.{x.ColumnName}").Distinct().Count()} Columns";
                    await File.AppendAllLinesAsync("./output_external_column_summery.txt", new[] { str });
                }

                ColumnsOfObject.Clear();
                ColumnsOfObject.Add(new(0, "Completed"));
            }
            else
            {
                var SelectedItem = DatabaseObjects[SelectedIndex];
                if (SelectedItem is not null)
                {
                    usage.AddUpdate(SelectedItem.Name);
                    if (SelectedItem.Type.Equals(Constant.Column))
                    {
                        var text = SelectedItem.Name;
                        Clipboard.SetText(text);
                        Conditions.Clear();
                    }
                    else
                    {
                        var text = await DM.GetObject(SelectedItem, Conditions, token);
                        Clipboard.SetText(text);
                        Conditions.Clear();
                    }

                    InTab = false;
                    tab_cols.Clear();
                }
            }
        }



        private bool CanExecuteSelectionNameCommand(object param)
        {
            return (SelectedIndex > -1 && SelectedIndex < DatabaseObjects.Count) || SearchToken.Command.Equals("prof");
        }
        private async Task ExecuteSelectionNameCommand(object param)
        {
            MainWindow window = param as MainWindow;
            if (window is not null)
            {
                window.Hide();
            }

            if (!preview_mode.Equals("Normal") && IsPopupOpen)
            {
                Clipboard.SetText(PreviewText);
                Conditions.Clear();
            }
            else
            {
                var SelectedItem = DatabaseObjects[SelectedIndex];
                if (SelectedItem is not null)
                {
                    Clipboard.SetText(SelectedItem.Name);
                }
            }
        }


        private bool CanExecutePopupCommand(object param)
        {
            return true;
        }
        private async Task ExecutePopupCommand(object param)
        {
            IsPopupOpen = !IsPopupOpen;
        }


        private bool CanExecuteChangePreviewModeCommand(object param)
        {
            return true;
        }
        private async Task ExecuteChangePreviewModeCommand(object param)
        {
            if (preview_mode.Equals("Normal"))
            {
                preview_mode = "External";
            }
            else if (preview_mode.Equals("External"))
            {
                preview_mode = "Table";
            }
            else if (preview_mode.Equals("Table"))
            {
                preview_mode = "Normal";
            }
            else
            {
                preview_mode = "Normal";
            }

            cst?.Cancel();              // cancel previous search
            cst = new CancellationTokenSource();
            var token = cst.Token;

            GetPreviewText(token);

            ColumnsOfObject.Add(new(0, preview_mode));
            await Task.Delay(1000);
            ColumnsOfObject.Clear();
        }


        private void ExecuteHideWindowCommand(object param)
        {
            var window = param as MainWindow;

            if (window is not null)
            {
                window.Hide();

                InTab = false;
            }
        }
        private void ExecuteExitCommand(object param) => System.Windows.Application.Current.Shutdown();


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
