using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGoAPI.Helpers.Hash.PokeHash
{
    public class PokeHashException : Exception
    {
        public string message { get; }
        public HttpResponseMessage response { get; }

        public PokeHashException(HttpResponseMessage response, string message)
        {
            this.response = response;
            this.message = message;
        }
    }
}
