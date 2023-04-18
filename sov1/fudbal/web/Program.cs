using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Security.Policy;

namespace web
{
    internal class Program
    {
        // Baza podataka
        public static List<Klub> Klubovi = new List<Klub>();

        // Rad servera
        public static void StartListening()
        {
            // EndPoint na http://localhost:8081/
            IPAddress ipAddress = IPAddress.Loopback;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 8081);

            // Napravi TCP soket
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bajndovanje soketa, red čekanja od 10 klijenata
            try
            {
                serverSocket.Bind(localEndPoint);
                serverSocket.Listen(10);

                // Slušanje zahteva za spajanje
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

                // Zahtev za dodavanje
                if (resource.Contains("dodaj?"))
                {
                    Console.WriteLine("Korisnik dodaje klub: ");

                    string responseText = "HTTP/1.0 200 OK\r\n\r\n";
                    sw.Write(responseText);
                    
                    int Imeind = resource.IndexOf("?naziv=") + 7;
                    int Gradind = resource.IndexOf("&grad=") + 6;
                    
                    string Ime = resource.Substring(Imeind, Gradind - 6 - Imeind);
                    string aktivan;
                    string Grad;
                    
                    if (resource.Contains("&aktivan=")) 
                    {
                        aktivan = "da";
                        Grad = resource.Substring(Gradind, resource.IndexOf("&aktivan=") - Gradind);
                    }
                    else
                    {
                        aktivan = "ne";
                        Grad = resource.Substring(Gradind, resource.Length - Gradind);
                    }

                    Klubovi.Add(new Klub(Ime, aktivan, Grad, 0.0));

                    // "Ispis" dinamičkog elementa (stranice sa tabelom)
                    sw.Write(Tabela(resource));                   
                }
                // Zahtev za unos bodova
                else if (resource.Contains("bodovi?"))
                {
                    Console.WriteLine("Korisnik unosi bodove: ");

                    string responseText = "HTTP/1.0 200 OK\r\n\r\n";
                    sw.Write(responseText);
                    
                    int Imeind = resource.IndexOf("?klub=") + 6;
                    int Bodoviind = resource.IndexOf("&bodovi=") + 8;
                    string Ime = resource.Substring(Imeind, Bodoviind - 8 - Imeind);
                    double Bodovi = double.Parse(resource.Substring(Bodoviind, resource.Length - Bodoviind));

                    foreach (Klub klub in Klubovi)
                        if (klub.klub == Ime)    
                            klub.bodovi = Bodovi;

                    // "Ispis" dinamičkog elementa (stranice sa tabelom)
                    sw.Write(Tabela(resource));
                }
                // Zahtev za izmenu kluba
                else if (resource.Contains("izmena"))
                {
                    Console.WriteLine("Korisnik trazi izmenu kluba: ");

                    string responseText = "HTTP/1.0 200 OK\r\n\r\n";
                    sw.Write(responseText);
                    
                    int Imeind = resource.IndexOf("izmena") + 6;
                    string Ime = resource.Substring(Imeind, resource.Length - Imeind);

                    Klub k = Klubovi[0];
                    foreach (Klub klub in Klubovi)
                        if (klub.klub == Ime)
                            k = klub;

                    string line = "";
                    
                    line += "<html><head><font color=\"green\" size=\"7\">Izmena podataka</font></head><body>";
                    line += $"<form name=\"izmenjen{Ime}\" action=\"izmenjen{Ime}\"><table><tr><td>Naziv: </td><td><input type=\"text\" name=\"naziv\" value=\"{k.klub}\"/></td></tr><tr><td>Grad: </td><td><select name=\"grad\">";

                    line += $"<option value=\"Novi Sad\">Novi Sad</option>";
                    line += $"<option value=\"Backa Palanka\">Backa Palanka</option>";
                    line += $"<option value=\"Beograd\">Beograd</option>";
                    line += $"<option value=\"Nis\">Nis</option>";

                    line += "</select></td></tr><tr><td>Aktivan: </td><td><input type=\"checkbox\" name=\"aktivan\"/></td></tr><tr><td></td><td><input type=\"submit\" value=\" Izmeni \"/></td></tr>";
                    line += "</table></form></body></html>";
                    
                    // "Ispis" dinamičkog elementa (stranice sa tabelom)
                    sw.Write(line);
                }
                // Prikaz izmenjenog kluba
                else if (resource.Contains("izmenjen"))
                {
                    Console.WriteLine("Korisnik je izmenio klub: ");

                    string responseText = "HTTP/1.0 200 OK\r\n\r\n";
                    sw.Write(responseText);

                    int StaroImeind = resource.IndexOf("izmenjen") + 8;
                    int NovoImeind = resource.IndexOf("?naziv=") + 7;
                    int Gradind = resource.IndexOf("&grad=") + 6;
                    string StaroIme = resource.Substring(StaroImeind, NovoImeind - 7 - StaroImeind);
                    string NovoIme = resource.Substring(NovoImeind, Gradind - 6 - NovoImeind);
                    
                    string aktivan;
                    string Grad;
                    
                    if (resource.Contains("&aktivan="))
                    {
                        aktivan = "da";
                        Grad = resource.Substring(Gradind, resource.IndexOf("&aktivan=") - Gradind);
                    }
                    else
                    {
                        aktivan = "ne";
                        Grad = resource.Substring(Gradind, resource.Length - Gradind);
                    }
                   
                    foreach (Klub klub in Klubovi)
                    {
                        if (klub.klub == StaroIme)
                        {
                            klub.klub = NovoIme;
                            klub.aktivan = aktivan;
                            klub.grad = Grad;
                            break;
                        }
                    }

                    // "Ispis" dinamičkog elementa (stranice sa tabelom)
                    sw.Write(Tabela(resource));
                }
                // Zahtev za prikaz vodećeg kluba
                else if (resource.Contains("vodeciKlub"))
                {
                    Console.WriteLine("Korisnik je zatrazio vodeci klub: ");

                    string responseText = "HTTP/1.0 200 OK\r\n\r\n";
                    sw.Write(responseText);
                    
                    Klub vodeci = Klubovi[0];
                    foreach (Klub klub in Klubovi)
                        if (klub.bodovi > vodeci.bodovi)
                            vodeci = klub;

                    string line = "";
                    line += $"<html><head><font size=\"8\">Trenutno vodeci klub: <b>{vodeci.klub}</b></font></head><body></body></html>";

                    // "Ispis" dinamičkog elementa (stranice sa klubom)
                    sw.Write(line);
                }
                else
                {
                    SendResponse(resource, socket, sw);
                }
            }

