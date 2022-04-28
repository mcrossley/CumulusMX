namespace CumulusMX.Stations
{
	public class EcowittSettings
	{
		public bool SetCustomServer { get; set; }
		public string GatewayAddr { get; set; }
		public string LocalAddr { get; set; }
		public int CustomInterval { get; set; }
		public bool ExtraEnabled { get; set; }
		public bool ExtraUseSolar { get; set; }
		public bool ExtraUseUv { get; set; }
		public bool ExtraUseTempHum { get; set; }
		public bool ExtraUseSoilTemp { get; set; }
		public bool ExtraUseSoilMoist { get; set; }
		public bool ExtraUseLeafWet { get; set; }
		public bool ExtraUseUserTemp { get; set; }
		public bool ExtraUseAQI { get; set; }
		public bool ExtraUseCo2 { get; set; }
		public bool ExtraUseLightning { get; set; }
		public bool ExtraUseLeak { get; set; }
		public string AppKey { get; set; }
		public string UserApiKey { get; set; }
		public string MacAddress { get; set; }
		public bool ExtraSetCustomServer { get; set; }
		public string ExtraGatewayAddr { get; set; }
		public string ExtraLocalAddr { get; set; }
		public int ExtraCustomInterval { get; set; }
		public int[] MapWN34 = new int[9];
	}
}
