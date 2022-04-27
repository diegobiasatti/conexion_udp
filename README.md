
# Conexión UDP

Programa desarrollado en C# (01/2021)

* Queda a la espera de lo que envie una plataforma externa, en un puerto determinado.

* Al recibir ciertos parámetros, se realiza una inserción en una DB MYSQL en la nube.

* Recopila datos utilizando HttpClient y se los pasa a formato JSON con la libreria Newtonsoft.Json 

                {"fecha"      , fecha     },
                { "hora"      , hora      },
                { "patente"   , patente   },
                { "latitud"   , latitud   },
                { "longitud"  , longitud  },
                { "velocidad" , velocidad },
                { "sentido"   , sentido   },
                { "gps"       , gps       },
                { "evento"    , evento    },
                { "s1"        , s1        },
                { "s2"        , s2        },
                { "s3"        , s3        },
* Cada X cantidad de minutos la app inserta en una DB de Access de forma local. 



## Captura

![App Screenshot](https://github.com/diegobiasatti/conexion_udp/blob/main/vista_.JPG?raw=true)

![App Screenshot](https://github.com/diegobiasatti/conexion_udp/blob/main/vista_1.JPG?raw=true)


## Observaciones

De no contar con Internet, aguarda 1 minuto y vuelve a intentar.


##  Desarrollado en
C# Console

.NET Framework 4.5

Library Newtonsoft.Json
