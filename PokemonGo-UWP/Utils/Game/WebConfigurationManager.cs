using PokemonGo_UWP.Entities;
using System;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace PokemonGo_UWP.Utils.Game
{
    /// <summary>
    ///     Manager that checks a web based configuration file
    /// </summary>
    public static class WebConfigurationManager
    {
        private const string WebConfigurationFileUrl = @"https://raw.githubusercontent.com/mtaheij/PoGo-UWP/master/webconfiguration.json";

        /// <summary>
        ///     Gets the web based configuration info and returns it
        /// </summary>
        /// <returns>WebConfiguration info</returns>
        public static async Task<WebConfigurationInfo> GetWebConfiguration()
        {
            try
            {
                var httpFilter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
                httpFilter.CacheControl.ReadBehavior = Windows.Web.Http.Filters.HttpCacheReadBehavior.MostRecent;

                //download webconfiguration info
                using (var client = new HttpClient(httpFilter))
                {
                    using (var response = await client.GetAsync(new Uri(WebConfigurationFileUrl), HttpCompletionOption.ResponseContentRead))
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        if (WebConfigurationInfo.SetInstance(json))
                        {
                            return WebConfigurationInfo.Instance;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
