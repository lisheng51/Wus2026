using System.IO;

namespace Wus2026.ServiceReferenceAanleveren
{
    public partial class aanleverenRequest
    {
        public aanleverenRequest(string aanleverkenmerk, string berichtsoort, identiteitType identiteitBelanghebbende, string rolBelanghebbende, string fileLocation, string autorisatieAdres)
        {
            // Create request           
            aanleverRequest = new aanleverRequest
            {
                aanleverkenmerk = aanleverkenmerk,
                berichtsoort = berichtsoort,
                identiteitBelanghebbende = identiteitBelanghebbende,
                rolBelanghebbende = rolBelanghebbende,
                berichtInhoud = new berichtInhoudType
                {
                    // File name to submit in message
                    bestandsnaam = Path.GetFileName(fileLocation),
                    // Read file as UTF-8 byte array
                    inhoud = File.ReadAllBytes(fileLocation),
                    mimeType = "application/xml"
                },
                autorisatieAdres = autorisatieAdres
            };
        }

        public aanleverenRequest(string aanleverkenmerk, string berichtsoort, identiteitType identiteitBelanghebbende, string rolBelanghebbende, berichtInhoudType berichtInhoud, string autorisatieAdres)
        {
            // Create request           
            aanleverRequest = new aanleverRequest
            {
                aanleverkenmerk = aanleverkenmerk,
                berichtsoort = berichtsoort,
                identiteitBelanghebbende = identiteitBelanghebbende,
                rolBelanghebbende = rolBelanghebbende,
                berichtInhoud = berichtInhoud,
                autorisatieAdres = autorisatieAdres
            };
        }
    }

    public partial class identiteitType
    {
        public identiteitType() { }

        public identiteitType(string nummer, string type)
        {
            this.nummer = nummer;
            this.type = type;
        }

        public override string ToString()
        {
            return $"{type}:{nummer}";
        }
    }
}
