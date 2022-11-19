using System;
using System.Globalization;
using System.Text;
using ServiceStack.Text;
using SQLite;

namespace CumulusMX
{
	class ExtraHum
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
		public double? Hum1 { get; set; }
		public double? Hum2 { get; set; }
		public double? Hum3 { get; set; }
		public double? Hum4 { get; set; }
		public double? Hum5 { get; set; }
		public double? Hum6 { get; set; }
		public double? Hum7 { get; set; }
		public double? Hum8 { get; set; }
		public double? Hum9 { get; set; }
		public double? Hum10 { get; set; }

		public string ToCSV(bool ToFile=false)
		{
			//var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var dateformat = ToFile ? "dd/MM/yy HH:mm" : "'\"'dd/MM/yy HH:mm'\"'";
			var blank = ToFile ? "" : "\"\"";
			var sep = ',';

			var sb = new StringBuilder(350);
			sb.Append(Time.ToString(dateformat, invDate)).Append(sep);
			sb.Append(Timestamp).Append(sep);
			sb.Append(Hum1.HasValue ? Hum1 : blank);
			sb.Append(sep);
			sb.Append(Hum2.HasValue ? Hum2 : blank);
			sb.Append(sep);
			sb.Append(Hum3.HasValue ? Hum3 : blank);
			sb.Append(sep);
			sb.Append(Hum4.HasValue ? Hum4 : blank);
			sb.Append(sep);
			sb.Append(Hum5.HasValue ? Hum5 : blank);
			sb.Append(sep);
			sb.Append(Hum6.HasValue ? Hum6 : blank);
			sb.Append(sep);
			sb.Append(Hum7.HasValue ? Hum7 : blank);
			sb.Append(sep);
			sb.Append(Hum8.HasValue ? Hum8 : blank);
			sb.Append(sep);
			sb.Append(Hum9.HasValue ? Hum9 : blank);
			sb.Append(sep);
			sb.Append(Hum10.HasValue ? Hum10 : blank);
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = long.Parse(data[1]);
			Hum1 = Utils.TryParseNullDouble(data[2]);
			Hum2 = Utils.TryParseNullDouble(data[3]);
			Hum3 = Utils.TryParseNullDouble(data[4]);
			Hum4 = Utils.TryParseNullDouble(data[5]);
			Hum5 = Utils.TryParseNullDouble(data[6]);
			Hum6 = Utils.TryParseNullDouble(data[7]);
			Hum7 = Utils.TryParseNullDouble(data[8]);
			Hum8 = Utils.TryParseNullDouble(data[9]);
			Hum9 = Utils.TryParseNullDouble(data[10]);
			Hum10 = Utils.TryParseNullDouble(data[11]);

			return true;
		}

		public void FromExtraLogFile(string[] data)
		{
			Timestamp = long.Parse(data[1]);
			Hum1 = Utils.TryParseNullInt(data[12]);
			Hum2 = Utils.TryParseNullInt(data[13]);
			Hum3 = Utils.TryParseNullInt(data[14]);
			Hum4 = Utils.TryParseNullInt(data[15]);
			Hum5 = Utils.TryParseNullInt(data[16]);
			Hum6 = Utils.TryParseNullInt(data[17]);
			Hum7 = Utils.TryParseNullInt(data[18]);
			Hum8 = Utils.TryParseNullInt(data[19]);
			Hum9 = Utils.TryParseNullInt(data[20]);
			Hum10 = Utils.TryParseNullInt(data[21]);
		}
	}
}
