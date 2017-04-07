using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using POGOLib.Official.Logging;
using POGOLib.Official.Util.Encryption.PokeHash;
using POGOLib.Official.Util.Hash.PokeHash;
using POGOProtos.Networking.Envelopes;

namespace POGOLib.Official.Util.Hash
{
    /// <summary>
    ///     This is the <see cref="IHasher"/> which uses the API
    ///     provided by https://www.pokefarmer.com/. If you want
    ///     to buy an API key, go to this url.
    ///     https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer
    /// 
    ///     Android version: 0.51.0
    ///     IOS version: 1.21.0
    /// </summary>
    public class PokeHashHasher : IHasher
    {
        private const string PokeHashUrl = "https://pokehash.buddyauth.com/";

        private const string PokeHashEndpoint = "api/v127_4/hash";

        private readonly List<PokeHashAuthKey> _authKeys;

        private readonly HttpClient _httpClient;

        private readonly Semaphore _keySelection;

        /// <summary>
        ///     Initializes the <see cref="PokeHashHasher"/>.
        /// </summary>
        /// <param name="authKey">The PokeHash authkey obtained from https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer. </param>
        public PokeHashHasher(string authKey) : this(new[] { authKey })
        {

        }

        /// <summary>
        ///     Initializes the <see cref="PokeHashHasher"/>.
        /// </summary>
        /// <param name="authKeys">The PokeHash authkeys obtained from https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer. </param>
        public PokeHashHasher(string[] authKeys)
        {
            if (authKeys.Length == 0)
                throw new ArgumentException($"{nameof(authKeys)} may not be empty.");

            _authKeys = new List<PokeHashAuthKey>();

            // We don't want any duplicate keys.
            foreach (var authKey in authKeys)
            {
                var pokeHashAuthKey = new PokeHashAuthKey(authKey);
                if (_authKeys.Contains(pokeHashAuthKey))
                    throw new Exception($"The auth key '{authKey}' is a duplicate.");

                _authKeys.Add(pokeHashAuthKey);
            }

            // Initialize HttpClient.
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(PokeHashUrl)
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("POGOLib (https://github.com/AeonLucid/POGOLib)");

            _keySelection = new Semaphore(1, 1);
        }

        public Version PokemonVersion { get; } = new Version("0.57.4");

        public long Unknown25 { get; } = -816976800928766045;

        public async Task<HashData> GetHashDataAsync(RequestEnvelope requestEnvelope, Signature signature, byte[] locationBytes, byte[][] requestsBytes, byte[] serializedTicket)
        {
            var requestData = new PokeHashRequest
            {
                Timestamp = signature.Timestamp,
                Latitude = requestEnvelope.Latitude,
                Longitude = requestEnvelope.Longitude,
                Altitude = requestEnvelope.Accuracy, // Accuracy actually is altitude
                AuthTicket = serializedTicket,
                SessionData = signature.SessionHash.ToByteArray(),
                Requests = new List<byte[]>(requestsBytes)
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

            using (var response = await PerformRequest(requestContent))
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                string message;

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var responseData = JsonConvert.DeserializeObject<PokeHashResponse>(responseContent);

                        return new HashData
                        {
                            LocationAuthHash = responseData.LocationAuthHash,
                            LocationHash = responseData.LocationHash,
                            RequestHashes = responseData.RequestHashes
                                .Select(x => (ulong)x)
                                .ToArray()
                        };

                    case HttpStatusCode.NotFound:
                        message = $"Hashing endpoint not found!";
                        break;

                    case HttpStatusCode.BadRequest:
                        message = $"Bad request sent to the hashing server! {responseContent}";
                        break;

                    case HttpStatusCode.Unauthorized:
                        message = "The auth key supplied for PokeHash was invalid.";
                        break;

                    case (HttpStatusCode)429:
                        message = $"Your request has been limited. {responseContent}";
                        break;

                    default:
                        message = $"We received an unknown HttpStatusCode ({response.StatusCode})..";
                        break;
                }

                // TODO: Find a better way to let the developer know of these issues.
                message = $"[PokeHash]: {message}";

                Logger.Error(message);
                throw new Exception(message);
            }
        }

