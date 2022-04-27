using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;

namespace server
{
    class Program
    {
        static int a = 0;
        static bool endApp = false;
        static string ip = "";
        static string database = "";
        static int reset = 0;
        static int puerto ;
        static TimeSpan span1, span2, span3;
        static string GetDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static readonly HttpClient client = new HttpClient();
        static void Main(string[] args)
        {

            span2 = TimeSpan.FromSeconds(1);
            Config();
            Timer t = new Timer(TimerCallback, null, 0, 180000); //son 3 minutos
            //Timer d = new Timer(ResetTimerCallback, null, 0, 10000);
             
            while ((!endApp))
            {
                try
                {
                    ExecuteServer(ip, puerto);
                }
                catch(Exception o) { }
            }
            Console.ReadLine();
        }
        private static void TimerCallback(Object o)
        {
            WriteDetalle("Actualizando datos.mdb : " + DateTime.Now);
            paraElMonster(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo),"Skycop",1);
        }
        private static void ResetTimerCallback(Object o)
        {
            span3 = span1.Subtract(span2);
            span1 = span3;
           
            Console.Title = "Skycop Reinicio en: " + span3;
            
            if (span3 == TimeSpan.Zero)
            {
                System.Diagnostics.Process.Start(GetDirectory + @"\server.exe");
                // Closes the current process
                Environment.Exit(0);

            }
        }
        public static void Config()
        {
            string[] lineas = File.ReadAllLines(GetDirectory + @"\config.txt");

            ip = lineas[0].Substring(3);
            puerto = Convert.ToInt32(lineas[1].Substring(7));
            database = lineas[2].Substring(5);
            Console.WriteLine(ip);
            reset = Convert.ToInt32(lineas[3].Substring(6));
            span1 = TimeSpan.FromMinutes(reset);
            //Console.WriteLine("La app se reiniciará a los " + reset + "milisegundos."); 
            try
            {
                IPAddress[] addresslist = Dns.GetHostAddresses(ip);
                ip = addresslist[0].ToString();
            }
            catch(Exception o)
            {
                ip = "127.0.0.1";
                WriteFullLineExit("Error en ip, revisar conexión a internet.");
                span1 = TimeSpan.FromMinutes(1);//cambio el reset a un minuto, en caso de no tener internet. Y vuelve a intentar.
            }
           
        }
       
