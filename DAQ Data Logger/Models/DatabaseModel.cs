using Microsoft.Win32;
using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace DAQ_Data_Logger.Models
{
    class DatabaseModel
    {
        private SaveFileDialog _saveDialog = new SaveFileDialog()
        {
            Filter = "SQLite File|*.sqlite",
            OverwritePrompt = false
            
        };
        /// <summary>
        /// SaveFile Dialog. Filters for SQLite databases.
        /// </summary>
        public SaveFileDialog SaveDialog
        {
            get { return _saveDialog; }
            set { _saveDialog = value; }
        }

        private OpenFileDialog _openDialog = new OpenFileDialog()
        {
            Filter = "SQLite File|*.sqlite"
        };
        /// <summary>
        /// OpenFile Dialog. Filters for SQLite databases.
        /// </summary>
        public OpenFileDialog OpenDialog
        {
            get { return _openDialog; }
            set { _openDialog = value; }
        }

        /// <summary>
        /// Connection to database.
        /// </summary>
        public SQLiteConnection Connection { get; set; }

        #region SQLite Commands
        private static string _createDataTableString = "CREATE TABLE Data (Time TEXT, Dev0 REAL, Dev1 REAL, Dev2 REAL, Dev3 REAL, Dev4 REAL, Dev5 REAL, Dev6 REAL, Dev7 REAL, Dev8 REAL, Dev9 REAL, Dev10 REAL, Dev11 REAL, Dev12 REAL, Dev13 REAL, Dev14 REAL, Dev15 REAL)";
        private SQLiteCommand _createDataTableCommand = new SQLiteCommand(_createDataTableString);
        /// <summary>
        /// Command for creating a DAQ Data table.
        /// </summary>
        public SQLiteCommand CreateDataTableCommand
        {
            get { return _createDataTableCommand; }
            set { _createDataTableCommand = value; }
        }

        private static string _createInfoTableString = "CREATE TABLE Info (RangeEnum INT)";
        private SQLiteCommand _createInfoTableCommand = new SQLiteCommand(_createInfoTableString);
        /// <summary>
        /// Command for creating a DAQ Info table.
        /// </summary>
        public SQLiteCommand CreateInfoTableCommand
        {
            get { return _createInfoTableCommand; }
            set { _createInfoTableCommand = value; }
        }

        private static string _insertRowString = "INSERT INTO Data VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
        private SQLiteCommand _insertRowCommand = new SQLiteCommand(_insertRowString);
        /// <summary>
        /// Command for Inserting a row into Data table. 17 Parameters.
        /// </summary>
        public SQLiteCommand InsertRowCommand
        {
            get { return _insertRowCommand; }
            set { _insertRowCommand = value; }
        }

        private static string _insertInfoString = "INSERT INTO Info VALUES (?)";
        private SQLiteCommand _insertInfoCommand = new SQLiteCommand(_insertInfoString);
        /// <summary>
        /// Command for Inserting a row into Info table. 1 Parameter.
        /// </summary>
        public SQLiteCommand InsertInfoCommand
        {
            get { return _insertInfoCommand; }
            set { _insertInfoCommand = value; }
        }

        private static string _getDateRangeString = "SELECT MIN(Time) AS First FROM Data UNION Select MAX(Time) AS Last FROM Data";
        private SQLiteCommand _getDateRangeCommand = new SQLiteCommand(_getDateRangeString);
        /// <summary>
        /// Command for Selecting the First and Last Date/Time in Data table.
        /// </summary>
        public SQLiteCommand GetDateRangeCommand
        {
            get { return _getDateRangeCommand; }
            set { _getDateRangeCommand = value; }
        }

        private static string _getDataRangeString = "SELECT * FROM Data WHERE STRFTIME('%Y-%m-%d %H:%M:%f', Time) BETWEEN ? AND ?";
        private SQLiteCommand _getDataRangeCommand = new SQLiteCommand(_getDataRangeString);
        /// <summary>
        /// Command for Selecting a range of Date/Time from the Data table
        /// </summary>
        public SQLiteCommand GetDataRangeCommand
        {
            get { return _getDataRangeCommand; }
            set { _getDataRangeCommand = value; }
        }

        private static string _getInfoString = "SELECT * FROM Info";
        private SQLiteCommand _getInfoCommand = new SQLiteCommand(_getInfoString);
        /// <summary>
        /// Command for Selecting all information from the Info table.
        /// </summary>
        public SQLiteCommand GetInfoCommand
        {
            get { return _getInfoCommand; }
            set { _getInfoCommand = value; }
        }
        #endregion

        /// <summary>
        /// Creates a SQLite connection and initializes all commands with it. Creates database file if necessary.
        /// </summary>
        /// <param name="FilePath">Path of database to be connected to.</param>
        /// <returns></returns>
        public string CreateConnection(string FilePath)
        {
            try
            {
                Connection = new SQLiteConnection("Data Source = " + FilePath + "; Version = 3;");
                //if file does not exist, create
                if (!File.Exists(FilePath))
                {
                    SQLiteConnection.CreateFile(FilePath);

                    //Insert tables
                    CreateDataTableCommand.Connection = Connection;
                    CreateInfoTableCommand.Connection = Connection;
                    ExecuteCommand(CreateDataTableCommand);
                    ExecuteCommand(CreateInfoTableCommand);
                }
                //Init commands
                InsertRowCommand.Connection = Connection;
                InsertInfoCommand.Connection = Connection;
                GetDateRangeCommand.Connection = Connection;
                GetDataRangeCommand.Connection = Connection;
                GetInfoCommand.Connection = Connection;
            }
            catch (Exception sqlex)
            {
                MessageBox.Show(sqlex.Message);
                return sqlex.Message;
            }
            return null;
        }

        /// <summary>
        /// Executes a query command. Not suitable for Read commands.
        /// </summary>
        /// <param name="Command">Command to be executed.</param>
        /// <returns></returns>
        public string ExecuteCommand(SQLiteCommand Command)
        {
            if (Connection != null)
            {
                try
                {
                    Connection.Open();
                    Command.ExecuteNonQuery();
                    Connection.Close();
                    Command.Parameters.Clear();
                }
                catch (SQLiteException sqlex)
                {
                    Connection.Close();
                    Command.Parameters.Clear();
                    return sqlex.Message;


                }
            }
            return "";
        }
    }
}
