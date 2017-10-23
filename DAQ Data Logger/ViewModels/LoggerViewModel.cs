using DAQ_Data_Logger.Models;
using DAQ_Data_Logger.ViewModels;
using GalaSoft.MvvmLight.Command;
using LiveCharts;
using LiveCharts.Geared;
using MccDaq;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static MccDaq.ErrorInfo;

namespace DAQ_Data_ViewModels
{
    class LoggerViewModel : ViewModelBase
    {
        //Models
        LoggerModel Logger = new LoggerModel();
        DatabaseModel Database = new DatabaseModel();

        //MccDaq Event Delegate
        EventCallback DataReceivedDelegate;

        //Timestamp for database
        private string _timeStamp;

        //Data array and WinBuffer
        private double[] _data = new double[16];
        private IntPtr _dataPtr { get; set; }

        //Extra MccDaq and private variables
        private ErrorInfo ErrorInfo { get; set; }
        private DaqDeviceDescriptor[] DeviceInventory { get; set; }
        private bool _updateChart { get; set; }
        private bool _isReading { get; set; }
        private int numChannels;
        #region Bindings

        private int _samplePeriod = 200;
        /// <summary>
        /// Databinding for the Sample Period in ms.
        /// </summary>
        public int SamplePeriod
        {
            get { return _samplePeriod; }
            set
            {
                if (value > 1000 && DaqBoard.BoardName == "USB-1408FS")
                    _samplePeriod = 1000;
                else
                    _samplePeriod = value;

                NotifyPropertyChanged(nameof(SamplePeriod));

                //Set sample rate as samples/second
                if (_samplePeriod <= 1000)
                    _sampleRate = (int)(1.0 / ((double)value / 1000.0));
                else
                    _sampleRate = (int)(1000 / ((double)value / 1000.0));
            }
        }
        //5 is the match for the default of 200ms
        private int _sampleRate = 5;

        private ObservableCollection<MccBoard> _mccBoards = new ObservableCollection<MccBoard>();
        /// <summary>
        /// Databinding for the list of DAQ Devices.
        /// </summary>
        public ObservableCollection<MccBoard> MccBoards
        {
            get { return _mccBoards; }
            set { NotifyPropertyChanged(nameof(MccBoards)); }
        }

        private MccBoard _daqBoard;
        /// <summary>
        /// Databinding and AutoProperty for the currently selected DAQ device. This will automatically update the DaqRanges collection to reflect supported values for the board.
        /// </summary>
        public MccBoard DaqBoard
        {
            get { return _daqBoard; }
            set
            {
                _daqBoard = value;
                if (_daqBoard != null)
                {
                    //Enable the DataReceived Event
                    _daqBoard.EnableEvent(EventType.OnDataAvailable, (int)EventParameter.LatchDI, DataReceivedDelegate, _dataPtr);

                    //Set the DaqRanges property
                    if (_daqBoard.BoardName == "USB-2416")
                    {
                        DaqRanges = Logger.Range2416;
                        numChannels = 15;
                    }
                    else if (_daqBoard.BoardName == "USB-1608G")
                    {
                        DaqRanges = Logger.Range1608G;
                        numChannels = 7;
                    }
                    else if (_daqBoard.BoardName == "USB-1408FS")
                    {
                        DaqRanges = Logger.Range1408FS;
                        numChannels = 3;
                    }
                }

                if (Filepath != "")
                {
                    CanStart = true;
                }
            }
        }

        private Range _resolution;
        /// <summary>
        /// Databinding for the currently selected resolution for aquisition.
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

        private ObservableCollection<Range> _daqRanges;
        /// <summary>
        /// Databinding for the collection of supported ranges for the current board.
        /// </summary>
        public ObservableCollection<Range> DaqRanges
        {
            get { return _daqRanges; }
            set
            {
                _daqRanges = value;
                NotifyPropertyChanged(nameof(DaqRanges));
            }
        }

        private DataTable _gridData = new DataTable();
        /// <summary>
        /// Databinding for the last set of values that were read in from the board.
        /// </summary>
        public DataTable GridData
        {
            get { return _gridData; }
            set
            {
                _gridData = value;
                NotifyPropertyChanged(nameof(GridData));
            }
        }

