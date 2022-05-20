﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace CumulusMX
{
	internal class ImetStation : WeatherStation
	{
		private const string sLineBreak = "\r\n";
		private bool midnightraindone;
		private double prevraintotal = -1;
		private int previousminute = 60;
		private string currentWritePointer = "";
		private int readCounter = 30;
		private bool stop = false;

		public ImetStation(Cumulus cumulus) : base(cumulus)
		{
			Cumulus.LogMessage("ImetUpdateLogPointer=" + cumulus.ImetOptions.UpdateLogPointer);
			Cumulus.LogMessage("ImetWaitTime=" + cumulus.ImetOptions.WaitTime);
			Cumulus.LogMessage("ImetReadDelay=" + cumulus.ImetOptions.ReadDelay);
			Cumulus.LogMessage("ImetBaudRate=" + cumulus.ImetOptions.BaudRate);
			Cumulus.LogMessage("Instromet: Attempting to open " + cumulus.ComportName);

			calculaterainrate = true;

			// No wind chill, so we calculate it
			cumulus.StationOptions.CalculatedWC = true;

			// Change the default dps for rain and sunshine from 1 to 2 for IMet stations
			cumulus.RainDPlaces = cumulus.SunshineDPlaces = 2;
			cumulus.RainDPlaceDefaults[0] = 2;  // mm
			cumulus.RainDPlaceDefaults[1] = 3;  // in
			cumulus.RainFormat = cumulus.SunFormat = "F2";

			comport = new SerialPort(cumulus.ComportName, cumulus.ImetOptions.BaudRate, Parity.None, 8, StopBits.One) {Handshake = Handshake.None, RtsEnable = true, DtrEnable = true};

			try
			{
				comport.ReadTimeout = 1000;
				comport.Open();
				Cumulus.LogMessage("COM port opened");
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Error opening COM port");
			}

			if (comport.IsOpen)
			{
				ImetSetLoggerInterval(cumulus.logints[cumulus.DataLogInterval]);
				if (cumulus.StationOptions.SyncTime)
				{
					SetStationClock();
				}
			}
		}

		public override void DoStartup()
		{
			if (comport.IsOpen)
			{
				// Read the data from the logger
				cumulus.CurrentActivity = "Reading archive data";
				startReadingHistoryData();
			}
		}

		private void ImetSetLoggerInterval(int interval)
		{
			Cumulus.LogMessage($"Setting logger interval to {interval} minutes");

			SendCommand("WRST,11," + interval * 60);
			// read the response
			string response = GetResponse("wrst");

			string data = ExtractText(response, "wrst");
			cumulus.LogDataMessage("Response: " + data);
			cumulus.ImetLoggerInterval = interval;
		}

		private void SetStationClock()
		{
			string datestr = DateTime.Now.ToString("yyyyMMdd");
			string timestr = DateTime.Now.ToString("HHmmss");

			cumulus.LogDataMessage($"WRTM,{datestr},{timestr}");

			SendCommand($"WRTM,{datestr},{timestr}");
			// read the response
			string response = GetResponse("wrtm");

			string data = ExtractText(response, "wrtm");
			cumulus.LogDataMessage("WRTM Response: " + data);
		}

		/*
		private string ReadStationClock()
		{
			SendCommand("RDTM");
			string response = GetResponse("rdtm");
			string data = ExtractText(response, "rdtm");
			return data;
		}
		*/

		private void ProgressLogs()
		{
			// MainForm.LogMessage('Advance log pointer');
			// advance the pointer
			SendCommand("PRLG,1");
			// read the response
			GetResponse("prlg");
		}

		/*
		private void RegressLogs(DateTime ts) // Move the log pointer back until the archive record timestamp is earlier
			// than the supplied ts, or the logs cannot be regressed any further
		{
			const int TIMEPOS = 4;
			const int DATEPOS = 5;
			bool done = false;
			int numlogs = GetNumberOfLogs();
			int previousnumlogs;
			bool dataOK;
			DateTime entryTS;

			Cumulus.LogMessage("Regressing logs to before " + ts);
			// regress the pointer
			SendCommand("RGLG,1");
			// read the response
			//string response = GetResponse("rglg");
			GetResponse("rglg");
			do
			{
				List<string> sl = GetArchiveRecord();
				try
				{
					int hour = Convert.ToInt32(sl[TIMEPOS].Substring(0, 2));
					int minute = Convert.ToInt32(sl[TIMEPOS].Substring(3, 2));
					int sec = Convert.ToInt32(sl[TIMEPOS].Substring(6, 2));
					int day = Convert.ToInt32(sl[DATEPOS].Substring(0, 2));
					int month = Convert.ToInt32(sl[DATEPOS].Substring(3, 2));
					int year = Convert.ToInt32(sl[DATEPOS].Substring(6, 2));
					Cumulus.LogMessage("Logger entry : Y = " + year + ", M = " + month + ", D = " + day + ", h = " + hour + ", m = " + minute + ", s = " + sec);

					entryTS = new DateTime(year, month, day, hour, minute, sec, 0);
					dataOK = true;
				}
				catch
				{
					Cumulus.LogMessage("Error in timestamp, unable to process logger data");
					dataOK = false;
					done = true;
					entryTS = DateTime.MinValue;
				}

				if (dataOK)
				{
					if (entryTS < ts)
					{
						done = true;
						Cumulus.LogMessage("Regressed far enough");
					}
					else
					{
						// regress the pointer
						SendCommand("RGLG,1");
						// read the response
						//response = GetResponse("rglg");
						GetResponse("rglg");
						previousnumlogs = numlogs;
						numlogs = GetNumberOfLogs();
						Cumulus.LogMessage("Number of logs = " + numlogs);
						if (numlogs == previousnumlogs)
						{
							done = true;
							Cumulus.LogMessage("Cannot regress any further");
						}
					}
				}
			} while (!done);
		}
		*/

		private void UpdateReadPointer()
		{
			cumulus.LogDebugMessage("Checking the read pointer");
			// If required, update the logger read pointer to match the current write pointer
			// It means the read pointer will always point to the last live record we read.
			SendCommand("RDST,14");
			// read the response
			var response1 = GetResponse("rdst");
			if (ValidChecksum(response1))
			{
				try
				{
					// Response: rdst,adr,dat
					// split the data
					var sl = new List<string>(response1.Split(','));
					var currPtr = sl[2];

					if (currentWritePointer.Equals(currPtr))
						return;

					// The write pointer does not equal the read pointer
					// write it back to the logger memory
					cumulus.LogDebugMessage($"Updating logger read pointer to {currPtr}");
					SendCommand("WRST,13," + currPtr);
					var response2 = GetResponse("wrst");
					if (ValidChecksum(response2))
					{
						// and if it all worked, update our pointer record
						currentWritePointer = currPtr;
					}
					else
					{
						Cumulus.LogMessage("WRST: Invalid checksum");
					}
				}
				catch
				{
				}
			}
			else
			{
				Cumulus.LogMessage("RDST: Invalid checksum");
			}
		}

		private void SendCommand(string command)
		{
			// First flush the receive buffer
			comport.DiscardInBuffer();
			comport.BaseStream.Flush();

			// Send the command
			cumulus.LogDebugMessage("Sending: " + command);
			LogRawStationData(command, true);

			comport.Write(command + sLineBreak);

			// Flush the first response - should be the echo of the command
			try
			{
				cumulus.LogDebugMessage("Discarding input: " + comport.ReadTo(sLineBreak));
			}
			catch
			{
				// probably a timeout - do nothing.
			}
			finally
			{
				Thread.Sleep(cumulus.ImetOptions.WaitTime);
			}
		}

		private string GetResponse(string expected)
		{
			string response = "";
			int attempts = 0;

			// The Instromet is odd, in that the serial connection is configured for human interaction rather than machine.
			// Command to logger...
			//    RDLG,58<CR><LF>
			// What is sent back...
			//    RDLG,58<CR><LF>
			//    rdlg,1,2,3,4,5,6,7,8,9,123<CR><LF>
			//    <CR><LF>
			//    >

			try
			{
				do
				{
					attempts ++;
					cumulus.LogDataMessage("Reading response from station, attempt " + attempts);
					response = comport.ReadTo(sLineBreak);
					byte[] ba = Encoding.Default.GetBytes(response);

					cumulus.LogDataMessage($"Response from station: '{response}'");
					LogRawStationData(response, false);
					//cumulus.LogDebugMessage("Hex: '" + BitConverter.ToString(ba) + "'");
				} while (!(response.Contains(expected)) && attempts < 6);

				// If we got the response and didn't time out, then wait for the command prompt before
				// returning so we know the logger is ready for the next command
				if ((response.Contains(expected)) && attempts < 6)
				{
					comport.ReadTo(">"); // just discard this
				}
			}
			catch
			{
				// Probably a timeout, just exit
			}

			return response;
		}

		private List<string> GetArchiveRecord()
		{
			List<string> sl = new List<string>();
			Cumulus.LogMessage("Get next log - RDLG,1");
			// request the archive data
			SendCommand("RDLG,1");
			// read the response
			string response = GetResponse("rdlg");
			// extract the bit we want from all the other crap (echo, newlines, prompt etc)
			string data = ExtractText(response, "rdlg");
			Cumulus.LogMessage(data);

			if (ValidChecksum(data))
			{
				try
				{
					// split the data
					sl = new List<string>(data.Split(','));
				}
				catch
				{
				}
			}

			return sl;
		}

		private int GetNumberOfLogs()
		{
			int attempts = 0;
			int num = 0;
			bool valid;
			string data;
			do
			{
				attempts++;

				// read number of available archive entries
				SendCommand("LGCT");
				Cumulus.LogMessage("Obtaining log count");
				// read the response
				string response = GetResponse("lgct");
				// extract the bit we want from all the other crap (echo, newlines, prompt etc)
				data = ExtractText(response, "lgct");
				cumulus.LogDataMessage("Response from LGCT=" + data);
				valid = ValidChecksum(data);
				cumulus.LogDebugMessage(valid ? "Checksum valid" : "!!! Checksum invalid !!!");
			} while (!valid && (attempts < 3));

			if (valid)
			{
				num = 0;
				try
				{
					// split the data
					var st = new List<string>(data.Split(','));

					if (st[1].Length > 0)
					{
						num = Convert.ToInt32(st[1]);
					}
				}
				catch
				{
					num = 0;
				}
			}
			else
			{
				Cumulus.LogMessage("Unable to read log count");
			}

			return num;
		}

		private static bool ValidChecksum(string str)
		{
			try
			{
				// split the data
				var sl = new List<string>(str.Split(','));

				// get number of fields in string
				int len = sl.Count;
				// checksum is last field
				int csum = Convert.ToInt32((sl[len - 1]));

				// calculate checksum of string
				uint sum = 0;
				int endpos = str.LastIndexOf(",");

				for (int i = 0; i <= endpos; i++)
				{
					sum = (sum + str[i])%256;
				}

				// 8-bit 1's complement
				sum = (~sum) % 256;

				return (sum == csum);
			}
			catch
			{
				return false;
			}
		}

		private static string ExtractText(string input, string after)
		{
			// return string after supplied string
			// used for extracting actual response from reply from station
			// assumes that the terminating CRLF is not present, as
			// readto() should have stripped this off
			int pos1 = input.IndexOf(after);
			//int pos2 = input.Length - 2;
			return pos1>=0 ? input[pos1..] : "";
		}

		public override void startReadingHistoryData()
		{
			Cumulus.LogMessage("Start reading history data");
			Cumulus.LogConsoleMessage("Start reading history data...");
			//lastArchiveTimeUTC = getLastArchiveTime();

			LoadLastHoursFromDataLogs(cumulus.LastUpdateTime);

			bw = new BackgroundWorker();
			//histprog = new historyProgressWindow();
			//histprog.Owner = mainWindow;
			//histprog.Show();
			bw.DoWork += new DoWorkEventHandler(bw_DoWork);
			//bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
			bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
			bw.WorkerReportsProgress = true;
			bw.RunWorkerAsync();
		}

		public override void Stop()
		{
			stop = true;
			StopMinuteTimer();

			// Call the common code in the base class
			base.Stop();
		}

		private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			//histprog.histprogTB.Text = "Processed 100%";
			//histprog.histprogPB.Value = 100;
			//histprog.Close();
			//mainWindow.FillLastHourGraphData();

			cumulus.CurrentActivity = "Normal running";
			Cumulus.LogMessage("Archive reading thread completed");
			DoDayResetIfNeeded();
			DoTrendValues(DateTime.Now);
			cumulus.StartTimersAndSensors();
			StartLoop();
		}

		private void bw_DoWork(object sender, DoWorkEventArgs e)
		{
			getAndProcessHistoryData();
			// Do it again in case it took a long time and there are new entries
			getAndProcessHistoryData();
		}

		public override void getAndProcessHistoryData()
		{
			// Positions of fields in logger data
			//const int IDPOS = 1;
			//const int TYPEPOS = 2;
			const int INTERVALPOS = 3;
			const int TIMEPOS = 4;
			const int DATEPOS = 5;
			//const int TEMP1MINPOS = 6;
			//const int TEMP1MAXPOS = 7;
			const int TEMP1AVGPOS = 8;
			//const int TEMP2MINPOS = 9;
			//const int TEMP2MAXPOS = 10;
			const int TEMP2AVGPOS = 11;
			//const int RELHUMMINPOS = 12;
			//const int RELHUMMAXPOS = 13;
			const int RELHUMAVGPOS = 14;
			//const int PRESSMINPOS = 15;
			//const int PRESSMAXPOS = 16;
			const int PRESSAVGPOS = 17;
			//const int WINDMINPOS = 18;
			const int WINDMAXPOS = 19;
			const int WINDAVGPOS = 20;
			const int DIRPOS = 21;
			const int SUNPOS = 22;
			const int RAINPOS = 23;

			DateTime timestamp = DateTime.MinValue;

			NumberFormatInfo provider = new NumberFormatInfo {NumberDecimalSeparator = "."};

			DateTime startfrom = cumulus.LastUpdateTime;
			int startindex = 0;
			int year = startfrom.Year;
			int month = startfrom.Month;
			int day = startfrom.Day;
			int hour = startfrom.Hour;
			int minute = startfrom.Minute;
			int sec = startfrom.Second;

			Cumulus.LogMessage($"Last update time = {year}/{month}/{day} {hour}:{minute}:{sec}");

			int recordsdone = 0;

			if (FirstRun)
			{
				// First time Cumulus has run, "delete" all the log entries as there may be
				// vast numbers and they will take hours to download only to be discarded

				//Cumulus.LogMessage("First run: PRLG,32760");
				// regress the pointer
				//comport.Write("PRLG,32760" + sLineBreak);
				// read the response
				//response = GetResponse("prlg");

				// Do it by updating the read pointer to match the write pointer
				// The recorded value for currentWritePointer will not have been set yet
				UpdateReadPointer();
			}

			Cumulus.LogMessage("Downloading history from " + startfrom);
			Cumulus.LogConsoleMessage("Reading archive data from " + startfrom + " - please wait");
			//RegressLogs(cumulus.LastUpdateTime);
			//bool valid = false;
			int numrecs = GetNumberOfLogs();
			Cumulus.LogMessage("Logs available = " + numrecs);
			if (numrecs > 0)
			{
				Cumulus.LogMessage("Number of history records = " + numrecs);
				// get the earliest record
				List<string> sl = GetArchiveRecord();
				bool dataOK;
				try
				{
					hour = Convert.ToInt32(sl[TIMEPOS][..2]);
					minute = Convert.ToInt32(sl[TIMEPOS].Substring(3, 2));
					sec = Convert.ToInt32(sl[TIMEPOS].Substring(6, 2));
					day = Convert.ToInt32(sl[DATEPOS][..2]);
					month = Convert.ToInt32(sl[DATEPOS].Substring(3, 2));
					year = Convert.ToInt32(sl[DATEPOS].Substring(6, 4));
					Cumulus.LogMessage("Logger entry : Y = " + year + ", M = " + month + ", D = " + day + ", h = " + hour + ", m = " + minute + ", s = " + sec);

					timestamp = new DateTime(year, month, day, hour, minute, sec, 0);
					dataOK = true;
				}
				catch
				{
					Cumulus.LogMessage("Error in earliest timestamp, unable to process logger data");
					dataOK = false;
				}

				if (dataOK)
				{
					Cumulus.LogMessage("Earliest timestamp " + timestamp);
					if (timestamp < cumulus.LastUpdateTime)
					{
						// startindex = 1;
						Cumulus.LogMessage("-----Earliest timestamp is earlier than required");
						Cumulus.LogMessage("-----Find first entry after " + cumulus.LastUpdateTime);
						startindex++; //  to allow for first log already read
						while ((startindex < numrecs) && (timestamp <= cumulus.LastUpdateTime))
						{
							// Move on to next entry
							ProgressLogs();
							sl = GetArchiveRecord();
							try
							{
								hour = Convert.ToInt32(sl[TIMEPOS][..2]);
								//minute = Convert.ToInt32(sl[TIMEPOS].Substring(3, 2));
								minute = Convert.ToInt32(sl[TIMEPOS][2..5]);
								sec = Convert.ToInt32(sl[TIMEPOS].Substring(6, 2));
								day = Convert.ToInt32(sl[DATEPOS][..2]);
								month = Convert.ToInt32(sl[DATEPOS].Substring(3, 2));
								year = Convert.ToInt32(sl[DATEPOS].Substring(6, 4));
								Cumulus.LogMessage("Logger entry zero: Y = " + year + ", M = " + month + ", D = " + day + ", h = " + hour + ", m = " + minute + ", s = " + sec);

								timestamp = new DateTime(year, month, day, hour, minute, sec, 0);
								Cumulus.LogMessage("New earliest timestamp " + timestamp);
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "Error in timestamp, skipping entry. Error");
								timestamp = DateTime.MinValue;
							}

						    startindex++;
						}
					}
				}

				if (startindex < numrecs)
				{
					// We still have entries to process
					Cumulus.LogMessage("-----Actual number of valid history records = " + (numrecs - startindex));
					// Compare earliest timestamp with the update time of the today file
					// and see if (they are on the same day
					//int hourInc = cumulus.GetHourInc();

					// set up controls for end of day roll-over
					int rollHour;
					if (cumulus.RolloverHour == 0)
					{
						rollHour = 0;
					}
					else if (cumulus.Use10amInSummer && (TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now)))
					{
						// Locale is currently on Daylight time
						rollHour = cumulus.RolloverHour + 1;
					}
					else
					{
						// Locale is currently on Standard time or unknown
						rollHour = cumulus.RolloverHour;
					}

					// Check to see if (today"s roll-over has been done
					// (we might be starting up in the roll-over hour)

					int luhour = cumulus.LastUpdateTime.Hour;

					var rolloverdone = luhour == rollHour;

					midnightraindone = luhour == 0;

					for (int i = startindex; i < numrecs; i++)
					{
						try
						{
							recordsdone++;
							sl = GetArchiveRecord();
							ProgressLogs();

							hour = Convert.ToInt32(sl[TIMEPOS][..2]);
							minute = Convert.ToInt32(sl[TIMEPOS].Substring(3, 2));
							sec = Convert.ToInt32(sl[TIMEPOS].Substring(6, 2));
							day = Convert.ToInt32(sl[DATEPOS][..2]);
							month = Convert.ToInt32(sl[DATEPOS].Substring(3, 2));
							year = Convert.ToInt32(sl[DATEPOS].Substring(6, 4));
							timestamp = new DateTime(year, month, day, hour, minute, sec);
							Cumulus.LogMessage("Processing logger data entry " + i + " for " + timestamp);

							int interval = (int) (Convert.ToDouble(sl[INTERVALPOS], provider)/60);
							// Check for roll-over

							if (hour != rollHour)
							{
								rolloverdone = false;
							}

							if (hour != 0)
							{
								midnightraindone = false;
							}

							if (sl[RELHUMAVGPOS].Length > 0)
							{
								DoHumidity((int) (Convert.ToDouble(sl[RELHUMAVGPOS], provider)), timestamp);
							}
							else
							{
								Humidity = null;
							}

							if ((sl[WINDAVGPOS].Length > 0) && (sl[WINDMAXPOS].Length > 0) && (sl[DIRPOS].Length > 0))
							{
								double windspeed = Convert.ToDouble(sl[WINDAVGPOS], provider);
								double windgust = Convert.ToDouble(sl[WINDMAXPOS], provider);
								int windbearing = Convert.ToInt32(sl[DIRPOS]);

								DoWind(windgust, windbearing, windspeed, timestamp);

								// add in "archivePeriod" minutes worth of wind speed to windrun
								WindRunToday += ((WindAverage.Value * WindRunHourMult[cumulus.Units.Wind]*interval)/60.0);

								DateTime windruncheckTS;
								if ((hour == rollHour) && (minute == 0))
									// this is the last logger entry before roll-over
									// fudge the timestamp to make sure it falls in the previous day
								{
									windruncheckTS = timestamp.AddMinutes(-1);
								}
								else
								{
									windruncheckTS = timestamp;
								}

								CheckForWindrunHighLow(windruncheckTS);

								// update dominant wind bearing
								CalculateDominantWindBearing(Bearing, WindAverage, interval);
							}

							if (sl[TEMP1AVGPOS].Length > 0)
							{
								DoTemperature(ConvertTempCToUser(Convert.ToDouble(sl[TEMP1AVGPOS], provider)), timestamp);

								// add in "archivePeriod" minutes worth of temperature to the temp samples
								tempsamplestoday += interval;
								TempTotalToday += (Temperature.Value * interval);

								// update chill hours
								if (Temperature < cumulus.ChillHourThreshold)
								{
									// add 1 minute to chill hours
									ChillHours += interval / 60.0;
								}

								// update heating/cooling degree days
								UpdateDegreeDays(interval);
							}
							else
							{
								DoTemperature(null, timestamp);
							}

							if (sl[TEMP2AVGPOS].Length > 0)
							{
								double temp2 = Convert.ToDouble(sl[TEMP2AVGPOS], provider);
								// supply in CELSIUS
								if (cumulus.ExtraDataLogging.Temperature)
								{
									DoExtraTemp(temp2, 1);
								}
								else
								{
									DoWetBulb(temp2, timestamp);
								}
							}

							if (sl[RAINPOS].Length > 0)
							{
								var raintotal = Convert.ToDouble(sl[RAINPOS], provider);
								double raindiff;
								if (prevraintotal == -1)
								{
									raindiff = 0;
								}
								else
								{
									raindiff = raintotal - prevraintotal;
								}

								double rainrate = ConvertRainMMToUser((raindiff) * (60.0 / cumulus.logints[cumulus.DataLogInterval]));

								DoRain(ConvertRainMMToUser(raintotal), rainrate, timestamp);

								prevraintotal = raintotal;
							}

							if ((sl[WINDAVGPOS].Length > 0) && (sl[TEMP1AVGPOS].Length > 0))
							{
								// wind chill
								var tempinC = ConvertUserTempToC(Temperature);
								var windinKPH = ConvertUserWindToKPH(WindAverage);
								var value = MeteoLib.WindChill(tempinC, windinKPH);
								// value is now in Celsius, convert to units in use
								value = ConvertTempCToUser(value);
								DoWindChill(value, timestamp);
							}

							if (sl[PRESSAVGPOS].Length > 0)
							{
								DoPressure(ConvertPressMBToUser(Convert.ToDouble(sl[PRESSAVGPOS], provider)), timestamp);
							}

							// Cause wind chill calc
							DoWindChill(null, timestamp);
							DoDewpoint(null, timestamp);
							DoApparentTemp(timestamp);
							DoFeelsLike(timestamp);
							DoHumidex(timestamp);
							DoCloudBaseHeatIndex(timestamp);

							// sunshine hours
							if (sl[SUNPOS].Length > 0)
							{
								DoSunHours(Convert.ToDouble(sl[SUNPOS], provider));
							}

							_ = cumulus.DoLogFile(timestamp, false);
							cumulus.MySqlStuff.DoRealtimeData(999, false, timestamp);

							AddRecentDataEntry(timestamp, WindAverage, RecentMaxGust, WindLatest, Bearing, AvgBearing, Temperature, WindChill, Dewpoint, HeatIndex,
								Humidity, Pressure, RainToday, SolarRad, UV, Raincounter, FeelsLike, Humidex, ApparentTemp, IndoorTemp, IndoorHum, CurrentSolarMax, RainRate, -1, -1);
							DoTrendValues(timestamp);

							if (cumulus.StationOptions.CalculatedET && timestamp.Minute == 0)
							{
								// Start of a new hour, and we want to calculate ET in Cumulus
								CalculateEvaoptranspiration(timestamp);
							}

							UpdatePressureTrendString();
							UpdateStatusPanel(timestamp);

							// Add current data to the lists of web service updates to be done
							cumulus.AddToWebServiceLists(timestamp);

							if ((hour == rollHour) && !rolloverdone)
							{
								// do roll-over
								Cumulus.LogMessage("Day roll-over " + timestamp);
								DayReset(timestamp);

								rolloverdone = true;
							}

							if ((hour == 0) && !midnightraindone)
							{
								ResetMidnightRain(timestamp);
								ResetSunshineHours();
								midnightraindone = true;
							}
						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "Error in data");
						}
					}
				}
				else
				{
					Cumulus.LogMessage("No history records to process");
				}
			}
			else
			{
				Cumulus.LogMessage("No history records to process");
			}
		}

		public override void Start()
		{
			Cumulus.LogMessage("Starting Instromet data reading thread");

			try
			{
				while (!stop)
				{
					ImetGetData();
					if (cumulus.ImetLoggerInterval != cumulus.logints[cumulus.DataLogInterval])
					{
						// logging interval has changed; update station to match
						ImetSetLoggerInterval(cumulus.logints[cumulus.DataLogInterval]);
					}
					else
					{
						Thread.Sleep(cumulus.ImetOptions.ReadDelay);
					}
				}
			}
			// Catch the ThreadAbortException
			catch (ThreadAbortException)
			{
			}
			finally
			{
				comport.Close();
			}
		}

		private void ImetGetData()
		{
			const int TEMP1POS = 1;
			const int TEMP2POS = 2;
			const int RELHUMPOS = 3;
			const int PRESSPOS = 4;
			const int WINDPOS = 5;
			const int DIRPOS = 6;
			const int SUNPOS = 7;
			const int RAINPOS = 8;
			//const int CHECKSUMPOS = 9;

			DateTime now = DateTime.Now;

			int h = now.Hour;
			int min = now.Minute;

			if (min != previousminute)
			{
				previousminute = min;

				if (cumulus.StationOptions.SyncTime && (h == cumulus.StationOptions.ClockSettingHour) && (min == 2))
				{
					// It's 0400, set the station clock
					SetStationClock();
				}
			}

			SendCommand("RDLV");
			// read the response
			var response = GetResponse("rdlv");

			if (ValidChecksum(response) && !stop)
			{
				// split the data
				var sl = new List<string>(response.Split(','));

				if (sl.Count != 10 && sl[0] != "rdlv")
				{
					Cumulus.LogMessage($"RDLV: Unexpected response: {response}");
					return;
				}

				// Parse data using decimal points rather than user's decimal separator
				NumberFormatInfo provider = new NumberFormatInfo {NumberDecimalSeparator = "."};

				double windspeed = -999;
				double temp1 = -999;
				int humidity = -999;

				double varDbl;
				int varInt;

				if (!string.IsNullOrEmpty(sl[DIRPOS]) && int.TryParse(sl[DIRPOS], out varInt) &&
				    !string.IsNullOrEmpty(sl[WINDPOS]) && double.TryParse(sl[WINDPOS], NumberStyles.Float, provider, out varDbl))
				{
					windspeed = varDbl;
					DoWind(ConvertWindMSToUser(windspeed), varInt, ConvertWindMSToUser(windspeed), now);
				}
				else
				{
					Cumulus.LogMessage($"RDLV: Unexpected wind dir/speed format, found: {sl[DIRPOS]}/{sl[WINDPOS]}");
				}


				if (!string.IsNullOrEmpty(sl[TEMP1POS]) && double.TryParse(sl[TEMP1POS], NumberStyles.Float, provider, out varDbl))
				{
					temp1 = varDbl;
					DoTemperature(ConvertTempCToUser(temp1), now);
					DoWindChill(null, now);
				}
				else
				{
					DoTemperature(null, now);
					DoWindChill(null, now);
					Cumulus.LogMessage($"RDLV: Unexpected temperature 1 format, found: {sl[TEMP1POS]}");
				}

				if (!string.IsNullOrEmpty(sl[TEMP2POS]))  // TEMP2 is optional
				{
					if (double.TryParse(sl[TEMP2POS], NumberStyles.Float, provider, out varDbl))
					{
						if (cumulus.ExtraDataLogging.Temperature)
						{
							// use second temp as Extra Temp 1
							DoExtraTemp(ConvertTempCToUser(varDbl), 1);
						}
						else
						{
							// use second temp as wet bulb
							DoWetBulb(ConvertTempCToUser(varDbl), now);
						}
					}
					else
					{
						Cumulus.LogMessage($"RDLV: Unexpected temperature 2 format, found: {sl[TEMP2POS]}");
						if (cumulus.ExtraDataLogging.Temperature)
						{
							DoExtraTemp(null, 1);
						}
						else
						{
							// use second temp as wet bulb
							DoWetBulb(null, now);
						}
					}
				}

				if (!string.IsNullOrEmpty(sl[RELHUMPOS]) && double.TryParse(sl[RELHUMPOS], NumberStyles.Float, provider, out varDbl))
				{
					humidity = Convert.ToInt32(varDbl);
					DoHumidity(humidity, now);
				}
				else
				{
					Humidity = null;
					Cumulus.LogMessage($"RDLV: Unexpected humidity format, found: {sl[RELHUMPOS]}");
				}

				if (!string.IsNullOrEmpty(sl[PRESSPOS]) && double.TryParse(sl[PRESSPOS], NumberStyles.Float, provider, out varDbl))
				{
					DoPressure(ConvertPressMBToUser(varDbl), now);
					UpdatePressureTrendString();
				}
				else
				{
					Cumulus.LogMessage($"RDLV: Unexpected pressure format, found: {sl[PRESSPOS]}");
				}


				if (!string.IsNullOrEmpty(sl[RAINPOS]) && double.TryParse(sl[RAINPOS], NumberStyles.Float, provider, out varDbl))
				{
					DoRain(ConvertRainMMToUser(varDbl), -1, now);
				}
				else
				{
					Cumulus.LogMessage($"RDLV: Unexpected rain format, found: {sl[RAINPOS]}");
				}

				if (!string.IsNullOrEmpty(sl[SUNPOS]) && double.TryParse(sl[SUNPOS], NumberStyles.Float, provider, out varDbl))
				{
					DoSunHours(varDbl);
				}
				else
				{
					Cumulus.LogMessage($"RDLV: Unexpected rain format, found: {sl[RAINPOS]}");
				}

				DoDewpoint(null, now);

				if (temp1 > -999 && humidity > -999)
				{
					DoHumidex(now);
					DoCloudBaseHeatIndex(now);

					if (windspeed > -999)
					{
						DoApparentTemp(now);
						DoFeelsLike(now);
					}
				}

				DoForecast("", false);

				UpdateStatusPanel(now);
				UpdateMQTT();
			}
			else
			{
				Cumulus.LogMessage("RDLV: Invalid checksum:");
				Cumulus.LogMessage(response);
			}

			if (!cumulus.ImetOptions.UpdateLogPointer || stop)
				return;

			// Keep the log pointer current, to avoid large numbers of logs
		    // being downloaded at next start-up
		    // Only do this every 30 read intervals
		    if (readCounter > 0)
		    {
			    readCounter--;
		    }
		    else
		    {
			    UpdateReadPointer();
			    readCounter = 30;
		    }
		}
	}
}