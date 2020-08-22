using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;


namespace ConvertDataToCommon
{
	static class ProcessCumulusIni
	{

		public static bool ProcessFile(string cumulusini, string newPath)
		{
			if (File.Exists(cumulusini))
			{
				Console.WriteLine($"\nProcessing Cumulus.ini: {cumulusini}");
				var linenum = 0;

				List<string> newContent = new List<string>();

				try
				{
					using (var sr = new StreamReader(cumulusini))
					{
						do
						{
							string Line = sr.ReadLine();
							linenum++;
							newContent.Add(ProcessLine(Line));
						} while (!sr.EndOfStream);
					}

					var newFile = newPath + "Cumulus.ini";
					Console.WriteLine($"   Writing Cumulus.ini file: {newFile}");
					File.WriteAllLines(newFile, newContent);

				}
				catch (Exception ex)
				{
					Console.WriteLine($"   Error on line {linenum}: {ex.Message}");
					return false;
				}
			}
			else
			{
				Console.WriteLine($"Error: Cumulus.ini file not found: {cumulusini}");
				return false;
			}
			return true;
		}


		private static string ProcessLine(string line)
		{
			if (line.StartsWith("StartDate="))
			{
				var startDateStr = line.Split('=')[1];
				var startDateObj = DateTime.Parse(startDateStr);
				return "StartDate=" + startDateObj.ToString("d", Program.cmxCulture);
			}
			else
			{
				return line;
			}

		}
	}
}

