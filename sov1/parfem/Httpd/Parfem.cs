using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Httpd
{
    public class Parfem
    {
        public string Id { get; set; }
        public string Naziv { get; set; }
        public string Nota { get; set; }
        public int Cena { get; set; }
        public string Akcija { get; set; }

        public override bool Equals(object obj)
        {
            return obj.Equals(this.Id);
        }
    }
}
