using Wus2026.ServiceReferenceAanleveren;
using Wus2026.ServiceReferenceOphalen;
using Wus2026.ServiceReferenceStatusInformatie;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Wus2026.WusChannel
{
    internal sealed class WusSoapClient
    {
		private const string SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
		private const string WsaNs = "http://www.w3.org/2005/08/addressing";
		private const string WsseNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
		private const string WsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        private const string KvNs = "http://logius.nl/digipoort/koppelvlakservices/1.2/";
        private const string X509ValueType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3";
        private const string Base64EncodingType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";

        private const string BodyId = "body_0";
        private const string TimestampId = "timestamp_0";
        private const string Address0Id = "address_0";
        private const string Address1Id = "address_1";
        private const string Address2Id = "address_2";
        private const string Address3Id = "address_3";
        private const string TokenId = "sec_0";

        private static readonly TimeSpan TimestampSkew = TimeSpan.FromMinutes(5);

        private readonly X509Certificate2 _clientCertificate;
        private readonly X509Certificate2 _serverCertificate;
        private readonly bool _includeBinarySecurityToken;
        private readonly HttpClient _httpClient;

        public WusSoapClient(X509Certificate2 clientCertificate, X509Certificate2 serverCertificate, bool includeBinarySecurityToken)
        {
            _clientCertificate = clientCertificate ?? throw new ArgumentNullException(nameof(clientCertificate));
            _serverCertificate = serverCertificate;
            _includeBinarySecurityToken = includeBinarySecurityToken;

            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = ValidateServerCertificate
            };
            handler.ClientCertificates.Add(_clientCertificate);
            _httpClient = new HttpClient(handler);
        }

        public aanleverenResponse Aanleveren(string endpoint, aanleverenRequest request)
        {
            var response = Send<aanleverRequest, aanleverResponse>(
                endpoint,
                Actions.AanleverenRequest,
                request.aanleverRequest,
                "aanleverRequest",
                "aanleverResponse");

            return new aanleverenResponse(response);
        }

        public Task<aanleverenResponse> AanleverenAsync(string endpoint, aanleverenRequest request)
        {
            return Task.FromResult(Aanleveren(endpoint, request));
        }

        public getBerichtenLijstResponse1 GetBerichtenLijst(string endpoint, getBerichtenLijstRequest1 request)
        {
            var response = Send<getBerichtenLijstRequest, getBerichtenLijstResponse>(
                endpoint,
                Actions.GetBerichtenLijstRequest,
                request.getBerichtenLijstRequest,
                "getBerichtenLijstRequest",
                "getBerichtenLijstResponse");

            return new getBerichtenLijstResponse1(response);
        }

        public getBerichtenKenmerkResponse1 GetBerichtenKenmerk(string endpoint, getBerichtenKenmerkRequest1 request)
        {
            var response = Send<getBerichtenKenmerkRequest, getBerichtenKenmerkResponse>(
                endpoint,
                Actions.GetBerichtenKenmerkRequest,
                request.getBerichtenKenmerkRequest,
                "getBerichtenKenmerkRequest",
                "getBerichtenKenmerkResponse");

            return new getBerichtenKenmerkResponse1(response);
        }

        public getStatussenProcesResponse1 GetStatussenProces(string endpoint, getStatussenProcesRequest1 request)
        {
            var response = Send<getStatussenProcesRequest, getStatussenProcesResponse>(
                endpoint,
                Actions.GetStatussenProcesRequest,
                request.getStatussenProcesRequest,
                "getStatussenProcesRequest",
                "getStatussenProcesResponse");

            return new getStatussenProcesResponse1(response);
        }

        public Task<getStatussenProcesResponse1> GetStatussenProcesAsync(string endpoint, getStatussenProcesRequest1 request)
        {
            return Task.FromResult(GetStatussenProces(endpoint, request));
        }

        private bool ValidateServerCertificate(HttpRequestMessage _, X509Certificate2 cert, X509Chain __, SslPolicyErrors ___)
        {
            if (_serverCertificate == null)
                return true;

            if (cert == null)
                return false;

            bool ok = string.Equals(cert.Thumbprint, _serverCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase);
            return ok;
        }

        private TResponse Send<TRequest, TResponse>(
            string endpoint,
            string action,
            TRequest request,
            string requestElementName,
            string responseElementName)
        {
            var envelope = BuildSignedEnvelope(action, endpoint, request, requestElementName);
            var xml = envelope.OuterXml;

            using var content = new StringContent(xml, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", action);

            var response = _httpClient.PostAsync(endpoint, content).GetAwaiter().GetResult();
            var responseXml = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                var fault = TryExtractSoapFault(responseXml);
                throw new InvalidOperationException(fault ?? $"SOAP request failed with HTTP {(int)response.StatusCode}.");
            }

            return DeserializeBody<TResponse>(responseXml, responseElementName);
        }

        private XmlDocument BuildSignedEnvelope<TRequest>(string action, string to, TRequest payload, string payloadElementName)
        {
            var doc = new XmlDocument { PreserveWhitespace = true };

			var envelope = doc.CreateElement("s", "Envelope", SoapNs);
			envelope.SetAttribute("xmlns:s", SoapNs);
			envelope.SetAttribute("xmlns:a", WsaNs);
			envelope.SetAttribute("xmlns:o", WsseNs);
			envelope.SetAttribute("xmlns:u", WsuNs);
			envelope.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
			envelope.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
			envelope.SetAttribute("xmlns:ds", SignedXml.XmlDsigNamespaceUrl);
			doc.AppendChild(envelope);

			var header = doc.CreateElement("s", "Header", SoapNs);
			var body = doc.CreateElement("s", "Body", SoapNs);
			envelope.AppendChild(header);
			envelope.AppendChild(body);

            var bodyId = BodyId;
            AddWsuId(body, bodyId);

            var actionId = AddAddressingHeader(doc, header, "Action", action, Address0Id);
            var messageId = AddAddressingHeader(doc, header, "MessageID", "urn:uuid:" + Guid.NewGuid(), Address1Id);
            var replyToId = AddReplyToHeader(doc, header, Address2Id);
            var toId = AddAddressingHeader(doc, header, "To", to, Address3Id);
			var security = doc.CreateElement("o", "Security", WsseNs);
			var mustUnderstand = doc.CreateAttribute("s", "mustUnderstand", SoapNs);
			mustUnderstand.Value = "1";
			security.Attributes.Append(mustUnderstand);
			header.AppendChild(security);

            var timestampId = TimestampId;
            var timestamp = CreateTimestamp(doc, timestampId);
            security.AppendChild(timestamp);

            string tokenId = null;
            if (_includeBinarySecurityToken)
            {
                tokenId = TokenId;
                var binaryToken = CreateBinarySecurityToken(doc, tokenId, _clientCertificate);
                security.AppendChild(binaryToken);
            }

            AppendPayload(doc, body, payload, payloadElementName);

            var signature = CreateSignature(doc, _clientCertificate, tokenId,
            [
                bodyId,
                timestampId,
                toId,
                actionId,
                messageId,
                replyToId
            ]);
            security.AppendChild(signature);

            return doc;
        }

		private static string AddAddressingHeader(XmlDocument doc, XmlElement header, string localName, string value, string id)
		{
			var element = doc.CreateElement("a", localName, WsaNs);
			element.InnerText = value;
			if (localName == "Action")
			{
				var mustUnderstand = doc.CreateAttribute("s", "mustUnderstand", SoapNs);
				mustUnderstand.Value = "1";
				element.Attributes.Append(mustUnderstand);
			}
            AddWsuId(element, id);
            header.AppendChild(element);
            return id;
        }

        private static string AddReplyToHeader(XmlDocument doc, XmlElement header, string id)
        {
			var replyTo = doc.CreateElement("a", "ReplyTo", WsaNs);
            AddWsuId(replyTo, id);

			var address = doc.CreateElement("a", "Address", WsaNs);
            address.InnerText = "http://www.w3.org/2005/08/addressing/anonymous";
            replyTo.AppendChild(address);

            header.AppendChild(replyTo);
            return id;
        }

        private static XmlElement CreateTimestamp(XmlDocument doc, string id)
        {
			var timestamp = doc.CreateElement("u", "Timestamp", WsuNs);
			AddWsuId(timestamp, id);

			var created = doc.CreateElement("u", "Created", WsuNs);
			created.InnerText = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);
			timestamp.AppendChild(created);

			var expires = doc.CreateElement("u", "Expires", WsuNs);
			expires.InnerText = XmlConvert.ToString(DateTime.UtcNow.Add(TimestampSkew), XmlDateTimeSerializationMode.Utc);
			timestamp.AppendChild(expires);

            return timestamp;
        }

        private static XmlElement CreateBinarySecurityToken(XmlDocument doc, string id, X509Certificate2 cert)
        {
			var token = doc.CreateElement("o", "BinarySecurityToken", WsseNs);
			AddWsuId(token, id);
			token.SetAttribute("EncodingType", Base64EncodingType);
			token.SetAttribute("ValueType", X509ValueType);
			token.InnerText = Convert.ToBase64String(cert.GetRawCertData());
            return token;
        }

        private static XmlElement CreateSignature(XmlDocument doc, X509Certificate2 cert, string tokenId, string[] referenceIds)
        {
            var signedXml = new WsuSignedXml(doc)
            {
                SigningKey = cert.GetRSAPrivateKey()
            };

            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

            foreach (var referenceId in referenceIds)
            {
                var reference = new Reference("#" + referenceId)
                {
                    DigestMethod = SignedXml.XmlDsigSHA1Url
                };
                reference.AddTransform(new XmlDsigExcC14NTransform());
                signedXml.AddReference(reference);
            }

            var keyInfo = new KeyInfo();
            if (!string.IsNullOrEmpty(tokenId))
            {
				var securityTokenReference = doc.CreateElement("o", "SecurityTokenReference", WsseNs);
				securityTokenReference.SetAttribute("xmlns:o", WsseNs);
				var referenceElement = doc.CreateElement("o", "Reference", WsseNs);
				referenceElement.SetAttribute("URI", "#" + tokenId);
				referenceElement.SetAttribute("ValueType", X509ValueType);
                securityTokenReference.AppendChild(referenceElement);
                keyInfo.AddClause(new KeyInfoSecurityToken(securityTokenReference));
            }
            else
            {
                var x509Data = new KeyInfoX509Data(cert);
                keyInfo.AddClause(x509Data);
            }
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();
            var signature = signedXml.GetXml();
            return (XmlElement)doc.ImportNode(signature, true);
        }

        private static void AppendPayload<TRequest>(XmlDocument doc, XmlElement body, TRequest payload, string payloadElementName)
        {
            var serializer = new XmlSerializer(payload.GetType(), new XmlRootAttribute(payloadElementName) { Namespace = KvNs });
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, KvNs);

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Encoding = new UTF8Encoding(false)
            };

            string payloadXml;
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            using (var xmlWriter = XmlWriter.Create(writer, settings))
            {
                serializer.Serialize(xmlWriter, payload, namespaces);
                payloadXml = writer.ToString();
            }

            var payloadDoc = new XmlDocument { PreserveWhitespace = true };
            payloadDoc.LoadXml(payloadXml);
            var imported = doc.ImportNode(payloadDoc.DocumentElement, true);
            body.AppendChild(imported);
        }

        private static TResponse DeserializeBody<TResponse>(string soapXml, string responseElementName)
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(soapXml);

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("soapenv", SoapNs);

            var body = doc.SelectSingleNode("//soapenv:Body", ns) as XmlElement;
            if (body == null)
                throw new InvalidOperationException("SOAP response does not contain a body.");

            XmlElement payload = null;
            foreach (XmlNode node in body.ChildNodes)
            {
                if (node is XmlElement element)
                {
                    payload = element;
                    break;
                }
            }

            if (payload == null)
                throw new InvalidOperationException("SOAP response body is empty.");

            var serializer = new XmlSerializer(typeof(TResponse), new XmlRootAttribute(responseElementName) { Namespace = KvNs });
            using var reader = new StringReader(payload.OuterXml);
            return (TResponse)serializer.Deserialize(reader);
        }

        private static string TryExtractSoapFault(string soapXml)
        {
            if (string.IsNullOrWhiteSpace(soapXml))
                return null;

            var doc = new XmlDocument { PreserveWhitespace = true };
            try
            {
                doc.LoadXml(soapXml);
            }
            catch (XmlException)
            {
                return null;
            }

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("soapenv", SoapNs);
            var fault = doc.SelectSingleNode("//soapenv:Fault", ns) as XmlElement;
            if (fault == null)
                return null;

            return fault.InnerText.Trim();
        }

        private static void AddWsuId(XmlElement element, string id)
        {
            var attr = element.OwnerDocument.CreateAttribute("u", "Id", WsuNs);
            attr.Value = id;
            element.Attributes.Append(attr);
        }

        private static class Actions
        {
            public const string AanleverenRequest = "http://logius.nl/digipoort/wus/2.0/aanleverservice/1.2/AanleverService/aanleverenRequest";
            public const string GetBerichtenLijstRequest = "http://logius.nl/digipoort/wus/2.0/ophaalservice/1.2/OphaalService/getBerichtenLijstRequest";
            public const string GetBerichtenKenmerkRequest = "http://logius.nl/digipoort/wus/2.0/ophaalservice/1.2/OphaalService/getBerichtenKenmerkRequest";
            public const string GetStatussenProcesRequest = "http://logius.nl/digipoort/wus/2.0/statusinformatieservice/1.2/StatusinformatieService/getStatussenProcesRequest";
        }

        private sealed class WsuSignedXml : SignedXml
        {
            public WsuSignedXml(XmlDocument document) : base(document)
            {
            }

            public override XmlElement GetIdElement(XmlDocument doc, string id)
            {
                var element = base.GetIdElement(doc, id);
                if (element != null)
                    return element;

                var ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("wsu", WsuNs);
                return doc.SelectSingleNode($"//*[@wsu:Id='{id}']", ns) as XmlElement;
            }
        }

		private sealed class KeyInfoSecurityToken : KeyInfoClause
		{
			private XmlElement _element;

			public KeyInfoSecurityToken(XmlElement element)
			{
				_element = element ?? throw new ArgumentNullException(nameof(element));
			}

			public override XmlElement GetXml()
			{
				return _element;
			}

			public override void LoadXml(XmlElement element)
			{
				_element = element ?? throw new ArgumentNullException(nameof(element));
			}
		}
    }
}
