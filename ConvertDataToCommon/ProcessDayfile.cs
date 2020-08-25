using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;


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
					case 22: // hi hum
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
					case 51: // hi humidex
						if (st[i].Length > 0)
						{
							DateTime.Parse(st[i]);
							st[i] = st[i].Replace(Program.oldTimeSep, Program.newTimeSep);
						}
						break;
					// decimals
					case 1: // hi gust
					case 4: // lo temp
					case 6: // hi temp
					case 8: // lo press
					case 10: // hi press
					case 12: // hi rrate
					case 14: // tot rain
					case 15: // avg temp
					case 16: // wind run
					case 17: // hi avg wind
					case 23: // tot evap
					case 24: // sunshine
					case 25: // hi HI
					case 27: // hi appT
					case 29: // lo appT
					case 31: // hi hour rain
					case 33: // lo WC
					case 35: // hi DP
					case 37: // lo DP
					case 40: // heat DG
					case 41: // cool DG
					case 44: // hi UV
					case 46: // hi feels
					case 48: // lo feels
					case 50: // hi humidex
						if (st[i].Length > 0)
						{
							double.Parse(st[i]);
							st[i] = st[i].Replace(Program.oldDecimal, Program.newDecimal);
						}
						break;
					// integers
					case 2: // gust bearing
					case 19: // lo hum
					case 21: // hi hum
					case 39: // dom bearing
					case 42: // hi solar
						if (st[i].Length > 0)
						{
							int.Parse(st[i]);
						}
						break;
					// all the other fields
					default:
						break;
				}
			}
			return string.Join(Program.newListSep, st.ToArray());
		}
	}
}

