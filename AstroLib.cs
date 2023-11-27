using System;

namespace CumulusMX
{
	internal static class AstroLib
	{
		public static int BrasSolar(double el, double erv, double nfac)
		{
			// el      solar elevation deg from horizon
			// erv     distance from earth to sun in AU
			// nfac    atmospheric turbidity parameter (2=clear, 4-5=smoggy)

			double sinal = Math.Sin(Trig.DegToRad(el)); // Sine of the solar elevation angle

			if (sinal < 0)
				return 0;

			// solar radiation on horizontal surface at top of atmosphere
			double i0 = (1367 / (erv * erv)) * sinal;

			// optical air mass
			double m = 1 / (sinal + (0.15 * Math.Pow(el + 3.885, -1.253)));

			// molecular scattering coefficient
			double al = 0.128 - (0.054 * Math.Log(m) / Math.Log(10));

			// clear-sky solar radiation at earth surface on horizontal surface (W/m^2)
			return (int) Math.Round(i0 * Math.Exp(-nfac * al * m));
		}

		public static int RyanStolzSolar(double el, double erv, double atc, double z)
		{
			// el      solar elevation deg from horizon
			// erv     distance from earth to sun in AU
			// atc     atmospheric transmission coefficient
			// z       elevation, metres

			double sinal = Math.Sin(Trig.DegToRad(el)); // Sine of the solar elevation angle

			if (sinal < 0)
				return 0;

			double al = Math.Asin(sinal);
			double a0 = Trig.RadToDeg(al); // convert the radians to degree

			double rm = Math.Pow(((288.0 - 0.0065 * z) / 288.0), 5.256)/(sinal + 0.15 * Math.Pow((a0 + 3.885), (-1.253)));

			double rsToa = 1360 * sinal / (erv * erv); // RS on the top of atmosphere

			return (int) Math.Round(rsToa * Math.Pow(atc, rm)); //RS on the ground
		}



		public static int SolarMax(DateTime timestamp, decimal longitude, decimal latitude, double altitude,
									  out double solarelevation, SolarOptions options)
		{
			double factor = 0;
			if (options.SolarCalc == 0)
			{
				factor = GetFactor(timestamp, options.RStransfactorJun, options.RStransfactorDec);
			}
			if (options.SolarCalc == 1)
			{
				factor = GetFactor(timestamp, options.BrasTurbidityJun, options.BrasTurbidityDec);
			}
			return SolarMax(timestamp, longitude, latitude, altitude, out solarelevation, factor, options.SolarCalc);
		}


		private static int SolarMax(DateTime timestamp, decimal longitude, decimal latitude, double altitude,
									  out double solarelevation, double factor, int method)
		{
			DateTime utctime = timestamp.ToUniversalTime();

			CalculateSunPosition(utctime, latitude, longitude, out solarelevation, out _);
			var dEpoch = new DateTime(1990, 1, 1, 0, 0, 0);
			double erv = CalcSunDistance(utctime, dEpoch);

			//cumulus.LogMessage(utctime+" lat="+latitude+" lon="+longitude+" sun elev="+solarelevation);
			if (method == 0)
			{
				return RyanStolzSolar(solarelevation, erv, factor, altitude);
			}
			if (method == 1)
			{
				return BrasSolar(solarelevation, erv, factor);
			}

			return 0;
		}

		// Calculate the interpolated factor using a cosine function
		private static double GetFactor(DateTime timestamp, double jun, double dec)
		{
			var range = jun - dec;
			var doy = timestamp.DayOfYear;
			return dec + Math.Cos((doy - 172) / 183.0 * Math.PI / 2) * range;
		}


		// From the NOAA_SolarCalculations_day spreadsheet
		// https://gml.noaa.gov/grad/solcalc/calcdetails.html
		#region NOAA_Solar

