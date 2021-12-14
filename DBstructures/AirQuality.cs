using System;
using System.Globalization;
using System.Text;
using SQLite;

namespace CumulusMX
{
	class AirQuality
	{
		[PrimaryKey]
		public DateTime Timestamp { get; set; }
		public double? Aq1 { get; set; }
		public double? AqAvg1 { get; set; }
		public double? Aq2 { get; set; }
		public double? AqAvg2 { get; set; }
		public double? Aq3 { get; set; }
		public double? AqAvg3 { get; set; }
		public double? Aq4 { get; set; }
		public double? AqAvg4 { get; set; }

		public string ToCSV()
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var sb = new StringBuilder(350);
			sb.Append(Timestamp.ToString("'\"'dd/MM/yy HH:mm'\"'", invDate)).Append(',');
			sb.Append(Utils.ToUnixTime(Timestamp)).Append(",\"");
			if (Aq1.HasValue) sb.Append(Aq1.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (AqAvg1.HasValue) sb.Append(AqAvg1.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (Aq2.HasValue) sb.Append(Aq2.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (AqAvg2.HasValue) sb.Append(AqAvg2.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (Aq3.HasValue) sb.Append(Aq3.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (AqAvg3.HasValue) sb.Append(AqAvg3.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (Aq4.HasValue) sb.Append(Aq4.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (AqAvg4.HasValue) sb.Append(AqAvg4.Value.ToString("F1", invNum));
			sb.Append('"');
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = Utils.FromUnixTime(long.Parse(data[1]));
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
	}
}
