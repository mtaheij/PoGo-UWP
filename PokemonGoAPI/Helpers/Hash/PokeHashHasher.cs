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
using POGOLib.Official.Util.Encryption.PokeHash;
using POGOLib.Official.Util.Hash.PokeHash;
using POGOProtos.Networking.Envelopes;
using PokemonGo.RocketAPI;
using PokemonGoAPI.Helpers.Hash.PokeHash;
using Windows.UI.Popups;
using PokemonGoAPI.Helpers.Hash;
using System.Diagnostics;
using PokemonGo.RocketAPI.Helpers;
using PokemonGoAPI.Exceptions;

namespace POGOLib.Official.Util.Hash
{
    /// <summary>
    ///     This is the <see cref="IHasher"/> which uses the API
    ///     provided by https://www.pokefarmer.com/. If you want
    ///     to buy an API key, go to this url.
    ///     https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer
    /// 
    ///     Android version: 0.57.2
    ///     IOS version: 1.27.2
    /// </summary>
    public class PokeHashHasher : IHasher
    {
        public class Stat
        {
            public DateTime Timestamp { get; set; }
            public long ResponseTime { get; set; }
        }
        private List<Stat> statistics = new List<Stat>();
        public bool VerboseLog { get; set; }
        private DateTime lastPrintVerbose = DateTime.Now;


        private const string PokeHashUrl = "http://pokehash.buddyauth.com/";

        private const string PokeHashEndpoint = "api/v127_2/hash";

        private readonly Semaphore _keySelectorMutex;

        private readonly List<PokeHashAuthKey> _authKeys;

        private readonly HttpClient _httpClient;

        /// <summary>
        ///     Initializes the <see cref="PokeHashHasher"/>.
        /// </summary>
        /// <param name="authKey">The PokeHash authkey obtained from https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer. </param>
        public PokeHashHasher(string authKey) : this(new []{ authKey })
        {

        }

        /// <summary>
        ///     Initializes the <see cref="PokeHashHasher"/>.
        /// </summary>
        /// <param name="authKeys">The PokeHash authkeys obtained from https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer. </param>
        public PokeHashHasher(IEnumerable<string> authKeys)
        {
            _keySelectorMutex = new Semaphore(1, 1);
            _authKeys = new List<PokeHashAuthKey>();

            // Default RPS at 1.
            foreach (var authKey in authKeys)
            {
                var pokeHashAuthKey = new PokeHashAuthKey(authKey);
                if (_authKeys.Contains(pokeHashAuthKey))
                    throw new Exception($"{nameof(_authKeys)} already contains authkey '{authKeys}'.");

                _authKeys.Add(pokeHashAuthKey);
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(PokeHashUrl)
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("PoGo-UWP");
        }

        public Version PokemonVersion { get; } = new Version("0.57.2");

        public long Unknown25 { get; } = -816976800928766045;

        public async Task<HashResponseContent> RequestHashesAsync(HashRequestContent hashRequest)
        {
            return await InternalRequestHashesAsync(hashRequest);
        }

        private async Task<HashResponseContent> InternalRequestHashesAsync(HashRequestContent request)
        {
            // NOTE: This is really bad. Don't create new HttpClient's all the time.
            // Use a single client per-thread if you need one.
            using (var client = new System.Net.Http.HttpClient())
            {
                // The URL to the hashing server.
                // Do not include "api/v1/hash" unless you know why you're doing it, and want to modify this code.
                client.BaseAddress = new Uri("http://pokehash.buddyauth.com/");

                // By default, all requests (and this example) are in JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Set the X-AuthToken to the key you purchased from Bossland GmbH
                client.DefaultRequestHeaders.Add("X-AuthToken", SelectPokeHashKey().AuthKey);

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.ASCII, "application/json");
                // An odd bug with HttpClient. You need to set the content type again.
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                Stopwatch watcher = new Stopwatch();
                HttpResponseMessage response = null;
                watcher.Start();
                Stat stat = new Stat() { Timestamp = DateTime.Now };
                try
                {
                    response = await client.PostAsync(PokeHashEndpoint, content);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    watcher.Stop();
                    stat.ResponseTime = watcher.ElapsedMilliseconds;
                    statistics.Add(stat);
                    statistics.RemoveAll(x => x.Timestamp < DateTime.Now.AddMinutes(-1));
                    if (VerboseLog && lastPrintVerbose.AddSeconds(15) < DateTime.Now)
                    {

                        if (statistics.Count > 0)
                        {
                            lastPrintVerbose = DateTime.Now;
                            double agv = statistics.Sum(x => x.ResponseTime) / statistics.Count;
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] (HASH SERVER)  in last 1 minute  {statistics.Count} request/min , AVG: {agv:0.00} ms/request , Fastest : {statistics.Min(t => t.ResponseTime)}, Slowest: {statistics.Max(t => t.ResponseTime)}");
                        }
                    }
                }

                // TODO: Fix this up with proper retry-after when we get rate limited.
                switch (response.StatusCode)
                {
                    // All good. Return the hashes back to the caller. :D
                    case HttpStatusCode.OK:
                        return JsonConvert.DeserializeObject<HashResponseContent>(await response.Content.ReadAsStringAsync());

                    // Returned when something in your request is "invalid". Also when X-AuthToken is not set.
                    // See the error message for why it is bad.
                    case HttpStatusCode.BadRequest:
                        string responseText = await response.Content.ReadAsStringAsync();
                        if (responseText.Contains("Unauthorized"))
                        {
                            throw new HasherException("You are not authorized to use this service. Please check that your API key is correct.");
                        }
                        Console.WriteLine($"Bad request sent to the hashing server! {responseText}");
                        break;

                    // This error code is returned when your "key" is not in a valid state. (Expired, invalid, etc)
                    case HttpStatusCode.Unauthorized:
                        throw new HasherException("You are not authorized to use this service. Please check that your API key is correct.");

                    // This error code is returned when you have exhausted your current "hashes per second" value
                    // You should queue up your requests, and retry in a second.
                    case (HttpStatusCode)429:
                        Console.WriteLine($"Your request has been limited. {await response.Content.ReadAsStringAsync()}");
                        long ratePeriodEndsAtTimestamp;
                        IEnumerable<string> ratePeriodEndHeaderValues;
                        if (response.Headers.TryGetValues("X-RatePeriodEnd", out ratePeriodEndHeaderValues))
                        {
                            // Get the rate-limit period ends at timestamp in seconds.
                            ratePeriodEndsAtTimestamp = Convert.ToInt64(ratePeriodEndHeaderValues.First());
                        }
                        else
                        {
                            // If for some reason we couldn't get the timestamp, just default to 2 second wait.
                            ratePeriodEndsAtTimestamp = Utils.GetTime(false) + 2;
                        }

                        long timeToWaitInSeconds = ratePeriodEndsAtTimestamp - Utils.GetTime(false);

                        if (timeToWaitInSeconds > 0)
                            await Task.Delay((int)(timeToWaitInSeconds * 1000));  // Wait until next rate-limit period begins.

                        return await RequestHashesAsync(request);
                    default:
                        throw new HasherException($"Hash API server ({client.BaseAddress}{PokeHashEndpoint}) might be down!");
                }
            }