        private Task<HttpResponseMessage> PerformRequest(HttpContent requestContent)
        {
            return Task.Run(async () =>
            {
                PokeHashAuthKey authKey;
                var extendedSelection = false;

                // Key selection
                try
                {
                    _keySelection.WaitOne();

                    //Logger.Warn(">>> Entering key selection.");

                    var availableKeys = _authKeys.Where(x => x.Requests < x.MaxRequestCount).ToArray();
                    if (availableKeys.Length > 0)
                    {
                        authKey = availableKeys.First();
                        authKey.Requests += 1;

                        //Logger.Warn("Found available auth key.");

                        // If the auth key has not been initialized yet, we need to have control a bit longer
                        // to configure it properly.
                        if (!authKey.IsInitialized)
                            extendedSelection = true;
                    }
                    else
                    {
                        Logger.Warn("No available auth keys found.");

                        authKey = _authKeys
                            .OrderBy(x => x.RatePeriodEnd)
                            .First();

                        var sleepTime = (int)Math.Ceiling(authKey.RatePeriodEnd.Subtract(DateTime.UtcNow).TotalMilliseconds);

                        Logger.Warn($"Key selection is sleeping for {sleepTime}ms.");

                        PokehashSleeping?.Invoke(this, sleepTime);

                        await Task.Delay(sleepTime);

                        // Rate limit is over, so reset requests.
                        authKey.Requests = 0;
                        // We have to receive the new rate period end.
                        extendedSelection = true;

                        Logger.Warn("Key selection is done sleeping.");
                    }
                }
                finally
                {
                    if (!extendedSelection)
                    {
                        //Logger.Warn("<<< Exiting key selection.");

                        _keySelection.Release();
                    }
                    else
                    {
                        Logger.Warn("=== Holding key selection.");
                    }
                }

                requestContent.Headers.Add("X-AuthToken", authKey.AuthKey);

                HttpResponseMessage response = null;
                try
                {
                    response = await _httpClient.PostAsync(PokeHashEndpoint, requestContent);
                }
                catch (Exception ex)
                {
                    throw new PokeHashException(ex.Message);
                }

                // Handle response
                try
                {
                    int maxRequestCount = 150;
                    int secs = 60;
                    int remaining = maxRequestCount;

                    IEnumerable<string> requestCountHeader;
                    if (response.Headers.TryGetValues("X-MaxRequestCount", out requestCountHeader))
                    {
                        int.TryParse(requestCountHeader.FirstOrDefault() ?? "1", out maxRequestCount);
                    }

                    IEnumerable<string> ratePeriodEndHeader;
                    if (response.Headers.TryGetValues("X-RatePeriodEnd", out ratePeriodEndHeader))
                    {
                        int.TryParse(ratePeriodEndHeader.FirstOrDefault() ?? "1", out secs);

                        //Logger.Warn($"Resets: {TimeUtil.GetDateTimeFromSeconds(secs)}");
                    }

                    IEnumerable<string> rateRequestsRemainingHeader;
                    if (response.Headers.TryGetValues("X-RateRequestsRemaining", out rateRequestsRemainingHeader))
                    {
                        int.TryParse(rateRequestsRemainingHeader.FirstOrDefault() ?? "1", out remaining);

                        //Logger.Warn($"Remaining / Max: {remaining} / {authKey.MaxRequestCount}");
                        //Logger.Warn($"Requests / ShouldBe: {authKey.Requests} / {authKey.MaxRequestCount - remaining}");
                    }

                    // Use parsed headers
                    if (!authKey.IsInitialized)
                    {
                        authKey.MaxRequestCount = maxRequestCount;
                        authKey.Requests = authKey.MaxRequestCount - remaining;
                        authKey.IsInitialized = true;
                    }

                    var ratePeriodEnd = TimeUtil.GetDateTimeFromSeconds(secs);
                    if (ratePeriodEnd > authKey.RatePeriodEnd)
                    {
                        //Logger.Warn($"[AuthKey: {authKey.AuthKey}] {authKey.RatePeriodEnd} increased to {ratePeriodEnd}.");

                        authKey.RatePeriodEnd = ratePeriodEnd;
                    }

                    return response;
                }
                finally
                {
                    if (extendedSelection)
                    {
                        //Logger.Warn("<<< Exiting extended key selection.");

                        _keySelection.Release();
                    }
                }
            });
        }

        public byte[] GetEncryptedSignature(byte[] signatureBytes, uint timestampSinceStartMs)
        {
            return PCryptPokeHash.Encrypt(signatureBytes, timestampSinceStartMs);
        }

        public event EventHandler<int> PokehashSleeping;
    }
}
