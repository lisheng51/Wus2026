using ServiceReferenceAanleveren;
using ServiceReferenceStatusInformatie;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Wus2026.WusChannel
{
	public class WusClient
	{
		public WusConnectionProfile Profile { get; private set; }

		X509Certificate2 ClientCertificate { get; set; }
		private readonly WusSoapClient _soapClient;

		public WusClient(WusConnectionProfile profile, X509Certificate2 clientCertificate)
		{
			if (profile?.EndpointStatusInformatieService == null)
				throw new ArgumentOutOfRangeException("profile", "Profile or profile endpoint cannot be null");

			Profile = profile;
			ClientCertificate = clientCertificate;
			_soapClient = new WusSoapClient(ClientCertificate, Profile.ServerCertificate, Profile.IncludeBinarySecurityToken);
		}

		public aanleverenResponse Aanleveren(aanleverenRequest request)
		{
			return _soapClient.Aanleveren(Profile.EndpointAanleverService, request);
		}

		public getStatussenProcesResponse1 StatusInformatie(getStatussenProcesRequest1 request)
		{
			return _soapClient.GetStatussenProces(Profile.EndpointStatusInformatieService, request);
		}
	}
}
