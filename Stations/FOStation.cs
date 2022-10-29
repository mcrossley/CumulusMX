using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Timers;
using HidSharp;
using Timer = System.Timers.Timer;

namespace CumulusMX
{
	internal class FOStation : WeatherStation
	{
		//private IDevice[] stations;
		//private IDevice device;

		private readonly double pressureOffset;
		private HidDevice hidDevice;
		private HidStream stream;
		private List<HistoryData> datalist;

		//private readonly int maxHistoryEntries;
		private int prevaddr = -1;
		private int prevraintotal = -1;
		private int ignoreraincount;
		private DateTime previousSensorClock;
		private DateTime previousStationClock;
		private DateTime previousSolarClock;
		private bool synchronising;
		private int synchroniseAttempts;
		private DateTime syncStart;
		private bool sensorSyncDone;
		private bool stationSyncDone;
		private bool solarSyncDone;
		private bool doSolarSync;
		//private DateTime lastraintip;
		//private int raininlasttip = 0;
		//private readonly double[] WindRunHourMult = {3.6, 1.0, 1.0, 1.0};
		private readonly Timer tmrDataRead;
		private int readCounter;
		private bool hadfirstsyncdata;
		private readonly byte[] prevdata = new byte[20];
		private readonly int foEntrysize;
		private readonly int foMaxAddr;
		//private int FOmaxhistoryentries;
		private readonly bool hasSolar;
		private bool readingData = false;

		const int DefaultVid = 0x1941;
		const int DefaultPid = 0x8021;

		internal FOStation(Cumulus cumulus) : base(cumulus)
		{
			var data = new byte[32];

			tmrDataRead = new Timer();

			calculaterainrate = true;

			hasSolar = cumulus.StationType == StationTypes.FineOffsetSolar;

			Cumulus.LogMessage("FO synchronise reads: " + cumulus.FineOffsetOptions.SyncReads);
			if (cumulus.FineOffsetOptions.SyncReads)
			{
				Cumulus.LogMessage("FO synchronise avoid time: " + cumulus.FineOffsetOptions.ReadAvoidPeriod);
				Cumulus.LogMessage($"FO last station time: {FOStationClockTime:s}");
				Cumulus.LogMessage($"FO last sensor time : {FOSensorClockTime:s}");
				if (hasSolar)
					Cumulus.LogMessage($"FO last solar time  : {FOSolarClockTime:s}");
			}

			if (hasSolar)
			{
				foEntrysize = 0x14;
				foMaxAddr = 0xFFEC;
				//maxHistoryEntries = 3264;
			}
			else
			{
				foEntrysize = 0x10;
				foMaxAddr = 0xFFF0;
				//maxHistoryEntries = 4080;
			}

			// no dew point data supplied by the station
			cumulus.StationOptions.CalculatedDP = true;


			do
			{
				if (OpenHidDevice())
				{
					// Get the block of data containing the logging interval
					Cumulus.LogMessage("Reading station logging interval");
					if (ReadAddress(0x10, data))
					{
						int logint = data[0];

						if (logint != cumulus.logints[cumulus.DataLogInterval])
						{
							var msg = $"Warning, your console logging interval ({logint} mins) does not match the Cumulus logging interval ({cumulus.logints[cumulus.DataLogInterval]} mins)";
							Cumulus.LogConsoleMessage(msg);
							Cumulus.LogMessage(msg);
							if (cumulus.FineOffsetOptions.SetLoggerInterval)
							{
								WriteAddress(0x10, (byte)cumulus.logints[cumulus.DataLogInterval]); // write the logging new logging interval
								WriteAddress(0x1A, 0xAA); // tell the station to read the new parameter
								do
								{
									Thread.Sleep(1000);  // sleep to let it reconfigure
									ReadAddress(0x10, data);
								} while (data[9] != 0);
							}
						}
					}

					// Get the block of data containing the abs and rel pressures
					Cumulus.LogMessage("Reading station pressure offset");

					double relpressure = (((data[17] & 0x3f) * 256) + data[16]) / 10.0f;
					double abspressure = (((data[19] & 0x3f) * 256) + data[18]) / 10.0f;
					pressureOffset = relpressure - abspressure;
					Cumulus.LogMessage("Rel pressure      = " + relpressure);
					Cumulus.LogMessage("Abs pressure      = " + abspressure);
					Cumulus.LogMessage("Calculated Offset = " + pressureOffset);
					if (cumulus.EwOptions.PressOffset < 9999.0)
					{
						Cumulus.LogMessage("Ignoring calculated offset, using offset value from cumulus.ini file");
						Cumulus.LogMessage("EWpressureoffset = " + cumulus.EwOptions.PressOffset);
						pressureOffset = cumulus.EwOptions.PressOffset;
					}
				}
				else
				{
					// pause for 10 seconds then try again
					Thread.Sleep(10000);
				}
			} while (hidDevice == null || stream == null || !stream.CanRead);
		}

		public override void DoStartup()
		{
			// Read the data from the logger
			startReadingHistoryData();
		}

		public override void startReadingHistoryData()
		{
			LoadLastHoursFromDataLogs(cumulus.LastUpdateTime);

			bw = new BackgroundWorker();
			bw.DoWork += bw_DoWork;
			bw.RunWorkerCompleted += bw_RunWorkerCompleted;
			bw.WorkerReportsProgress = true;
			bw.RunWorkerAsync();
		}

		public override void Stop()
		{
			Cumulus.LogMessage("Stopping data read timer");
			tmrDataRead.Stop();
			Cumulus.LogMessage("Stopping minute timer");
			StopMinuteTimer();
			Cumulus.LogMessage("Nullifying hidDevice");
			hidDevice = null;
			Cumulus.LogMessage("Exit FOStation.Stop()");

			// Call the common code in the base class
			base.Stop();
		}

		private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			cumulus.NormalRunning = true;
			Cumulus.LogMessage("Archive reading thread completed");
			Start();
			DoDayResetIfNeeded();
			DoTrendValues(DateTime.Now);
			cumulus.StartTimersAndSensors();
		}

