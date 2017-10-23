using System;

namespace DAQ_Data_Logger.Models
{
    /// <summary>
    /// Helper class for LiveCharts and DateTime axes.
    /// </summary>
    public class DateModel
    {
        public DateTime Time { get; set; }
        public double Value { get; set; }
    }
}
