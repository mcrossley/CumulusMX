using System;
using System.Globalization;
using System.Text;
using ServiceStack.Text;
using SQLite;

namespace CumulusMX
{
	class SoilMoist
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
		public int? Moist1 { get; set; }
		public int? Moist2 { get; set; }
		public int? Moist3 { get; set; }
		public int? Moist4 { get; set; }
		public int? Moist5 { get; set; }
		public int? Moist6 { get; set; }
		public int? Moist7 { get; set; }
		public int? Moist8 { get; set; }
		public int? Moist9 { get; set; }
		public int? Moist10 { get; set; }
		public int? Moist11 { get; set; }
		public int? Moist12 { get; set; }
		public int? Moist13 { get; set; }
		public int? Moist14 { get; set; }
		public int? Moist15 { get; set; }
		public int? Moist16 { get; set; }

		public string ToCSV(bool ToFile = false)
		{
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var dateformat = ToFile ? "dd/MM/yy HH:mm" : "'\"'dd/MM/yy HH:mm'\"'";
			var blank = ToFile ? "" : "\"\"";
			var sep = ',';

			var sb = new StringBuilder(350);
			sb.Append(Time.ToString(dateformat, invDate)).Append(sep);
			sb.Append(Timestamp).Append(sep);
			sb.Append(Moist1.HasValue ? Moist1 : blank);
			sb.Append(sep);
			sb.Append(Moist2.HasValue ? Moist2 : blank);
			sb.Append(sep);
			sb.Append(Moist3.HasValue ? Moist3 : blank);
			sb.Append(sep);
			sb.Append(Moist4.HasValue ? Moist4 : blank);
			sb.Append(sep);
			sb.Append(Moist5.HasValue ? Moist5 : blank);
			sb.Append(sep);
			sb.Append(Moist6.HasValue ? Moist6 : blank);
			sb.Append(sep);
			sb.Append(Moist7.HasValue ? Moist7 : blank);
			sb.Append(sep);
			sb.Append(Moist8.HasValue ? Moist8 : blank);
			sb.Append(sep);
			sb.Append(Moist9.HasValue ? Moist9 : blank);
			sb.Append(sep);
			sb.Append(Moist10.HasValue ? Moist10 : blank);
			sb.Append(sep);
			sb.Append(Moist11.HasValue ? Moist11 : blank);
			sb.Append(sep);
			sb.Append(Moist12.HasValue ? Moist12 : blank);
			sb.Append(sep);
			sb.Append(Moist13.HasValue ? Moist13 : blank);
			sb.Append(sep);
			sb.Append(Moist14.HasValue ? Moist14 : blank);
			sb.Append(sep);
			sb.Append(Moist15.HasValue ? Moist15 : blank);
			sb.Append(sep);
			sb.Append(Moist16.HasValue ? Moist16 : blank);
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = long.Parse(data[1]);
			Moist1 = Utils.TryParseNullInt(data[2]);
			Moist2 = Utils.TryParseNullInt(data[3]);
			Moist3 = Utils.TryParseNullInt(data[4]);
			Moist4 = Utils.TryParseNullInt(data[5]);
			Moist5 = Utils.TryParseNullInt(data[6]);
			Moist6 = Utils.TryParseNullInt(data[7]);
			Moist7 = Utils.TryParseNullInt(data[8]);
			Moist8 = Utils.TryParseNullInt(data[9]);
			Moist9 = Utils.TryParseNullInt(data[10]);
			Moist10 = Utils.TryParseNullInt(data[11]);
			Moist11 = Utils.TryParseNullInt(data[12]);
			Moist12 = Utils.TryParseNullInt(data[13]);
			Moist13 = Utils.TryParseNullInt(data[14]);
			Moist14 = Utils.TryParseNullInt(data[15]);
			Moist15 = Utils.TryParseNullInt(data[16]);
			Moist16 = Utils.TryParseNullInt(data[17]);

			return true;
		}

		public void FromExtraLogFile(string[] data)
		{
			Timestamp = long.Parse(data[1]);
			Moist1 = Utils.TryParseNullInt(data[36]);
			Moist2 = Utils.TryParseNullInt(data[37]);
			Moist3 = Utils.TryParseNullInt(data[38]);
			Moist4 = Utils.TryParseNullInt(data[39]);

			Moist5 = Utils.TryParseNullInt(data[56]);
			Moist6 = Utils.TryParseNullInt(data[57]);
			Moist7 = Utils.TryParseNullInt(data[58]);
			Moist8 = Utils.TryParseNullInt(data[59]);
			Moist9 = Utils.TryParseNullInt(data[60]);
			Moist10 = Utils.TryParseNullInt(data[61]);
			Moist11 = Utils.TryParseNullInt(data[62]);
			Moist12 = Utils.TryParseNullInt(data[63]);
			Moist13 = Utils.TryParseNullInt(data[64]);
			Moist14 = Utils.TryParseNullInt(data[65]);
			Moist15 = Utils.TryParseNullInt(data[66]);
			Moist16 = Utils.TryParseNullInt(data[66]);
		}
	}
}
