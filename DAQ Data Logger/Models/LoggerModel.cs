using MccDaq;
using System.Collections.ObjectModel;

namespace DAQ_Data_Logger.Models
{
    /// <summary>
    /// Model for a DataLogger. Includes Supporded range collections.
    /// </summary>
    class LoggerModel
    {
        /// <summary>
        /// Supported ranges for a USB-2416.
        /// </summary>
        private Range[] _range2416 =
        {
            Range.BipPt078Volts,
            Range.BipPt156Volts,
            Range.BipPt312Volts,
            Range.BipPt625Volts,
            Range.Bip1Pt25Volts,
            Range.Bip2Pt5Volts,
            Range.Bip5Volts,
            Range.Bip10Volts,
            Range.Bip20Volts
        };
        public ObservableCollection<Range> Range2416
        {
            get { return new ObservableCollection<Range>(_range2416); }
        }

        /// <summary>
        /// Supported ranges for a USB-1608G.
        /// </summary>
        private Range[] _range1608G =
        {
            Range.Bip1Volts,
            Range.Bip2Volts,
            Range.Bip5Volts,
            Range.Bip10Volts
        };
        public ObservableCollection<Range> Range1608G
        {
            get { return new ObservableCollection<Range>(_range1608G); }
        }

        /// <summary>
        /// Supported ranges for a USB-1408FS.
        /// </summary>
        private Range[] _range1408FS =
        {
            Range.Bip1Volts,
            Range.Bip1Pt25Volts,
            Range.Bip2Volts,
            Range.Bip2Pt5Volts,
            Range.Bip4Volts,
            Range.Bip5Volts,
            Range.Bip10Volts,
            Range.Bip20Volts
        };
        public ObservableCollection<Range> Range1408FS
        {
            get { return new ObservableCollection<Range>(_range1408FS); }
        }
    }
}
