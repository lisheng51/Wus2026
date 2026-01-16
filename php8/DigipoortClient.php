<?php

declare(strict_types=1);

final class DigipoortClient
{
    private const SOAP_NS = 'http://schemas.xmlsoap.org/soap/envelope/';
    private const WSA_NS = 'http://www.w3.org/2005/08/addressing';
    private const WSSE_NS = 'http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd';
    private const WSU_NS = 'http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd';
    private const KV_NS = 'http://logius.nl/digipoort/koppelvlakservices/1.2/';
    private const X509_VALUE_TYPE = 'http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3';
    private const BASE64_ENCODING_TYPE = 'http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary';
    private const EXC_C14N = 'http://www.w3.org/2001/10/xml-exc-c14n#';
    private const RSA_SHA1 = 'http://www.w3.org/2000/09/xmldsig#rsa-sha1';
    private const SHA1 = 'http://www.w3.org/2000/09/xmldsig#sha1';

    private const BODY_ID = 'body_0';
    private const TIMESTAMP_ID = 'timestamp_0';
    private const ADDRESS_0_ID = 'address_0';
    private const ADDRESS_1_ID = 'address_1';
    private const ADDRESS_2_ID = 'address_2';
    private const ADDRESS_3_ID = 'address_3';
    private const TOKEN_ID = 'sec_0';

    public const ENDPOINT_AANLEVER_SERVICE_URL = 'https://wus.preproductie.digipoort.logius.nl/wus/2.0/aanleverservice/1.2';
    public const ENDPOINT_STATUS_INFORMATIE_SERVICE_URL = 'https://wus.preproductie.digipoort.logius.nl/wus/2.0/statusinformatieservice/1.2';
    public const AUSP_SERVICE_URL = 'http://geenausp.nl';
    public const BERICHT_INHOUD_MIME_TYPE = 'application/xml';

    private string $clientCertificatePath;
    private string $clientPrivateKeyPath;
    private string $serverCertificatePath;

    public function __construct(
        string $clientCertificatePath,
        string $clientPrivateKeyPath,
        string $serverCertificatePath,
    ) {
        $this->clientCertificatePath = $clientCertificatePath;
        $this->clientPrivateKeyPath = $clientPrivateKeyPath;
        $this->serverCertificatePath = $serverCertificatePath;
    }

    public function aanleveren(Aangifte $aangifte): string
    {
        $payload = [
            'aanleverkenmerk' => $aangifte->aanleverkenmerk,
            'berichtsoort' => $aangifte->berichtsoort,
            'identiteitBelanghebbende' => [
                'nummer' => $aangifte->identiteitNummer,
                'type' => $aangifte->identiteitType,
            ],
            'rolBelanghebbende' => $aangifte->rolBelanghebbende,
            'berichtInhoud' => [
                'mimeType' => self::BERICHT_INHOUD_MIME_TYPE,
                'bestandsnaam' => $aangifte->bestandsnaam,
                'inhoud' => $this->loadInhoud($aangifte),
            ],
            'autorisatieAdres' => self::AUSP_SERVICE_URL,
        ];

        $envelope = $this->buildSignedEnvelope(
            'http://logius.nl/digipoort/wus/2.0/aanleverservice/1.2/AanleverService/aanleverenRequest',
            self::ENDPOINT_AANLEVER_SERVICE_URL,
            'aanleverRequest',
            $payload,
        );

        return $this->send(self::ENDPOINT_AANLEVER_SERVICE_URL, 'aanleverenRequest', $envelope);
    }

    public function statusInformatie(string $kenmerk): string
    {
        $payload = [
            'kenmerk' => $kenmerk,
            'autorisatieAdres' => self::AUSP_SERVICE_URL,
        ];

        $envelope = $this->buildSignedEnvelope(
            'http://logius.nl/digipoort/wus/2.0/statusinformatieservice/1.2/StatusinformatieService/getStatussenProcesRequest',
            self::ENDPOINT_STATUS_INFORMATIE_SERVICE_URL,
            'getStatussenProcesRequest',
            $payload,
        );

        return $this->send(self::ENDPOINT_STATUS_INFORMATIE_SERVICE_URL, 'getStatussenProcesRequest', $envelope);
    }