		private static void CalculateSunPosition(DateTime dateTime, decimal latitude, decimal longitude, out double altitude, out double azimuth)
		{
			// We will use DateTime.ToOADate() which automatically includes the TZ
			var zone = 0.0;

			var julianDate = dateTime.ToOADate() + 2415018.5;
			var julianCentry = (julianDate - 2451545) / 36525.0;
			var geoMeanLongSun = Trig.PutIn360Deg(280.46646 + julianCentry * (36000.76983 + julianCentry * 0.0003032));
			var geoMeanAnomSun = 357.52911 + julianCentry * (35999.05029 - 0.0001537 * julianCentry);
			var eccEarthOrbit = 0.016708634 - julianCentry * (0.000042037 + 0.0000001267 * julianCentry);
			var sunEqCentre = Math.Sin(Trig.DegToRad(geoMeanAnomSun)) * (1.914602 - julianCentry * (0.004817 + 0.000014 * julianCentry)) + Math.Sin(Trig.DegToRad(2 * geoMeanAnomSun)) * (0.019993 - 0.000101 * julianCentry) + Math.Sin(Trig.DegToRad(3 * geoMeanAnomSun)) * 0.000289;
			var sunTrueLong = geoMeanLongSun + sunEqCentre;
			//var sunTrueAnom = geoMeanAnomSun + sunEqCentre;
			//var sunRadVector = (1.000001018 * (1 - eccEarthOrbit * eccEarthOrbit)) / (1 + eccEarthOrbit * Math.Cos(Trig.DegToRad(sunTrueAnom)));
			var sunAppLong = sunTrueLong - 0.00569 - 0.00478 * Math.Sin(Trig.DegToRad(125.04 - 1934.136 * julianCentry));
			var meanObliqEcplitic = 23 + (26 + ((21.448 - julianCentry * (46.815 + julianCentry * (0.00059 - julianCentry * 0.001813)))) / 60.0) / 60.0;
			var obliqCorr = meanObliqEcplitic + 0.00256 * Math.Cos(Trig.DegToRad(125.04 - 1934.136 * julianCentry));
			//var sunRA = Trig.RadToDeg(Math.Atan2(Math.Cos(Trig.DegToRad(sunAppLong)), Math.Cos(Trig.DegToRad(obliqCorr)) * Math.Sin(Trig.DegToRad(obliqCorr))));
			var sunDec = Trig.RadToDeg(Math.Asin(Math.Sin(Trig.DegToRad(obliqCorr)) * Math.Sin(Trig.DegToRad(sunAppLong))));
			var varY = Math.Tan(Trig.DegToRad(obliqCorr / 2.0)) * Math.Tan(Trig.DegToRad(obliqCorr / 2.0));
			var eqOfTime = 4 * Trig.RadToDeg(varY * Math.Sin(2 * Trig.DegToRad(geoMeanLongSun)) - 2 * eccEarthOrbit * Math.Sin(Trig.DegToRad(geoMeanAnomSun)) + 4 * eccEarthOrbit * varY * Math.Sin(Trig.DegToRad(geoMeanAnomSun)) * Math.Cos(2 * Trig.DegToRad(geoMeanLongSun)) - 0.5 * varY * varY * Math.Sin(4 * Trig.DegToRad(geoMeanLongSun)) - 1.25 * eccEarthOrbit * eccEarthOrbit * Math.Sin(2 * Trig.DegToRad(geoMeanAnomSun)));
			//var haSunRise = Trig.RadToDeg(Math.Acos(Math.Cos(Trig.DegToRad(90.833)) / (Math.Cos(Trig.DegToRad(latitude)) * Math.Cos(Trig.DegToRad(sunDec))) - Math.Tan(Trig.DegToRad(latitude)) * Math.Tan(Trig.DegToRad(sunDec))));
			//var solarNoonLst = (720.0 - 4 * longitude - eqOfTime + zone * 60.0) / 1440.0;
			//var sunriseTimeLst = solarNoonLst - haSunRise * 4 / 1440.0;
			//var sunsetTimeLst = solarNoonLst + haSunRise * 4 / 1440.0;
			//var sunlightDurationMins = 8 * haSunRise;
			var trueSolarTime = Trig.PutInRange(dateTime.TimeOfDay.TotalMinutes + eqOfTime + 4 * (double)longitude - 60 * zone, 1440);
			var hourAngle = trueSolarTime / 4.0 < 0 ? trueSolarTime / 4.0 + 180 : trueSolarTime / 4.0 - 180;
			var solarZenithAngle = Trig.RadToDeg(Math.Acos(Math.Sin(Trig.DegToRad((double)latitude)) * Math.Sin(Trig.DegToRad(sunDec)) + Math.Cos(Trig.DegToRad((double)latitude)) * Math.Cos(Trig.DegToRad(sunDec)) * Math.Cos(Trig.DegToRad(hourAngle))));
			var solarElevation = 90 - solarZenithAngle;

			double refraction;

			if (solarElevation > 85)
				refraction = 0;
			else if (solarElevation > 5)
				refraction = 58.1 / Math.Tan(Trig.DegToRad(solarElevation)) - 0.07 / Math.Pow(Math.Tan(Trig.DegToRad(solarElevation)), 3) + 0.000086 / Math.Pow(Math.Tan(Trig.DegToRad(solarElevation)), 5);
			else if (solarElevation > -0.575)
				refraction = 1735.0 + solarElevation * (-518.2 + solarElevation * (103.4 + solarElevation * (-12.79 + solarElevation * 0.711)));
			else
				refraction = -20.772 / Math.Tan(Trig.DegToRad(solarElevation));

			altitude = solarElevation + refraction / 3600.0;

			if (hourAngle > 0)
				azimuth = Trig.PutIn360Deg(Trig.DegToRad(Math.Acos(((Math.Sin(Trig.DegToRad((double)latitude)) * Math.Cos(Trig.DegToRad(solarZenithAngle))) - Math.Sin(Trig.DegToRad(sunDec))) / (Math.Sin(Trig.DegToRad((double)latitude)) * Math.Sin(Trig.DegToRad(solarZenithAngle))))) + 180);
			else
				azimuth = Trig.PutIn360Deg(540 - Trig.DegToRad(Math.Acos(((Math.Sin(Trig.DegToRad((double)latitude)) * Math.Cos(Trig.DegToRad(solarZenithAngle))) - Math.Sin(Trig.DegToRad(sunDec))) / (Math.Cos(Trig.DegToRad((double)latitude)) * Math.Sin(Trig.DegToRad(solarZenithAngle))))));

		}


