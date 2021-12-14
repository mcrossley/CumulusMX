using System;
using System.Globalization;
using System.Text;
using SQLite;

namespace CumulusMX
{
	class LeafTemp
	{
		[PrimaryKey]
		public DateTime Timestamp { get; set; }
		public double? Temp1 { get; set; }
		public double? Temp2 { get; set; }
		public double? Temp3 { get; set; }
		public double? Temp4 { get; set; }

		public string ToCSV()
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var sb = new StringBuilder(350);
			sb.Append(Timestamp.ToString("'\"'dd/MM/yy HH:mm'\"'", invDate)).Append(',');
			sb.Append(Utils.ToUnixTime(Timestamp)).Append(",\"");
			if (Temp1.HasValue) sb.Append(Temp1.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (Temp2.HasValue) sb.Append(Temp2.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (Temp3.HasValue) sb.Append(Temp3.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (Temp4.HasValue) sb.Append(Temp4.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append('"');
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = Utils.FromUnixTime(long.Parse(data[1]));
			Temp1 = Utils.TryParseNullDouble(data[2]);
			Temp2 = Utils.TryParseNullDouble(data[3]);
			Temp3 = Utils.TryParseNullDouble(data[4]);
			Temp4 = Utils.TryParseNullDouble(data[5]);

			return true;
		}
	}
}