            return null;
        }

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
            
            using (var response = await PerformRequest(requestContent).ConfigureAwait(false))
            {
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                
                string message;

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var responseData = JsonConvert.DeserializeObject<PokeHashResponse>(responseContent);
                        Logger.Write($"HTTP Hash OK");
                        return new HashData
                        {
                            LocationAuthHash = responseData.LocationAuthHash,
                            LocationHash = responseData.LocationHash,
                            RequestHashes = responseData.RequestHashes
                                .Select(x => (ulong) x)
                                .ToArray()
                        };

                    case HttpStatusCode.BadRequest:
                        message = $"Bad request sent to the hashing server! {responseContent}";
                        break;
                    
                    case HttpStatusCode.Unauthorized:
                        message = "The auth key supplied for PokeHash was invalid.";
                        break;
                    
                    case (HttpStatusCode) 429:
                        message = $"Your request has been limited. {responseContent}";
                        break;

                    case HttpStatusCode.ServiceUnavailable:
                        message = $"The service is unavailable, highlatency(?)";
                        break;

                    default:
                        message = $"We received an unknown HttpStatusCode ({response.StatusCode})..";
                        break;
                }

                // TODO: Find a better way to let the developer know of these issues.
                message = $"[PokeHash]: {message}";

                Logger.Write(message);
                throw new PokeHashException(response, message);
            }
        }

        private int accessId;
        
        private PokeHashAuthKey SelectPokeHashKey()
        {
            PokeHashAuthKey authKey = null;
            var currentAccessId = accessId++;
            var directlyUsable = false;

            // Key Selection
            try
            {
                _keySelectorMutex.WaitOne();

                // First check, are any keys directly useable?
                foreach (var key in _authKeys)
                {
                    if (key.WaitListCount != 0 ||
                        !key.IsUsable()) continue;

                    directlyUsable = true;

                    // Increment requests because the key is directly used after this semaphore.
                    authKey = key;
                    authKey.Requests++;

                    break;
                }

                if (authKey == null)
                {
                    // Second check, search for the best candidate.
                    var waitingTime = int.MaxValue;

                    foreach (var key in _authKeys)
                    {
                        var keyWaitingTime = key.GetTimeLeft();
                        if (keyWaitingTime >= waitingTime) continue;

                        waitingTime = keyWaitingTime;
                        authKey = key;
                    }

                    if (authKey == null)
                        throw new Exception($"No {nameof(authKey)} was set.");

                    authKey.WaitListCount++;

                    Logger.Write($"[PokeHash][{currentAccessId}][{authKey.AuthKey}] Best one takes {waitingTime}s. (Waitlist: {authKey.WaitListCount}, Requests: {authKey.Requests})");
                }
            }
            finally
            {
                _keySelectorMutex.Release();
            }

            // return the auth token
            return authKey;
        }

        private Task<HttpResponseMessage> PerformRequest(HttpContent requestContent)
        {
            return Task.Run(async () =>
            {
                var currentAccessId = accessId++;
                var directlyUsable = false;

                PokeHashAuthKey authKey = null;

                // Key Selection
                try
                {
                    _keySelectorMutex.WaitOne();

                    // First check, are any keys directly useable?
                    foreach (var key in _authKeys)
                    {
                        if (key.WaitListCount != 0 ||
                            !key.IsUsable()) continue;

                        directlyUsable = true;

                        // Increment requests because the key is directly used after this semaphore.
                        authKey = key;
                        authKey.Requests++;

                        break;
                    }

                    if (authKey == null)
                    {
                        // Second check, search for the best candidate.
                        var waitingTime = int.MaxValue;

                        foreach (var key in _authKeys)
                        {
                            var keyWaitingTime = key.GetTimeLeft();
                            if (keyWaitingTime >= waitingTime) continue;

                            waitingTime = keyWaitingTime;
                            authKey = key;
                        }

                        if (authKey == null)
                            throw new Exception($"No {nameof(authKey)} was set.");

                        authKey.WaitListCount++;

                        Logger.Write($"[PokeHash][{currentAccessId}][{authKey.AuthKey}] Best one takes {waitingTime}s. (Waitlist: {authKey.WaitListCount}, Requests: {authKey.Requests})");
                    }
                }
                finally
                {
                    _keySelectorMutex.Release();
                }

                // Add the auth token to the headers
                requestContent.Headers.Add("X-AuthToken", authKey.AuthKey);

                if (directlyUsable)
                {
                    var response = await _httpClient.PostAsync(PokeHashEndpoint, requestContent).ConfigureAwait(false);

                    ParseHeaders(authKey, response.Headers);

                    return response;
                }

                // Throttle waitlist
                try
                {
                    authKey.WaitList.WaitOne();

                    //Logger.Warn("Auth key waitlist join.");

                    if (!authKey.IsUsable())
                    {
                        Logger.Write($"[PokeHash][{currentAccessId}][{authKey.AuthKey}] Cooldown of {60 - DateTime.UtcNow.Second}s. (Waitlist: {authKey.WaitListCount}, Requests: {authKey.Requests})");
                        
                        await Task.Delay(TimeSpan.FromSeconds(60 - DateTime.UtcNow.Second)).ConfigureAwait(false);
                    }

                    // A request was done in this rate period
                    authKey.Requests++;

                    var response = await _httpClient.PostAsync(PokeHashEndpoint, requestContent).ConfigureAwait(false);

                    ParseHeaders(authKey, response.Headers);

                    return response;
                }
                finally
                {
                    //Logger.Debug($"[PokeHash][{currentAccessId}][{authKey.AuthKey}] Used (Waitlist: {authKey.WaitListCount}, Requests: {authKey.Requests})");
                    //Logger.Warn("Auth key waitlist release.");

                    authKey.WaitListCount--;
                    authKey.WaitList.Release();
                }

            });
        }

        private void ParseHeaders(PokeHashAuthKey authKey, HttpHeaders responseHeaders)
        {
            if (!authKey.MaxRequestsParsed)
            {
                // If we haven't parsed the max requests yet, do that.
                IEnumerable<string> requestCountHeader;
                if (responseHeaders.TryGetValues("X-MaxRequestCount", out requestCountHeader))
                {
                    int maxRequests;

                    int.TryParse(requestCountHeader.FirstOrDefault() ?? "1", out maxRequests);

                    authKey.MaxRequests = maxRequests;
                    authKey.MaxRequestsParsed = true;
                }
            }
            
            IEnumerable<string> ratePeriodEndHeader;
            if (responseHeaders.TryGetValues("X-RatePeriodEnd", out ratePeriodEndHeader))
            {
                int secs;
                int.TryParse(ratePeriodEndHeader.FirstOrDefault() ?? "1", out secs);

                //Logger.Warn($"Resets: {TimeUtil.GetDateTimeFromSeconds(secs)}");
            }
            
            IEnumerable<string> rateRequestsRemainingHeader;
            if (responseHeaders.TryGetValues("X-RateRequestsRemaining", out rateRequestsRemainingHeader))
            {
                int remaining;
                int.TryParse(rateRequestsRemainingHeader.FirstOrDefault() ?? "1", out remaining);
                
                //Logger.Warn($"Remaining / Max: {remaining} / {authKey.MaxRequests}");
                //Logger.Warn($"Requests / ShouldBe: {authKey.Requests} / {authKey.MaxRequests - remaining}");
            }
        }

        public byte[] GetEncryptedSignature(byte[] signatureBytes, uint timestampSinceStartMs)
        {
            return PCryptPokeHash.Encrypt(signatureBytes, timestampSinceStartMs);
        }
    }
}
