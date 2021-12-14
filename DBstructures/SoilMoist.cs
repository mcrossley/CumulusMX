using System;
using System.Globalization;
using System.Text;
using SQLite;

namespace CumulusMX
{
	class SoilMoist
	{
		[PrimaryKey]
		public DateTime Timestamp { get; set; }
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

		public string ToCSV()
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var sb = new StringBuilder(350);
			sb.Append(Timestamp.ToString("'\"'dd/MM/yy HH:mm'\"'", invDate)).Append(',');
			sb.Append(Utils.ToUnixTime(Timestamp)).Append(",\"");
			if (Moist1.HasValue) sb.Append(Moist1.Value);
			sb.Append("\",\"");
			if (Moist2.HasValue) sb.Append(Moist2.Value);
			sb.Append("\",\"");
			if (Moist3.HasValue) sb.Append(Moist3.Value);
			sb.Append("\",\"");
			if (Moist4.HasValue) sb.Append(Moist4.Value);
			sb.Append("\",\"");
			if (Moist5.HasValue) sb.Append(Moist5.Value);
			sb.Append("\",\"");
			if (Moist6.HasValue) sb.Append(Moist6.Value);
			sb.Append("\",\"");
			if (Moist7.HasValue) sb.Append(Moist7.Value);
			sb.Append("\",\"");
			if (Moist8.HasValue) sb.Append(Moist8.Value);
			sb.Append("\",\"");
			if (Moist9.HasValue) sb.Append(Moist9.Value);
			sb.Append("\",\"");
			if (Moist10.HasValue) sb.Append(Moist10.Value);
			sb.Append("\",\"");
			if (Moist11.HasValue) sb.Append(Moist11.Value);
			sb.Append("\",\"");
			if (Moist12.HasValue) sb.Append(Moist12.Value);
			sb.Append("\",\"");
			if (Moist13.HasValue) sb.Append(Moist13.Value);
			sb.Append("\",\"");
			if (Moist14.HasValue) sb.Append(Moist14.Value);
			sb.Append("\",\"");
			if (Moist15.HasValue) sb.Append(Moist15.Value);
			sb.Append("\",\"");
			if (Moist16.HasValue) sb.Append(Moist16.Value);
			sb.Append('"');
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = Utils.FromUnixTime(long.Parse(data[1]));
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
	}
}