		private void bw_DoWork(object sender, DoWorkEventArgs e)
		{
			//var ci = new CultureInfo("en-GB");
			//System.Threading.Thread.CurrentThread.CurrentCulture = ci;
			//cumulus.LogDebugMessage("Lock: Station waiting for the lock");
			Cumulus.syncInit.Wait();
			//cumulus.LogDebugMessage("Lock: Station has the lock");
			try
			{
				getAndProcessHistoryData();
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Exception occurred reading archive data");
			}

			//cumulus.LogDebugMessage("Lock: Station releasing the lock");
			Cumulus.syncInit.Release();
		}

		public override void getAndProcessHistoryData()
		{
			var data = new byte[32];
			int interval = 0;
			Cumulus.LogMessage("Current culture: " + CultureInfo.CurrentCulture.DisplayName);
			//DateTime now = DateTime.Now;
			Cumulus.LogMessage(DateTime.Now.ToString("G"));
			Cumulus.LogMessage("Start reading history data");
			Cumulus.LogConsoleMessage("Downloading Archive Data");
			DateTime timestamp = DateTime.Now;
			//LastUpdateTime = DateTime.Now; // lastArchiveTimeUTC.ToLocalTime();
			Cumulus.LogMessage("Last Update = " + cumulus.LastUpdateTime);
			cumulus.LogDebugMessage("Reading fixed memory block");
			if (!ReadAddress(0, data))
			{
				return;
			}

			// get address of current location
			int addr = data[31] * 256 + data[30];
			int previousaddress = addr;

			// get the number of logger entries the console has recorded
			int logEntries = data[28] * 256 + data[27];
			cumulus.LogDebugMessage($"Console has {logEntries} log entries");

			Cumulus.LogMessage("Reading current address " + addr.ToString("X4"));
			if (!ReadAddress(addr, data))
			{
				return;
			}

			bool moredata = true;

			datalist = new List<HistoryData>();

			while (moredata)
			{
				var followinginterval = interval;
				interval = data[0];
				cumulus.LogDebugMessage($"This logger record interval = {interval} mins");

				// calculate time stamp of previous history data
				timestamp = timestamp.AddMinutes(-interval);

				// Now for the first record we need to work out what logging interval that belongs to...
				if (datalist.Count == 0)
				{
					// number of ticks in a logging interval
					var intTicks = TimeSpan.FromMinutes(cumulus.logints[cumulus.DataLogInterval]).Ticks;
					// date/time of the last log interval
					var lastLogInterval = new DateTime((DateTime.Now.Ticks / intTicks) * intTicks);
					// which log interval does this data belong to, the last or the one before that?
					timestamp = timestamp > lastLogInterval ? lastLogInterval : lastLogInterval.AddTicks(-intTicks);
				}

				if ((interval != 255) && (timestamp > cumulus.LastUpdateTime) && (datalist.Count < logEntries))
				{
					// Test if the current address has changed
					cumulus.LogDebugMessage("Reading fixed memory block");
					if (!ReadAddress(0, data))
					{
						return;
					}
					var newAddr = data[31] * 256 + data[30];
					if (newAddr != previousaddress)
					{
						// The current logger address has changed, pause to allow console to sort itself out
						cumulus.LogDebugMessage("Console logger location changed, pausing for a sort while");
						previousaddress = newAddr;
						Thread.Sleep(2000);
					}

					// Read previous data
					addr -= foEntrysize;
					if (addr < 0x100)
					{
						addr = foMaxAddr; // wrap around
					}

					Cumulus.LogMessage("Read logger entry for " + timestamp + " address " + addr.ToString("X4"));
					if (!ReadAddress(addr, data))
					{
						return;
					}
					cumulus.LogDebugMessage("Logger Data block: " + BitConverter.ToString(data, 0, foEntrysize));

					// add history data to collection

					var histData = new HistoryData
					{
						timestamp = timestamp,
						interval = interval,
						followinginterval = followinginterval,
						inHum = data[1] == 255 ? 10 : data[1],
						outHum = data[4] == 255 ? 10 : data[4]
					};
					double outtemp = (data[5] + (data[6] & 0x7F)*256)/10.0f;
					var sign = (byte) (data[6] & 0x80);
					if (sign == 0x80) outtemp = -outtemp;
					if (outtemp > -200) histData.outTemp = outtemp;
					histData.windGust = (data[10] + ((data[11] & 0xF0)*16))/10.0f;
					histData.windSpeed = (data[9] + ((data[11] & 0x0F)*256))/10.0f;
					histData.windBearing = (int) (data[12]*22.5f);

					histData.rainCounter = data[13] + (data[14]*256);

					double intemp = (data[2] + (data[3] & 0x7F)*256)/10.0f;
					sign = (byte) (data[3] & 0x80);
					if (sign == 0x80) intemp = -intemp;
					histData.inTemp = intemp;
					// Get pressure and convert to sea level
					histData.pressure = (data[7] + (data[8]*256))/10.0f + pressureOffset;
					histData.SensorContactLost = (data[15] & 0x40) == 0x40;
					if (hasSolar)
					{
						histData.uvVal = data[19];
						histData.solarVal = (data[16] + (data[17]*256) + (data[18]*65536))/10.0;
					}

					datalist.Add(histData);

					//bw.ReportProgress(datalist.Count, "collecting");

					if (!Program.service)
					{
						Console.Write($"\r - Downloaded {datalist.Count} records, current date - {histData.timestamp:g}");
					}
				}
				else
				{
					moredata = false;
				}
			}

			if (!Program.service)
			{
				Console.WriteLine("");
			}
			Cumulus.LogConsoleMessage("Completed read of history data from the console");
			Cumulus.LogMessage("Number of history entries = " + datalist.Count);

			if (datalist.Count > 0)
			{
				ProcessHistoryData();
			}

			//using (cumulusEntities dataContext = new cumulusEntities())
			//{
			//    UpdateHighsAndLows(dataContext);
			//}
		}

