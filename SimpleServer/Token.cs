using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleServer
{
    public class Token
    {
        public readonly static Token Null = new Token();
        
        internal Token() { }
        
        public bool IsNull() => this == Null;

        public static bool IsNull(Token token) => token == Null;
    }
}
