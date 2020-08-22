using System;
using System.Globalization;
using System.IO;

namespace ConvertDataToCommon
{
	class Program
	{
		public static string path;
		public static string newPath;
		public static CultureInfo cmxCulture = new CultureInfo(CultureInfo.InvariantCulture.Name, true);
		public static string oldDateSep;
		public static string oldTimeSep;
		public static string oldListSep;
		public static string oldDecimal;
		public static string newDateSep;
		public static string newTimeSep;
		public static string newListSep;
		public static string newDecimal;


		static void Main(string[] args)
		{

			path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "data";
			for (int i = 0; i < args.Length; i++)
			{
				try
				{
					if (args[i] == "-lang" && args.Length >= i)
					{
						var lang = args[++i];

						CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(lang);
						CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(lang);
					}
					else if (args[i] == "-path" && args.Length >= i)
					{
						//Directory.SetCurrentDirectory(args[++i]);
						path =  Path.GetFullPath(args[++i]);

					}
					else
					{
						Console.WriteLine($"Invalid command line argument \"{args[i]}\"");
					}

				}
				catch
				{
					Console.WriteLine("Error procssing command line arguments");
				}

			}
			path = Path.GetFullPath(path) + Path.DirectorySeparatorChar;
			newPath = path + "conv" + Path.DirectorySeparatorChar;

			Directory.CreateDirectory(newPath);

			//path += Path.DirectorySeparatorChar;
			Console.WriteLine("Convert Cumulus MX data from culture: " + CultureInfo.CurrentCulture.DisplayName);
			Console.WriteLine("Looking for data in: " + path);

			cmxCulture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
			cmxCulture.DateTimeFormat.LongDatePattern = "yyyy-MM-dd HH:mm";
			cmxCulture.DateTimeFormat.DateSeparator = "-";
			cmxCulture.NumberFormat.NumberGroupSeparator = "";


			oldDateSep = CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator;
			oldTimeSep = CultureInfo.CurrentCulture.DateTimeFormat.TimeSeparator;
			oldListSep = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
			oldDecimal = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

			newDateSep = cmxCulture.DateTimeFormat.DateSeparator;
			newTimeSep = cmxCulture.DateTimeFormat.TimeSeparator;
			newListSep = cmxCulture.TextInfo.ListSeparator;
			newDecimal = cmxCulture.NumberFormat.NumberDecimalSeparator;

			// Process log files
			if (!ProcessLogFiles.ProcessDir(path, newPath))
			{
				Console.WriteLine("Aborting conversion due to error in the monthly log file processing");
				Environment.Exit(1);
			}

			// Process extra log files
			if (!ProcessExtraLogFiles.ProcessDir(path, newPath))
			{
				Console.WriteLine("Aborting conversion due to error in the monthly extra log file processing");
				Environment.Exit(1);
			}


			// Process day file
			if (!ProcessDayFile.ProcessFile(path + "Dayfile.txt", newPath))
			{
				Console.WriteLine("Aborting conversion due to error in the day file file processing");
				Environment.Exit(1);
			}

			// Process Cumulus.ini
			if (!ProcessCumulusIni.ProcessFile(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cumulus.ini", newPath)
			{
				Console.WriteLine("Aborting conversion due to error in the Cumulus.ini file processing");
				Environment.Exit(1);
			}

			// Process other ini files?

			Console.WriteLine("\n\nProcessing Complete!");
			Console.WriteLine("Press Enter to terminate");
			Console.ReadLine();
		}
	}
}