		private void ProcessHistoryData()
		{
			int totalentries = datalist.Count;

			Cumulus.LogConsoleMessage("Processing history data, number of entries = " + totalentries);

			int rollHour = Math.Abs(cumulus.GetHourInc());
			int luhour = cumulus.LastUpdateTime.Hour;
			bool rolloverdone = luhour == rollHour;
			bool midnightraindone = luhour == 0;
			int recCount = datalist.Count;
			int processedCount = 0;

			while (datalist.Count > 0)
			{
				HistoryData historydata = datalist[^1];

				DateTime timestamp = historydata.timestamp;

				Cumulus.LogMessage("Processing data for " + timestamp);

				int h = timestamp.Hour;

				//  if outside roll-over hour, roll-over yet to be done
				if (h != rollHour)
				{
					rolloverdone = false;
				}

				// In roll-over hour and roll-over not yet done
				if (h == rollHour && !rolloverdone)
				{
					// do roll-over
					Cumulus.LogMessage("Day roll-over " + timestamp.ToShortTimeString());
					DayReset(timestamp);

					rolloverdone = true;
				}

				// Not in midnight hour, midnight rain yet to be done
				if (h != 0)
				{
					midnightraindone = false;
				}

				// In midnight hour and midnight rain (and sun) not yet done
				if (h == 0 && !midnightraindone)
				{
					ResetMidnightRain(timestamp);
					ResetSunshineHours(timestamp);
					ResetMidnightTemperatures(timestamp);
					midnightraindone = true;
				}

				// Indoor Humidity ======================================================
				if (historydata.inHum > 100 || historydata.inHum < 0)
				{
					// 255 is the overflow value, when RH gets below 10% - ignore
					DoIndoorHumidity(null);
					Cumulus.LogMessage("Ignoring bad data: inhum = " + historydata.inHum);
				}
				else
				{
					DoIndoorHumidity(historydata.inHum);
				}

				// Indoor Temperature ===================================================
				if (historydata.inTemp < -50 || historydata.inTemp > 50)
				{
					Cumulus.LogMessage("Ignoring bad data: intemp = " + historydata.inTemp);
				}
				else
				{
					DoIndoorTemp(ConvertTempCToUser(historydata.inTemp));
				}

				// Pressure =============================================================

				if ((historydata.pressure < cumulus.EwOptions.MinPressMB) || (historydata.pressure > cumulus.EwOptions.MaxPressMB))
				{
					Cumulus.LogMessage("Ignoring bad data: pressure = " + historydata.pressure);
					Cumulus.LogMessage("                   offset = " + pressureOffset);
				}
				else
				{
					DoPressure(ConvertPressMBToUser(historydata.pressure), timestamp);
				}

				if (historydata.SensorContactLost)
				{
					Cumulus.LogMessage("Sensor contact lost; ignoring outdoor data");
				}
				else
				{
					// Outdoor Humidity =====================================================
					if (historydata.outHum > 100 || historydata.outHum < 0)
					{
						// 255 is the overflow value, when RH gets below 10% - ignore
						Humidity = null;
						Cumulus.LogMessage("Ignoring bad data: outhum = " + historydata.outHum);
					}
					else
					{
						DoHumidity(historydata.outHum, timestamp);
					}

					// Wind =================================================================
					if (historydata.windGust > 60 || historydata.windGust < 0)
					{
						Cumulus.LogMessage("Ignoring bad data: gust = " + historydata.windGust);
					}
					else if (historydata.windSpeed > 60 || historydata.windSpeed < 0)
					{
						Cumulus.LogMessage("Ignoring bad data: speed = " + historydata.windSpeed);
					}
					else
					{
						DoWind(ConvertWindMSToUser(historydata.windGust), historydata.windBearing, ConvertWindMSToUser(historydata.windSpeed), timestamp);
					}

					// Outdoor Temperature ==================================================
					if (historydata.outTemp < -50 || historydata.outTemp > 70)
					{
						DoTemperature(null, timestamp);
						Cumulus.LogMessage("Ignoring bad data: outtemp = " + historydata.outTemp);
					}
					else
					{
						DoTemperature(ConvertTempCToUser(historydata.outTemp), timestamp);
						// add in 'archivePeriod' minutes worth of temperature to the temp samples
						tempsamplestoday += historydata.interval;
						TempTotalToday += (Temperature.Value * historydata.interval);
					}

					// update chill hours
					if (Temperature.HasValue && Temperature.Value < cumulus.ChillHourThreshold)
					{
						// add 1 minute to chill hours
						ChillHours += (historydata.interval / 60.0);
					}

					var raindiff = prevraintotal == -1 ? 0 : historydata.rainCounter - prevraintotal;

					// record time of last rain tip, to use in
					// normal running rain rate calc NB rain rate calc not currently used
					/*
					if (raindiff > 0)
					{
						lastraintip = timestamp;

						raininlasttip = raindiff;
					}
					else
					{
						lastraintip = DateTime.MinValue;

						raininlasttip = 0;
					}
					*/
					double rainrate;

					if (raindiff > 100)
					{
						Cumulus.LogMessage("Warning: large increase in rain gauge tip count: " + raindiff);
						rainrate = 0;
					}
					else
					{
						if (historydata.interval > 0)
						{
							rainrate = ConvertRainMMToUser((raindiff * 0.3) * (60.0 / historydata.interval));
						}
						else
						{
							rainrate = 0;
						}
					}

					DoRain(ConvertRainMMToUser(historydata.rainCounter*0.3), rainrate, timestamp);

					prevraintotal = historydata.rainCounter;

					DoDewpoint(null, timestamp);
					DoWindChill(null, timestamp);

					if (Temperature.HasValue && Humidity.HasValue)
					{
						DoApparentTemp(timestamp);
						DoFeelsLike(timestamp);
						DoHumidex(timestamp);
						DoCloudBaseHeatIndex(timestamp);
					}
					if (hasSolar)
					{
						if (historydata.uvVal < 0 || historydata.uvVal > 16)
						{
							Cumulus.LogMessage("Invalid UV-I value ignored: " + historydata.uvVal);
							DoUV(null, timestamp);
						}
						else
							DoUV(historydata.uvVal, timestamp);

						if (historydata.solarVal >= 0 && historydata.solarVal <= 300000)
						{
							DoSolarRad((int) Math.Floor(historydata.solarVal*cumulus.SolarOptions.LuxToWM2), timestamp);

							// add in archive period worth of sunshine, if sunny
							if ((SolarRad > CurrentSolarMax*cumulus.SolarOptions.SunThreshold/100) && (SolarRad >= cumulus.SolarOptions.SolarMinimum))
								SunshineHours += (historydata.interval/60.0);

							LightValue = historydata.solarVal;
						}
						else
						{
							Cumulus.LogMessage("Invalid solar value ignored: " + historydata.solarVal);
							DoSolarRad(null, timestamp);
						}
					}
				}
				// add in 'following interval' minutes worth of wind speed to windrun
				if (WindAverage.HasValue)
				{
					Cumulus.LogMessage("Windrun: " + WindAverage.Value.ToString(cumulus.WindFormat) + cumulus.Units.WindText + " for " + historydata.followinginterval + " minutes = " +
									(WindAverage.Value * WindRunHourMult[cumulus.Units.Wind] * historydata.followinginterval / 60.0).ToString(cumulus.WindRunFormat) + cumulus.Units.WindRunText);

					WindRunToday += (WindAverage.Value * WindRunHourMult[cumulus.Units.Wind] * historydata.followinginterval / 60.0);

					// update dominant wind bearing
					CalculateDominantWindBearing(Bearing, WindAverage, historydata.interval);

					CheckForWindrunHighLow(timestamp);
				}

				// update heating/cooling degree days
				UpdateDegreeDays(historydata.interval);

				bw.ReportProgress((totalentries - datalist.Count)*100/totalentries, "processing");

				_ = cumulus.DoLogFile(timestamp,false);
				_ = cumulus.DoExtraLogFile(timestamp);
				cumulus.MySqlStuff.DoRealtimeData(999, false, timestamp);

				AddRecentDataWithAq(timestamp, WindAverage, RecentMaxGust, WindLatest, Bearing, AvgBearing, Temperature, WindChill, Dewpoint, HeatIndex,
					Humidity, Pressure, RainToday, SolarRad, UV, Raincounter, FeelsLike, Humidex, ApparentTemp, IndoorTemp, IndoorHum, CurrentSolarMax, RainRate);
				DoTrendValues(timestamp);

				if (cumulus.StationOptions.CalculatedET && timestamp.Minute == 0)
				{
					// Start of a new hour, and we want to calculate ET in Cumulus
					CalculateEvaoptranspiration(timestamp);
				}

				UpdatePressureTrendString();
				UpdateStatusPanel(timestamp);
				cumulus.AddToWebServiceLists(timestamp);
				datalist.RemoveAt(datalist.Count - 1);

				if (!Program.service)
				{
					processedCount++;

					Console.Write("\r - processed " + (((double)processedCount) / recCount).ToString("P0"));
				}
			}

			if (!Program.service)
			{
				Console.WriteLine("");
			}
			Cumulus.LogConsoleMessage("End processing history data");
		}

