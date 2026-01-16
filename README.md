# Introduction

```php
<?php
$clientCertificate = '/path/to/client-cert.pem';
$clientPrivateKey = '/path/to/client-key.pem';
$serverCertificate = '/path/to/server-cert.pem';

$wsdl = '/path/to/digipoort.wsdl';

$context = stream_context_create([
    'ssl' => [
        'local_cert' => $clientCertificate,
        'local_pk' => $clientPrivateKey,
        'cafile' => $serverCertificate,
        'verify_peer' => true,
        'verify_peer_name' => true,
        'allow_self_signed' => false,
    ],
]);

$client = new SoapClient($wsdl, [
    'stream_context' => $context,
    'trace' => true,
    'cache_wsdl' => WSDL_CACHE_NONE,
]);

$bestand = [
    'fileLocation' => 'D:\\ms_net\\WinFormsApp1\\bin\\Debug\\net8.0-windows\\inhoud.xml',
    'identiteit_nummer' => '001000044B39', // LoonHeffingsNummer
    'identiteit_type' => 'LHnr', // BTW => Omzetbelasting, LHnr => LoonAangifte
    'berichtsoort' => 'Aangifte_LH',
    'aanleverkenmerk' => 'Happyflow',
    'rolBelanghebbende' => 'Intermediair',
    'bestandsnaam' => 'inhoud.xml',
];

$aanleverResponse = $client->__soapCall('Aanleveren', [$bestand]);

$kenmerk = $aanleverResponse->kenmerk ?? null;
$statusResponse = $client->__soapCall('StatusInformatie', [$kenmerk]);
```

For a PHP 8-compatible client that signs the WS-Security headers directly, see `php8/DigipoortClient.php`.

https://www.logius.nl/domeinen/gegevensuitwisseling/digipoort/wat-is-het/koppelvlakken/wus-voor-bedrijven
