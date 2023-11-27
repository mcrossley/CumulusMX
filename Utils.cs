﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using ServiceStack;
using Swan;


// A rag tag of useful functions

namespace CumulusMX
{
	internal static class Utils
	{
		public static DateTime FromUnixTime(long unixTime)
		{
			// Cconvert Unix TS seconds to local time
			var utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTime);
			return utcTime.ToLocalTime();
		}

		public static long ToUnixTime(DateTime dateTime)
		{
			return dateTime.ToUnixEpochDate();
		}

		public static long ToJsTime(DateTime dateTime)
		{
			return (long) dateTime.ToUnixEpochDate() * 1000;
		}

		// SPECIAL JS TS for graphs. It looks like a JS TS, but is the local time as if it were UTC.
		// Used for the graph data, as HighCharts is going to display UTC date/times to be consistent across TZ
		public static long ToPseudoJSTime(DateTime timestamp)
		{
			return (long) DateTime.SpecifyKind(timestamp, DateTimeKind.Utc).ToUnixEpochDate() * 1000;
		}

		// SPECIAL Unix TS for graphs. It looks like a Unix TS, but is the local time as if it were UTC.
		// Used for the graph data, as HighCharts is going to display UTC date/times to be consistent across TZ
		public static long ToPseudoUnixTime(DateTime timestamp)
		{
			return (long) DateTime.SpecifyKind(timestamp, DateTimeKind.Utc).ToUnixEpochDate();
		}

		public static DateTime RoundTimeUpToInterval(DateTime dateTime, TimeSpan intvl)
		{
			return new DateTime((dateTime.Ticks + intvl.Ticks - 1) / intvl.Ticks * intvl.Ticks, dateTime.Kind);
		}

