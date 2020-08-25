using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace ConvertDataToCommon
{
	class ProcessExtraLogFiles
	{
		static string year = "";
		static string month = "";
		static int fileCnt;

		public static bool ProcessDir(string path, string newPath)
		{
			var currCnt = 1;

			Console.WriteLine("\n\nProcessing monthly extra log files...");

			var filesToProc = FindFiles(path);

			if (filesToProc.Length == 0)
			{
				Console.WriteLine("Did not find any extra monthly extra log files to process.");
				Console.WriteLine("Is this expected [Y/N]?");
				var resp = Console.ReadKey().KeyChar;
				if (resp == 'Y' || resp == 'y')
					return true;
				else
					return false;
			}
			else
			{
				// Process each file
				for (var i = 0; i < filesToProc.Length; i++)
				{
					if (!ProcessLogFile(filesToProc[i], newPath, currCnt))
					{
						return false;
					}
					currCnt++;
				}
			}
			return true;
		}

		private static string[] FindFiles(string folder)
		{
			// Find the all the monthly log files
			var files = Directory.GetFiles(folder, "ExtraLog*.txt");
			fileCnt = files.Length;
			Console.WriteLine($"Found {fileCnt} monthly extra log files to process");
			return files;
		}

		private static bool ProcessLogFile(string file, string newPath, int cnt)
		{
			Console.WriteLine($"\n  ({cnt}/{fileCnt}) Processing monthly extra log file: {file}");
			var linenum = 0;

			List<string> newContent = new List<string>();

			try
			{
				using (var sr = new StreamReader(file))
				{
					do
					{
						string Line = sr.ReadLine();
						linenum++;
						newContent.Add(ProcessLine(Line));
					} while (!sr.EndOfStream);
				}

				var newFile = newPath + file.Split(Path.DirectorySeparatorChar).Last();
				Console.WriteLine($"   Writing new monthly extra log file: {newFile}");
				File.WriteAllLines(newFile, newContent);

			}
			catch (Exception ex)
			{
				Console.WriteLine($"   Error on line {linenum}: {ex.Message}");
				return false;
			}
			return true;
		}


		private static string ProcessLine(string line)
		{
			var st = new List<string>(line.Split(Program.oldListSep[0]));

			//foreach(var field in st)
			for (var i = 0; i < st.Count; i++)
			{
				switch (i)
				{
					case 0: // date
						DateTime.Parse(st[i]);
						// change date from dd/mm/yy to dd-mm-yy
						st[i] = st[i].Replace(Program.oldDateSep, Program.newDateSep);
						break;
					case 1: // time
						DateTime.Parse(st[i]);
						st[i] = st[i].Replace(Program.oldTimeSep, Program.newTimeSep);
						break;
					// decimals
					case int n when (
						(n >= 2 && n <= 11) ||  // temp 1-10
						(n >= 22 && n <= 35) || // dp 1-10, soil temp 1-4
						(n == 40 || n == 41) || // leaf temp 1-2
						(n >= 44 && n <= 55) || // soil temp 5-16
						(n > 67)  // AQ 1-4, AQ av 1-4, user temp 1-8
						):
						if (st[i].Length > 0)
						{
							double.Parse(st[i]);
							st[i] = st[i].Replace(Program.oldDecimal, Program.newDecimal);
						}
						break;
					// integers
					case int n when (
						(n >= 12 && n <= 21) || // hum 1-10
						(n >= 36 && n <= 40) || // soil moist 1-4
						(n == 42 || n == 43) || // leaf wet 1-2
						(n >= 56 && n <= 67)    // soil moist 5-16
						):
						if (st[i].Length > 0)
						{
							int.Parse(st[i]);
						}
						break;
					default: // all the other fields
						break;
				}
			}
			return string.Join(Program.newListSep, st.ToArray());
		}
	}
}
