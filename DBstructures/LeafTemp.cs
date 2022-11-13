using System;
using System.Globalization;
using System.Text;
using ServiceStack.Text;
using SQLite;

namespace CumulusMX
{
	class LeafTemp
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
				time = value.FromUnixTime();
			}
		}
		public double? Temp1 { get; set; }
		public double? Temp2 { get; set; }
		public double? Temp3 { get; set; }
		public double? Temp4 { get; set; }

		public string ToCSV(bool ToFile=false)
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var dateformat = ToFile ? "dd/MM/yy HH:mm" : "'\"'dd/MM/yy HH:mm'\"'";
			var blank = ToFile ? "" : "\"\"";
			var sep = ',';

			var sb = new StringBuilder(350);
			sb.Append(Time.ToLocalTime().ToString(dateformat, invDate)).Append(sep);
			sb.Append(Utils.ToUnixTime(Time)).Append(sep);
			sb.Append(Temp1.HasValue ? Temp1.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp2.HasValue ? Temp2.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp3.HasValue ? Temp3.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp4.HasValue ? Temp4.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Time = Utils.FromUnixTime(long.Parse(data[1]));
			Temp1 = Utils.TryParseNullDouble(data[2]);
			Temp2 = Utils.TryParseNullDouble(data[3]);
			Temp3 = Utils.TryParseNullDouble(data[4]);
			Temp4 = Utils.TryParseNullDouble(data[5]);

			return true;
		}
	}
}
