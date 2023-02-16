﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Swan;
using Renci.SshNet.Messages;
using System.Security.Cryptography;
using ServiceStack;
using System.IO;
using System.Text;
using static SQLite.SQLite3;
using System.Threading.Tasks;
using System.Collections.Generic;

// A rag tag of useful functions

namespace CumulusMX
{
	internal class Utils
	{
		public static DateTime FromUnixTime(long unixTime)
		{
			// WWL uses UTC ticks, convert to local time
			var utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTime);
			return utcTime.ToLocalTime();
		}

		public static long ToUnixTime(DateTime dateTime)
		{
			return (long)dateTime.ToUniversalTime().ToUnixEpochDate();
		}

		public static long ToJsTime(DateTime dateTime)
		{
			return ToUnixTime(dateTime) * 1000;
		}

		public static DateTime RoundTimeUpToInterval(DateTime dateTime, TimeSpan intvl)
		{
			return new DateTime((dateTime.Ticks + intvl.Ticks - 1) / intvl.Ticks * intvl.Ticks, dateTime.Kind);
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
			using (var md5 = System.Security.Cryptography.MD5.Create())
			{
				var hashBytes = md5.ComputeHash(bytes);
				return ByteArrayToHexString(hashBytes);
			}
		}

		public static string GetMd5String(string str)
		{
			return GetMd5String(System.Text.Encoding.ASCII.GetBytes(str));
		}

		public static string GetSHA256Hash(string key, string data)
		{
			// Initialize the keyed hash object.
			using (HMACSHA256 hmac = new HMACSHA256(key.ToAsciiBytes()))
			{
				// convert string to stream
				byte[] byteArray = Encoding.UTF8.GetBytes(data);
				MemoryStream stream = new MemoryStream(byteArray);
				// Compute the hash of the input string.
				byte[] hashValue = hmac.ComputeHash(stream);
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
			// Horrible hack, but we have localised separators, but UK sequence, so localised parsing may fail
			// Determine separators from the strings, allow for multi-byte!
			var datSep = Regex.Match(d, @"[^0-9]+").Value;

			// Converts a date string in UK order to a DateTime
			string[] date = d.Split(new string[] { datSep }, StringSplitOptions.None);

			int D = Convert.ToInt32(date[0]);
			int M = Convert.ToInt32(date[1]);
			int Y = Convert.ToInt32(date[2]);
			if (Y < 1900)
			{
				Y += Y > 70 ? 1900 : 2000;
			}
			return new DateTime(Y, M, D);
		}

		public static DateTime ddmmyyhhmmStrToDate(string d, string t)
		{
			// Horrible hack, but we have localised separators, but UK sequence, so localised parsing may fail
			// Determine separators from the strings, allow for multi-byte!
			var datSep = Regex.Match(d, @"[^0-9]+").Value;
			var timSep = Regex.Match(t, @"[^0-9]+").Value;

			// Converts a date string in UK order to a DateTime
			string[] date = d.Split(new string[] { datSep }, StringSplitOptions.None);
			string[] time = t.Split(new string[] { timSep }, StringSplitOptions.None);

			int D = Convert.ToInt32(date[0]);
			int M = Convert.ToInt32(date[1]);
			int Y = Convert.ToInt32(date[2]);

			// Double check - just in case we get a four digit year!
			if (Y < 1900)
			{
				Y += Y > 70 ? 1900 : 2000;
			}
			int h = Convert.ToInt32(time[0]);
			int m = Convert.ToInt32(time[1]);

			return new DateTime(Y, M, D, h, m, 0);
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
			catch {}
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
			catch {}

			// finally, give up and just return a 0.0.0.0 IP!
			return IPAddress.Any;
		}

		public static void RunExternalTask(string task, string parameters, bool wait, bool redirectError = false)
		{
			var process = new System.Diagnostics.Process();
			process.StartInfo.FileName = task;
			process.StartInfo.Arguments = parameters;
			process.StartInfo.UseShellExecute = false;
			//process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = redirectError;
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
	}
}
