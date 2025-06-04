using SkyLineSQL.Utility;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;

namespace SkyLineSQL
{
    public class ProfileEventModel
    {
        public string TextData {get; set; }
        public string LoginName {get; set;}
        public int CPU {get; set;}
        public long Reads {get; set;}
        public long Writes {get; set;}
        public long Duration {get; set;}
        public int SPID {get; set;}
        public DateTime StartTime {get; set;}
        public DateTime EndTime {get; set;}
        public string DatabaseName {get; set;}
        public string ObjectName {get; set;}
        public string ApplicationName {get; set;}
        public string HostName { get; set; }
    }

    public class ProfilerWindowViewModel : INotifyPropertyChanged
    {
        private readonly DataManager DM;
        public ObservableCollection<ProfileEventModel> Events { get; set; }

        private ProfileEventModel selectedEvent;

        public ProfileEventModel SelectedEvent
        {
            get { return selectedEvent; }
            set { selectedEvent = value;  OnPropertyChanged(); }
        }


        public ICommand StartProfilerCommand { get; }
        public ICommand PauseProfilerCommand { get; }
        public ICommand StopProfilerCommand { get; }

        public ProfilerWindowViewModel(DataManager DM)
        {
            Events = new();
            this.DM = DM;

            StartProfilerCommand = new RelayCommand(ExecuteStartProfilerCommand);
            PauseProfilerCommand = new RelayCommand(ExecutePauseProfilerCommand);
            StopProfilerCommand = new RelayCommand(ExecuteStopProfilerCommand);


            //ProfileEventModel eventModel = new()
            //{
            //    TextData = "TextData",
            //    LoginName = "LoginName",
            //    CPU = 0,
            //    Reads =12,
            //    Writes = 15,
            //    Duration = 150,
            //    SPID = 52,
            //    StartTime = DateTime.Now,
            //    EndTime = DateTime.Now.AddSeconds(15),
            //    DatabaseName = "BalaniRebuild",
            //    ObjectName = "usp_Client_Get",
            //    ApplicationName = "SkyLineSQL",
            //    HostName = "Server" 
            //};

            //Events.Add(eventModel);
            //Events.Add(eventModel);
        }

        private void ExecuteStartProfilerCommand(object param)
        {
            StartProfiling();
        }
        private void ExecutePauseProfilerCommand(object param)
        {
            PauseProfiling();
        }
        private void ExecuteStopProfilerCommand(object param)
        {
            StopProfiling();
        }

        IDbConnection m_Conn;
        RawTraceReader m_Rdr;
        Thread m_Thr;
        bool m_NeedStop = true;
        private ProfilingStateEnum m_ProfilingState;
        private Exception m_profilerexception;

        private readonly ProfilerEvent m_EventStarted = new ProfilerEvent();
        private readonly ProfilerEvent m_EventStopped = new ProfilerEvent();
        private readonly ProfilerEvent m_EventPaused = new ProfilerEvent();


        private void StartProfiling()
        {
            if (m_ProfilingState == ProfilingStateEnum.psPaused)
            {
                ResumeProfiling();
                return;
            }

            if (m_Conn != null && m_Conn.State == ConnectionState.Open)
            {
                m_Conn.Close();
            }


            m_Conn = DM.GetProfilerConnection();
            m_Conn.Open();

            m_Rdr = new RawTraceReader(m_Conn);
            m_Rdr.CreateTrace();

            m_Rdr.SetEvent(ProfilerEvents.TSQL.SQLBatchCompleted,
                 ProfilerEventColumns.TextData,
                 ProfilerEventColumns.LoginName,
                 ProfilerEventColumns.CPU,
                 ProfilerEventColumns.Reads,
                 ProfilerEventColumns.Writes,
                 ProfilerEventColumns.Duration,
                 ProfilerEventColumns.SPID,
                 ProfilerEventColumns.StartTime,
                 ProfilerEventColumns.EndTime,
                 ProfilerEventColumns.DatabaseName,
                 ProfilerEventColumns.ApplicationName,
                 ProfilerEventColumns.HostName
               );

            m_Rdr.SetEvent(ProfilerEvents.StoredProcedures.RPCCompleted,
                      ProfilerEventColumns.TextData, ProfilerEventColumns.LoginName,
                      ProfilerEventColumns.CPU, ProfilerEventColumns.Reads,
                      ProfilerEventColumns.Writes, ProfilerEventColumns.Duration,
                      ProfilerEventColumns.SPID
                      , ProfilerEventColumns.StartTime, ProfilerEventColumns.EndTime
                      , ProfilerEventColumns.DatabaseName
                      , ProfilerEventColumns.ObjectName
                      , ProfilerEventColumns.ApplicationName
                      , ProfilerEventColumns.HostName
            );


            m_Rdr.SetFilter(ProfilerEventColumns.LoginName, LogicalOperators.AND, ComparisonOperators.Like, "developer_user");
            m_Rdr.SetFilter(ProfilerEventColumns.DatabaseName, LogicalOperators.AND, ComparisonOperators.Like, "BalaniRebuild");
            m_Rdr.SetFilter(ProfilerEventColumns.ApplicationName, LogicalOperators.AND, ComparisonOperators.NotLike, "SkyLineSQL");

            StartProfilerThread();
        }