		#endregion


		// http://guideving.blogspot.co.uk/2010/08/sun-position-in-c.html
		#region sun-position-in-c
		/*
		private static void CalculateSunPosition(
			DateTime dateTime, double latitude, double longitude, out double altitude, out double azimuth)
		{
			const double Deg2Rad = Math.PI / 180.0;
			const double Rad2Deg = 180.0 / Math.PI;
			// Number of days from J2000.0.
			double julianDate = 367 * dateTime.Year -
				(int)((7.0 / 4.0) * (dateTime.Year + (int)((dateTime.Month + 9.0) / 12.0))) +
				(int)((275.0 * dateTime.Month) / 9.0) +
				dateTime.Day - 730531.5;

			double julianCenturies = julianDate / 36525.0;

			// Sidereal Time
			double siderealTimeHours = 6.6974 + 2400.0513 * julianCenturies;

			double siderealTimeUT = siderealTimeHours +
				(366.2422 / 365.2422) * dateTime.TimeOfDay.TotalHours;

			double siderealTime = siderealTimeUT * 15 + longitude;

			// Refine to number of days (fractional) to specific time.
			julianDate += dateTime.TimeOfDay.TotalDays;
			julianCenturies = julianDate / 36525.0;

			// Solar Coordinates
			double meanLongitude = Trig.CorrectAngleTo2Pi(Deg2Rad *
				(280.466 + 36000.77 * julianCenturies));

			double meanAnomaly = Trig.CorrectAngleTo2Pi(Deg2Rad *
				(357.52911 + 35999.05029 * julianCenturies));

			double equationOfCenter = Deg2Rad * ((1.914602 - 0.004817 * julianCenturies) *
				Math.Sin(meanAnomaly) + 0.019993 * Math.Sin(2 * meanAnomaly));

			double elipticalLongitude =
				Trig.CorrectAngleTo2Pi(meanLongitude + equationOfCenter);

			double obliquity = (23.439 - 0.013 * julianCenturies) * Deg2Rad;

			// Right Ascension
			double rightAscension = Math.Atan2(
				Math.Cos(obliquity) * Math.Sin(elipticalLongitude),
				Math.Cos(elipticalLongitude));

			double declination = Math.Asin(
				Math.Sin(rightAscension) * Math.Sin(obliquity));

			// Horizontal Coordinates
			double hourAngle = Trig.CorrectAngleTo2Pi(siderealTime * Deg2Rad) - rightAscension;

			if (hourAngle > Math.PI)
			{
				hourAngle -= 2 * Math.PI;
			}

			altitude = Math.Asin(Math.Sin(latitude * Deg2Rad) *
				Math.Sin(declination) + Math.Cos(latitude * Deg2Rad) *
				Math.Cos(declination) * Math.Cos(hourAngle));

			// refraction correction
			// work in degrees
			altitude *= Rad2Deg;
			double refractionCorrection;
			if (altitude > 85.0 || altitude <= -0.575)
			{
				refractionCorrection = 0.0;
			}
			else
			{
				double te = Math.Tan(Trig.DegToRad(altitude));
				if (altitude > 5.0)
				{
					refractionCorrection = 58.1 / te - 0.07 / (te * te * te) + 0.000086 / (te * te * te * te * te);
				}
				else if (altitude > -0.575)
				{
					double step1 = (-12.79 + altitude * 0.711);
					double step2 = (103.4 + altitude * (step1));
					double step3 = (-518.2 + altitude * (step2));
					refractionCorrection = 1735.0 + altitude * (step3);
				}
				else
				{
					refractionCorrection = -20.774 / te;
				}
				refractionCorrection /= 3600.0;
			}
			altitude += refractionCorrection;

			// Nominator and denominator for calculating Azimuth
			// angle. Needed to test which quadrant the angle is in.
			double aziNom = -Math.Sin(hourAngle);
			double aziDenom =
				Math.Tan(declination) * Math.Cos(latitude * Deg2Rad) -
				Math.Sin(latitude * Deg2Rad) * Math.Cos(hourAngle);

			azimuth = Math.Atan(aziNom / aziDenom);

			if (aziDenom < 0) // In 2nd or 3rd quadrant
			{
				azimuth += Math.PI;
			}
			else if (aziNom < 0) // In 4th quadrant
			{
				azimuth += 2 * Math.PI;
			}

			azimuth *= Rad2Deg;
			//altitude = altitude * Rad2Deg;
		}
		*/

