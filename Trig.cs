﻿using System;

namespace CumulusMX
{
	public static class Trig
	{

		public static double DegToRad(double pfDeg)
		{
			return pfDeg / 180 * Math.PI;
		}

		public static double RadToDeg(double pfRad)
		{
			return pfRad * 180 / Math.PI;
		}

		public static double Cos(double pfDeg)
		{
			return Math.Cos(DegToRad(pfDeg));
		}

		public static double Sin(double pfDeg)
		{
			return Math.Sin(DegToRad(pfDeg));
		}

		public static double Tan(double pfDeg)
		{
			return Math.Tan(DegToRad(pfDeg));
		}

		public static double Cosec(double pfDeg)
		{
			return (1.0 / Math.Sin(DegToRad(pfDeg)));
		}

		public static double Sec(double pfDeg)
		{
			return (1.0 / Math.Cos(DegToRad(pfDeg)));
		}

		public static double Cot(double pfDeg)
		{
			return (1.0 / Math.Tan(DegToRad(pfDeg)));
		}

		public static double Acos(double pfNum)
		{
			return RadToDeg(Math.Acos(pfNum));
		}

		public static double Asin(double pfNum)
		{
			return RadToDeg(Math.Asin(pfNum));
		}

		public static double Atan(double pfNum)
		{
			return RadToDeg(Math.Atan(pfNum));
		}

		public static double Cosh(double pfDeg)
		{
			return Math.Cosh(DegToRad(pfDeg));
		}

		public static double Sinh(double pfDeg)
		{
			return Math.Sinh(DegToRad(pfDeg));
		}

		public static double Tanh(double pfDeg)
		{
			return Math.Tanh(DegToRad(pfDeg));
		}

		public static double Cosech(double pfDeg)
		{
			return (1.0 / Math.Sinh(DegToRad(pfDeg)));
		}

		public static double Sech(double pfDeg)
		{
			return (1.0 / Math.Cosh(DegToRad(pfDeg)));
		}

		public static double Coth(double pfDeg)
		{
			return (1.0 / Math.Tanh(DegToRad(pfDeg)));
		}

		public static double PutIn360Deg(double pfDeg)
		{
			while (pfDeg >= 360)
			{
				pfDeg -= 360;
			}
			while (pfDeg < 0)
			{
				pfDeg += 360;
			}
			return pfDeg;
		}

		/*!
		* \brief Corrects an angle.
		*
		* \param angleInRadians An angle expressed in radians.
		* \return An angle in the range 0 to 2*PI.
		 *
		 * http://guideving.blogspot.co.uk/2010/08/sun-position-in-c.html
		*/
		public static double CorrectAngleTo2Pi(double angleInRadians)
		{
			if (angleInRadians < 0)
			{
				return 2 * Math.PI - (Math.Abs(angleInRadians) % (2 * Math.PI));
			}

			if (angleInRadians > 2 * Math.PI)
			{
				return angleInRadians % (2 * Math.PI);
			}

			return angleInRadians;
		}


		public static double PutIn24Hour(double pfHour)
		{
			while (pfHour >= 24)
			{
				pfHour -= 24;
			}
			while (pfHour < 0)
			{
				pfHour += 24;
			}
			return pfHour;
		}

		public static double TanQuadrant(double pfX, double pfY, double pfTanVal)
		{
			if ((pfY >= 0) && (pfX >= 0))
			{
				while (pfTanVal >= 90)
				{
					pfTanVal -= 90;
				}
				while (pfTanVal < 0)
				{
					pfTanVal += 90;
				}
			}
			else if ((pfY < 0) && (pfX >= 0))
			{
				while (pfTanVal >= 360)
				{
					pfTanVal -= 90;
				}
				while (pfTanVal < 270)
				{
					pfTanVal += 90;
				}
			}
			else if ((pfY >= 0) && (pfX < 0))
			{
				while (pfTanVal >= 180)
				{
					pfTanVal -= 90;
				}
				while (pfTanVal < 90)
				{
					pfTanVal += 90;
				}
			}
			else if ((pfY < 0) && (pfX < 0))
			{
				while (pfTanVal >= 270)
				{
					pfTanVal -= 90;
				}
				while (pfTanVal < 180)
				{
					pfTanVal += 90;
				}
			}
			return pfTanVal;
		}

		public static void DegToDMS(double degrees, out int d, out int m, out int s)
		{
			int secs = (int)(degrees * 60 * 60);

			s = secs % 60;

			secs = (secs - s) / 60;

			m = secs % 60;
			d = secs / 60;
		}

		/// <summary>
		/// Returns the angle from bearing2 to bearing1, in the range -180 to +180 degrees
		/// </summary>
		/// <param name="bearing1"></param>
		/// <param name="bearing2"></param>
		/// <returns>the required angle</returns>
		public static int getShortestAngle(int bearing1, int bearing2)
		{
			int diff = bearing2 - bearing1;

			if (diff >= 180)
			{
				// result is obtuse and positive, subtract 360 to go the other way
				diff -= 360;
			}
			else
			{
				if (diff <= -180)
				{
					// result is obtuse and negative, add 360 to go the other way
					diff += 360;
				}
			}
			return diff;
		}


	}
}