    private function loadInhoud(Aangifte $aangifte): string
    {
        if ($aangifte->inhoud !== null && $aangifte->inhoud !== '') {
            return base64_encode($aangifte->inhoud);
        }

        if ($aangifte->fileLocation === '') {
            return '';
        }

        $contents = file_get_contents($aangifte->fileLocation);
        if ($contents === false) {
            throw new RuntimeException('Unable to read aangifte file contents.');
        }

        return base64_encode($contents);
    }

    private function send(string $endpoint, string $soapAction, DOMDocument $envelope): string
    {
        $xml = $envelope->saveXML();

        $curl = curl_init($endpoint);
        curl_setopt($curl, CURLOPT_POST, true);
        curl_setopt($curl, CURLOPT_POSTFIELDS, $xml);
        curl_setopt($curl, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($curl, CURLOPT_HTTPHEADER, [
            'Content-Type: text/xml; charset=utf-8',
            'SOAPAction: ' . $soapAction,
        ]);
        curl_setopt($curl, CURLOPT_SSLCERT, $this->clientCertificatePath);
        curl_setopt($curl, CURLOPT_SSLKEY, $this->clientPrivateKeyPath);
        curl_setopt($curl, CURLOPT_CAINFO, $this->serverCertificatePath);
        curl_setopt($curl, CURLOPT_SSL_VERIFYPEER, true);
        curl_setopt($curl, CURLOPT_SSL_VERIFYHOST, 2);

        $response = curl_exec($curl);
        if ($response === false) {
            $error = curl_error($curl);
            curl_close($curl);
            throw new RuntimeException('SOAP request failed: ' . $error);
        }

        $status = curl_getinfo($curl, CURLINFO_HTTP_CODE);
        curl_close($curl);

        if ($status < 200 || $status >= 300) {
            throw new RuntimeException('SOAP request failed with HTTP ' . $status . '.');
        }

        return $response;
    }

    private function buildSignedEnvelope(string $action, string $to, string $payloadElementName, array $payload): DOMDocument
    {
        $doc = new DOMDocument('1.0', 'UTF-8');
        $doc->preserveWhiteSpace = true;

        $envelope = $doc->createElementNS(self::SOAP_NS, 's:Envelope');
        $envelope->setAttributeNS('http://www.w3.org/2000/xmlns/', 'xmlns:a', self::WSA_NS);
        $envelope->setAttributeNS('http://www.w3.org/2000/xmlns/', 'xmlns:o', self::WSSE_NS);
        $envelope->setAttributeNS('http://www.w3.org/2000/xmlns/', 'xmlns:u', self::WSU_NS);
        $envelope->setAttributeNS('http://www.w3.org/2000/xmlns/', 'xmlns:ds', 'http://www.w3.org/2000/09/xmldsig#');
        $doc->appendChild($envelope);

        $header = $doc->createElementNS(self::SOAP_NS, 's:Header');
        $body = $doc->createElementNS(self::SOAP_NS, 's:Body');
        $envelope->appendChild($header);
        $envelope->appendChild($body);

        $this->addWsuId($body, self::BODY_ID);

        $actionId = $this->addAddressingHeader($doc, $header, 'Action', $action, self::ADDRESS_0_ID, true);
        $messageId = $this->addAddressingHeader($doc, $header, 'MessageID', 'urn:uuid:' . $this->uuid(), self::ADDRESS_1_ID, false);
        $replyToId = $this->addReplyToHeader($doc, $header, self::ADDRESS_2_ID);
        $toId = $this->addAddressingHeader($doc, $header, 'To', $to, self::ADDRESS_3_ID, false);

        $security = $doc->createElementNS(self::WSSE_NS, 'o:Security');
        $security->setAttributeNS(self::SOAP_NS, 's:mustUnderstand', '1');
        $header->appendChild($security);

        $timestamp = $this->createTimestamp($doc, self::TIMESTAMP_ID);
        $security->appendChild($timestamp);

        $binaryToken = $this->createBinarySecurityToken($doc, self::TOKEN_ID);
        $security->appendChild($binaryToken);

        $this->appendPayload($doc, $body, $payloadElementName, $payload);

        $signature = $this->createSignature(
            $doc,
            self::TOKEN_ID,
            [self::BODY_ID, self::TIMESTAMP_ID, $toId, $actionId, $messageId, $replyToId],
        );
        $security->appendChild($signature);

        return $doc;
    }

    private function addAddressingHeader(DOMDocument $doc, DOMElement $header, string $localName, string $value, string $id, bool $mustUnderstand): string
    {
        $element = $doc->createElementNS(self::WSA_NS, 'a:' . $localName);
        $element->nodeValue = $value;
        if ($mustUnderstand) {
            $element->setAttributeNS(self::SOAP_NS, 's:mustUnderstand', '1');
        }
        $this->addWsuId($element, $id);
        $header->appendChild($element);
        return $id;
    }

    private function addReplyToHeader(DOMDocument $doc, DOMElement $header, string $id): string
    {
        $replyTo = $doc->createElementNS(self::WSA_NS, 'a:ReplyTo');
        $this->addWsuId($replyTo, $id);

        $address = $doc->createElementNS(self::WSA_NS, 'a:Address');
        $address->nodeValue = 'http://www.w3.org/2005/08/addressing/anonymous';
        $replyTo->appendChild($address);

        $header->appendChild($replyTo);
        return $id;
    }

    private function createTimestamp(DOMDocument $doc, string $id): DOMElement
    {
        $timestamp = $doc->createElementNS(self::WSU_NS, 'u:Timestamp');
        $this->addWsuId($timestamp, $id);

        $created = $doc->createElementNS(self::WSU_NS, 'u:Created');
        $created->nodeValue = gmdate('Y-m-d\TH:i:s\Z');
        $timestamp->appendChild($created);

        $expires = $doc->createElementNS(self::WSU_NS, 'u:Expires');
        $expires->nodeValue = gmdate('Y-m-d\TH:i:s\Z', time() + 300);
        $timestamp->appendChild($expires);

        return $timestamp;
    }

    private function createBinarySecurityToken(DOMDocument $doc, string $id): DOMElement
    {
        $token = $doc->createElementNS(self::WSSE_NS, 'o:BinarySecurityToken');
        $this->addWsuId($token, $id);
        $token->setAttribute('EncodingType', self::BASE64_ENCODING_TYPE);
        $token->setAttribute('ValueType', self::X509_VALUE_TYPE);
        $token->nodeValue = $this->loadCertificateBase64();
        return $token;
    }

    private function createSignature(DOMDocument $doc, string $tokenId, array $referenceIds): DOMElement
    {
        $signature = $doc->createElementNS('http://www.w3.org/2000/09/xmldsig#', 'ds:Signature');
        $signedInfo = $doc->createElement('ds:SignedInfo');

        $canonicalizationMethod = $doc->createElement('ds:CanonicalizationMethod');
        $canonicalizationMethod->setAttribute('Algorithm', self::EXC_C14N);
        $signedInfo->appendChild($canonicalizationMethod);

        $signatureMethod = $doc->createElement('ds:SignatureMethod');
        $signatureMethod->setAttribute('Algorithm', self::RSA_SHA1);
        $signedInfo->appendChild($signatureMethod);

        foreach ($referenceIds as $referenceId) {
            $reference = $doc->createElement('ds:Reference');
            $reference->setAttribute('URI', '#' . $referenceId);

            $transforms = $doc->createElement('ds:Transforms');
            $transform = $doc->createElement('ds:Transform');
            $transform->setAttribute('Algorithm', self::EXC_C14N);
            $transforms->appendChild($transform);
            $reference->appendChild($transforms);

            $digestMethod = $doc->createElement('ds:DigestMethod');
            $digestMethod->setAttribute('Algorithm', self::SHA1);
            $reference->appendChild($digestMethod);

            $digestValue = $doc->createElement('ds:DigestValue');
            $digestValue->nodeValue = $this->digestReference($doc, $referenceId);
            $reference->appendChild($digestValue);

            $signedInfo->appendChild($reference);
        }

        $signature->appendChild($signedInfo);

        $signatureValue = $doc->createElement('ds:SignatureValue');
        $signatureValue->nodeValue = $this->signData($signedInfo->C14N(true, false));
        $signature->appendChild($signatureValue);

        $keyInfo = $doc->createElement('ds:KeyInfo');
        $securityTokenReference = $doc->createElementNS(self::WSSE_NS, 'o:SecurityTokenReference');
        $referenceElement = $doc->createElementNS(self::WSSE_NS, 'o:Reference');
        $referenceElement->setAttribute('URI', '#' . $tokenId);
        $referenceElement->setAttribute('ValueType', self::X509_VALUE_TYPE);
        $securityTokenReference->appendChild($referenceElement);
        $keyInfo->appendChild($securityTokenReference);
        $signature->appendChild($keyInfo);

        return $signature;
    }

    private function digestReference(DOMDocument $doc, string $referenceId): string
    {
        $xpath = new DOMXPath($doc);
        $xpath->registerNamespace('wsu', self::WSU_NS);
        $nodes = $xpath->query("//*[@wsu:Id='{$referenceId}']");
        if ($nodes === false || $nodes->length === 0) {
            throw new RuntimeException('Unable to find reference node: ' . $referenceId);
        }

        $node = $nodes->item(0);
        $canonical = $node->C14N(true, false);
        return base64_encode(sha1($canonical, true));
    }

    private function signData(string $data): string
    {
        $privateKey = openssl_pkey_get_private('file://' . $this->clientPrivateKeyPath);
        if ($privateKey === false) {
            throw new RuntimeException('Unable to load client private key.');
        }

        $signature = '';
        $ok = openssl_sign($data, $signature, $privateKey, OPENSSL_ALGO_SHA1);
        openssl_free_key($privateKey);

        if ($ok === false) {
            throw new RuntimeException('Unable to sign SOAP envelope.');
        }

        return base64_encode($signature);
    }

    private function loadCertificateBase64(): string
    {
        $contents = file_get_contents($this->clientCertificatePath);
        if ($contents === false) {
            throw new RuntimeException('Unable to read client certificate.');
        }

        $contents = preg_replace('/-----BEGIN CERTIFICATE-----|-----END CERTIFICATE-----|\s+/', '', $contents);
        if ($contents === null) {
            throw new RuntimeException('Unable to parse client certificate.');
        }

        return $contents;
    }

    private function addWsuId(DOMElement $element, string $id): void
    {
        $element->setAttributeNS(self::WSU_NS, 'u:Id', $id);
    }

    private function appendPayload(DOMDocument $doc, DOMElement $body, string $payloadElementName, array $payload): void
    {
        $payloadElement = $doc->createElementNS(self::KV_NS, $payloadElementName);
        $this->appendArray($doc, $payloadElement, $payload);
        $body->appendChild($payloadElement);
    }

    private function appendArray(DOMDocument $doc, DOMElement $parent, array $data): void
    {
        foreach ($data as $key => $value) {
            if (is_array($value)) {
                $child = $doc->createElement($key);
                $this->appendArray($doc, $child, $value);
                $parent->appendChild($child);
                continue;
            }

            $child = $doc->createElement($key);
            $child->nodeValue = (string) $value;
            $parent->appendChild($child);
        }
    }

    private function uuid(): string
    {
        $data = random_bytes(16);
        $data[6] = chr((ord($data[6]) & 0x0f) | 0x40);
        $data[8] = chr((ord($data[8]) & 0x3f) | 0x80);
        return vsprintf('%s%s-%s-%s-%s-%s%s%s', str_split(bin2hex($data), 4));
    }
}
