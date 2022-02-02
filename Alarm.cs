using System;

namespace CumulusMX
{
	public class Alarm
	{
		public Cumulus cumulus { get; set; }

		public bool Enabled { get; set; }
		public double Value { get; set; }
		public bool Sound { get; set; }
		public string SoundFile { get; set; }
		public string name;

		bool triggered;

		public Alarm(Cumulus Cuml, string Name)
		{
			cumulus = Cuml;
			name = Name;
		}


		public bool Triggered
		{
			get => triggered;
			set
			{
				if (value)
				{
					triggerCount++;
					// If we get a new trigger, record the time
					TriggeredTime = DateTime.Now;

					// do we have a threshold value
					if (Enabled && !triggered && triggerCount >= TriggerThreshold)
					{
						triggered = true;
						Cumulus.LogMessage($"Alarm '{name}' triggered");

						// If we were not set before, so we need to send an email?
						if (Email && cumulus.SmtpOptions.Enabled)
						{
							// Construct the message - preamble, plus values
							var msg = cumulus.AlarmEmailPreamble + "\r\n" + string.Format(EmailMsg, Value, Units);
							if (!string.IsNullOrEmpty(LastError))
							{
								msg += "\r\nLast error: " + LastError;
							}
							_ = cumulus.emailer.SendEmail(cumulus.AlarmDestEmail, cumulus.AlarmFromEmail, cumulus.AlarmEmailSubject, msg, cumulus.AlarmEmailHtml);
						}
					}
				}
				else if (triggered)
				{
					// If the trigger is cleared, check if we should be latching the value
					if (Latch)
					{
						if (DateTime.Now > TriggeredTime.AddHours(LatchHours))
						{
							// We are latching, but the latch period has expired, clear the trigger
							triggered = false;
							triggerCount = 0;

							if (Enabled)
							{
								Cumulus.LogMessage($"Alarm '{name}' cleared");
							}
						}
					}
					else
					{
						// No latch, just clear the trigger
						triggered = false;
						triggerCount = 0;

						if (Enabled)
						{
							Cumulus.LogMessage($"Alarm '{name}' cleared");
						}
					}
				}
			}
		}
		public DateTime TriggeredTime { get; set; }
		public bool Notify { get; set; }
		public bool Email { get; set; }
		public bool Latch { get; set; }
		public int LatchHours { get; set; }
		public string EmailMsg { get; set; }
		public string Units { get; set; }
		public string LastError { get; set; }
		int triggerCount = 0;
		public int TriggerThreshold { get; set; }

		public void Clear()
		{
			if (Latch && triggered && DateTime.Now > TriggeredTime.AddHours(LatchHours))
			{
				triggered = false;
				triggerCount = 0;

				if (Enabled)
				{
					Cumulus.LogMessage($"Alarm '{name}' cleared");
				}
			}
		}
	}

	public class AlarmChange : Alarm
	{
		bool upTriggered;

		public AlarmChange(Cumulus Cuml, string Name) : base(Cuml, Name)
		{
		}


		public bool UpTriggered
		{
			get => upTriggered;
			set
			{
				if (value)
				{
					// If we get a new trigger, record the time
					UpTriggeredTime = DateTime.Now;

					if (Enabled && !upTriggered)
					{
						upTriggered = true;
						Cumulus.LogMessage($"Alarm '{name}' up triggered");

						// If we were not set before, so we need to send an email?
						if (Email && cumulus.SmtpOptions.Enabled)
						{
							// Construct the message - preamble, plus values
							var msg = Program.cumulus.AlarmEmailPreamble + "\r\n" + string.Format(EmailMsgUp, Value, Units);
							_ = cumulus.emailer.SendEmail(cumulus.AlarmDestEmail, cumulus.AlarmFromEmail, cumulus.AlarmEmailSubject, msg, cumulus.AlarmEmailHtml);
						}
					}
				}
				else if (upTriggered)
				{
					// If the trigger is cleared, check if we should be latching the value
					if (Latch)
					{
						if (DateTime.Now > UpTriggeredTime.AddHours(LatchHours))
						{
							// We are latching, but the latch period has expired, clear the trigger
							upTriggered = false;
							Cumulus.LogMessage($"Alarm '{name}' up cleared");
						}
					}
					else
					{
						// No latch, just clear the trigger
						upTriggered = false;
						Cumulus.LogMessage($"Alarm '{name}' up cleared");
					}
				}
			}
		}
		public DateTime UpTriggeredTime { get; set; }


		bool downTriggered;
		public bool DownTriggered
		{
			get => downTriggered;
			set
			{
				if (value)
				{
					// If we get a new trigger, record the time
					DownTriggeredTime = DateTime.Now;

					if (Enabled && !downTriggered)
					{
						downTriggered = true;
						Cumulus.LogMessage($"Alarm '{name}' down triggered");

						// If we were not set before, so we need to send an email?
						if (Email && cumulus.SmtpOptions.Enabled)
						{
							// Construct the message - preamble, plus values
							var msg = Program.cumulus.AlarmEmailPreamble + "\n" + string.Format(EmailMsgDn, Value, Units);
							_ = cumulus.emailer.SendEmail(cumulus.AlarmDestEmail, cumulus.AlarmFromEmail, cumulus.AlarmEmailSubject, msg, cumulus.AlarmEmailHtml);
						}
					}
				}
				else if (downTriggered)
				{
					// If the trigger is cleared, check if we should be latching the value
					if (Latch)
					{
						if (DateTime.Now > DownTriggeredTime.AddHours(LatchHours))
						{
							// We are latching, but the latch period has expired, clear the trigger
							downTriggered = false;
							Cumulus.LogMessage($"Alarm '{name}' down cleared");
						}
					}
					else
					{
						// No latch, just clear the trigger
						downTriggered = false;
						Cumulus.LogMessage($"Alarm '{name}' down cleared");
					}
				}
			}
		}

		public DateTime DownTriggeredTime { get; set; }

		public string EmailMsgUp { get; set; }
		public string EmailMsgDn { get; set; }

		public new void Clear()
		{
			if (Latch && upTriggered && DateTime.Now > UpTriggeredTime.AddHours(LatchHours))
			{
				upTriggered = false;

				if (Enabled)
				{
					Cumulus.LogMessage($"Alarm '{name}' up cleared");
				}
			}
			if (Latch && downTriggered && DateTime.Now > DownTriggeredTime.AddHours(LatchHours))
			{
				downTriggered = false;

				if (Enabled)
				{
					Cumulus.LogMessage($"Alarm '{name}' down cleared");
				}
			}
		}
	}
}