		#endregion

		private static double CalcSunDistance(DateTime dDate, DateTime dEpoch)
		{
			const double fAcc = 0.0000001;
			double fD = GetDaysBetween(dDate, dEpoch);
			double fSolarMEL = GetSolarMEL(dEpoch, true);
			double fSolarPL = GetSolarPerigeeLong(dEpoch, true);
			double fSunEarthEcc = GetSunEarthEcc(dEpoch, true);
			//double fOblique = GetEarthObliquity(dEpoch, true);
			const double fSMA = 149598500.0;

			double fN = (360.0 / 365.242191)*fD;
			fN = Trig.PutIn360Deg(fN);
			double fM = fN + fSolarMEL - fSolarPL;
			fM = Trig.PutIn360Deg(fM);
			fM = Trig.DegToRad(fM);
			double fE = CalcEccentricAnomaly(fM, fM, fSunEarthEcc, fAcc);
			double fTanV2 = Math.Sqrt((1.0 + fSunEarthEcc) / (1.0 - fSunEarthEcc)) * Math.Tan(fE / 2.0);
			double fV = Math.Atan(fTanV2)*2.0;
			double fDistance = (fSMA * (1.0 - (fSunEarthEcc*fSunEarthEcc))) / (1.0 + (fSunEarthEcc*Math.Cos(fV)));

			// Convert from km to AU
			return fDistance / 149597871.0;
		}

		private static double GetSunEarthEcc(DateTime dDate, bool b0Epoch)
		{
			//Returns the eccentricity of Earth's orbit around the sun for the specified date

			double fJD = GetJulianDay(dDate, 0);
			if (b0Epoch)
			{
				fJD -= 1.0;
			}
			double fT = (fJD - 2415020.0) / 36525.0;
			double fEcc = 0.01675104 - (0.0000418 * fT) - (0.000000126 * fT * fT);
			return fEcc;
		}

