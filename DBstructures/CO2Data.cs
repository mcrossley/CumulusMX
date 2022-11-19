using System;
using System.Globalization;
using System.Text;
using ServiceStack.Text;
using SQLite;

namespace CumulusMX
{
	class CO2Data
	{
		private DateTime _time;
		private long _timestamp;

		[Ignore]
		public DateTime Time
		{
			get { return _time; }
			set
			{
				if (value.Kind == DateTimeKind.Unspecified)
					_time = DateTime.SpecifyKind(value, DateTimeKind.Local);
				else
					_time = value;
				Timestamp = value.ToUnixTime();
			}
		}

		[PrimaryKey]
		public long Timestamp
		{
			get { return _timestamp; }
			set
			{
				_timestamp = value;
				_time = value.FromUnixTime().ToLocalTime();
			}
		}
		public int? CO2now { get; set; }
		public int? CO2avg { get; set; }
		public double? Pm2p5 { get; set; }
		public double? Pm2p5avg { get; set; }
		public double? Pm10 { get; set; }
		public double? Pm10avg { get; set; }
		public double? Temp { get; set; }
		public double? Hum { get; set; }

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
			sb.Append(CO2now.HasValue ? CO2now : blank);
			sb.Append(sep);
			sb.Append(CO2avg.HasValue ? CO2avg.Value : blank);
			sb.Append(sep);
			sb.Append(Pm2p5.HasValue ? Pm2p5.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(Pm2p5avg.HasValue ? Pm2p5avg.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(Pm10.HasValue ? Pm10.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(Pm10avg.HasValue ? Pm10avg.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp.HasValue ? Temp.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(Hum.HasValue ? Hum.Value.ToString("F0", invNum) : blank);
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = long.Parse(data[1]);
			CO2now = Utils.TryParseNullInt(data[2]);
			CO2avg = Utils.TryParseNullInt(data[3]);
			Pm2p5 = Utils.TryParseNullDouble(data[4]);
			Pm2p5avg = Utils.TryParseNullDouble(data[5]);
			Pm10 = Utils.TryParseNullDouble(data[6]);
			Pm10avg = Utils.TryParseNullDouble(data[7]);
			Temp = Utils.TryParseNullDouble(data[8]);
			Hum = Utils.TryParseNullDouble(data[9]);

			return true;
		}

		public void FromExtraLogFile(string[] data)
		{
			Timestamp = long.Parse(data[1]);
			CO2now = Utils.TryParseNullInt(data[84]);
			CO2avg = Utils.TryParseNullInt(data[85]);
			Pm2p5 = Utils.TryParseNullDouble(data[86]);
			Pm2p5avg = Utils.TryParseNullDouble(data[87]);
			Pm10 = Utils.TryParseNullDouble(data[88]);
			Pm10avg = Utils.TryParseNullDouble(data[89]);
			Temp = Utils.TryParseNullDouble(data[90]);
			Hum = Utils.TryParseNullDouble(data[91]);
		}
	}
}
