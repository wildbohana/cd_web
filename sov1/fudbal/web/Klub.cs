using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web
{
    public class Klub
    {
        public string klub { get; set; }
        public string aktivan { get; set; }
        public string grad { get; set; }
        public double bodovi { get; set; }

        public Klub(string klub, string aktivan, string grad, double bodovi)
        {
            this.klub = klub;
            this.aktivan = aktivan;
            this.grad = grad;
            this.bodovi = bodovi;
        }
    }
}