        private int _xAxisMin = 0;
        /// <summary>
        /// Databinding for the minimum value to display on a LiveChart.
        /// </summary>
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
        /// Databinding for the maximum value to display on a LiveChart.
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

        private SeriesCollection _series = new SeriesCollection();
        /// <summary>
        /// Databinding for the GearedLineSeries to display on a LiveChart.
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

        private int _displayPoints = 100;
        /// <summary>
        /// Databinding for number of values to display on a LiveChart. Bound betwen 100 and 20000.
        /// </summary>
        public int DisplayPoints
        {
            get { return _displayPoints; }
            set
            {
                if (value > 20000)
                    _displayPoints = 20000;
                else if (value < 100)
                    _displayPoints = 100;
                else
                    _displayPoints = value;

                //Rebound the X axis if necessary.
                if (XAxisMax < value)
                    XAxisMax = value;
                if (XAxisMax > value)
                    XAxisMax = SampleCount + value;
                if (XAxisMax - value < 0)
                    XAxisMin = 0;
                else
                    XAxisMin = XAxisMax - value;

                NotifyPropertyChanged(nameof(DisplayPoints));
            }
        }

        private string _filepath = "";
        /// <summary>
        /// Databinding for the path of the SQLite Database.
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

        private int _sampleCount = 0;
        /// <summary>
        /// Databinding for the number of samples taken. Updates Samples string.
        /// </summary>
        public int SampleCount
        {
            get { return _sampleCount; }
            set
            {
                _sampleCount = value;
                Samples = "Samples: " + _sampleCount;
                NotifyPropertyChanged(nameof(SampleCount));
            }
        }

        private string _samples = "";
        /// <summary>
        /// Databinding for the display of number of samples.
        /// </summary>
        public string Samples
        {
            get { return _samples; }
            set
            {
                _samples = value;
                NotifyPropertyChanged(nameof(Samples));
            }

        }

        private string _daqDevices = "";
        /// <summary>
        /// Databinding for the count string for the number of DAQ Devices. Set by DaqBoard property.
        /// </summary>
        public string DaqDevices
        {
            get { return _daqDevices; }
            set
            {
                _daqDevices = value;
                NotifyPropertyChanged(nameof(DaqDevices));
            }
        }

