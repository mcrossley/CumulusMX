﻿using System;
using System.Globalization;
using System.Text;
using ServiceStack.Text;
using SQLite;

namespace CumulusMX
{
	class LeafWet
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
		public double? Wet1 { get; set; }
		public double? Wet2 { get; set; }
		public double? Wet3 { get; set; }
		public double? Wet4 { get; set; }
		public double? Wet5 { get; set; }
		public double? Wet6 { get; set; }
		public double? Wet7 { get; set; }
		public double? Wet8 { get; set; }

		public string ToCSV(bool ToFile=false)
		{
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var dateformat = ToFile ? "dd/MM/yy HH:mm" : "'\"'dd/MM/yy HH:mm'\"'";
			var blank = ToFile ? "" : "\"\"";
			var sep = ',';

			var sb = new StringBuilder(350);
			sb.Append(Time.ToLocalTime().ToString(dateformat, invDate)).Append(sep);
			sb.Append(Utils.ToUnixTime(Time)).Append(sep);
			sb.Append(Wet1.HasValue ? Wet1.Value.ToString("F1") : blank);
			sb.Append(sep);
			sb.Append(Wet2.HasValue ? Wet2.Value.ToString("F1") : blank);
			sb.Append(sep);
			sb.Append(Wet3.HasValue ? Wet3.Value.ToString("F1") : blank);
			sb.Append(sep);
			sb.Append(Wet4.HasValue ? Wet4.Value.ToString("F1") : blank);
			sb.Append(sep);
			sb.Append(Wet5.HasValue ? Wet5.Value.ToString("F1") : blank);
			sb.Append(sep);
			sb.Append(Wet6.HasValue ? Wet6.Value.ToString("F1") : blank);
			sb.Append(sep);
			sb.Append(Wet7.HasValue ? Wet7.Value.ToString("F1") : blank);
			sb.Append(sep);
			sb.Append(Wet8.HasValue ? Wet8.Value.ToString("F1") : blank);
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Time = Utils.FromUnixTime(long.Parse(data[1]));
			Wet1 = Utils.TryParseNullDouble(data[2]);
			Wet2 = Utils.TryParseNullDouble(data[3]);
			Wet3 = Utils.TryParseNullDouble(data[4]);
			Wet4 = Utils.TryParseNullDouble(data[5]);
			Wet5 = Utils.TryParseNullDouble(data[6]);
			Wet6 = Utils.TryParseNullDouble(data[7]);
			Wet7 = Utils.TryParseNullDouble(data[8]);
			Wet8 = Utils.TryParseNullDouble(data[9]);

			return true;
		}
	}
}
