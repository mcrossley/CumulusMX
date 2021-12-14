using System;
using System.Globalization;
using System.Text;
using SQLite;

namespace CumulusMX
{
	class ExtraDewPoint
	{
		[PrimaryKey]
		public DateTime Timestamp { get; set; }
		public double? DewPoint1 { get; set; }
		public double? DewPoint2 { get; set; }
		public double? DewPoint3 { get; set; }
		public double? DewPoint4 { get; set; }
		public double? DewPoint5 { get; set; }
		public double? DewPoint6 { get; set; }
		public double? DewPoint7 { get; set; }
		public double? DewPoint8 { get; set; }
		public double? DewPoint9 { get; set; }
		public double? DewPoint10 { get; set; }

		public string ToCSV()
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var sb = new StringBuilder(350);
			sb.Append(Timestamp.ToString("'\"'dd/MM/yy HH:mm'\"'", invDate)).Append(',');
			sb.Append(Utils.ToUnixTime(Timestamp)).Append(",\"");
			if (DewPoint1.HasValue) sb.Append(DewPoint1.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (DewPoint2.HasValue) sb.Append(DewPoint2.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (DewPoint3.HasValue) sb.Append(DewPoint3.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (DewPoint4.HasValue) sb.Append(DewPoint4.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (DewPoint5.HasValue) sb.Append(DewPoint5.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (DewPoint6.HasValue) sb.Append(DewPoint6.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (DewPoint7.HasValue) sb.Append(DewPoint7.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (DewPoint8.HasValue) sb.Append(DewPoint8.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (DewPoint9.HasValue) sb.Append(DewPoint9.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (DewPoint10.HasValue) sb.Append(DewPoint10.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append('"');
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = Utils.FromUnixTime(long.Parse(data[1]));
			DewPoint1 = Utils.TryParseNullDouble(data[2]);
			DewPoint2 = Utils.TryParseNullDouble(data[3]);
			DewPoint3 = Utils.TryParseNullDouble(data[4]);
			DewPoint4 = Utils.TryParseNullDouble(data[5]);
			DewPoint5 = Utils.TryParseNullDouble(data[6]);
			DewPoint6 = Utils.TryParseNullDouble(data[7]);
			DewPoint7 = Utils.TryParseNullDouble(data[8]);
			DewPoint8 = Utils.TryParseNullDouble(data[9]);
			DewPoint9 = Utils.TryParseNullDouble(data[10]);
			DewPoint10 = Utils.TryParseNullDouble(data[11]);

			return true;
		}
	}
}