		/// <summary>
		///     Read and process data in a loop, sleeping between reads
		/// </summary>
		public override void Start()
		{
			tmrDataRead.Elapsed += DataReadTimerTick;
			tmrDataRead.Interval = 16000;
			tmrDataRead.Enabled = true;

			readingData = true;
			GetAndProcessData();
			readingData = false;
		}

		private bool OpenHidDevice()
		{
			var devicelist = DeviceList.Local;

			int vid = (cumulus.FineOffsetOptions.VendorID < 0 ? DefaultVid : cumulus.FineOffsetOptions.VendorID);
			int pid = (cumulus.FineOffsetOptions.ProductID < 0 ? DefaultPid : cumulus.FineOffsetOptions.ProductID);

			Cumulus.LogMessage("Looking for Fine Offset station, VendorID=0x" + vid.ToString("X4") + " ProductID=0x" + pid.ToString("X4"));
			Cumulus.LogConsoleMessage("Looking for Fine Offset station");

			hidDevice = devicelist.GetHidDeviceOrNull(vendorID: vid, productID: pid);

			if (hidDevice != null)
			{
				Cumulus.LogMessage("Fine Offset station found");
				Cumulus.LogConsoleMessage("Fine Offset station found");

				if (hidDevice.TryOpen(out stream))
				{
					Cumulus.LogMessage("Stream opened");
					Cumulus.LogConsoleMessage("Connected to station");
					stream.Flush();
					return true;
				}
				else
				{
					Cumulus.LogMessage("Stream open failed");
					return false;
				}
			}
			else
			{
				Cumulus.LogMessage("*** Fine Offset station not found ***");
				Cumulus.LogMessage("Found the following USB HID Devices...");
				int cnt = 0;
				foreach (HidDevice device in devicelist.GetHidDevices())
				{
					Cumulus.LogMessage($"   {device}");
					cnt++;
				}

				if (cnt == 0)
				{
					Cumulus.LogMessage("No USB HID devices found!");
				}

				return false;
			}
		}


