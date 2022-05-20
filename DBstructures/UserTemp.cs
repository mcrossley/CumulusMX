﻿using System;
using System.Globalization;
using System.Text;
using SQLite;

namespace CumulusMX
{
	class UserTemp
	{
		[PrimaryKey]
		public DateTime Timestamp { get; set; }
		public double? Temp1 { get; set; }
		public double? Temp2 { get; set; }
		public double? Temp3 { get; set; }
		public double? Temp4 { get; set; }
		public double? Temp5 { get; set; }
		public double? Temp6 { get; set; }
		public double? Temp7 { get; set; }
		public double? Temp8 { get; set; }
		public double? Temp9 { get; set; }
		public double? Temp10 { get; set; }

		public string ToCSV(bool ToFile=false)
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var dateformat = ToFile ? "dd/MM/yy HH:mm" : "'\"'dd/MM/yy HH:mm'\"'";
			var blank = ToFile ? "" : "\"\"";
			var sep = ',';

			var sb = new StringBuilder(350);
			sb.Append(Timestamp.ToString(dateformat, invDate)).Append(',');
			sb.Append(Utils.ToUnixTime(Timestamp));
			sb.Append(sep);
			sb.Append(Temp1.HasValue ? Temp1.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp2.HasValue ? Temp2.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp3.HasValue ? Temp3.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp4.HasValue ? Temp4.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp5.HasValue ? Temp5.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp6.HasValue ? Temp6.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp7.HasValue ? Temp7.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp8.HasValue ? Temp8.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp9.HasValue ? Temp9.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp10.HasValue ? Temp10.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
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
			Temp5 = Utils.TryParseNullDouble(data[6]);
			Temp6 = Utils.TryParseNullDouble(data[7]);
			Temp7 = Utils.TryParseNullDouble(data[8]);
			Temp8 = Utils.TryParseNullDouble(data[9]);
			Temp9 = Utils.TryParseNullDouble(data[10]);
			Temp10 = Utils.TryParseNullDouble(data[11]);

			return true;
		}
	}
}