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

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string textBox = "";

        public string TextBox
        {
            get { return textBox; }
            set { textBox = value; OnPropertyChanged(); GenerateSearchToken(); }
        }

        private void GenerateSearchToken()
        {
            if (textBox.StartsWith("/"))
            {
                if (textBox.Contains(" "))
                {
                    var cmd = textBox.Split(" ").First();
                    var txt = textBox.Split(" ").Last();

                    SearchToken.Command = cmd.Replace("/", "");
                    SearchToken.Text = txt;
                }
                else
                {
                    SearchToken.Command = textBox.Replace("/", "");
                    SearchToken.Text = "";
                }
            }
            else
            {
                SearchToken.Command = "a";
                SearchToken.Text = textBox;
            }
        }


        public ObservableCollection<DataModel> DatabaseObjects { get; set; }


        private int selectedIndex;

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; OnPropertyChanged(); }
        }


        public DataManager DM { get; set; }
        public SearchToken SearchToken { get; set; }

        public ICommand ChangeDatabaseCommand { get; }
        public ICommand SearchDatabaseCommand { get; }
        public ICommand ReloadDatabaseCommand { get; }

        public ICommand NavigationUpCommand { get; }
        public ICommand NavigationDownCommand { get; }
        public ICommand SelectionCommand { get; }

        public ICommand HideWindowCommand { get; }
        public ICommand ExitCommand { get; }

        public MainWindowViewModel()
        {
            DatabaseObjects = new();

            DM = new();

            SearchToken = new();

            ChangeDatabaseCommand = new RelayCommand(ExecuteChangeDatabaseCommand);
            SearchDatabaseCommand = new RelayCommandAsync(ExecuteSearchDatabaseCommand, CanExecuteSearchDatabaseCommand);
            ReloadDatabaseCommand = new RelayCommand(ExecuteReloadDatabaseCommand);

            NavigationUpCommand = new RelayCommand(ExecuteNavigationUpCommand, CanExecuteNavigationUpCommand);
            NavigationDownCommand = new RelayCommand(ExecuteNavigationDownCommand, CanExecuteNavigationDownCommand);
            SelectionCommand = new RelayCommandAsync(ExecuteSelectionCommand, CanExecuteSelectionCommand);

            HideWindowCommand = new RelayCommand(ExecuteHideWindowCommand);
            ExitCommand = new RelayCommand(ExecuteExitCommand);
        }

        private void ExecuteChangeDatabaseCommand(object param)
        {
            DM.ChangeDatabase();
        }


        

        private bool CanExecuteSearchDatabaseCommand(object param)
        {
            return ((SearchToken.Text.Length >= 3 && SearchToken.Command.Length > 0) || SearchToken.Command == "s");
        }
        private async Task ExecuteSearchDatabaseCommand(object param)
        {
            DatabaseObjects.Clear();

            if (SearchToken.Command == "s")
            {
                foreach (var item in await DM.StartProfiler(5))
                {
                    DatabaseObjects.Add(item);
                }

                if (DatabaseObjects.Count > 0)
                {
                    SelectedIndex = 0;
                }
                return;
            }

            Dictionary<char, List<string>> SQlCommands = new()
                {
                    {'u',  new() {"'U'"}},
                    {'p',  new() {"'P'" }},
                    {'t',  new() {"'TR'"}},
                    {'f',  new() {"'IF'", "'FN'"}},
                    {'v',  new() {"'V'"}},
                    {'a',  new() {"'U'", "'P'", "'TR'", "'IF'", "'FN'", "'V'"}},
                };

            List<string> filters = new();
            bool deepSearch = false;
            foreach (var cmd in SearchToken.Command)
            {
                if (SQlCommands.ContainsKey(cmd))
                {
                    filters.AddRange(SQlCommands[cmd]);
                }
                else if (cmd == 'd')
                {
                    deepSearch = true;
                }
            }

            if (deepSearch == true && filters.Count == 0)
            {
                filters.AddRange(SQlCommands['a']);
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
            }
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
        private void ExecuteNavigationUpCommand(object param)
        {
            var index = (int)param;
            SelectedIndex = index - 1;
        }

        private bool CanExecuteNavigationDownCommand(object param)
        {
            var index = (int)param;
            return index < DatabaseObjects.Count;
        }
        private void ExecuteNavigationDownCommand(object param)
        {
            var index = (int)param;
            SelectedIndex = index + 1;
        }

        private bool CanExecuteSelectionCommand(object param)
        {
            return SelectedIndex > -1 && SelectedIndex < DatabaseObjects.Count;
        }
        private async Task ExecuteSelectionCommand(object param)
        {
            var SelectedItem = DatabaseObjects[SelectedIndex];
            if (SelectedItem is not null)
            {
                var text = await DM.GetObject(SelectedItem);
                Clipboard.SetText(text);
            }

            MainWindow window = param as MainWindow;
            if (window is not null)
            {
                window.Hide();
            }
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