		/*
		private static double GetEarthObliquity(DateTime dDate, bool b0Epoch)
		{
			//Returns the obliquity of Earth's orbit around the sun for the specified date

			double fJD = GetJulianDay(dDate, 0);
			if (b0Epoch)
			{
				fJD -= 1.0;
			}

			double fT = (fJD - 2415020.0)/36525.0;
			double fDeltaObl = (46.815*fT) + (0.0006*fT*fT) + (0.00181*fT*fT*fT);
			double fObl = 23.43929167 - (fDeltaObl/3600.0);
			return fObl;
		}
		*/

		private static double CalcEccentricAnomaly(double fEGuess, double fMA, double fEcc, double fAcc)
		{
			//Calc Eccentric Anomaly to specified accuracy
			double fE;

			double fEG = fEGuess;

			double fDelta = fEG - (fEcc * Math.Sin(fEG)) - fMA;
			if (Math.Abs(fDelta) > fAcc)
			{
				double fDeltaE = (fDelta / (1.0 - (fEcc * Math.Cos(fEG))));
				double fETmp = fEG - fDeltaE;
				fE = CalcEccentricAnomaly(fETmp, fMA, fEcc, fAcc);
			}
			else
			{
				fE = fEGuess;
			}
			return fE;
		}

		private static long GetDaysBetween(DateTime dDate, DateTime dSecDate)
		{
			double fJD1 = GetJulianDay(dDate, 0);
			double fJD2 = GetJulianDay(dSecDate, 0);
			double fDays = (fJD1 - fJD2);

			if (fDays >= 0)
			{
				fDays = (long) Math.Floor(fDays);
			}
			else
			{
				fDays = (long) Math.Floor(Math.Abs(fDays)) * -1;
			}
			return (long) Math.Floor(fDays);
		}

		private static double GetJulianDay(DateTime dDate, int iZone)
		{
			double iGreg;
			double fC;

			dDate = CalcUTFromZT(dDate, iZone);

			int iYear = dDate.Year;
			int iMonth = dDate.Month;
			int iDay = dDate.Day;
			int iHour = dDate.Hour;
			int iMinute = dDate.Minute;
			int iSecond = dDate.Second;
			double fFrac = iDay + ((iHour + (iMinute / 60) + (iSecond / 60 / 60)) / 24);
			iGreg = iYear < 1582 ? 0 : 1;
			if ((iMonth == 1) || (iMonth == 2))
			{
				iYear -= 1;
				iMonth += 12;
			}

			double fA = (long) Math.Floor(iYear / 100.0);
			double fB = (2 - fA + (long) Math.Floor(fA/4)) * iGreg;
			if (iYear < 0)
			{
				fC = (int) Math.Floor((365.25 * iYear) - 0.75);
			}
			else
			{
				fC = (int) Math.Floor(365.25 * iYear);
			}
			double fD = (int) Math.Floor(30.6001 * (iMonth + 1));
			double fJD = fB + fC + fD + 1720994.5;
			fJD += fFrac;
			return fJD;
		}

		private static DateTime CalcUTFromZT(DateTime dDate, int iZone)
		{
			if (iZone >= 0)
			{
				return dDate.Subtract(new TimeSpan(iZone, 0, 0));
			}

			return dDate.AddHours(Math.Abs(iZone));
		}

		private static double GetSolarMEL(DateTime dDate, bool b0Epoch)
		{
			//Returns the Sun's Mean Ecliptic Longitude for the specified date

			double fJD = GetJulianDay(dDate, 0);
			if (b0Epoch)
			{
				fJD -= 1.0;
			}

			double fT = (fJD - 2415020.0) / 36525.0;
			double fLong = 279.6966778 + (36000.76892 * fT) + (0.0003025 * fT * fT);
			fLong = Trig.PutIn360Deg(fLong);
			return fLong;
		}

		private static double GetSolarPerigeeLong(DateTime dDate, bool b0Epoch)
		{
			//Returns the Sun's Perigee Longitude for the specified date

			double fJD = GetJulianDay(dDate, 0);

			if (b0Epoch)
			{
				fJD -= 1.0;
			}

			double fT = (fJD - 2415020.0) / 36525.0;
			double fLong = 281.2208444 + (1.719175 * fT) + (0.000452778 * fT * fT);
			fLong = Trig.PutIn360Deg(fLong);
			return fLong;
		}
	}
}