		/// <summary>
		///     Read the 32 bytes starting at 'address'
		/// </summary>
		/// <param name="address">The address of the data</param>
		/// <param name="buff">Where to return the data</param>
		private bool ReadAddress(int address, byte[] buff)
		{
			//Cumulus.LogMessage("Reading address " + address.ToString("X6"));
			var lowbyte = (byte) (address & 0xFF);
			var highbyte = (byte) (address >> 8);

			// Returns 9-byte USB packet, with report ID in first byte
			var response = new byte[9];
			const int responseLength = 9;
			const int startByte = 1;

			var request = new byte[] {0, 0xa1, highbyte, lowbyte, 0x20, 0xa1, highbyte, lowbyte, 0x20};

			int ptr = 0;

			if (hidDevice == null)
			{
				if (!DataStopped)
				{
					DataStoppedTime = DateTime.Now;
					DataStopped = true;
				}
				cumulus.DataStoppedAlarm.LastError = "USB device no longer detected";
				cumulus.DataStoppedAlarm.Triggered = true;
				return false;
			}

			//response = device.WriteRead(0x00, request);
			try
			{
				stream.Write(request);
			}
			catch (Exception ex)
			{
				Cumulus.LogConsoleMessage("Error sending command to station - it may need resetting", ConsoleColor.Red, true);
				cumulus.LogExceptionMessage(ex, "Error sending command to station - it may need resetting");
				if (!DataStopped)
				{
					DataStoppedTime = DateTime.Now;
					DataStopped = true;
				}
				cumulus.DataStoppedAlarm.LastError = "Error reading data from station - it may need resetting. " + ex.Message;
				cumulus.DataStoppedAlarm.Triggered = true;
				return false;
			}

			Thread.Sleep(cumulus.FineOffsetOptions.ReadTime);
			for (int i = 1; i < 5; i++)
			{
				//Cumulus.LogMessage("Reading 8 bytes");
				try
				{
					stream.Read(response, 0, responseLength);
				}
				catch (Exception ex)
				{
					Cumulus.LogConsoleMessage("Error reading data from station - it may need resetting", ConsoleColor.Red, true);
					cumulus.LogExceptionMessage(ex, "Error reading data from station - it may need resetting");
					if (!DataStopped)
					{
						DataStoppedTime = DateTime.Now;
						DataStopped = true;
					}
					cumulus.DataStoppedAlarm.LastError = "Error reading data from station - it may need resetting. " + ex.Message;
					cumulus.DataStoppedAlarm.Triggered = true;
					return false;
				}

				var recData = " Data" + i + ": "  + BitConverter.ToString(response, startByte, responseLength - startByte);
				for (int j = startByte; j < responseLength; j++)
				{
					buff[ptr++] = response[j];
				}
				cumulus.LogDataMessage(recData);
				LogRawStationData(recData, false);
			}
			return true;
		}

		private bool WriteAddress(int address, byte val)
		{
			var addrlowbyte = (byte)(address & 0xFF);
			var addrhighbyte = (byte)(address >> 8);

			var request = new byte[] { 0, 0xa2, addrhighbyte, addrlowbyte, 0x20, 0xa2, val, 0, 0x20 };

			if (hidDevice == null)
			{
				return false;
			}

			//response = device.WriteRead(0x00, request);
			try
			{
				stream.Write(request);
			}
			catch (Exception ex)
			{
				Cumulus.LogConsoleMessage("Error sending command to station - it may need resetting");
				cumulus.LogExceptionMessage(ex, "Error sending command to station - it may need resetting");
				if (!DataStopped)
				{
					DataStoppedTime = DateTime.Now;
					DataStopped = true;
				}
				cumulus.DataStoppedAlarm.LastError = "Error sending command to station - it may need resetting: " + ex.Message;
				cumulus.DataStoppedAlarm.Triggered = true;
				return false;
			}

			return true;
		}

		private void DataReadTimerTick(object state, ElapsedEventArgs elapsedEventArgs)
		{
			if (DataStopped)
			{
				Cumulus.LogMessage("Attempting to reopen the USB device...");
				// We are not getting any data from the station, try reopening the USB connection
				if (stream != null)
				{
					try
					{
						stream.Close();
					}
					catch { }
				}

				if (!OpenHidDevice())
				{
					Cumulus.LogMessage("Failed to reopen the USB device");
					return;
				}
			}

			if (!readingData)
			{
				readingData = true;
				GetAndProcessData();
				readingData = false;
			}
		}

