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

		public string ToCSV(bool ToFile=false)
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var dateformat = ToFile ? "dd/MM/yy HH:mm" : "'\"'dd/MM/yy HH:mm'\"'";
			var blank = ToFile ? "" : "\"\"";
			var sep = ',';

			var sb = new StringBuilder(350);
			sb.Append(Timestamp.ToString(dateformat, invDate)).Append(sep);
			sb.Append(Utils.ToUnixTime(Timestamp)).Append(sep);
			sb.Append(DewPoint1.HasValue ? DewPoint1.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(DewPoint2.HasValue ? DewPoint2.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(DewPoint3.HasValue ? DewPoint3.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(DewPoint4.HasValue ? DewPoint4.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(DewPoint5.HasValue ? DewPoint5.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(DewPoint6.HasValue ? DewPoint6.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(DewPoint7.HasValue ? DewPoint7.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(DewPoint8.HasValue ? DewPoint8.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(DewPoint9.HasValue ? DewPoint9.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(DewPoint10.HasValue ? DewPoint10.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
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
