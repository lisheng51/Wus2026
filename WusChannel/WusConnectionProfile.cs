using System.Security.Cryptography.X509Certificates;

namespace Wus2026.WusChannel
{
	public class WusConnectionProfile
	{
		public string EndpointAanleverService { get; set; }
		public string EndpointStatusInformatieService { get; set; }
		public string EndpointOphaalService { get; set; }
		public string AuspService { get; set; }
		public X509Certificate2 ServerCertificate { get; set; }
		/// <summary>
		/// Include wsse:BinarySecurityToken in WS-Security header.
		/// </summary>
		public bool IncludeBinarySecurityToken { get; set; } = true;
	}
}