        private string _status = "";
        /// <summary>
        /// Databinding for general purpose Status text.
        /// </summary>
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged(nameof(Status));
            }
        }

        private Brush _statusBrush = Brushes.Black;
        /// <summary>
        /// Databinding for Status property brush.
        /// </summary>
        public Brush StatusBrush
        {
            get { return _statusBrush; }
            set
            {
                _statusBrush = value;
                NotifyPropertyChanged(nameof(StatusBrush));
            }
        }

        private Boolean _canStart = false;
        /// <summary>
        /// Databinding for can start daquistion.
        /// </summary>
        public Boolean CanStart
        {
            get { return _canStart; }
            set
            {
                _canStart = value;
                NotifyPropertyChanged(nameof(CanStart));
            }
        }

        private Boolean _canStop = false;
        /// <summary>
        /// Databinding for can stop daquisition.
        /// </summary>
        public Boolean CanStop
        {
            get { return _canStop; }
            set
            {
                _canStop = value;
                NotifyPropertyChanged(nameof(CanStop));
            }
        }

        private Boolean _canBrowse = true;
        /// <summary>
        /// Databinding for can browse for database.
        /// </summary>
        public Boolean CanBrowse
        {
            get { return _canBrowse; }
            set
            {
                _canBrowse = value;
                NotifyPropertyChanged(nameof(CanBrowse));
            }
        }

        private Boolean _canSelectRange = true;
        /// <summary>
        /// Databinding for can select daquisition resolution.
        /// </summary>
        public Boolean CanSelectRange
        {
            get { return _canSelectRange; }
            set
            {
                _canSelectRange = value;
                NotifyPropertyChanged(nameof(CanSelectRange));
            }
        }

        #endregion

        #region Commands

        private ICommand _startCommand;
        /// <summary>
        /// Command for the Start button.
        /// </summary>
        public ICommand StartCommand
        {
            get
            {
                if (_startCommand == null)
                    _startCommand = new RelayCommand(() => StartDaq());
                return _startCommand;
            }
        }
        private void StartDaq()
        {
            //Insert the range value into the table
            if (CanSelectRange)
            {
                Database.InsertInfoCommand.Parameters.Add(new SQLiteParameter("Resolution", (int)Resolution));
                Database.ExecuteCommand(Database.InsertInfoCommand);
                CanSelectRange = false;
            }

            //Begin Daquisition
            int packetSize = 16;

            _dataPtr = MccService.ScaledWinBufAllocEx(32);
            ScanOptions options = ScanOptions.SingleIo | ScanOptions.Continuous | ScanOptions.Background | ScanOptions.ScaleData;
            if (SamplePeriod > 1000)
            {
                options |= ScanOptions.HighResRate;
            }

            if (DaqBoard.BoardName == "USB-1608G")
            {
                packetSize = 8;
            }

            if (DaqBoard.BoardName == "USB-1408FS")
            {
                MccService.WinBufFreeEx(_dataPtr);
                _dataPtr = MccService.WinBufAllocEx(4);
                packetSize = 2;
                options = ScanOptions.SingleIo | ScanOptions.Continuous | ScanOptions.Background;
            }

            _daqBoard.AInScan(0, numChannels, 2 * packetSize, ref _sampleRate, Resolution, _dataPtr, options);
            _isReading = true;
            BeginChartUpdateAsync();

            //Set buttons and status label to Running state
            CanStart = false;
            CanStop = true;
            CanBrowse = false;
            Status = "Running";
            StatusBrush = Brushes.LimeGreen;
        }

        private ICommand _stopCommand;
        /// <summary>
        /// Command for the Stop button.
        /// </summary>
        public ICommand StopCommand
        {
            get
            {
                if (_stopCommand == null)
                    _stopCommand = new RelayCommand(() => StopDaq());
                return _stopCommand;
            }
        }
        private void StopDaq()
        {
            //Stop Daquisition
            _daqBoard.StopBackground(MccDaq.FunctionType.AiFunction);
            //Reset buttons and flag for chart update
            _isReading = false;
            MccService.WinBufFreeEx(_dataPtr);
            CanStart = true;
            CanStop = false;
            CanBrowse = true;
            Status = "Stopped";
            StatusBrush = Brushes.Red;
        }

        private ICommand _browseCommand;
        /// <summary>
        /// Command for the Browse button.
        /// </summary>
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
            Database.SaveDialog.FileOk += SaveFile_FileOk;
            Database.SaveDialog.ShowDialog();
        }

        #endregion

        /// <summary>
        /// Constructor. Initializes GridData, Series, DaqDevices, and MccBoards properties.
        /// </summary>
        public LoggerViewModel()
        {
            //Init GridData
            GridData.Columns.Add(new DataColumn("Device", typeof(int)));
            GridData.Columns.Add(new DataColumn("Value", typeof(double)));

            //Init Series
            for (int i = 0; i < 16; i++)
            {
                GridData.Rows.Add(i, 0);
                GLineSeries gSeries = new GLineSeries
                {
                    Title = "Device " + i.ToString(),
                    Values = new GearedValues<double>().WithQuality(Quality.Low),
                    Fill = Brushes.Transparent,
                    StrokeThickness = .5,
                    PointGeometry = null                 
                };
                Series.Add(gSeries);
            }

            //Init DataReceived event
            DataReceivedDelegate = DaqBoard_DataReceived;
            DeviceInventory = DaqDeviceManager.GetDaqDeviceInventory(DaqDeviceInterface.Any);
            ErrorInfo = MccDaq.MccService.ErrHandling(MccDaq.ErrorReporting.PrintAll, MccDaq.ErrorHandling.DontStop);

            DaqDevices = "DAQ Devices: " + DeviceInventory.Length.ToString();

            //Init MccBoards
            MccBoard board;
            for (int i = 0; i < DeviceInventory.Length; i++)
            {
                try
                {
                    board = DaqDeviceManager.CreateDaqDevice(0, DeviceInventory[0]);
                    MccBoards.Add(board);

                    Status = "Stopped";
                    StatusBrush = Brushes.Red;
                }
                catch (ULException ule)
                {
                    if (ule.ErrorInfo.Value == ErrorCode.BoardNumInUse)
                    {
                        board = new MccBoard(i);
                        MccBoards.Add(board);
                        Status = "Stopped";
                        StatusBrush = Brushes.Red;
                    }
                    else
                    {
                        Status = ule.Message;
                    }
                }
            }

            //Prepare for user input
            _isReading = false;
            _updateChart = false;
        }

        /// <summary>
        /// DataReceived Event handler
        /// </summary>
        bool tick = true;
        private void DaqBoard_DataReceived(int BoardNum, EventType EventType, uint EventData, IntPtr pUserData)
        {
            //Get timestamp immediately, increment SampleCount.
            _timeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            SampleCount++;

            //Copy input to Data[], using temp[] to notify the change.
            if (DaqBoard.BoardName != "USB-1408FS")
            {

                double[] temp = new double[16];
                if (tick)
                {
                    MccService.ScaledWinBufToArray(_dataPtr, temp, 0, numChannels + 1);
                    tick = false;
                }
                else
                {
                    MccService.ScaledWinBufToArray(_dataPtr, temp, numChannels + 1, numChannels + 1);
                    tick = true;
                }
                _data = temp;
            }
            else
            {
                short[] tempShort = new short[4];
                MccService.WinBufToArray(_dataPtr, tempShort, 0, 4);
                float[] tempFloat = new float[4];
                double[] tempDouble = new double[16];

                for (int i = 0; i < 4; i++)
                {
                    DaqBoard.ToEngUnits(Resolution, tempShort[i], out tempFloat[i]);
                    tempDouble[i] = (double)tempFloat[i];
                }
                _data = tempDouble;
            }

            //Update chart on the worker thread
            _updateChart = true;

            //Prep command parameters
            Database.InsertRowCommand.Parameters.Add(new SQLiteParameter("Time", _timeStamp));
            
            //TODO: Use a Sync Queue to insert data




            for (int i = 0; i < _data.Length; i++)
            {
                if (_data[i] != 0.0)
                {
                    GridData.Rows[i][1] = _data[i];
                    Database.InsertRowCommand.Parameters.Add(new SQLiteParameter("Dev" + i.ToString(), _data[i]));
                }
                else
                {
                    Database.InsertRowCommand.Parameters.Add(new SQLiteParameter("Dev" + i.ToString(), null));
                }
                //If more than ~1hr of data at 200ms, remove to keep memory low
                if (SampleCount > 20000)
                {
                    Series[i].Values.RemoveAt(0);
                }
            }

            //Execute Command
            Database.ExecuteCommand(Database.InsertRowCommand);






            //Update X Axis bounds
            if (SampleCount >= XAxisMax)
            {
                XAxisMax = SampleCount;
                XAxisMin = SampleCount - DisplayPoints;
            }
        }

        /// <summary>
        /// Event handler for when user selects database.
        /// </summary>
        private void SaveFile_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            for (int i = 0; i < Series.Count; i++)
            {
                Series[i].Values.Clear();
                SampleCount = 0;
            }
            //Set Filepath and initialize connection with database
            Filepath = ((SaveFileDialog)sender).FileName;
            Database.CreateConnection(Filepath);

            //Load any existing data from INFO table
            CanSelectRange = true;
            if (Database.Connection != null)
            {
                try
                {
                    Database.Connection.Open();
                    SQLiteDataReader reader = Database.GetInfoCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        try
                        {
                            Resolution = (Range)reader[0];
                            CanSelectRange = false;
                        }
                        catch (Exception ex)
                        { }
                    }
                    reader.Close();
                    Database.Connection.Close();
                }
                catch (SQLiteException sqlex)
                {
                }
            }

            //Set Start
            if (DaqBoard != null)
            {
                CanStart = true;
            }
        }

        /// <summary>
        /// Starts a worker thread to handle the chart updates
        /// </summary>
        private void BeginChartUpdateAsync()
        {
            //Action to execute on worker thread
            Action DataAvailable = () =>
            {
                //Keep thread alive while reading
                while (_isReading)
                {
                    //only update on data available
                    while (_updateChart)
                    {
                        for (int i = 0; i < _data.Length; i++)
                        {
                            Series[i].Values.Add(_data[i]);
                        }

                        _updateChart = false;
                    }
                }
            };

            //Start new thread
            Task.Factory.StartNew(DataAvailable);
        }
    }
}