		/// <summary>
		///     Read current data and process it
		/// </summary>
		private void GetAndProcessData()
		{
			//   Curr Reading Loc
			// 0  Time Since Last Save
			// 1  Hum In
			// 2  Temp In
			// 3  "
			// 4  Hum Out
			// 5  Temp Out
			// 6  "
			// 7  Pressure
			// 8  "
			// 9  Wind Speed m/s
			// 10  Wind Gust m/s
			// 11  Speed and Gust top nibbles (Gust top nibble)
			// 12  Wind Dir
			// 13  Rain counter
			// 14  "
			// 15  status

			// 16 Solar (Lux)
			// 17 "
			// 18 "
			// 19 UV

			//var ci = new CultureInfo("en-GB");
			//System.Threading.Thread.CurrentThread.CurrentCulture = ci;

			var data = new byte[32];
			var now = DateTime.Now;

			if (cumulus.FineOffsetOptions.SyncReads && !synchronising)
			{
				var doSensorSync = DateTime.Now.Subtract(FOSensorClockTime).TotalDays > 1;
				var doStationSync = DateTime.Now.Subtract(FOStationClockTime).TotalDays > 1;
				doSolarSync = hasSolar && DateTime.Now.Subtract(FOSolarClockTime).TotalDays > 1;

				if (doSensorSync || doStationSync || doSolarSync)
				{
					doSolarSync = hasSolar;

					if (hasSolar && DateTime.Now.Subtract(FOSolarClockTime).TotalDays > 1)
					{
						if (DateTime.Now.CompareTo(cumulus.SunRiseTime.AddMinutes(30)) > 0)
						{
							// after sunrise
							if (DateTime.Now.CompareTo(cumulus.SunSetTime.AddMinutes(-30)) > 0)
							{
								// before sunset, we can go!
							}
							else
							{
								// after sunset, delay until after next sunrise plus 0.5 hour (approx, we use todays sunrise rather than tomorrows)
								var delayMins = 1440 - FOSolarClockTime.TimeOfDay.TotalMinutes; // from solar time to midnight
								delayMins += cumulus.SunRiseTime.TimeOfDay.TotalMinutes + 30;   // from midnight to sunrise plus 1/2 hour
								FOSolarClockTime.AddMinutes(delayMins);
								doSolarSync = false;
							}
						}
						else
						{
							// prior to sunrise, delay until after sunrise
							var delayMins = cumulus.SunRiseTime.TimeOfDay.TotalMinutes - FOSolarClockTime.TimeOfDay.TotalMinutes + 30;
							FOSolarClockTime.AddMinutes(delayMins);
							doSolarSync = false;
						}
					}

					if (synchroniseAttempts == 0)
					{
						InitialiseSync(doSolarSync);
					}

					// (re)synchronise data reads to try to avoid USB lock-up problem
					if ((!sensorSyncDone || !solarSyncDone || !stationSyncDone) && synchroniseAttempts <= 2)
					{
						StartSynchronising();
					}
				}
				else
				{
					int secsToSkip = 0;

					bool sensorclockOK = true;

					// Check that were not within N seconds of the station updating memory
					if (FOSensorClockTime != DateTime.MinValue)
					{
						secsToSkip = (int)(Math.Floor(now.Subtract(FOSensorClockTime).TotalSeconds)) % 48;

						sensorclockOK = (secsToSkip >= (cumulus.FineOffsetOptions.ReadAvoidPeriod - 1)) && (secsToSkip <= (47 - cumulus.FineOffsetOptions.ReadAvoidPeriod));
					}

					bool stationclockOK = true;
					if (FOStationClockTime != DateTime.MinValue)
					{
						secsToSkip = (int)(Math.Floor(now.Subtract(FOStationClockTime).TotalSeconds)) % 60;

						stationclockOK = (secsToSkip >= (cumulus.FineOffsetOptions.ReadAvoidPeriod - 1)) && (secsToSkip <= (59 - cumulus.FineOffsetOptions.ReadAvoidPeriod));
					}


					bool solarclockOK = true;
					if (hasSolar && FOSolarClockTime != DateTime.MinValue)
					{
						secsToSkip = (int)(Math.Floor(now.Subtract(FOSolarClockTime).TotalSeconds)) % 60;

						solarclockOK = (secsToSkip >= (cumulus.FineOffsetOptions.ReadAvoidPeriod - 1)) && (secsToSkip <= (59 - cumulus.FineOffsetOptions.ReadAvoidPeriod));
					}

					if (!sensorclockOK || !stationclockOK || !solarclockOK)
					{
						if (!solarclockOK)
						{
							cumulus.LogDebugMessage("Synchronise: Within " + cumulus.FineOffsetOptions.ReadAvoidPeriod + " seconds of solar change, skipping read");
							return;
						}

						if (!stationclockOK)
						{
							cumulus.LogDebugMessage("Synchronise: Within " + cumulus.FineOffsetOptions.ReadAvoidPeriod + " seconds of console clock minute change, skipping read");
							return;
						}

						if (!sensorclockOK)
						{
							cumulus.LogDebugMessage("Synchronise: Within " + cumulus.FineOffsetOptions.ReadAvoidPeriod + " seconds of sensor data change, delaying reads");

							int delay = 0;
							if (secsToSkip < 8)
							{
								delay = 8 - secsToSkip;
							}
							else if (secsToSkip > 40)
							{
								delay = 8 + secsToSkip % 8;
							}

							Cumulus.LogMessage($"Pausing for {delay} seconds to unsynchronise data reads with 48 second updates...");

							// We'll shift the timer by x seconds to try and avoid it at completely
							tmrDataRead.Enabled = false;
							Thread.Sleep(delay * 1000);
							tmrDataRead.Enabled = true;
						}
					}
				}
			}

			// get the block of memory containing the current data location

			cumulus.LogDataMessage("Reading first block");
			if (!ReadAddress(0, data))
			{
				return;
			}

			int addr = (data[31]*256) + data[30];

			cumulus.LogDataMessage("First block read, addr = " + addr.ToString("X4"));

			if (prevaddr == -1)
			{
				prevaddr = addr;
				hadfirstsyncdata = false;
			}
			else if (addr != prevaddr)
			{
				// location has changed, skip this read to give it chance to update
				//Cumulus.LogMessage("Location changed, skipping");
				cumulus.LogDebugMessage("Address changed, delay reading data this time");
				cumulus.LogDebugMessage("addr=" + addr.ToString("X4") + " previous=" + prevaddr.ToString("X4"));

				if (synchronising && !stationSyncDone)
				{
					//cumulus.LogConsoleMessage(" - Console clock minute changed");
					Cumulus.LogMessage("Synchronise: Console clock minute changed");

					FOStationClockTime = DateTime.Now;
					stationSyncDone = true;
				}

				prevaddr = addr;
				hadfirstsyncdata = false;

				Thread.Sleep(5000); // delay reading data block for 5 seconds
			}


			cumulus.LogDataMessage("Reading data, addr = " + addr.ToString("X4"));

			if (!ReadAddress(addr, data))
			{
				return;
			}

			cumulus.LogDataMessage("Data read - " + BitConverter.ToString(data));
			LogRawStationData(BitConverter.ToString(data), false);

			now = DateTime.Now;

			if (synchronising)
			{
				bool datachanged = false;
				// ReadCounter determines whether we actually process the data (every 10 seconds)
				readCounter++;
				if (hadfirstsyncdata)
				{
					// Sensor Data change detection

					// ignore the first byte as this is a time increment every minute
					for (int i = 1; i < 16; i++)
					{
						if (prevdata[i] != data[i])
						{
							datachanged = true;
						}
					}

					if (datachanged)
					{
						//Cumulus.LogConsoleMessage("Sensor data changed");
						Cumulus.LogMessage("Synchronise: Sensor data changed");

						if (!sensorSyncDone)
						{
							FOSensorClockTime = now;
							sensorSyncDone = true;
						}

						for (int i = 1; i < 16; i++)
						{
							prevdata[i] = data[i];
						}
					}

					// station clock minute change
					// the minutes in the data block only seems to update when data is written to the block,
					// so we cannot use that as an accurate indication of when the console clock minute changes
					/*
					if (prevdata[0] != data[0])
					{
						cumulus.LogConsoleMessage("Console clock minute changed");
						cumulus.LogMessage("Synchronise: Console clock minute changed");

						if (!stationSync)
						{
							FOStationClockTime = now;
							stationSync = true;
						}

						prevdata[0] = data[0];
					}
					*/
				}
				else
				{
					hadfirstsyncdata = true;
					for (int i = 0; i < 16; i++)
					{
						prevdata[i] = data[i];
					}

				}

				// now do the solar
				if (hasSolar)
				{
					datachanged = false;
					if (hadfirstsyncdata)
					{
						for (int i = 16; i < 20; i++)
						{
							if (prevdata[i] != data[i])
							{
								datachanged = true;
							}
						}

						if (datachanged)
						{
							Cumulus.LogConsoleMessage("Solar data changed");
							Cumulus.LogMessage("Synchronise: Solar data changed");

							if (!solarSyncDone)
							{
								FOSolarClockTime = now;
								solarSyncDone = true;
							}

							for (int i = 16; i < 20; i++)
							{
								prevdata[i] = data[i];
							}
						}
					}
				}

				if (sensorSyncDone && solarSyncDone && stationSyncDone)
				{
					StopSynchronising();
					FinaliseSync();
				}
				else if (DateTime.Now.Subtract(syncStart).TotalMinutes > 1)
				{
					StopSynchronising();

					if (synchroniseAttempts == 3)
					{
						FinaliseSync();
					}
				}

					readCounter++;
			}


			if (!synchronising || (readCounter % 20) == 0)
			{
				LatestFOReading = addr.ToString("X4") + " Data: " + BitConverter.ToString(data, 0, 16);
				cumulus.LogDataMessage("Latest Block: " + LatestFOReading);

				// Indoor Humidity ====================================================
				int inhum = data[1];
				if (inhum > 100 || inhum < 0)
				{
					// bad value
					DoIndoorHumidity(null);
					Cumulus.LogMessage("Ignoring bad data: inhum = " + inhum);
				}
				else
				{
					// 255 is the overflow value, when RH gets below 10% - use 10%
					if (inhum == 255)
					{
						inhum = 10;
					}

					if (inhum > 0)
					{
						DoIndoorHumidity(inhum);
					}
				}

				// Indoor temperature ===============================================
				double intemp = ((data[2]) + (data[3] & 0x7F)*256)/10.0f;
				var sign = (byte) (data[3] & 0x80);
				if (sign == 0x80)
				{
					intemp = -intemp;
				}

				if (intemp < -50 || intemp > 50)
				{
					Cumulus.LogMessage("Ignoring bad data: intemp = " + intemp);
				}
				else
				{
					DoIndoorTemp(ConvertTempCToUser(intemp));
				}

				// Pressure =========================================================
				double pressure = (data[7] + ((data[8] & 0x3f)*256))/10.0f + pressureOffset;

				if (pressure < cumulus.EwOptions.MinPressMB || pressure > cumulus.EwOptions.MaxPressMB)
				{
					// bad value
					Cumulus.LogMessage("Ignoring bad data: pressure = " + pressure);
					Cumulus.LogMessage("                     offset = " + pressureOffset);
				}
				else
				{
					DoPressure(ConvertPressMBToUser(pressure), now);
					// Get station pressure in hPa by subtracting offset and calibrating
					// EWpressure offset is difference between rel and abs in hPa
					// PressOffset is user calibration in user units.
					pressure = (pressure - pressureOffset) * ConvertUserPressureToHPa(cumulus.Calib.Press.Mult).Value + ConvertUserPressureToHPa(cumulus.Calib.Press.Offset).Value;
					StationPressure = ConvertPressMBToUser(pressure);

					UpdatePressureTrendString();
				}

				var status = data[15];
				if ((status & 0x40) != 0)
				{
					SensorContactLost = true;
					Cumulus.LogMessage("Sensor contact lost; ignoring outdoor data");
				}
				else
				{
					SensorContactLost = false;

					// Outdoor Humidity ===================================================
					int outhum = data[4];
					if (outhum > 100 || outhum < 0)
					{
						// bad value
						// 255 is the overflow value, when RH gets below 10%
						Humidity = null;
						Cumulus.LogMessage("Ignoring bad data: outhum = " + outhum);
					}
					else
					{
						DoHumidity(outhum, now);
					}

					// Wind =============================================================
					double gust = (data[10] + ((data[11] & 0xF0)*16))/10.0f;
					double windspeed = (data[9] + ((data[11] & 0x0F)*256))/10.0f;
					var winddir = (int) (data[12]*22.5f);

					if (gust > 60 || gust < 0)
					{
						// bad value
						Cumulus.LogMessage("Ignoring bad data: gust = " + gust);
					}
					else if (windspeed > 60 || windspeed < 0)
					{
						// bad value
						Cumulus.LogMessage("Ignoring bad data: speed = " + gust);
					}
					else
					{
						DoWind(ConvertWindMSToUser(gust), winddir, ConvertWindMSToUser(windspeed), now);
					}

					// Outdoor Temperature ==============================================
					double outtemp = ((data[5]) + (data[6] & 0x7F)*256)/10.0f;
					sign = (byte) (data[6] & 0x80);
					if (sign == 0x80) outtemp = -outtemp;

					if (outtemp < -50 || outtemp > 70)
					{
						// bad value
						DoTemperature(null, now);
						Cumulus.LogMessage("Ignoring bad data: outtemp = " + outtemp);
					}
					else
					{
						DoTemperature(ConvertTempCToUser(outtemp), now);

						// calculate wind chill
						// The 'global average speed will have been determined by the call of DoWind
						// so use that in the wind chill calculation
						var avgspeedKPH = ConvertUserWindToKPH(WindAverage);

						// windinMPH = calibwind * 2.23693629;
						// calculate wind chill from calibrated C temp and calibrated win in KPH
						var val = MeteoLib.WindChill(ConvertUserTempToC(Temperature), avgspeedKPH);

						DoWindChill(ConvertTempCToUser(val), now);

						DoDewpoint(null, now);
						DoApparentTemp(now);
						DoFeelsLike(now);
						DoHumidex(now);
						DoCloudBaseHeatIndex(now);
					}

					// Rain ============================================================
					int raintot = data[13] + (data[14]*256);
					if (prevraintotal == -1)
					{
						// first reading
						prevraintotal = raintot;
						Cumulus.LogMessage("Rain total count from station = " + raintot);
					}

					int raindiff = Math.Abs(raintot - prevraintotal);

					if (raindiff > cumulus.EwOptions.MaxRainTipDiff)
					{
						Cumulus.LogMessage("Warning: large difference in rain gauge tip count: " + raindiff);

						ignoreraincount++;

						if (ignoreraincount == 6)
						{
							Cumulus.LogMessage("Six consecutive rain readings; accepting value. Adjusting start of day figure to compensate");
							raindaystart += (raindiff*0.3);
							// adjust current rain total counter
							Raincounter += (raindiff*0.3);
							Cumulus.LogMessage("Setting raindaystart to " + raindaystart);
							ignoreraincount = 0;
						}
						else
						{
							Cumulus.LogMessage("Ignoring rain counter reading " + ignoreraincount);
						}
					}
					else
					{
						ignoreraincount = 0;
					}

					if (ignoreraincount == 0)
					{
						DoRain(ConvertRainMMToUser(raintot*0.3), -1, now);
						prevraintotal = raintot;
					}

					// Solar/UV
					if (hasSolar)
					{
						LightValue = (data[16] + (data[17]*256) + (data[18]*65536))/10.0;

						if (LightValue < 300000)
						{
							DoSolarRad((int) (LightValue * cumulus.SolarOptions.LuxToWM2), now);
						}

						int UVreading = data[19];

						if (UVreading < 0 || UVreading > 16)
						{
							Cumulus.LogMessage("Ignoring UV-I reading " + UVreading);
							DoUV(null, now);
						}
						else
						{
							DoUV(UVreading, now);
						}
					}

					UpdateStatusPanel(now);
					UpdateMQTT();
					DoForecast(string.Empty, false);
				}

				if (cumulus.SensorAlarm.Enabled)
				{
					cumulus.SensorAlarm.Triggered = SensorContactLost;
				}
			}
		}

