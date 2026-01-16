<?php

declare(strict_types=1);

final class Aangifte
{
    public function __construct(
        public string $fileLocation,
        public string $identiteitNummer,
        public string $identiteitType,
        public string $berichtsoort,
        public string $aanleverkenmerk,
        public string $rolBelanghebbende,
        public string $bestandsnaam,
        public ?string $inhoud = null,
    ) {
    }
}
