using System;
using System.Globalization;
using System.Text;
using ServiceStack.Text;
using SQLite;

namespace CumulusMX
{
	class SoilTemp
	{
		private DateTime _time;
		private long _timestamp;

		[Ignore]
		public DateTime Time
		{
			get { return _time; }
			set
			{
				if (value.Kind == DateTimeKind.Unspecified)
					_time = DateTime.SpecifyKind(value, DateTimeKind.Local);
				else
					_time = value;
				Timestamp = value.ToUnixTime();
			}
		}

		[PrimaryKey]
		public long Timestamp
		{
			get { return _timestamp; }
			set
			{
				_timestamp = value;
				_time = value.FromUnixTime().ToLocalTime();
			}
		}
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
		public double? Temp11 { get; set; }
		public double? Temp12 { get; set; }
		public double? Temp13 { get; set; }
		public double? Temp14 { get; set; }
		public double? Temp15 { get; set; }
		public double? Temp16 { get; set; }

		public string ToCSV(bool ToFile = false)
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var dateformat = ToFile ? "dd/MM/yy HH:mm" : "'\"'dd/MM/yy HH:mm'\"'";
			var blank = ToFile ? "" : "\"\"";
			var sep = ',';

			var sb = new StringBuilder(350);
			sb.Append(Time.ToString(dateformat, invDate)).Append(',');
			sb.Append(Timestamp).Append(sep);
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
			sb.Append(sep);
			sb.Append(Temp11.HasValue ? Temp11.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp12.HasValue ? Temp12.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp13.HasValue ? Temp13.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp14.HasValue ? Temp14.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp15.HasValue ? Temp15.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Temp16.HasValue ? Temp16.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields

			// we ignore the date/time string in field zero
			Timestamp = long.Parse(data[1]);
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
			Temp11 = Utils.TryParseNullDouble(data[12]);
			Temp12 = Utils.TryParseNullDouble(data[13]);
			Temp13 = Utils.TryParseNullDouble(data[14]);
			Temp14 = Utils.TryParseNullDouble(data[15]);
			Temp15 = Utils.TryParseNullDouble(data[16]);
			Temp16 = Utils.TryParseNullDouble(data[17]);

			return true;
		}

		public void FromExtraLogFile(string[] data)
		{
			Timestamp = long.Parse(data[1]);
			Temp1 = Utils.TryParseNullDouble(data[32]);
			Temp2 = Utils.TryParseNullDouble(data[33]);
			Temp3 = Utils.TryParseNullDouble(data[34]);
			Temp4 = Utils.TryParseNullDouble(data[35]);

			Temp5 = Utils.TryParseNullDouble(data[44]);
			Temp6 = Utils.TryParseNullDouble(data[45]);
			Temp7 = Utils.TryParseNullDouble(data[46]);
			Temp8 = Utils.TryParseNullDouble(data[47]);
			Temp9 = Utils.TryParseNullDouble(data[48]);
			Temp10 = Utils.TryParseNullDouble(data[49]);
			Temp11 = Utils.TryParseNullDouble(data[50]);
			Temp12 = Utils.TryParseNullDouble(data[51]);
			Temp13 = Utils.TryParseNullDouble(data[52]);
			Temp14 = Utils.TryParseNullDouble(data[53]);
			Temp15 = Utils.TryParseNullDouble(data[54]);
			Temp16 = Utils.TryParseNullDouble(data[55]);
		}
	}
}