		private void StartSynchronising()
		{
			synchronising = true;
			synchroniseAttempts++;
			hadfirstsyncdata = false;
			readCounter = 0;
			syncStart = DateTime.Now;
			Cumulus.LogMessage("Start Synchronising with console");
			Cumulus.LogConsoleMessage(DateTime.Now.ToString("yy-MM-dd HH:mm:ss ") + "Start Synchronising with console, run #" + synchroniseAttempts, ConsoleColor.Gray, true);

			tmrDataRead.Interval = 500; // half a second
		}

		private void StopSynchronising()
		{
			int secsdiff;

			synchronising = false;
			tmrDataRead.Interval = 16000; // 16 seconds

			var foundSensor = true;
			var foundStation = true;
			var foundSolar = true;

			if (sensorSyncDone && previousSensorClock != DateTime.MinValue)
			{
				secsdiff = (int) Math.Floor((FOSensorClockTime - previousSensorClock).TotalSeconds) % 48;
				if (secsdiff > 24)
				{
					secsdiff = 48 - secsdiff;
				}
				Cumulus.LogMessage("Sensor clock  " + FOSensorClockTime.ToLongTimeString() + " drift = " + secsdiff + " seconds");
			}
			else if (sensorSyncDone)
			{
				Cumulus.LogMessage("Station clock " + FOStationClockTime.ToLongTimeString());
			}
			else
			{
				foundSensor = false;
				Cumulus.LogMessage("Synchronisation: No sensor change time found");
			}

			if (stationSyncDone && previousStationClock == DateTime.MinValue)
			{
				secsdiff = (int)Math.Floor((FOStationClockTime - previousStationClock).TotalSeconds) % 60;
				if (secsdiff > 30)
				{
					secsdiff = 60 - secsdiff;
				}
				Cumulus.LogMessage("Station clock  " + FOStationClockTime.ToLongTimeString() + " drift = " + secsdiff + " seconds");
			}
			else if (stationSyncDone)
			{
				Cumulus.LogMessage("Station clock " + FOStationClockTime.ToLongTimeString());
			}
			else
			{
				foundStation = false;
				Cumulus.LogMessage("Synchronisation: No station clock change time found");
			}


			if (hasSolar)
			{
				if (solarSyncDone && previousSolarClock != DateTime.MinValue)
				{
					secsdiff = (int)Math.Floor((FOSolarClockTime - previousSolarClock).TotalSeconds) % 60;
					if (secsdiff > 30)
					{
						secsdiff = 60 - secsdiff;
					}
					Cumulus.LogMessage("Solar clock  " + FOSolarClockTime.ToLongTimeString() + " drift = " + secsdiff + " seconds");
				}
				else if (solarSyncDone)
				{
					Cumulus.LogMessage("Solar clock " + FOSolarClockTime.ToLongTimeString());
				}
				else
				{
					foundSolar = false;
					Cumulus.LogMessage("Synchronisation: No solar change time found");
				}
			}

			Cumulus.LogConsoleMessage($" - Found times for:- sensor: {foundSensor}, station: {foundStation} {(hasSolar ? (", solar:" + (doSolarSync ? foundSolar.ToString() : "supressed")) : "")}", ConsoleColor.Gray);


			Cumulus.LogMessage("Stop Synchronising");
			Cumulus.LogConsoleMessage(DateTime.Now.ToString("yy-MM-dd HH:mm:ss ") + "Stop Synchronising", ConsoleColor.Gray, true);
		}

