# Introduction 

                WusSoapClient wusClient = Digipoort.Client(clientCertificate, ServerCertificate);
                Aangifte bestand = new()
                {
                    fileLocation = "D:\\ms_net\\WinFormsApp1\\bin\\Debug\\net8.0-windows\\inhoud.xml",

                    identiteit_nummer = "001000044B39", //LoonHeffingsNummer //[identiteit_nummer]
                    identiteit_type = "LHnr",   //BTW => Omzetbelasting, LHnr=>LoonAangifte [identiteit_type]

                    berichtsoort = "Aangifte_LH",   //[berichtsoort]
                    aanleverkenmerk = "Happyflow",
                    rolBelanghebbende = "Intermediair", //[rolBelanghebbende]
                    bestandsnaam = "inhoud.xml"
                };
                aanleverenResponse aanleverResponse = Digipoort.Aanleveren(wusClient, bestand);
                getStatussenProcesResponse1 statusResponse = Digipoort.StatusInformatie(wusClient, kenmerk);




                https://www.logius.nl/domeinen/gegevensuitwisseling/digipoort/wat-is-het/koppelvlakken/wus-voor-bedrijven