            // Zatvaranje svih tokova podataka
            sr.Close();
            sw.Close();
            stream.Close();

            // Zatvaranje soketa
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            //return 0;
        }

        public static string Tabela(string resource)
        {
            string line = "";
            int br = 1;

            line += "<html><head><font color=\"blue\" size=\"7\">Tabela</font></head><body>";
            line += "<table border=\"2\"><tr><th>#</th><th>Klub</th><th>Bodovi</th><th>Akcije</th></tr>";

            foreach (Klub klub in Klubovi)
            {
                line += $"<tr><th>{br}</th><th>{klub.klub}</th><th>{klub.bodovi}</th><th><a href=\"izmena{klub.klub}\">Izmena Podataka</a></th></tr>";
                br++;
            }

            line += "</table></br><a href=\"index.html\">Unesi novi</a></br><a href=\"vodeciKlub\">Prikazi vodeci klub</a>";
            line += "</br><h2><font color=\"blue\" size=\"6\">Upis bodova</font></h2></br>";
            line += "<form name=\"bodovi\" action=\"bodovi\"><table><tr><td>Klub: </td><td><select name=\"klub\">";

            foreach (Klub klub in Klubovi)
            {
                line += $"<option value=\"{klub.klub}\">{klub.klub}</option>";
            }

            line += "</select></td></tr><tr><td>Bodovi: </td><td><input type=\"text\" name=\"bodovi\"/></td></tr><tr><td colspan=\"2\" align=\"center\"><input type=\"submit\" value=\" Unesi \"/></td></tr>";
            line += "</table></form></body></html>";

            return line;
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

            // Ignorisemo ostatak zaglavlja
            string s1;
            while (!(s1 = sr.ReadLine()).Equals(""))
                Console.WriteLine(s1);

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
            if (!fi.Exists)
            {
                // Ako datoteka ne postoji, vratimo kod za grešku
                responseText = "" +
                    "HTTP/1.0 404 File not found\r\n" +
                    "Content-type: text/html; charset=UTF-8\r\n\r\n<b>404 Нисам нашао:" +
                    fi.Name + "</b>";

                sw.Write(responseText);
                Console.WriteLine("Could not find resource: " + fi.Name);
                return;
            }

            // Ispišemo zaglavlje HTTP odgovora
            responseText = "HTTP/1.0 200 OK\r\n\r\n";
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
