using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BloemendaalConsultancyLib.ServiceReferenceAanleveren;
using BloemendaalConsultancyLib.ServiceReferenceOphalen;
using BloemendaalConsultancyLib.ServiceReferenceStatusInformatie;

namespace BloemendaalConsultancyLib.WusChannel
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

		public getBerichtenLijstResponse1 OphalenBerichtenLijst(getBerichtenLijstRequest1 request)
		{
			return _soapClient.GetBerichtenLijst(Profile.EndpointOphaalService, request);
		}

		public getBerichtenKenmerkResponse1 OphalenBericht(getBerichtenKenmerkRequest1 request)
		{
			return _soapClient.GetBerichtenKenmerk(Profile.EndpointOphaalService, request);
		}

		public getBerichtenLijstRequest1 CreateBerichtenLijstRequest(string berichtsoort, DateTime tijdstempelVanaf, DateTime tijdstempelTot)
		{
			return new getBerichtenLijstRequest1(berichtsoort, tijdstempelVanaf, tijdstempelTot, Profile.AuspService);
		}

		public getBerichtenKenmerkRequest1 CreateBerichtenKenmerkRequest(string kenmerk)
		{
			return new getBerichtenKenmerkRequest1(kenmerk, Profile.AuspService);
		}

		public aanleverenResponse Aanleveren(aanleverenRequest request)
		{
			return _soapClient.Aanleveren(Profile.EndpointAanleverService, request);
		}

		public async Task<aanleverenResponse> AanleverenAsync(aanleverenRequest request)
		{
			return await _soapClient.AanleverenAsync(Profile.EndpointAanleverService, request).ConfigureAwait(false);
		}

		public getStatussenProcesResponse1 StatusInformatie(string kenmerk)
		{
			return StatusInformatie(new getStatussenProcesRequest1(kenmerk, Profile.AuspService));
		}

		public getStatussenProcesResponse1 StatusInformatie(getStatussenProcesRequest1 request)
		{
			return _soapClient.GetStatussenProces(Profile.EndpointStatusInformatieService, request);
		}

		public async Task<getStatussenProcesResponse1> StatusInformatieAsync(getStatussenProcesRequest1 request)
		{
			return await _soapClient.GetStatussenProcesAsync(Profile.EndpointStatusInformatieService, request).ConfigureAwait(false);
		}

		public aanleverenRequest CreateAanleverRequest(string aanleverkenmerk, string berichtsoort, ServiceReferenceAanleveren.identiteitType identiteitBelanghebbende, string rolBelanghebbende, string fileLocation)
		{
			// Create request           
			return new aanleverenRequest(aanleverkenmerk, berichtsoort, identiteitBelanghebbende, rolBelanghebbende, fileLocation, Profile.AuspService);
		}
	}
}
