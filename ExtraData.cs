using System;
using System.Globalization;
using System.Text;
using SQLite;

namespace CumulusMX
{
	class ExtraData
	{
		[PrimaryKey]
		public DateTime Timestamp { get; set; }         // 0  DateTime
														// N/A 1  Current Unix timestamp

		public string ToCSV()
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var sb = new StringBuilder(350);
			sb.Append(Timestamp.ToString("'\"'dd/MM/yy HH:mm'\"'", invDate)).Append(',');
			sb.Append(Utils.ToUnixTime(Timestamp)).Append(",\"");
			/*
			if (Temp.HasValue) sb.Append(Temp.Value.ToString(Program.cumulus.TempFormat, invNum)); ;
			sb.Append("\",\"");
			if (Humidity.HasValue) sb.Append(Humidity.Value); ;
			sb.Append("\",\"");
			if (DewPoint.HasValue) sb.Append(DewPoint.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (WindAvg.HasValue) sb.Append(WindAvg.Value.ToString(Program.cumulus.WindFormat, invNum));
			sb.Append("\",\"");
			if (WindGust10m.HasValue) sb.Append(WindGust10m.Value.ToString(Program.cumulus.WindFormat, invNum));
			sb.Append("\",\"");
			if (WindAvgDir.HasValue) sb.Append(WindAvgDir.Value);
			sb.Append("\",\"");
			if (RainRate.HasValue) sb.Append(RainRate.Value.ToString(Program.cumulus.RainFormat, invNum));
			sb.Append("\",\"");
			if (RainToday.HasValue) sb.Append(RainToday.Value.ToString(Program.cumulus.RainFormat, invNum));
			sb.Append("\",\"");
			if (Pressure.HasValue) sb.Append(Pressure.Value.ToString(Program.cumulus.PressFormat, invNum));
			sb.Append("\",\"");
			if (RainCounter.HasValue) sb.Append(RainCounter.Value.ToString(Program.cumulus.RainFormat, invNum));
			sb.Append("\",\"");
			if (InsideTemp.HasValue) sb.Append(InsideTemp.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (InsideHumidity.HasValue) sb.Append(InsideHumidity.Value);
			sb.Append("\",\"");
			if (WindLatest.HasValue) sb.Append(WindLatest.Value.ToString(Program.cumulus.WindFormat, invNum));
			sb.Append("\",\"");
			if (WindChill.HasValue) sb.Append(WindChill.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (HeatIndex.HasValue) sb.Append(HeatIndex.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (UV.HasValue) sb.Append(UV.Value.ToString(Program.cumulus.UVFormat, invNum));
			sb.Append("\",\"");
			if (SolarRad.HasValue) sb.Append(SolarRad.Value);
			sb.Append("\",\"");
			if (ET.HasValue) sb.Append(ET.Value.ToString(Program.cumulus.ETFormat, invNum));
			sb.Append("\",\"");
			if (AnnualET.HasValue) sb.Append(AnnualET.Value.ToString(Program.cumulus.ETFormat, invNum));
			sb.Append("\",\"");
			if (Apparent.HasValue) sb.Append(Apparent.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (SolarMax.HasValue) sb.Append(SolarMax.Value);
			sb.Append("\",\"");
			if (Sunshine.HasValue) sb.Append(Sunshine.Value.ToString(Program.cumulus.SunFormat, invNum));
			sb.Append("\",\"");
			if (WindDir.HasValue) sb.Append(WindDir.Value);
			sb.Append("\",\"");
			if (RG11Rain.HasValue) sb.Append(RG11Rain.Value.ToString(Program.cumulus.RainFormat, invNum));
			sb.Append("\",\"");
			if (RainMidnight.HasValue) sb.Append(RainMidnight.Value.ToString(Program.cumulus.RainFormat, invNum));
			sb.Append("\",\"");
			if (FeelsLike.HasValue) sb.Append(FeelsLike.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append("\",\"");
			if (Humidex.HasValue) sb.Append(Humidex.Value.ToString(Program.cumulus.TempFormat, invNum));
			sb.Append('"');
			*/
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields
			var data2 = new string[Cumulus.NumExtraLogFileFields];
			Array.Copy(data, data2, data.Length);

			// we ignore the date/time string in field zero
			Timestamp = Utils.FromUnixTime(long.Parse(data2[1]));
			/*
			Temp = Utils.TryParseNullDouble(data2[2]);
			Humidity = Utils.TryParseNullInt(data2[3]);
			DewPoint = Utils.TryParseNullDouble(data2[4]);
			WindAvg = Utils.TryParseNullDouble(data2[5]);
			WindGust10m = Utils.TryParseNullDouble(data2[6]);
			WindAvgDir = Utils.TryParseNullInt(data2[7]);
			RainRate = Utils.TryParseNullDouble(data2[8]);
			RainToday = Utils.TryParseNullDouble(data2[9]);
			Pressure = Utils.TryParseNullDouble(data2[10]);
			RainCounter = Utils.TryParseNullDouble(data2[11]);
			InsideTemp = Utils.TryParseNullDouble(data2[12]);
			InsideHumidity = Utils.TryParseNullInt(data2[13]);
			WindLatest = Utils.TryParseNullDouble(data2[14]);
			WindChill = Utils.TryParseNullDouble(data2[15]);
			HeatIndex = Utils.TryParseNullDouble(data2[16]);
			UV = Utils.TryParseNullDouble(data2[17]);
			SolarRad = Utils.TryParseNullInt(data2[18]);
			ET = Utils.TryParseNullDouble(data2[19]);
			AnnualET = Utils.TryParseNullDouble(data2[20]);
			Apparent = Utils.TryParseNullDouble(data2[21]);
			SolarMax = Utils.TryParseNullInt(data2[22]);
			Sunshine = Utils.TryParseNullDouble(data2[23]);
			WindDir = Utils.TryParseNullInt(data2[24]);
			RG11Rain = Utils.TryParseNullDouble(data2[25]);
			RainMidnight = Utils.TryParseNullDouble(data2[26]);
			FeelsLike = Utils.TryParseNullDouble(data2[27]);
			Humidex = Utils.TryParseNullDouble(data2[28]);
			*/
			return true;
		}

	}
}
