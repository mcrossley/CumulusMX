using System;
using System.Globalization;
using System.Text;
using SQLite;

namespace CumulusMX
{
	class ExtraHum
	{
		[PrimaryKey]
		public DateTime Timestamp { get; set; }
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

		public string ToCSV()
		{
			//var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var sb = new StringBuilder(350);
			sb.Append(Timestamp.ToString("'\"'dd/MM/yy HH:mm'\"'", invDate)).Append(',');
			sb.Append(Utils.ToUnixTime(Timestamp)).Append(",\"");
			if (Hum1.HasValue) sb.Append(Hum1.Value);
			sb.Append("\",\"");
			if (Hum2.HasValue) sb.Append(Hum2.Value);
			sb.Append("\",\"");
			if (Hum3.HasValue) sb.Append(Hum3.Value);
			sb.Append("\",\"");
			if (Hum4.HasValue) sb.Append(Hum4.Value);
			sb.Append("\",\"");
			if (Hum5.HasValue) sb.Append(Hum5.Value);
			sb.Append("\",\"");
			if (Hum6.HasValue) sb.Append(Hum6.Value);
			sb.Append("\",\"");
			if (Hum7.HasValue) sb.Append(Hum7.Value);
			sb.Append("\",\"");
			if (Hum8.HasValue) sb.Append(Hum8.Value);
			sb.Append("\",\"");
			if (Hum9.HasValue) sb.Append(Hum9.Value);
			sb.Append("\",\"");
			if (Hum10.HasValue) sb.Append(Hum10.Value);
			sb.Append('"');
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = Utils.FromUnixTime(long.Parse(data[1]));
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
	}
}
