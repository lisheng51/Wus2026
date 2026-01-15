using ServiceReferenceAanleveren;
using ServiceReferenceStatusInformatie;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Wus2026.WusChannel;

namespace Wus2026
{
    public class Digipoort
    {
        public static string EndpointAanleverServiceUrl => "https://wus.preproductie.digipoort.logius.nl/wus/2.0/aanleverservice/1.2";
        public static string EndpointStatusInformatieServiceUrl => "https://wus.preproductie.digipoort.logius.nl/wus/2.0/statusinformatieservice/1.2";
        public static string AuspServiceUrl => "http://geenausp.nl";
        public static string BerichtInhoudMimeType => "application/xml";

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
            getStatussenProcesRequest getStatussenProcesRequest = new()
            {
                kenmerk = kenmerk,
                autorisatieAdres = wusClient.Profile.AuspService
            };

            getStatussenProcesRequest1 requestBody = new()
            {
                getStatussenProcesRequest = getStatussenProcesRequest
            };
            getStatussenProcesResponse1 statusResponse = wusClient.StatusInformatie(requestBody);
            return statusResponse;
        }

        public static aanleverenResponse Aanleveren(WusClient wusClient, Aangifte aangifte)
        {
            berichtInhoudType berichtInhoud = new()
            {
                mimeType = BerichtInhoudMimeType,
                bestandsnaam = aangifte.bestandsnaam,
                inhoud = string.IsNullOrEmpty(aangifte.fileLocation) == false ? File.ReadAllBytes(aangifte.fileLocation) : Encoding.UTF8.GetBytes(aangifte.inhoud)
            };

            ServiceReferenceAanleveren.identiteitType identity = new()
            {
                nummer = aangifte.identiteit_nummer,
                type = aangifte.identiteit_type
            };


            aanleverRequest aanleverRequest = new()
            {
                aanleverkenmerk = aangifte.aanleverkenmerk,
                berichtsoort = aangifte.berichtsoort,
                identiteitBelanghebbende = identity,
                rolBelanghebbende = aangifte.rolBelanghebbende,
                berichtInhoud = berichtInhoud,
                autorisatieAdres = wusClient.Profile.AuspService
            };

            aanleverenRequest aanleverRequestBody = new()
            {
                aanleverRequest = aanleverRequest
            };

            aanleverenResponse aanleverResponse = wusClient.Aanleveren(aanleverRequestBody);
            return aanleverResponse;
        }
    }

}
