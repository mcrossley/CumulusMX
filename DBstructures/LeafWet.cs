using System;
using System.Globalization;
using System.Text;
using SQLite;

namespace CumulusMX
{
	class LeafWet
	{
		[PrimaryKey]
		public DateTime Timestamp { get; set; }
		public int? Wet1 { get; set; }
		public int? Wet2 { get; set; }
		public int? Wet3 { get; set; }
		public int? Wet4 { get; set; }
		public int? Wet5 { get; set; }
		public int? Wet6 { get; set; }
		public int? Wet7 { get; set; }
		public int? Wet8 { get; set; }

		public string ToCSV()
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var sb = new StringBuilder(350);
			sb.Append(Timestamp.ToString("'\"'dd/MM/yy HH:mm'\"'", invDate)).Append(',');
			sb.Append(Utils.ToUnixTime(Timestamp)).Append(",\"");
			if (Wet1.HasValue) sb.Append(Wet1.Value);
			sb.Append("\",\"");
			if (Wet2.HasValue) sb.Append(Wet2.Value);
			sb.Append("\",\"");
			if (Wet3.HasValue) sb.Append(Wet3.Value);
			sb.Append("\",\"");
			if (Wet4.HasValue) sb.Append(Wet4.Value);
			sb.Append("\",\"");
			if (Wet5.HasValue) sb.Append(Wet5.Value);
			sb.Append("\",\"");
			if (Wet6.HasValue) sb.Append(Wet6.Value);
			sb.Append("\",\"");
			if (Wet7.HasValue) sb.Append(Wet7.Value);
			sb.Append("\",\"");
			if (Wet8.HasValue) sb.Append(Wet8.Value);
			sb.Append('"');
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = Utils.FromUnixTime(long.Parse(data[1]));
			Wet1 = Utils.TryParseNullInt(data[2]);
			Wet2 = Utils.TryParseNullInt(data[3]);
			Wet3 = Utils.TryParseNullInt(data[4]);
			Wet4 = Utils.TryParseNullInt(data[5]);
			Wet5 = Utils.TryParseNullInt(data[6]);
			Wet6 = Utils.TryParseNullInt(data[7]);
			Wet7 = Utils.TryParseNullInt(data[8]);
			Wet8 = Utils.TryParseNullInt(data[9]);

			return true;
		}
	}
}
