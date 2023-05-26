using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    public class Utakmica
    {
        public int ID { get; set; }
        public string Gost { get; set; }
        public string Domacin { get; set; }
        public string Pobednik { get; set; }
        public string Liga { get; set; }
    }
}