		public static long ToUnixTime(DateTimeOffset dateTime)
		{
			return dateTime.ToUnixTimeSeconds();
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
			var hex = new StringBuilder(ba.Length * 2);
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

		public static string GetSHA256Hash(string key, string data)
		{
			byte[] hashValue;
			// Initialize the keyed hash object.
			using (HMACSHA256 hmac = new HMACSHA256(key.ToAsciiBytes()))
			{
				// convert string to stream
				byte[] byteArray = Encoding.UTF8.GetBytes(data);
				using (MemoryStream stream = new MemoryStream(byteArray))
				{
					// Compute the hash of the input string.
					hashValue = hmac.ComputeHash(stream);
				}
				return BitConverter.ToString(hashValue).Replace("-", string.Empty).ToLower();
			}
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

		public static DateTime? AddTimeToDate(DateTime date, string time, int hrInc)
		{
			TimeSpan tim;
			if (TimeSpan.TryParseExact(time, "hh\\:mm", CultureInfo.InvariantCulture.DateTimeFormat, out tim))
			{
				// hrInc is a negative offest to the start of day
				if (hrInc == 0 || tim.Hours > -hrInc)
				{
					// the time is added to the meteo base date
					return date.Add(tim);
				}
				else
				{
					// the time is added to the following day
					return date.AddDays(1).Add(tim);
				}
			}
			return null;
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

		public static string GetLogFileSeparator(string line, string defSep)
		{
			// we know the dayfile and monthly log files start with
			// dd/MM/yy,NN,...
			// dd/MM/yy,hh:mm,N.N,....
			// so we just need to find the first separator after the date before a number

			var reg = Regex.Match(line, @"\d{2}[^\d]+\d{2}[^\d]+\d{2}([^\d])");
			if (reg.Success)
				return reg.Groups[1].Value;
			else
				return defSep;
		}

		public static IPAddress GetIpWithDefaultGateway()
		{
			try
			{
				// First try and find the IPv4 address that also has the default gateway
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
			catch { }
			try
			{
				// next just return the first IPv4 address found
				var host = Dns.GetHostEntry(Dns.GetHostName());
				foreach (var ip in host.AddressList)
				{
					if (ip.AddressFamily == AddressFamily.InterNetwork)
					{
						return ip;
					}
				}
			}
			catch { }

			// finally, give up and just return a 0.0.0.0 IP!
			return IPAddress.Any;
		}

		public static async Task CopyFileAsync(string sourceFile, string destinationFile)
		{
			using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
			using var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
			await sourceStream.CopyToAsync(destinationStream);
		}

		public static void CopyFileSync(string sourceFile, string destinationFile)
		{
			using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
			using var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan);
			sourceStream.CopyTo(destinationStream);
		}

		public static string ExceptionToString(Exception ex, out string message)
		{
			var sb = new StringBuilder();

			message = ex.Message;
			sb.AppendLine("");
			sb.AppendLine("Exception Type: " + ex.GetType().FullName);
			sb.AppendLine("Message: " + ex.Message);
			sb.AppendLine("Source: " + ex.Source);
			foreach (var key in ex.Data.Keys)
			{
				sb.AppendLine(key.ToString() + ": " + ex.Data[key].ToString());
			}

			if (String.IsNullOrEmpty(ex.StackTrace))
			{
				sb.AppendLine("Environment Stack Trace: " + ex.StackTrace);
			}
			else
			{
				sb.AppendLine("Stack Trace: " + ex.StackTrace);
			}

			/*
			var st = new StackTrace(ex, true);
			foreach (var frame in st.GetFrames())
			{
				if (frame.GetFileLineNumber() < 1)
					continue;

				sb.Append("File: " + frame.GetFileName());
				sb.AppendLine("  Linenumber: " + frame.GetFileLineNumber());
			}
			*/

			if (ex.InnerException != null)
			{
				sb.AppendLine("Inner Exception... ");
				sb.AppendLine(ExceptionToString(ex.InnerException, out message));
			}

			return sb.ToString();
		}

		public static void RunExternalTask(string task, string parameters, bool wait)
		{
			var process = new System.Diagnostics.Process();
			process.StartInfo.FileName = task;
			process.StartInfo.Arguments = parameters;
			process.StartInfo.UseShellExecute = false;
			//process.StartInfo.RedirectStandardOutput = true;
			//process.StartInfo.RedirectStandardError = true;
			process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			process.StartInfo.CreateNoWindow = true;
			process.Start();

			if (wait)
			{
				process.WaitForExit();
			}
		}

		public static bool ByteArraysEqual(byte[] b1, byte[] b2)
		{
			if (b1 == b2) return true;
			if (b1 == null || b2 == null) return false;
			if (b1.Length != b2.Length) return false;
			for (int i = 0; i < b1.Length; i++)
			{
				if (b1[i] != b2[i]) return false;
			}
			return true;
		}

		public static Exception GetOriginalException(Exception ex)
		{
			while (ex.InnerException != null)
			{
				ex = ex.InnerException;
			}

			return ex;
		}

		public static async Task<string> ReadAllTextAsync(string path, Encoding encoding)
		{
			const int DefaultBufferSize = 4096;
			const FileOptions DefaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

			var text = string.Empty;

			// Open the FileStream with the same FileMode, FileAccess
			// and FileShare as a call to File.OpenText would've done.
			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultOptions))
			using (var reader = new StreamReader(stream, encoding))
			{
				text = await reader.ReadToEndAsync();
			}

			return text;
		}

		public static async Task<Byte[]> ReadAllBytesAsync(string path)
		{
			const int DefaultBufferSize = 4096;
			const FileOptions DefaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

			Byte[] data;

			// Open the FileStream with the same FileMode, FileAccess
			// and FileShare as a call to File.OpenText would've done.
			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultOptions))
			{
				data = await stream.ReadFullyAsync();
			}

			return data;
		}

		public static bool FilesEqual(string path1, string path2)
		{
			// very crude check - highly unlikey different versions will have the same file lengths
			// if one or both files do not exist, catch the error and fail the check
			try
			{
				var fi1 = new FileInfo(path1);
				var fi2 = new FileInfo(path2);

				if (fi1.Length != fi2.Length)
					return false;
				else
				{
					return System.Diagnostics.FileVersionInfo.GetVersionInfo(path1).FileVersion == System.Diagnostics.FileVersionInfo.GetVersionInfo(path2).FileVersion;
				}
			}
			catch
			{
				return false;
			}
		}
	}
}
