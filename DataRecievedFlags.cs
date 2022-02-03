using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CumulusMX
{
	class DataReceivedFlags
	{
		private readonly WeatherStation station;
		private readonly Cumulus cumulus;

		// primary data
		internal bool Temperature;
		internal bool Humidity;
		internal bool Wind { get; set; }
		internal bool Pressure;
		internal bool DewPoint;
		// extra data
		internal bool Solar;
		internal bool UV;
		internal bool[] ExtraTemp;
		internal bool[] ExtraHum;
		internal bool[] ExtraDewPoint;
		internal bool[] ExtraUserTemp;

		internal bool[] SoilMoisture;
		internal bool[] SoilTemp;
		internal bool[] LeafTemp;
		internal bool[] LeafWetness;
		internal bool[] AirQuality;
		internal bool[] AirQualityAvg;
		internal bool WindChill;
		internal bool IndoorHum;
		internal bool IndoorTemp;

		internal DataReceivedFlags(WeatherStation stn, Cumulus cuml)
		{
			station = stn;
			cumulus = cuml;

			ExtraTemp = new bool[11];
			ExtraHum = new bool[11];
			ExtraDewPoint = new bool[11];
			ExtraUserTemp = new bool[11];
			SoilMoisture = new bool[17];
			SoilTemp = new bool[17];
			LeafTemp = new bool[5];
			LeafWetness = new bool[9];
			AirQuality = new bool[5];
			AirQualityAvg = new bool[5];
		}

		internal void CheckDataValuesForUpdate()
		{
			// Check to see if we have had data for the values in the last minute.
			// If we have, clear the flag so it will be set again if the value is received
			// If not then set the station variable (and any derived variables to null
			if (Temperature)
			{
				Temperature = false;
			}
			else
			{
				station.Temperature = null;
				station.ApparentTemp = null;
				station.HeatIndex = null;
				station.Humidex = null;
				if (cumulus.StationOptions.CalculatedDP)
					station.Dewpoint = null;
				if (cumulus.StationOptions.CalculatedWC)
					station.WindChill = null;
			}

			if (Humidity)
			{
				Humidity = false;
			}
			else
			{
				station.Humidity = null;
				station.ApparentTemp = null;
				station.Humidex = null;
				if (cumulus.StationOptions.CalculatedDP)
					station.Dewpoint = null;
			}

			if (Wind)
			{
				Wind = false;
			}
			else
			{
				station.WindLatest = null;
				station.AvgBearing = null;
				station.Bearing = null;
				station.AvgBearing = null;
				if (cumulus.StationOptions.CalculatedWC)
					station.WindChill = null;
				station.ApparentTemp = null;
			}


			if (Pressure)
			{
				Pressure = false;
			}
			else
			{
				station.Pressure = null;
			}

			if (DewPoint)
			{
				DewPoint = false;
			}
			else
			{
				station.Dewpoint = null;
			}

			if (Solar)
			{
				Solar = false;
			}
			else
			{
				station.SolarRad = null;
			}

			if (UV)
			{
				UV = false;
			}
			else
			{
				station.UV = null;
			}

			for (var i = 1; i <ExtraTemp.Length; i++)
			{
				if (ExtraTemp[i])
				{
					ExtraTemp[i] = false;
				}
				else
				{
					station.ExtraTemp[i] = null;
					if (cumulus.StationOptions.CalculatedDP)
						station.ExtraDewPoint[i] = null;
				}

				if (ExtraHum[i])
				{
					ExtraHum[i] = false;
				}
				else
				{
					station.ExtraHum[i] = null;
					if (cumulus.StationOptions.CalculatedDP)
						station.ExtraDewPoint[i] = null;
				}

				if (ExtraDewPoint[i])
				{
					ExtraDewPoint[i] = false;
				}
				else
				{
					station.ExtraDewPoint[i] = null;
				}

				if (ExtraUserTemp[i])
				{
					ExtraUserTemp[i] = false;
				}
				else
				{
					station.UserTemp[i] = null;
				}
			}

			for (var i = 1; i < SoilMoisture.Length; i++)
			{
				if (SoilMoisture[i])
					SoilMoisture[i] = false;
				else
					station.SoilMoisture[i] = null;

				if (SoilTemp[i])
					SoilTemp[i] = false;
				else
					station.SoilTemp[i] = null;
			}

			for (var i = 1; i < 8; i++)
			{
				if (LeafWetness[i])
					LeafWetness[i] = false;
				else
					station.LeafWetness[i] = null;
			}

			for (var i = 1; i < 5; i++)
			{
				if (LeafTemp[i])
					LeafTemp[i] = false;
				else
					station.LeafTemp[i] = null;
				if (AirQuality[i])
					AirQuality[i] = false;
				else
					station.AirQuality[i] = null;

				if (AirQualityAvg[i])
					AirQualityAvg[i] = false;
				else
					station.AirQualityAvg[i] = null;
			}
		}

	}
}
