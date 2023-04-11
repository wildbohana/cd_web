using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Httpd
{
    public class User
    {
        public string Username { get; set; }
        public string Name { get; set; }
        public string Lastname { get; set; }

        public override bool Equals(object obj)
        {
            return obj.Equals(this.Username);
        }
    }
}
