using System;
using System.Globalization;
using System.Text;
using SQLite;

namespace CumulusMX
{
	class CO2Data
	{
		[PrimaryKey]
		public DateTime Timestamp { get; set; }
		public int? CO2now { get; set; }
		public int? CO2avg { get; set; }
		public double? Pm2p5 { get; set; }
		public double? Pm2p5avg { get; set; }
		public double? Pm10 { get; set; }
		public double? Pm10avg { get; set; }
		public double? Temp { get; set; }
		public double? Hum { get; set; }

		public string ToCSV()
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var sb = new StringBuilder(350);
			sb.Append(Timestamp.ToString("'\"'dd/MM/yy HH:mm'\"'", invDate)).Append(',');
			sb.Append(Utils.ToUnixTime(Timestamp)).Append(",\"");
			if (CO2now.HasValue) sb.Append(CO2now.Value);
			sb.Append("\",\"");
			if (CO2avg.HasValue) sb.Append(CO2avg.Value);
			sb.Append("\",\"");
			if (Pm2p5.HasValue) sb.Append(Pm2p5.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (Pm2p5avg.HasValue) sb.Append(Pm2p5avg.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (Pm10.HasValue) sb.Append(Pm10.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (Pm10avg.HasValue) sb.Append(Pm10avg.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (Temp.HasValue) sb.Append(Temp.Value.ToString("F1", invNum));
			sb.Append("\",\"");
			if (Hum.HasValue) sb.Append(Hum.Value.ToString("F0", invNum));
			sb.Append('"');
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = Utils.FromUnixTime(long.Parse(data[1]));
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
	}
}