		private void InitialiseSync(bool solarSync)
		{
			synchroniseAttempts = 0;
			sensorSyncDone = false;
			stationSyncDone = false;
			solarSyncDone = !(hasSolar && solarSync);

			previousSensorClock = FOSensorClockTime;
			previousStationClock = FOStationClockTime;
			previousSolarClock = FOSolarClockTime;
		}

		private void FinaliseSync()
		{
			Cumulus.LogMessage("Finalise Synchronisation");
			// the best we can do is assume the station and CMX clocks are in sync - possibly true if the station has an RCC, otherwise...!
			if (!stationSyncDone)
			{
				var oneMin = new TimeSpan(0, 1, 0);
				var now = DateTime.Now;
				FOStationClockTime = now.AddTicks(-(now.Ticks % oneMin.Ticks));
				Cumulus.LogMessage("Finalise Synchronisation - set station clock change to match CMX clock - best we can do");
			}
		}


		private class HistoryData
		{
			public int inHum;

			public double inTemp;
			public int interval;
			public int outHum;

			public double outTemp;

			public double pressure;

			public int rainCounter;
			public DateTime timestamp;
			public int windBearing;
			public double windGust;

			public double windSpeed;
			public int uvVal;
			public double solarVal;
			public bool SensorContactLost;
			public int followinginterval;
		}
	}
}
