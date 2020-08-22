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
