using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;


namespace ConvertDataToCommon
{
	static class ProcessDayFile
	{

		public static bool ProcessFile(string dayfile, string newPath)
		{
			if (File.Exists(dayfile))
			{
				Console.WriteLine($"\nProcessing day file: {dayfile}");
				var linenum = 0;

				List<string> newContent = new List<string>();

				try
				{
					using (var sr = new StreamReader(dayfile))
					{
						do
						{
							string Line = sr.ReadLine();
							linenum++;
							newContent.Add(ProcessLine(Line));
						} while (!sr.EndOfStream);
					}

					var newFile = newPath + "Dayfile.txt";
					Console.WriteLine($"   Writing new day file: {newFile}");
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
				Console.WriteLine($"Error: Dayfile file not found: {dayfile}");
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
							// change date order from dd/mm/yy to dd-mm-yy
						st[i] = st[i].Replace(Program.oldDateSep, Program.newDateSep);
						break;
					// times
					case 3: // hi gust
					case 5: // lo temp
					case 7: // hi temp
					case 9: // lo press
					case 11: // hi press
					case 13: // hi rrate
					case 18: // hi avg wind
					case 20: // lo hum
					case 26: // hi HI
					case 28: // hi appT
					case 30: // lo appT
					case 32: // hi hour rain
					case 34: // lo WC
					case 36: // hi DP
					case 38: // lo DP
					case 43: // hi solar
					case 45: // hi UV
					case 47: // hi feels
					case 49: // lo feels
						st[i] = st[i].Replace(Program.oldTimeSep, Program.newTimeSep);
						break;
					default: // all the other fields
						st[i] = st[i].Replace(Program.oldDecimal, Program.newDecimal);
						break;
				}
			}
			return string.Join(Program.newListSep, st.ToArray());
		}
	}
}

