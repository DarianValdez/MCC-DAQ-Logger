 using DAQ_Data_Logger.Models;
using GalaSoft.MvvmLight.Command;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Geared;
using MccDaq;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DAQ_Data_Logger.ViewModels
{
    class DatabaseBrowserViewModel : ViewModelBase
    {
        //Models
        DatabaseModel Database = new DatabaseModel();

        #region Bindings
        private string[] _deviceNames = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15" };
        /// <summary>
        /// Databinding for Names of all devices.
        /// </summary>
        public string[] DeviceNames
        {
            get { return _deviceNames; }
        }

        private ObservableCollection<bool> _deviceSelected = new ObservableCollection<bool>(new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false });
        /// <summary>
        /// Databinding for whether a Device is selected to load.
        /// </summary>
        public ObservableCollection<bool> DeviceSelected
        {
            get { return _deviceSelected; }
            set
            {
                _deviceSelected = value;
                NotifyPropertyChanged(nameof(DeviceSelected));
            }
        }

        private string _filepath = "";
        /// <summary>
        /// Databinding for Path of database.
        /// </summary>
        public string Filepath
        {
            get { return _filepath; }
            set
            {
                _filepath = value;
                NotifyPropertyChanged(nameof(Filepath));
            }
        }

        private Range _resolution;
        /// <summary>
        /// Databinding for Range loaded from database.
        /// </summary>
        public Range Resolution
        {
            get { return _resolution; }
            set
            {
                _resolution = value;
                NotifyPropertyChanged(nameof(Resolution));
            }
        }

        private DateTime? _databaseStartTime;
        /// <summary>
        /// Databinding for first Date/Time in the database.
        /// </summary>
        public DateTime? DatabaseStartTime
        {
            get { return _databaseStartTime; }
            set
            {
                _databaseStartTime = value;
                NotifyPropertyChanged(nameof(DatabaseStartTime));
            }
        }

        private DateTime? _databaseEndTime;
        /// <summary>
        /// Databinding for last Date/Time in the database.
        /// </summary>
        public DateTime? DatabaseEndTime
        {
            get { return _databaseEndTime; }
            set
            {
                _databaseEndTime = value;
                NotifyPropertyChanged(nameof(DatabaseEndTime));
            }
        }

        private DateTime? _selectedStartTime;
        /// <summary>
        /// Databinding for the selected start Date/Time to load.
        /// </summary>
        public DateTime? SelectedStartTime
        {
            get { return _selectedStartTime; }
            set
            {
                _selectedStartTime = value;
                NotifyPropertyChanged(nameof(SelectedStartTime));
            }
        }

        private DateTime? _selectedEndTime;
        /// <summary>
        /// Databinding for the selected end Date/Time to load.
        /// </summary>
        public DateTime? SelectedEndTime
        {
            get { return _selectedEndTime; }
            set
            {
                _selectedEndTime = value;
                NotifyPropertyChanged(nameof(SelectedEndTime));
            }
        }

        private Func<double, string> _formatter = value => new System.DateTime((long) (value* TimeSpan.FromSeconds(1).Ticks)).ToString("yyyy-MM-dd HH:mm:ss");
        /// <summary>
        /// Databinding for X axis label formatter in a LiveChart.
        /// </summary>
        public Func<double, string> Formatter
        {
            get { return _formatter; }
        }

        /// <summary>
        /// Databinding for X axis Minimum in a LiveChart.
        /// </summary>
        private int _xAxisMin = 0;
        public int XAxisMin
        {
            get { return _xAxisMin; }
            set
            {
                _xAxisMin = value;
                NotifyPropertyChanged(nameof(XAxisMin));
            }
        }

        private int _xAxisMax = 100;
        /// <summary>
        /// Databinding for X axis Maximum in a LiveChart.
        /// </summary>
        public int XAxisMax
        {
            get { return _xAxisMax; }
            set
            {
                _xAxisMax = value;
                NotifyPropertyChanged(nameof(XAxisMax));
            }
        }

        private SeriesCollection _series;
        /// <summary>
        /// Databinding for Series collection in a LiveChart.
        /// </summary>
        public SeriesCollection Series
        {
            get { return _series; }
            set
            {
                _series = value;
                NotifyPropertyChanged(nameof(Series));
            }
        }
        #endregion

        #region Commands
        /// <summary>
        /// Command for the Browse button.
        /// </summary>
        private ICommand _browseCommand;
        public ICommand BrowseCommand
        {
            get
            {
                if (_browseCommand == null)
                    _browseCommand = new RelayCommand(() => BrowseDialog());
                return _browseCommand;
            }
        }
        private void BrowseDialog()
        {
            Database.OpenDialog.FileOk += OpenDialog_FileOk;
            Database.OpenDialog.ShowDialog();
        }

        private ICommand _loadCommand;
        /// <summary>
        /// Command for the Load button.
        /// </summary>
        public ICommand LoadCommand
        {
            get
            {
                if (_loadCommand == null)
                    _loadCommand = new RelayCommand(() => LoadDatabase());
                return _loadCommand;
            }
        }
        private void LoadDatabase()
        {
            //set visibility of all ranges
            Database.GetDataRangeCommand.Parameters.Clear();
            for (int i = 0; i < 16; i++)
            {
                Series[i].Values.Clear();
                ((GLineSeries)Series[i]).Visibility = (Visibility)Convert.ToInt32(!_deviceSelected[i]);
            }

            //add start/end time parameters
            Database.GetDataRangeCommand.Parameters.Add(new SQLiteParameter("Start", ((DateTime)SelectedStartTime).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff")));
            Database.GetDataRangeCommand.Parameters.Add(new SQLiteParameter("End", ((DateTime)SelectedEndTime).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff")));

            if (Database.Connection != null)
            {
                try
                {
                    Database.Connection.Open();
                    SQLiteDataReader reader = Database.GetDataRangeCommand.ExecuteReader();

                    int i;
                    object[] _data = new object[17];
                    //load all data from database
                    while (reader.Read())
                    {
                        reader.GetValues(_data);
                        DateTime time = DateTime.ParseExact((string)reader[0], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                        DateModel value;
                        for (i = 1; i < _data.Length; i++)
                        {
                            //only add to series if series is enabled
                            if (DeviceSelected[i - 1] && _data[i] != DBNull.Value)
                            {
                                //add new datemodel
                                value = new DateModel()
                                {
                                    Time = time,
                                    Value = (double)_data[i]
                                };
                                Series[i - 1].Values.Add(value);
                            }
                            else if (_data[i] == DBNull.Value)
                            {
                                value = new DateModel()
                                {
                                    Time = time
                                };
                                Series[i - 1].Values.Add(value);
                            }
                        }
                    }
                    reader.Close();
                    Database.Connection.Close();
                }
                catch (SQLiteException sqlex)
                {
                }
            }
        }

        /// <summary>
        /// Command for the SelectAll button.
        /// </summary>
        private ICommand _selectAllCommand;
        public ICommand SelectAllCommand
        {
            get
            {
                if (_selectAllCommand == null)
                    _selectAllCommand = new RelayCommand(() => SelectAll(true));
                return _selectAllCommand;
            }
        }

        private ICommand _selectNoneCommand;
        /// <summary>
        /// Command for the SelectNone button.
        /// </summary>
        public ICommand SelectNoneCommand
        {
            get
            {
                if (_selectNoneCommand == null)
                    _selectNoneCommand = new RelayCommand(() => SelectAll(false));
                return _selectNoneCommand;
            }
        }
        private void SelectAll(bool value)
        {
            for (int i = 0; i < 16; i++)
            {
                DeviceSelected[i] = value;
            }
        }

        #endregion

        /// <summary>
        /// Constructor. Initializes X axis formatting, and Series properties.
        /// </summary>
        public DatabaseBrowserViewModel()
        {
            var DateConfig = Mappers.Xy<DateModel>()
                .X(DateModel => (double)DateModel.Time.Ticks / TimeSpan.FromSeconds(1).Ticks)
                .Y(DateModel => DateModel.Value);

            Series = new SeriesCollection(DateConfig);
            for (int i = 0; i < 16; i++)
            {
                GLineSeries gSeries = new GLineSeries
                {
                    Title = "Device " + i.ToString(),
                    Values = new GearedValues<DateModel>().WithQuality(Quality.Low),
                    Fill = Brushes.Transparent,
                    StrokeThickness = .5,
                    PointGeometry = null
                };
                Series.Add(gSeries);
            }
        }

        /// <summary>
        /// Event Handler for OpenDialog.
        /// </summary>
        private void OpenDialog_FileOk(object sender, CancelEventArgs e)
        {
            //Get filepath and create connection
            Filepath = ((OpenFileDialog)sender).FileName;
            Database.CreateConnection(Filepath);

            if (Database.Connection != null)
            {
                try
                {
                    //Get dates from database
                    Database.Connection.Open();
                    SQLiteDataReader reader = Database.GetDateRangeCommand.ExecuteReader();

                    DateTime[] temp = new DateTime[2];
                    int i = 0;
                    while (reader.Read())
                    {
                        DateTime.TryParseExact((string)reader[0], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out temp[i]);
                        i++;
                    }
                    reader.Close();

                    reader = Database.GetInfoCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        try
                        {
                            Resolution = (Range)reader[0];
                        }
                        catch (Exception ex)
                        { }
                    }
                    reader.Close();
                    Database.Connection.Close();

                    //set start/end times
                    DatabaseStartTime = temp[0];
                    SelectedStartTime = temp[0];
                    DatabaseEndTime = temp[1];
                    SelectedEndTime = temp[1].Subtract(TimeSpan.FromSeconds(1));
                }
                catch (SQLiteException sqlex)
                {
                }
            }
        }
    }
}
