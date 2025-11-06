using SkyLineSQL.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

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
            set { themeColor = value; OnPropertyChanged(); }
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
            DM = new();

            SearchToken = new();

            ChangeDatabaseCommand = new RelayCommand(ExecuteChangeDatabaseCommand);
            SearchDatabaseCommand = new RelayCommandAsync(ExecuteSearchDatabaseCommand, CanExecuteSearchDatabaseCommand);
            ReloadDatabaseCommand = new RelayCommand(ExecuteReloadDatabaseCommand);

            NavigationUpCommand = new RelayCommand(ExecuteNavigationUpCommand, CanExecuteNavigationUpCommand);
            NavigationDownCommand = new RelayCommand(ExecuteNavigationDownCommand, CanExecuteNavigationDownCommand);
            SelectionCommand = new RelayCommandAsync(ExecuteSelectionCommand, CanExecuteSelectionCommand);
            SelectionNameCommand = new RelayCommand(ExecuteSelectionNameCommand, CanExecuteSelectionNameCommand);
            PopupCommand = new RelayCommandAsync(ExecutePopupCommand, CanExecutePopupCommand);

            HideWindowCommand = new RelayCommand(ExecuteHideWindowCommand);
            ExitCommand = new RelayCommand(ExecuteExitCommand);
        }

        private void ExecuteChangeDatabaseCommand(object param)
        {
            var newDB = DM.ChangeDatabase();
            ThemeColor = newDB.ThemeColor;
        }


        private bool CanExecuteSearchDatabaseCommand(object param)
        {
            return (SearchToken.Text.Length >= 3 && SearchToken.Command.Length > 0);
        }

        //private static readonly SemaphoreLocker _locker = new SemaphoreLocker();

        private async Task ExecuteSearchDatabaseCommand(object param)
        {
            WorkInProgress = Visibility.Visible;
            DatabaseObjects.Clear();

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
        private void ExecuteNavigationUpCommand(object param)
        {
            var index = (int)param;
            SelectedIndex = index - 1;
            IsPopupOpen = false;
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
            IsPopupOpen = false;
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
                    var text = await DM.GetObject(SelectedItem);
                    Clipboard.SetText(text);
                }
            }
        }



        private bool CanExecuteSelectionNameCommand(object param)
        {
            return (SelectedIndex > -1 && SelectedIndex < DatabaseObjects.Count) || SearchToken.Command.Equals("prof");
        }
        private void ExecuteSelectionNameCommand(object param)
        {
            MainWindow window = param as MainWindow;
            if (window is not null)
            {
                window.Hide();
            }

            var SelectedItem = DatabaseObjects[SelectedIndex];
            if (SelectedItem is not null)
            {
                Clipboard.SetText(SelectedItem.Name);
            }
        }


        private bool CanExecutePopupCommand(object param)
        {
            return (SelectedIndex > -1 && SelectedIndex < DatabaseObjects.Count);
        }
        private async Task ExecutePopupCommand(object param)
        {
            var SelectedItem = DatabaseObjects[SelectedIndex];
            if (SelectedItem is not null)
            {
                var parameters = await DM.GetParameterDefination(SelectedItem);
                PopupHeading = SelectedItem.Name;

                RichTextBox rctBox = param as RichTextBox;
                if (rctBox is not null)
                {
                    rctBox.Document.Blocks.Clear();

                    foreach (var parameter in parameters)
                    {
                        Paragraph paragraph = new Paragraph
                        {
                            Margin = new Thickness(0),
                            LineHeight = 18,
                        };

                        // White: Parameter name
                        paragraph.Inlines.Add(new Run(parameter.Name + " ")
                        {
                            Foreground = Brushes.White,
                            FontWeight = FontWeights.Bold,
                        });

                        // Blue: Data type
                        paragraph.Inlines.Add(new Run(parameter.DataType + " ")
                        {
                            Foreground = Brushes.LightBlue
                        });

                        if (parameter.GetMaxLength() != string.Empty)
                        {
                            // Yellow: Range
                            paragraph.Inlines.Add(new Run(parameter.GetMaxLength())
                            {
                                Foreground = Brushes.LightGoldenrodYellow
                            });
                        }

                        rctBox.Document.Blocks.Add(paragraph);
                    }
                }
            }

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
