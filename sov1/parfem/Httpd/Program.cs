using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Httpd
{
    class Program
    {
        // Lista parfema
        public static List<Parfem> parfemi = new List<Parfem>();

        // Rad servera
        public static void StartListening()
        {
            // Localhost, port 8080
            IPAddress ipAddress = IPAddress.Loopback;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 8080);

            // Otvaranje TCP utičice
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bajndovanje soketa, red čekanja od 10 klijenata
            try
            {
                serverSocket.Bind(localEndPoint);
                serverSocket.Listen(10);

                // Prisluškivanje zahteva za povezivanje
                while (true)
                {
                    Console.WriteLine("Čekanje na konekciju...");

                    Socket socket = serverSocket.Accept();
                    Task t = Task.Factory.StartNew(() => Run(socket));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPritisni ENTER za nastavak...");
            Console.Read();
        }

        private static void Run(Socket socket)
        {
            // Otvaranje potrebnih tokova podataka
            NetworkStream stream = new NetworkStream(socket);
            StreamReader sr = new StreamReader(stream);
            StreamWriter sw = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true };

            string resource = GetResource(sr);

            if (resource != null)
            {
                // Podrazumevana stranica je index.html
                if (resource.Equals("")) resource = "index.html";

                Console.WriteLine("Zahtev od " + socket.RemoteEndPoint + ": " + resource + "\n");

                // Nema zahteva
                if (resource.StartsWith("index.html"))
                {
                    SendResponse(resource, socket, sw);
                }
                // Zahtev za dodavanje
                else if (resource.StartsWith("dodaj?id="))
                {
                    string[] tekst = resource.Split(new string[] { "id=", "naziv=", "nota=", "cena=", "akcija=" }, StringSplitOptions.None);
                    string responseText = "HTTP/1.0 200 OK\r\n\r\n";
                    sw.Write(responseText);

                    var id = GetPropertyValue(tekst[1]);
                    var naziv = GetPropertyValue(tekst[2]);
                    var nota = GetPropertyValue(tekst[3]);
                    int cena = 0;                        
                    var akcija = "";

                    if (!String.IsNullOrEmpty(tekst[4]))
                        cena = int.Parse(GetPropertyValue(tekst[4]));

                    // Ako je štiklirana akcija, URL će u sebi sadržati akcija=on, ako nije, neće ništa imati
                    akcija = resource.Contains("akcija") ? "Da" : "Ne";

                    // Početak html stranice
                    sw.Write("<html><body>");

                    // Ako nismo upisali ID, ispisaće nam celu tabelu
                    if (String.IsNullOrEmpty(id))
                    {
                        sw.Write(Tabela());
                    }
                    else
                    {
                        bool nadjen = false;

                        foreach (Parfem p in parfemi)
                        {
                            if (p.Id.Equals(id))
                            {
                                sw.Write($"Parfem sa ID-om: {id} vec postoji.");
                                nadjen = true;
                                break;
                            }
                        }

                        if (nadjen == false)
                            parfemi.Add(new Parfem { Id = id, Naziv = naziv, Nota = nota, Cena = cena, Akcija = akcija });

                        sw.Write(Tabela());
                    }

                    sw.Write("</body></html>");            
                }
                // Zahtev za pretragu
                else if (resource.Contains("find?"))
                {
                    string[] rez = resource.Split(new string[] { "cena=" }, StringSplitOptions.None);
                    int cena = int.Parse(rez[1]);

                    string responseText = "HTTP/1.0 200 OK\r\n\r\n";
                    sw.Write(responseText);
                    sw.Write("<html><body>");

                    if (parfemi.Count == 0)
                    {
                        sw.Write("<h3>Niste uneli nista</h3>");
                    }
                    else
                    {
                        bool pronadjen = false;

                        foreach (Parfem p in parfemi)
                        {
                            if (p.Cena <= cena)
                            {
                                sw.Write($"<h5>Pronadjen je parfem {p.Naziv} sa cenom {p.Cena}</h5>");
                                pronadjen = true;
                            }                           
                        }

                        if (pronadjen == false)
                        {
                            sw.Write($"<h3>Parfem sa manjom cenom od {cena} ne postoji</h3>");
                        }

                        sw.Write(Tabela());
                    }

                    sw.Write("</body></html>");
                }
                else
                {
                    SendResponse(resource, socket, sw);
                }
            }

            // Zatvaranje otvorenih tokova podataka
            sr.Close();
            sw.Close();
            stream.Close();

            // Zatvaranje utičnica
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        private static string GetPropertyValue(string field)
        {
            var newField = field.Split('&')[0];
            newField = Uri.UnescapeDataString(newField);
            newField = newField.Replace("+", " ");

            return newField;
        }

        private static string Tabela()
        {
            string result = "";
            result += "<table border=\"2\">";

            result += "<tr><th colspan=\"6\"> Parfemi </th></tr>";
            result += "<tr><th> Rbr </th><th> Id </th><th> Naziv </th><th> Mirisna nota </th><th> Cena </th><th> Na akciji? </th></tr>";

            int brojac = 1;
            foreach (Parfem p in parfemi)
            {
                result += $"<tr> <td>{brojac}</td> <td>{p.Id}</td> <td>{p.Naziv}</td> <td>{p.Nota}</td> <td>{p.Cena}</td> <td>{p.Akcija}</td> </tr>";
                brojac++;
            }

            result += "</table>";
            result += "<a href=\"/index.html\">Nazad</a>";

            return result;
        }

        private static string Lista()
        {
            string rez = "";
            rez += "<ol>";

            foreach (Parfem p in parfemi)
            {
                rez += $"<li> {p.Naziv} {p.Cena} </li>";
            }

            rez += "</ol>";
            rez += "<a href=\"/index.html\">Nazad</a>";

            return rez;
        }

        private static string GetResource(StreamReader sr)
        {
            string line = sr.ReadLine();

            if (line == null) return null;

            String[] tokens = line.Split(' ');

            // Prva linija HTTP zahteva: METOD /resurs HTTP/verzija
            // Obrađujemo samo GET metodu
            string method = tokens[0];

            if (!method.Equals("GET"))
            {
                return null;
            }

            string rsrc = tokens[1];

            // Izbacimo znak '/' sa pocetka
            rsrc = rsrc.Substring(1);

            // Ignorišemo ostatak zaglavlja
            string s1;
            while (!(s1 = sr.ReadLine()).Equals("")) Console.WriteLine(s1);

            Console.WriteLine("Request: " + line);
            return rsrc;
        }

        private static void SendResponse(string resource, Socket socket, StreamWriter sw)
        {
            // Ako u resource-u imamo bilo šta što nije slovo ili cifra, možemo da konvertujemo u "normalan" oblik
            resource = Uri.UnescapeDataString(resource);

            // Pripremimo putanju do našeg web root-a
            resource = "../../../" + resource;
            FileInfo fi = new FileInfo(resource);

            string responseText;

            // Ako datoteka ne postoji, vratimo kod za grešku
            if (!fi.Exists)
            {
                responseText = "" +
                    "HTTP/1.0 404 File not found\r\n" +
                    "Content-type: text/html; charset=UTF-8\r\n\r\n<b>404 Нисам нашао:" +
                    fi.Name + "</b>";

                sw.Write(responseText);
                Console.WriteLine("Could not find resource: " + fi.Name);
                return;
            }

            // Ispišemo zaglavlje HTTP odgovora
            responseText = "HTTP/1.0 200 OK\r\nContent-type: text/html; charset=UTF-8\r\n\r\n";
            sw.Write(responseText);

            // A zatim i datoteku
            socket.SendFile(resource);
        }

        public static int Main(String[] args)
        {
            StartListening();
            return 0;
        }
    }
}
