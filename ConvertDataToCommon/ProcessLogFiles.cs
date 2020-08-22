using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace ConvertDataToCommon
{
	static class ProcessLogFiles
	{
		static string year = "";
		static string month = "";
		static int fileCnt;

		public static bool ProcessDir(string path, string newPath)
		{
			var currCnt = 1;

			Console.WriteLine("\n\nProcessing monthly log files...");

			var filesToProc = FindFiles(path);

			if (filesToProc.Length == 0)
			{
				Console.WriteLine("Error: Did not find any monthly log files to process");
				return false;
			}
			else
			{
				// Process each file
				for (var i=0; i< filesToProc.Length; i++)
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
			Regex reg = new Regex(@".*[0-9]{2}log.txt");
			// Find the all the monthly log files
			var files = Directory.GetFiles(folder, "*log.txt")
				.Where(path => reg.IsMatch(path))
				.ToArray();

			fileCnt = files.Length;
			Console.WriteLine($"Found {fileCnt} monthly log files to process");
			return files;
		}

		private static bool ProcessLogFile(string file, string newPath, int cnt)
		{
			Console.WriteLine($"\n  ({cnt}/{fileCnt}) Processing monthly log file: {file}");
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

				var newFile = $"{newPath}{year}-{month}-log.txt";
				Console.WriteLine($"   Writing new monthly log file: {newFile}");
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
			var st = new List<string>(Regex.Split(line, Program.oldListSep));

			//foreach(var field in st)
			for (var i = 0; i < st.Count; i++)
			{
				if (i == 0) // date
				{
					// change date order from dd/mm/yy to dd-mm-yy
					st[i] = st[i].Replace(Program.oldDateSep, Program.newDateSep);
				}
				else if (i == 1) // time
				{
					st[i] = st[i].Replace(Program.oldTimeSep, Program.newTimeSep);
				}
				else // all the other fields
				{
					st[i] = st[i].Replace(Program.oldDecimal, Program.newDecimal);
				}
			}
			return string.Join(Program.newListSep, st.ToArray());
		}
	}
}
