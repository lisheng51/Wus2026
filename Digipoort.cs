using BloemendaalConsultancyLib.ServiceReferenceAanleveren;
using BloemendaalConsultancyLib.ServiceReferenceStatusInformatie;
using BloemendaalConsultancyLib.WusChannel;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace BloemendaalConsultancyLib
{
    public class Digipoort
    {
        public static string EndpointAanleverServiceUrl => "https://wus.preproductie.digipoort.logius.nl/wus/2.0/aanleverservice/1.2";
        public static string EndpointStatusInformatieServiceUrl => "https://wus.preproductie.digipoort.logius.nl/wus/2.0/statusinformatieservice/1.2";
        public static string AuspServiceUrl => "http://geenausp.nl";
        public static string berichtInhoudMimeType => "application/xml";

        public static WusClient Client(X509Certificate2 clientCertificate, X509Certificate2 serverCertificate)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            WusConnectionProfile profile = new()
            {
                ServerCertificate = serverCertificate,
                EndpointAanleverService = EndpointAanleverServiceUrl,
                EndpointStatusInformatieService = EndpointStatusInformatieServiceUrl,
                AuspService = AuspServiceUrl,
            };

            WusClient wusClientNew = new(profile, clientCertificate);
            return wusClientNew;
        }

        public static getStatussenProcesResponse1 StatusInformatie(WusClient wusClient, string kenmerk)
        {
            getStatussenProcesResponse1 statusResponse = wusClient.StatusInformatie(kenmerk);
            return statusResponse;
        }

        public static aanleverenResponse Aanleveren(WusClient wusClient, Aangifte aangifte)
        {
            berichtInhoudType berichtInhoud = new();
            berichtInhoud.mimeType = berichtInhoudMimeType;
            berichtInhoud.bestandsnaam = aangifte.bestandsnaam;
            berichtInhoud.inhoud = string.IsNullOrEmpty(aangifte.fileLocation) == false ? File.ReadAllBytes(aangifte.fileLocation) : Encoding.UTF8.GetBytes(aangifte.inhoud);
            ServiceReferenceAanleveren.identiteitType identity = new ServiceReferenceAanleveren.identiteitType(aangifte.identiteit_nummer, aangifte.identiteit_type);
            aanleverenRequest aanleverRequest = new(aangifte.aanleverkenmerk, aangifte.berichtsoort, identity, aangifte.rolBelanghebbende, berichtInhoud, wusClient.Profile.AuspService);
            aanleverenResponse aanleverResponse = wusClient.Aanleveren(aanleverRequest);
            return aanleverResponse;
        }
    }

}