        private void PauseProfiling()
        {
            using (var cn = DM.GetProfilerConnection())
            {
                cn.Open();
                m_Rdr.StopTrace(cn);
                cn.Close();
            }

            m_ProfilingState = ProfilingStateEnum.psPaused;
            NewEventArrived(m_EventPaused, true);
        }

        private void ResumeProfiling()
        {
            StartProfilerThread();
        }

        private void StopProfiling()
        {
            using (var cn = DM.GetProfilerConnection())
            {
                cn.Open();
                m_Rdr.StopTrace(cn);
                m_Rdr.CloseTrace(cn);
                cn.Close();
            }

            m_NeedStop = true;
            if (m_Thr.IsAlive)
            {
                try
                {
                    m_Thr.Abort();
                }
                catch (Exception)
                {

                }

            }

            m_Thr.Join();
            m_ProfilingState = ProfilingStateEnum.psStopped;
            NewEventArrived(m_EventStopped, true);
        }


        private void StartProfilerThread()
        {
            m_Rdr.Close();
            m_Rdr.StartTrace();
            m_Thr = new Thread(ProfilerThread) { IsBackground = true, Priority = ThreadPriority.Lowest };
            m_NeedStop = false;
            m_ProfilingState = ProfilingStateEnum.psProfiling;
            NewEventArrived(m_EventStarted, true);
            m_Thr.Start();
        }

        private void NewEventArrived(ProfilerEvent evt, bool last)
        {
            ProfileEventModel eventModel = new() {
                TextData = GetEventCaption(evt),
                LoginName = evt.LoginName,
                CPU = evt.CPU,
                Reads = evt.Reads,
                Writes = evt.Writes,
                Duration = evt.Duration,
                SPID = evt.SPID,
                StartTime = evt.StartTime,
                EndTime = evt.EndTime,
                DatabaseName = evt.DatabaseName,
                ObjectName = evt.ObjectName,
                ApplicationName = evt.ApplicationName,
                HostName = evt.HostName
            };

            Events.Add(eventModel);
        }

        private string GetEventCaption(ProfilerEvent evt)
        {
            if (evt == m_EventStarted)
            {
                return "Trace started";
            }

            if (evt == m_EventPaused)
            {
                return "Trace paused";
            }

            if (evt == m_EventStopped)
            {
                return "Trace stopped";
            }

            return evt.TextData;
        }

        private void ProfilerThread(object state)
        {
            try
            {
                while (!m_NeedStop && m_Rdr.TraceIsActive)
                {
                    var evt = m_Rdr.Next();
                    if (evt != null)
                    {
                        lock (this)
                        {
                            App.Current.Dispatcher.Invoke((System.Action)delegate
                            {
                                ProfileEventModel eventModel = new()
                                {
                                    TextData = GetEventCaption(evt),
                                    LoginName = evt.LoginName,
                                    CPU = evt.CPU,
                                    Reads = evt.Reads,
                                    Writes = evt.Writes,
                                    Duration = evt.Duration,
                                    SPID = evt.SPID,
                                    StartTime = evt.StartTime,
                                    EndTime = evt.EndTime,
                                    DatabaseName = evt.DatabaseName,
                                    ObjectName = evt.ObjectName,
                                    ApplicationName = evt.ApplicationName,
                                    HostName = evt.HostName
                                };

                                Events.Add(eventModel);
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                lock (this)
                {
                    if (!m_NeedStop && m_Rdr.TraceIsActive)
                    {
                        m_profilerexception = e;
                    }
                }
            }
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
