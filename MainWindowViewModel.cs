using SkyLineSQL.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public List<string> Conditions { get; set; }


        private string previewText;

        public string PreviewText
        {
            get { return previewText; }
            set { previewText = value; OnPropertyChanged(); }
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

        public ICommand ChangeDatabaseCommand { get; }
        public ICommand SearchDatabaseCommand { get; }
        public ICommand ReloadDatabaseCommand { get; }

        public ICommand NavigationUpCommand { get; }
        public ICommand NavigationDownCommand { get; }
        public ICommand SelectionCommand { get; }
        public ICommand SelectionNameCommand { get; }
        public ICommand PopupCommand { get; }

        public ICommand HideWindowCommand { get; }
        public ICommand ExitCommand { get; }

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

            HideWindowCommand = new RelayCommand(ExecuteHideWindowCommand);
            ExitCommand = new RelayCommand(ExecuteExitCommand);
        }

        private async Task GenerateColumns()
        {
            Conditions.Clear();

            int i = 1;
            ColumnsOfObject.Clear();
            var cols = await DM.GetColumns(DatabaseObjects[SelectedIndex]);
            foreach (var col in cols.Take(10))
            {
                ColumnsOfObject.Add(new(i, col));
                i++;
            }
        }

        private async Task GetPreviewText()
        {
            PreviewText = await DM.GetPreviewText(DatabaseObjects[SelectedIndex]);
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
            return (SearchToken.Text.Length >= 3 && SearchToken.Command.Length > 0);
        }

        private async Task ExecuteSearchDatabaseCommand(object key)
        {
            char keyChar = (char)key;
            if (char.IsDigit(keyChar))
            {
                int k = int.Parse(key.ToString());
                var c = ColumnsOfObject.FirstOrDefault(x => x.Key == k);

                if (c != null)
                {
                    Conditions.Add(c.Value);
                    return;
                }
            }

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
                foreach (var item in await DM.SearchDeepObject(filters, SearchToken.Text))
                {
                    DatabaseObjects.Add(item);
                }
            }
            else
            {
                foreach (var item in await DM.SearchObject(filters, SearchToken.Text))
                {
                    DatabaseObjects.Add(item);
                }
            }

            if (DatabaseObjects.Count > 0)
            {
                SelectedIndex = 0;

                GetPreviewText();
                GenerateColumns();
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
            var index = (int)param;
            SelectedIndex = index - 1;
            //IsPopupOpen = false;

            GetPreviewText();
            GenerateColumns();
        }

        private bool CanExecuteNavigationDownCommand(object param)
        {
            var index = (int)param;
            return index < DatabaseObjects.Count;
        }
        private async Task ExecuteNavigationDownCommand(object param)
        {
            var index = (int)param;
            SelectedIndex = index + 1;
            //IsPopupOpen = false;

            GetPreviewText();
            GenerateColumns();
        }


        private bool CanExecuteSelectionCommand(object param)
        {
            return (SelectedIndex > -1 && SelectedIndex < DatabaseObjects.Count) || SearchToken.Command.Equals("prof");
        }
        private async Task ExecuteSelectionCommand(object param)
        {
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
            else
            {
                var SelectedItem = DatabaseObjects[SelectedIndex];
                if (SelectedItem is not null)
                {
                    var text = await DM.GetObject(SelectedItem, Conditions);
                    Clipboard.SetText(text);
                    Conditions.Clear();
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

            var SelectedItem = DatabaseObjects[SelectedIndex];
            if (SelectedItem is not null)
            {
                //var text = await DM.GetObject(SelectedItem);
                //SSMSHelper.OpenQueryInSSMS(DM.CurrentConnection.ConnectionString, text);

                Clipboard.SetText(SelectedItem.Name);
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



        private void ExecuteHideWindowCommand(object param)
        {
            var window = param as MainWindow;

            if (window is not null)
            {
                window.Hide();
            }
        }
        private void ExecuteExitCommand(object param) => Application.Current.Shutdown();


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
