using System;
using System.Globalization;
using System.Text;
using ServiceStack.Text;
using SQLite;

namespace CumulusMX
{
	class AirQuality
	{
		private DateTime time;
		private long timestamp;

		[Ignore]
		public DateTime Time
		{
			get { return time; }
			set
			{
				time = value;
				Timestamp = value.ToUnixTime();
			}
		}

		[PrimaryKey]
		public long Timestamp
		{
			get { return timestamp; }
			set
			{
				timestamp = value;
				time = value.FromUnixTime().ToLocalTime();
			}
		}
		public double? Aq1 { get; set; }
		public double? AqAvg1 { get; set; }
		public double? Aq2 { get; set; }
		public double? AqAvg2 { get; set; }
		public double? Aq3 { get; set; }
		public double? AqAvg3 { get; set; }
		public double? Aq4 { get; set; }
		public double? AqAvg4 { get; set; }

		public string ToCSV(bool ToFile = false)
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var dateformat = ToFile ? "dd/MM/yy HH:mm" : "'\"'dd/MM/yy HH:mm'\"'";
			var blank = ToFile ? "" : "\"\"";
			var sep = ',';

			var sb = new StringBuilder(350);
			sb.Append(Time.ToString(dateformat, invDate)).Append(sep);
			sb.Append(Timestamp).Append(sep);
			sb.Append(Aq1.HasValue ? Aq1.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(AqAvg1.HasValue ? AqAvg1.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(Aq2.HasValue ? Aq2.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(AqAvg2.HasValue ? AqAvg2.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(Aq3.HasValue ? Aq3.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(AqAvg3.HasValue ? AqAvg3.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(Aq4.HasValue ? Aq4.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(AqAvg4.HasValue ? AqAvg4.Value.ToString("F1", invNum) : blank);
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = long.Parse(data[1]);
			Aq1 = Utils.TryParseNullDouble(data[2]);
			AqAvg1 = Utils.TryParseNullDouble(data[3]);
			Aq2 = Utils.TryParseNullDouble(data[4]);
			AqAvg2 = Utils.TryParseNullDouble(data[5]);
			Aq3 = Utils.TryParseNullDouble(data[6]);
			AqAvg3 = Utils.TryParseNullDouble(data[7]);
			Aq4 = Utils.TryParseNullDouble(data[8]);
			AqAvg4 = Utils.TryParseNullDouble(data[9]);

			return true;
		}

		public void FromExtraLogFile(string[] data)
		{
			Timestamp = long.Parse(data[1]);
			Aq1 = Utils.TryParseNullDouble(data[68]);
			Aq2 = Utils.TryParseNullDouble(data[69]);
			Aq3 = Utils.TryParseNullDouble(data[70]);
			Aq4 = Utils.TryParseNullDouble(data[71]);
			AqAvg1 = Utils.TryParseNullDouble(data[72]);
			AqAvg2 = Utils.TryParseNullDouble(data[73]);
			AqAvg3 = Utils.TryParseNullDouble(data[74]);
			AqAvg4 = Utils.TryParseNullDouble(data[75]);
		}
	}
}
