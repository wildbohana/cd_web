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
        // Baza podataka
        public static List<User> users = new List<User>();

        public static void StartListening()
        {
            // Lokalna IP adresa za naš server, port 8080
            IPAddress ipAddress = IPAddress.Loopback;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 8080);

            // Pravimo TCP/IP utičnicu
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // Spoj na lokalni EP, red čekanja 10
                serverSocket.Bind(localEndPoint);
                serverSocket.Listen(10);

                // Započni prisluškivanje zahteva od klijenata
                while (true)
                {
                    Console.WriteLine("Čekanje na konekciju ...");

                    // Accept je blokirajuća funkcija
                    Socket socket = serverSocket.Accept();

                    // Kada primimo zahtev, pravimo nit koja će obraditi taj zahtev
                    Task t = Task.Factory.StartNew(() => Run(socket));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPritisni ENTER da nastaviš ...");
            Console.Read();
        }

        // Obrada zahteva klijenta
        private static void Run(Socket socket)
        {
            // Pokretanje svih potrebnih stream-ova
            NetworkStream stream = new NetworkStream(socket);
            StreamReader sr = new StreamReader(stream);
            StreamWriter sw = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true };

            // Izvlači resurs iz http request-a
            string resource = GetResource(sr);

            if (resource != null)
            {
                // Podrazumevani resurs nam je početna stranica
                if (resource.Equals("")) resource = "index.html";
                Console.WriteLine("Zahtev od " + socket.RemoteEndPoint + " : " + resource + "\n");

                // Ako je zahtev za dodavanje korisnika
                if (resource.Contains("add?username="))
                {
                    // Izdvajamo podatke o korisniku kog trebamo da dodamo
                    // user[0] == "add?"
                    string[] user = resource.Split(new string[] { "username=", "name=", "lastname=" }, StringSplitOptions.None);

                    // Pripremamo odgovor od servera za klijenta
                    string responseText = "HTTP/1.0 200 OK\r\n\r\n";
                    sw.Write(responseText);

                    // Podaci o korisniku:
                    var username = GetPropertyValue(user[1]);
                    var name = GetPropertyValue(user[2]);
                    var lastname = GetPropertyValue(user[3]);

                    Console.WriteLine($"Pronadjeni Username: {username}, Name: {name}, Lastname: {lastname}");

                    // Pravimo html stranicu (dinamički element)
                    sw.Write("<html><body>");

                    // Ako nemamo username, izlistaj sve korisnike
                    if (String.IsNullOrEmpty(username))
                    {
                        sw.WriteLine(GetAllUsers());
                    }
                    else
                    {
                        // Ako već imamo tog korisnika
                        if (users.Contains(new User { Username = username }))
                        {
                            sw.Write($"<h1>Korisnik sa: {username} vec postoji.</h1>");
                        }
                        else
                        {
                            users.Add(new User { Username = username, Name = name, Lastname = lastname });
                            sw.Write($"<h1>Uspesno dodavanje: {username}</h1>");
                            sw.WriteLine(GetAllUsers());
                        }
                    }

                    // Dodajemo link do početne stranice, i potrebne zatvarajuće tagove
                    sw.WriteLine("<a href=\"/index.html\">Home</a>");
                    sw.WriteLine("</body></html>");
                }
                // Ako je zahtev za traženje korisnika
                else if (resource.Contains("find?username="))
                {
                    // Pripremamo odgovor od servera za klijenta
                    // \r\n == EOL (End of Line)
                    string responseText = "HTTP/1.0 200 OK\r\n\r\n";
                    sw.Write(responseText);

                    // Pravimo html stranicu (dinamički element)
                    sw.Write("<html><body>");

                    // Izdvajamo podatke o korisniku
                    string[] user = resource.Split(new string[] { "username=" }, StringSplitOptions.None);
                    var username = GetPropertyValue(user[1]);

                    // Ako nemamo poslat username, izlistaj sve korisnike
                    if (String.IsNullOrEmpty(username))
                    {
                        sw.WriteLine(GetAllUsers());
                    }
                    else
                    {
                        // Ako imamo korisnika u listi
                        if (users.Contains(new User { Username = username }))
                        {
                            User findUser = users.Find(u => u.Equals(username));
                            sw.Write($"<h1>Korisnik sa: {username} postoji.</h1>");
                            sw.Write($"<p>Korisnicko ime: {findUser.Username} Ime: {findUser.Name} Prezime:{findUser.Lastname}.</p>");
                        }
                        else
                        {
                            sw.Write($"<h1>Korisnik sa: {username} ne postoji.</h1>");
                        }
                    }

                    // Dodajemo link do početne stranice, i potrebne zatvarajuće tagove
                    sw.WriteLine("<a href=\"/index.html\">Home</a>");
                    sw.WriteLine("</body></html>");
                }
                // Ako je zahtev za brisanje korisnika
                else if (resource.Contains("delete?username="))
                {
                    // Pripremamo odgovor od servera za klijenta
                    string responseText = "HTTP/1.0 200 OK\r\n\r\n";
                    sw.Write(responseText);

                    // Pravimo html stranicu (dinamički element)
                    sw.Write("<html><body>");

                    // Izdvajamo podatke o korisniku
                    string[] user = resource.Split(new string[] { "username=" }, StringSplitOptions.None);
                    var username = GetPropertyValue(user[1]);

                    // Ako nam nije poslat username, izlistavamo sve korisnike
                    if (String.IsNullOrEmpty(username))
                    {
                        sw.WriteLine(GetAllUsers());
                    }
                    else
                    {
                        // Ako korisnik postoji
                        if (users.Contains(new User { Username = username }))
                        {
                            users.RemoveAll(u => u.Equals(username));
                            sw.Write($"<h1>Korisnik {username} je uklonjen.</h1>");
                        }
                        else
                        {
                            sw.Write($"<h1>Korisnik sa: {username} ne postoji.</h1>");
                        }
                    }

                    // Dodajemo link do početne stranice, i potrebne zatvarajuće tagove
                    sw.WriteLine("<a href=\"/index.html\">Home</a>");
                    sw.WriteLine("</body></html>");
                }
                // Podrazumevan odgovor - samo šalje resurs
                else
                {
                    SendResponse(resource, socket, sw);
                }
            }

            // Zatvaramo sve otvorene stram-ove
            sr.Close();
            sw.Close();
            stream.Close();

            // Gasimo utičnice
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            //return 0;
        }

        // Izvlačenje vrednosti iz stringa dobijenog u resursu
        private static string GetPropertyValue(string field)
        {
            var newField = field.Split('&')[0];

            // "Normalizacija" stringa koji obrađujemo
            newField = Uri.UnescapeDataString(newField);
            newField = newField.Replace("+", " ");

            return newField;
        }

        // Ispis svih korisnika
        private static string GetAllUsers()
        {
            // Ordered list (nabrojiva lista)
            string result = "<ol>";

            if (users.Count == 0)
            {
                result = "<h3> Lista je prazna! </h3>";
                return result;
            }

            foreach (User user in users)
                result += "<li>" + user.Username + "</li>\n";
            
            // Zatvarajući tag za listu
            result += "</ol>";

            return result;
        }

        // Nabavi resurs lol
        private static string GetResource(StreamReader sr)
        {
            string line = sr.ReadLine();
            if (line == null) return null;

            String[] tokens = line.Split(' ');

            // Prva linija HTTP zahteva: METOD /resurs HTTP/verzija
            // Obrađujemo samo GET metodu
            string method = tokens[0];
            if (!method.Equals("GET"))
                return null;
            
            // rsrc - resurs
            string rsrc = tokens[1];

            // Izbacimo znak '/' sa početka
            rsrc = rsrc.Substring(1);

            // Ignorišemo ostatak zaglavlja
            string s1;
            while (!(s1 = sr.ReadLine()).Equals(""))
                Console.WriteLine(s1);

            Console.WriteLine("Request: " + line);
            return rsrc;
        }

        // Server vraća odgovor klijentu
        private static void SendResponse(string resource, Socket socket, StreamWriter sw)
        {
            // Ako u resursu imamo bilo šta što nije slovo ili cifra, možemo to da konvertujemo u "normalan" oblik
            // resource = Uri.UnescapeDataString(resource);

            // Pripremimo putanju do našeg web root-a
            resource = "../../../" + resource;
            FileInfo fi = new FileInfo(resource);
            string responseText;

            // Ako datoteka ne postoji, vratimo kod za grešku
            if (!fi.Exists)
            {
                responseText = "" +
                    "HTTP/1.0 404 File not found\r\n" +
                    "Content-type: text/html; charset=UTF-8\r\n\r\n<b>404 Нисам пронашао:" +
                    fi.Name + 
                    "</b>";

                sw.Write(responseText);
                Console.WriteLine("Traženje resursa nije uspelo: " + fi.Name);
                return;
            }

            // Ispišemo zaglavlje HTTP odgovora
            responseText = "HTTP/1.0 200 OK\r\nContent-type: text/html; charset=UTF-8\r\n\r\n";
            sw.Write(responseText);

            // Zatim ispišemo i datoteku (dinamički generisanu stranicu)
            socket.SendFile(resource);
        }

        // Main, logično
        public static int Main(String[] args)
        {
            StartListening();
            return 0;
        }
    }
}
