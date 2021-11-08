﻿using Swan;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

// A rag tag of useful functions

namespace CumulusMX
{
	internal static class Utils
	{
		public static DateTime FromUnixTime(long unixTime)
		{
			// WWL uses UTC ticks, convert to local time
			var utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTime);
			return utcTime.ToLocalTime();
		}

		public static long ToUnixTime(DateTime dateTime)
		{
			return dateTime.ToUniversalTime().ToUnixEpochDate();
		}

		// The JSON data we generate for HighCharts uses a "pseudo-UTC" timestamp
		// This is the local time converted to the UTC timestamp *as if it were already UTC*
		// This gets around TZ issues in HighCharts which by default accepts and displays UTC date.times
		public static long ToGraphTime(DateTime dateTime)
		{
			return (long)(dateTime - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
		}

		public static string ByteArrayToHexString(byte[] ba)
		{
			System.Text.StringBuilder hex = new System.Text.StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}


		public static string GetMd5String(byte[] bytes)
		{
			using var md5 = System.Security.Cryptography.MD5.Create();
			var hashBytes = md5.ComputeHash(bytes);
			return ByteArrayToHexString(hashBytes);
		}

		public static string GetMd5String(string str)
		{
			return GetMd5String(System.Text.Encoding.ASCII.GetBytes(str));
		}

		public static bool ValidateIPv4(string ipString)
		{
			if (string.IsNullOrWhiteSpace(ipString))
			{
				return false;
			}

			string[] splitValues = ipString.Split('.');
			if (splitValues.Length != 4)
			{
				return false;
			}

			byte tempForParsing;

			return splitValues.All(r => byte.TryParse(r, out tempForParsing));
		}


		public static DateTime ddmmyyStrToDate(string d)
		{
			// Only use the invariant date format as the date in is in local TZ
			return DateTime.ParseExact(d, "dd/MM/yy", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None);
		}


		public static DateTime ddmmyyyyStrToDate(string d)
		{
			// Only use the invariant date format as the date in is in local TZ
			return DateTime.ParseExact(d, "dd/MM/yyyy", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None);
		}

		public static DateTime ddmmyyyy_hhmmStrToDate(string dt)
		{
			// Only use the invariant date format as the date in is in local TZ
			return DateTime.ParseExact(dt, "dd/MM/yyyy+HH:mm", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None);
		}

		public static DateTime AddTimeToDate(DateTime date, string time)
		{
			return date.Add(TimeSpan.ParseExact(time, "hh\\:mm", CultureInfo.InvariantCulture.DateTimeFormat));
		}

		public static int? TryParseNullInt(string val)
		{
			double outVal;
			// we allow for a decimal, because log files someties get mangled in Excel etc!
			return double.TryParse(val, out outVal) ? Convert.ToInt32(outVal) : null;
		}

		public static double? TryParseNullDouble(string val)
		{
			double outVal;
			return double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out outVal) ? outVal : null;
		}

		public static DateTime? TryParseNullTimeSpan(DateTime baseDate, string val, string format)
		{
			TimeSpan tim;
			return TimeSpan.TryParseExact(val, format, CultureInfo.InvariantCulture, out tim) ? baseDate.Add(tim) : null;
		}

	}
}
