using Swan;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

		/// <summary>
		/// Rounds datetime up, down or to nearest minutes and all smaller units to zero
		/// </summary>
		/// <param name="dt">static extension method</param>
		/// <param name="rndmin">mins to round to</param>
		/// <param name="directn">Up,Down,Nearest</param>
		/// <returns>rounded datetime with all smaller units than mins rounded off</returns>
		public static DateTime RoundToNearestMinuteProper(this DateTime dt, int rndmin, RoundingDirection directn)
		{
			if (rndmin == 0) //can be > 60 mins
				return dt;

			TimeSpan d = TimeSpan.FromMinutes(rndmin); //this can be passed as a parameter, or use any timespan unit FromDays, FromHours, etc.

			long delta = 0;
			Int64 modTicks = dt.Ticks % d.Ticks;

			switch (directn)
			{
				case RoundingDirection.Up:
					delta = modTicks != 0 ? d.Ticks - modTicks : 0;
					break;
				case RoundingDirection.Down:
					delta = -modTicks;
					break;
				case RoundingDirection.Nearest:
					{
						bool roundUp = modTicks > (d.Ticks / 2);
						var offset = roundUp ? d.Ticks : 0;
						delta = offset - modTicks;
						break;
					}

			}
			return new DateTime(dt.Ticks + delta, dt.Kind);
		}

		public enum RoundingDirection
		{
			Up,
			Down,
			Nearest
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


		public static IPAddress GetIpWithDefaultGateway()
		{
			return NetworkInterface
				.GetAllNetworkInterfaces()
				.Where(n => n.OperationalStatus == OperationalStatus.Up)
				.Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
				.Where(n => n.GetIPProperties().GatewayAddresses.Count > 0)
				.SelectMany(n => n.GetIPProperties().UnicastAddresses)
				.Where(n => n.Address.AddressFamily == AddressFamily.InterNetwork)
				.Where(n => n.IPv4Mask.ToString() != "0.0.0.0")
				.Select(g => g.Address)
				.First();
		}

		public static async Task CopyFileAsync(string sourceFile, string destinationFile)
		{
			using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
			using var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
			await sourceStream.CopyToAsync(destinationStream);
		}

		public static void CopyFileSync(string sourceFile, string destinationFile)
		{
			using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
			using var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan);
			sourceStream.CopyTo(destinationStream);
		}
	}
}
