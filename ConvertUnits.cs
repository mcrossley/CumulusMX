
namespace CumulusMX
{
	internal static class ConvertUnits
	{
		/// <summary>
		///  Convert temp supplied in C to units in use
		/// </summary>
		/// <param name="value">Temp in C</param>
		/// <returns>Temp in configured units</returns>
		public static double? TempCToUser(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Temp == 1 ? MeteoLib.CToF(value.Value) : value;
		}

		/// <summary>
		///  Convert temp supplied in F to units in use
		/// </summary>
		/// <param name="value">Temp in F</param>
		/// <returns>Temp in configured units</returns>
		public static double? TempFToUser(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Temp == 0 ? MeteoLib.FtoC(value.Value) : value;
		}

		/// <summary>
		///  Convert temp supplied in user units to C
		/// </summary>
		/// <param name="value">Temp in configured units</param>
		/// <returns>Temp in C</returns>
		public static double? UserTempToC(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Temp == 1 ? MeteoLib.FtoC(value.Value) : value;
		}

		/// <summary>
		///  Convert temp supplied in user units to F
		/// </summary>
		/// <param name="value">Temp in configured units</param>
		/// <returns>Temp in F</returns>
		public static double? UserTempToF(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Temp == 1 ? value : MeteoLib.CToF(value.Value);
		}

		/// <summary>
		///  Converts wind supplied in m/s to user units
		/// </summary>
		/// <param name="value">Wind in m/s</param>
		/// <returns>Wind in configured units</returns>
		public static double? WindMSToUser(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Wind switch
			{
				0 => value,
				1 => value * 2.23693629,
				2 => value * 3.6,
				3 => value * 1.94384449,
				_ => (double?)0,
			};
		}

		/// <summary>
		///  Converts wind supplied in mph to user units
		/// </summary>
		/// <param name="value">Wind in mph</param>
		/// <returns>Wind in configured units</returns>
		public static double? WindMPHToUser(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Wind switch
			{
				0 => value * 0.44704,
				1 => value,
				2 => value * 1.60934,
				3 => value * 0.868976,
				_ => (double?)0,
			};
		}

		/// <summary>
		/// Converts wind in user units to m/s
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static double? WindToMS(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Wind switch
			{
				0 => value,
				1 => value / 2.23693629,
				2 => value / 3.6F,
				3 => value / 1.94384449,
				_ => (double?)0,
			};
		}

		/// <summary>
		/// Converts value in kilometres to distance unit based on users configured wind units
		/// </summary>
		/// <param name="value"></param>
		/// <returns>Wind in configured units</returns>
		public static double? KmtoUserUnits(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Wind switch
			{
				// m/s
				0 or 2 => value,
				// mph
				1 => value * 0.621371,
				// knots
				3 => value * 0.539957,
				_ => (double?)0,
			};
		}

		/// <summary>
		///  Converts windrun supplied in user units to km
		/// </summary>
		/// <param name="value">Windrun in configured units</param>
		/// <returns>Wind in km</returns>
		public static double? WindRunToKm(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Wind switch
			{
				// m/s
				0 or 2 => value,
				// mph
				1 => value / 0.621371192,
				// knots
				3 => value / 0.539957,
				_ => (double?)0,
			};
		}

		/// <summary>
		///  Converts windrun supplied in user units to miles
		/// </summary>
		/// <param name="value">Windrun in configured units</param>
		/// <returns>Wind in mi</returns>
		public static double? WindRunToMi(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Wind switch
			{
				// m/s
				0 or 2 => value * 0.621371192,
				// mph
				1 => value,
				// knots
				3 => value / 0.8689762,
				_ => (double?)0,
			};
		}

		/// <summary>
		///  Converts windrun supplied in user units to nautical miles
		/// </summary>
		/// <param name="value">Windrun in configured units</param>
		/// <returns>Wind in Nm</returns>
		public static double? WindRunToNm(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Wind switch
			{
				// m/s
				0 or 2 => value * 0.539956803,
				// mph
				1 => value * 0.8689762,
				// knots
				3 => value,
				_ => (double?)0,
			};
		}

		public static double? UserWindToKPH(double? value) // input is in Units.Wind units, convert to km/h
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Wind switch
			{
				// m/s
				0 => value * 3.6,
				// mph
				1 => value * 1.609344,
				// kph
				2 => value,
				// knots
				3 => value * 1.852,
				_ => value,
			};
		}