        public static async void ConsumoApi(string fecha, string hora, string patente, string latitud,
            string longitud, string velocidad, string sentido, string gps, string evento, string s1, string s2, string s3)
        {
           var postData = new Dictionary<string, string>
              {
                { "fecha"     , fecha     },
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
                { "s3"        , s3        }
              };

            var stringPayload = JsonConvert.SerializeObject(postData);
            var responseContent="";
            // Wrap our JSON inside a StringContent which then can be used by the HttpClient class
            var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");
            using (var httpClient = new HttpClient())
            {
                
                // Do the actual request and await the response
                var httpResponse = await httpClient.PostAsync("http://direccion_web", httpContent);
                
                // If the response contains content we want to read it!
                if (httpResponse.Content != null)
                {
                    responseContent = await httpResponse.Content.ReadAsStringAsync();
                    
                    WriteDetalle(responseContent);

                    if (responseContent.Contains("true"))
                    {
                        WriteFullLine("");
                        WriteFullLine("Insercion a DB OK.");
                        WriteFullLine("");
                        WriteDetalle("Esperando nuevos datos...");
                    }
                    else
                    {
                        WriteFullLineExit("");
                        WriteFullLineExit("Error en la insercion a la DB");
                        WriteFullLineExit("");
                        WriteDetalle("");
                    }

                    // From here on you could deserialize the ResponseContent back again to a concrete C# type using Json.Net
                }
            }
            

        }
        public static void paraElMonster(string upd, string app, int fecha_hora_or_descripcion_error)
        {
            string _connectionString = database;
            try
            {
                using (OleDbConnection connection = new OleDbConnection(string.Format("Provider = Microsoft.ACE.OLEDB.12.0; Data Source ={0}", _connectionString)))
                {
                    if (fecha_hora_or_descripcion_error == 1) //upd FechaHora
                    {
                        using (OleDbCommand updateCommand = new OleDbCommand("UPDATE EstadosApps SET [FechaHora] = ? WHERE [Aplicacion] = ?", connection))
                        {
                            connection.Open();

                            updateCommand.Parameters.AddWithValue("@FechaHora", upd);
                            updateCommand.Parameters.AddWithValue("@Aplicacion", app);

                            updateCommand.ExecuteNonQuery();
                        }
                    }
                    else if (fecha_hora_or_descripcion_error == 0) //upd Descripcion Error
                    {
                        using (OleDbCommand updateCommand = new OleDbCommand("UPDATE EstadosApps SET [DescripcionError] = ? WHERE [Aplicacion] = ?", connection))
                        {
                            connection.Open();

                            updateCommand.Parameters.AddWithValue("@DescripcionError", upd);
                            updateCommand.Parameters.AddWithValue("@Aplicacion", app);

                            updateCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch(Exception o)
            {
                Log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Main exception: " + o);
            }
        }
        public static void ExecuteServer(string ip, int puerto)
        {
            
            TcpListener tcpServer = null;
            UdpClient udpServer = null;
            int port = puerto;
            
            Console.WriteLine("Escuchando UDP en "+ port);
            
            try
            {
                udpServer = new UdpClient(port);
                tcpServer = new TcpListener(IPAddress.Parse(ip), port);

                var udpThread = new Thread(new ParameterizedThreadStart(UDPServerProc));
                udpThread.IsBackground = true;
                udpThread.Name = "UDP server thread";
                udpThread.Start(udpServer);

               /* var tcpThread = new Thread(new ParameterizedThreadStart(TCPServerProc));
                tcpThread.IsBackground = true;
                tcpThread.Name = "TCP server thread";
                tcpThread.Start(tcpServer);
               */

                Console.WriteLine("<X> Para finalizar Session.");
                //Console.ReadLine();
                if ((Console.ReadLine() == "x") || (Console.ReadLine() == "X")) { Console.ReadLine(); }
            }
            catch (Exception ex)
            {
                paraElMonster(ex.ToString(), "Skycop", 0);
                //Log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Main exception: " + ex);
            }
            finally
            {
                if (udpServer != null)
                    udpServer.Close();

               /* if (tcpServer != null)
                    tcpServer.Stop();
               */
            }
            WriteFullLineExit("");
            WriteFullLineExit("ESTA SEGURO DE SALIR ?");
            WriteFullLineExit("");
            WriteFullLineExit("Escribir <SI> para Salir.");
            if ((Console.ReadLine() == "si") || (Console.ReadLine() == "SI"))
            {
                endApp = true;
               
            }

           // Console.ReadLine();
        }
        static void WriteFullLine(string value)
        {
            // Write an entire line to the console with the string.
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(value.PadRight(Console.WindowWidth - 1));
            // Reset the color.
            Console.ResetColor();
        }
        static void WriteFullLineExit(string value)
        {
            // Write an entire line to the console with the string.
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(value.PadRight(Console.WindowWidth - 1));
            // Reset the color.
            Console.ResetColor();
        }
        static void WriteDetalle(string value)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(value.PadRight(Console.WindowWidth - 1));
            Console.ResetColor();
        }
        private static void UDPServerProc(object arg)
        {
            Console.WriteLine("UDP server Inicializado");
            WriteDetalle("Esperando nuevos datos...");
            Timer d = new Timer(ResetTimerCallback, null, 0, 1000);
           
            

            try
            {
                UdpClient server = (UdpClient)arg;
                IPEndPoint remoteEP;
                byte[] buffer;

                for (; ; )
                {
                    remoteEP = null;
                    buffer = server.Receive(ref remoteEP);

                    if (buffer != null && buffer.Length > 0)
                    {
                        string recibido = Encoding.ASCII.GetString(buffer);
                       
                        WriteFullLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + recibido);
                        WriteDetalle("Fecha:"+recibido.Substring(2, 6));
                        WriteDetalle("Hora:" + recibido.Substring(8, 6));
                        WriteDetalle("Patente:" + recibido.Substring(14, 7));
                        WriteDetalle("Latitud:" + recibido.Substring(21, 9));
                        WriteDetalle("Longitud:" + recibido.Substring(30, 10));
                        WriteDetalle("Velocidad:" + recibido.Substring(40, 3));
                        WriteDetalle("Sentido:" + recibido.Substring(43, 3));
                        WriteDetalle("PosGPS:" + recibido.Substring(46, 1));
                        WriteDetalle("Evento:" + recibido.Substring(47, 2));
                        // WriteDetalle("Sensor1:" + recibido.Substring(49, 3));
                        //WriteDetalle("Sensor2:" + recibido.Substring(52, 3));
                        //WriteDetalle("Sensor3:" + recibido.Substring(55, 3));
                        if (recibido.Substring(47, 2) != "01")
                        {
                            ConsumoApi(
                            recibido.Substring(2, 6),
                            recibido.Substring(8, 6),
                            recibido.Substring(14, 7),
                            recibido.Substring(21, 9),
                            recibido.Substring(30, 10),
                            recibido.Substring(40, 3),
                            recibido.Substring(43, 3),
                            recibido.Substring(46, 1),
                            recibido.Substring(47, 2),
                            "",
                            "",
                            ""
                            //recibido.Substring(49, 3),
                            //recibido.Substring(52, 3),
                            //recibido.Substring(55, 3)
                            );
                        }
                        
                       
                    }
                }
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode != 10004) // unexpected
                paraElMonster(ex.ToString(), "Skycop", 0);
                //Log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " UDPServerProc exception: " + ex);
            }
            catch (Exception ex)
            {
                paraElMonster(ex.ToString(), "Skycop", 0);
                //Log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " UDPServerProc exception: " + ex);
            }

            WriteFullLineExit("UDP server thread finished");
        }

        private static void Log(string error)
        {
            string path = (GetDirectory + @"\log.txt");
            
            StreamWriter sw = new StreamWriter(path, true);
            sw.WriteLine(error);
            sw.Close();

            
        }
        /*
        private static void TCPServerProc(object arg)
        {
            Console.WriteLine("TCP server thread started");

            try
            {
                TcpListener server = (TcpListener)arg;
                byte[] buffer = new byte[2048];
                int count;

                server.Start();

                for (; ; )
                {
                    TcpClient client = server.AcceptTcpClient();

                    using (var stream = client.GetStream())
                    {
                        while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            Console.WriteLine("TCP: " + Encoding.ASCII.GetString(buffer, 0, count));
                        }
                    }
                    client.Close();
                }
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode != 10004) // unexpected
                    Console.WriteLine("TCPServerProc exception: " + ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("TCPServerProc exception: " + ex);
            }

            Console.WriteLine("TCP server thread finished");
        }
        */

    }
}