		public static double? UserWindToMS(double? value) // input is in Units.Wind units, convert to m/s
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Wind switch
			{
				// m/s
				0 => value,
				// mph
				1 => value * 0.44704,
				// kph
				2 => value * 0.2777778,
				// knots
				3 => value * 0.5144444,
				_ => value,
			};
		}

		public static double? UserWindToMPH(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Wind switch
			{
				0 => value * 2.23693629,
				1 => value,
				2 => value * 0.621371,
				3 => value * 1.15077945,
				_ => (double?)0,
			};
		}

		public static double? UserWindToKnots(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Wind switch
			{
				0 => value * 1.943844,
				1 => value * 0.8689758,
				2 => value * 0.5399565,
				3 => value,
				_ => (double?)0,
			};
		}


		/// <summary>
		/// Converts rain in mm to units in use
		/// </summary>
		/// <param name="value">Rain in mm</param>
		/// <returns>Rain in configured units</returns>
		public static double? RainMMToUser(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Rain == 1 ? value * 0.0393700787 : value;
		}

		/// <summary>
		/// Converts rain in inches to units in use
		/// </summary>
		/// <param name="value">Rain in mm</param>
		/// <returns>Rain in configured units</returns>
		public static double? RainINToUser(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Rain == 1 ? value : value * 25.4;
		}

		/// <summary>
		/// Converts rain in units in use to mm
		/// </summary>
		/// <param name="value">Rain in configured units</param>
		/// <returns>Rain in mm</returns>
		public static double? UserRainToMM(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Rain == 1 ? value / 0.0393700787 : value;
		}

		public static double? UserRainToIN(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Rain == 0 ? value * 0.0393700787 : value;
		}


		/// <summary>
		/// Convert pressure in mb to units in use
		/// </summary>
		/// <param name="value">pressure in mb</param>
		/// <returns>pressure in configured units</returns>
		public static double? PressMBToUser(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Press == 2 ? value * 0.0295333727 : value;
		}

		/// <summary>
		/// Convert pressure in inHg to units in use
		/// </summary>
		/// <param name="value">pressure in mb</param>
		/// <returns>pressure in configured units</returns>
		public static double? PressINHGToUser(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Press == 2 ? value : value * 33.8638866667;
		}

		/// <summary>
		/// Convert pressure in units in use to mb
		/// </summary>
		/// <param name="value">pressure in configured units</param>
		/// <returns>pressure in mb</returns>
		public static double? UserPressToMB(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Press == 2 ? value / 0.0295333727 : value;
		}

		/// <summary>
		/// Convert pressure in units in use to hPa
		/// </summary>
		/// <param name="value">pressure in configured units</param>
		/// <returns>pressure in mb</returns>
		public static double? UserPressToHpa(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Press == 2 ? value / 0.0295333727 : value;
		}

		/// <summary>
		/// Convert pressure in units in use to inHg
		/// </summary>
		/// <param name="value">pressure in configured units</param>
		/// <returns>pressure in mb</returns>
		public static double? UserPressToIN(double? value)
		{
			if (!value.HasValue)
				return value;

			return Program.cumulus.Units.Press == 2 ? value : value * 0.0295333727;
		}

		/// <summary>
		/// Takes speed in user units, returns Bft number
		/// </summary>
		/// <param name="windspeed"></param>
		/// <returns></returns>
		public static int Beaufort(double? speed)
		{
			double windspeedMS = UserWindToMS(speed ?? 0.0).Value;
			if (windspeedMS < 0.3)
				return 0;
			else if (windspeedMS < 1.6)
				return 1;
			else if (windspeedMS < 3.4)
				return 2;
			else if (windspeedMS < 5.5)
				return 3;
			else if (windspeedMS < 8.0)
				return 4;
			else if (windspeedMS < 10.8)
				return 5;
			else if (windspeedMS < 13.9)
				return 6;
			else if (windspeedMS < 17.2)
				return 7;
			else if (windspeedMS < 20.8)
				return 8;
			else if (windspeedMS < 24.5)
				return 9;
			else if (windspeedMS < 28.5)
				return 10;
			else if (windspeedMS < 32.7)
				return 11;
			else return 12;
		}
	}
}
