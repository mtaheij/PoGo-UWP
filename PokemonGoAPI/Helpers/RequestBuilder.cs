using System;
using System.Linq;
using Google.Protobuf;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using static POGOProtos.Networking.Envelopes.RequestEnvelope.Types;
using System.Collections.Generic;
using POGOLib.Official.Util.Hash;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;
using Google.Protobuf.Collections;
using POGOProtos.Networking.Platform.Requests;
using POGOProtos.Networking.Platform;
using Windows.Devices.Geolocation;
using GeoCoordinatePortable;
using Troschuetz.Random;
using PokemonGoAPI.Helpers;
using Newtonsoft.Json;
using POGOProtos.Enums;
using static POGOProtos.Networking.Envelopes.Signature.Types;
using PokemonGoAPI.Helpers.Hash;

namespace PokemonGo.RocketAPI.Helpers
{
    public class RequestBuilder
    {
        private readonly double _accuracy;
        private readonly AuthTicket _authTicket;
        private readonly string _authToken;
        private readonly AuthType _authType;
        private readonly IDeviceInfo _deviceInfo;
        private readonly double _latitude;
        private readonly double _longitude;
        private readonly Random _random = new Random();
        private uint _requestId = 0;

        private Random RandomDevice;
        private TRandom TRandomDevice;
        private LehmerRng _lehmerRng;
        private readonly Client _client;
        private readonly ISettings _settings;
        private ByteString _sessionHash;
        private int _requestCount;
        private float _course;

        public RequestBuilder(string authToken, AuthType authType, double latitude, double longitude, double accuracy, IDeviceInfo deviceInfo, AuthTicket authTicket = null, Client client = null)
        {
            _authToken = authToken;
            _authType = authType;
            _latitude = latitude;
            _longitude = longitude;
            _accuracy = accuracy;
            _authTicket = authTicket;
            _deviceInfo = deviceInfo;

            RandomDevice = new Random();
            TRandomDevice = new TRandom();
            _client = client;
            _settings = _client.Settings;
            _lehmerRng = new LehmerRng();
            if (_sessionHash == null)
                GenerateNewHash();

            _requestCount = 1;
            _course = (float)GenRandom(0, 359.9);
        }

        public void GenerateNewHash()
        {
            var hashBytes = new byte[16];
            RandomDevice.NextBytes(hashBytes);

            _sessionHash = ByteString.CopyFrom(hashBytes);
        }

        public double GenRandom(double min, double max)
        {
            return RandomDevice.NextDouble() * (max - min) + min;
        }

        //public RequestEnvelope SetRequestEnvelopeUnknown6(RequestEnvelope requestEnvelope)
        //{
        //    if (_sessionHash == null)
        //    {
        //        //_sessionHash = new byte[32];
        //        _sessionHash = new byte[16];
        //        _random.NextBytes(_sessionHash);
        //    }

        //    byte[] authSeed = requestEnvelope.AuthTicket != null ?
        //        requestEnvelope.AuthTicket.ToByteArray() :
        //        requestEnvelope.AuthInfo.ToByteArray();


        //    var normAccel = new Vector(_deviceInfo.Sensors.AccelRawX, _deviceInfo.Sensors.AccelRawY, _deviceInfo.Sensors.AccelRawZ);
        //    normAccel.NormalizeVector(1.0f);//1.0f on iOS, 9.81 on Android?

        //    // Hashing code here
        //    bool UseHashServer = true;
        //    ulong LocHash1 = 0;
        //    ulong LocHash2 = 0;
        //    ByteString SessHash = null;
        //    long Unk25 = 0;
        //    ulong Timestmp = (ulong)DateTime.UtcNow.ToUnixTime();
        //    List<ulong> ReqHash = new List<ulong>();
        //    ByteString EncSig = null;

        //    if (UseHashServer)
        //    {
        //        var sign = new Signature
        //        {
        //            Timestamp = Timestmp,
        //            SessionHash = ByteString.CopyFrom(_sessionHash)
        //        };
        //        var locationBytes = new List<byte>();
        //        locationBytes.AddRange(BitConverter.GetBytes(requestEnvelope.Latitude).Reverse());
        //        locationBytes.AddRange(BitConverter.GetBytes(requestEnvelope.Longitude).Reverse());
        //        locationBytes.AddRange(BitConverter.GetBytes(requestEnvelope.Accuracy).Reverse());

        //        var requestBytes = new List<byte[]>();
        //        foreach (var req in requestEnvelope.Requests)
        //        {
        //            requestBytes.Add(req.ToByteArray());
        //        }

        //        var task = Task.Run(async () => await Client.Hasher.GetHashDataAsync(requestEnvelope, sign, locationBytes.ToArray(), requestBytes.ToArray(), authSeed).ConfigureAwait(false));
        //        var hashContent = task.WaitAndUnwrapException();

        //        LocHash1 = hashContent.LocationAuthHash;
        //        LocHash2 = hashContent.LocationHash;
        //        SessHash = ByteString.CopyFrom(_sessionHash);
        //        Unk25 = Client.Hasher.Unknown25;
        //        foreach (var req in hashContent.RequestHashes)
        //        {
        //            ReqHash.Add(req);
        //        }

        //    }
        //    else
        //    {
        //        LocHash1 = Utils.GenerateLocation1(authSeed, requestEnvelope.Latitude, requestEnvelope.Longitude,
        //                    requestEnvelope.Accuracy, _deviceInfo.VersionData.HashSeed1);
        //        LocHash2 = Utils.GenerateLocation2(requestEnvelope.Latitude, requestEnvelope.Longitude,
        //                    requestEnvelope.Accuracy, _deviceInfo.VersionData.HashSeed1);
        //        SessHash = ByteString.CopyFrom(_sessionHash);
        //        Unk25 = _deviceInfo.VersionData.VersionHash;
        //        foreach (var request in requestEnvelope.Requests)
        //        {
        //            ReqHash.Add(Utils.GenerateRequestHash(authSeed, request.ToByteArray(), _deviceInfo.VersionData.HashSeed1));
        //        }
        //    }

        //    var sig = new Signature
        //    {
        //        LocationHash1 = (int)LocHash1,
        //        LocationHash2 = (int)LocHash2,
        //        SessionHash = SessHash,
        //        Unknown25 = Unk25,
        //        Timestamp = Timestmp,
        //        TimestampSinceStart = (ulong)_deviceInfo.TimeSnapshot,
        //        SensorInfo =
        //        {
        //            new Signature.Types.SensorInfo
        //            {
        //                GravityX = normAccel.X,
        //                GravityY = normAccel.Y,
        //                GravityZ = normAccel.Z,
        //                AttitudePitch = -_deviceInfo.Sensors.AccelRawX,
        //                AttitudeYaw = -_deviceInfo.Sensors.AccelRawY,
        //                AttitudeRoll = -_deviceInfo.Sensors.AccelRawZ,
        //                LinearAccelerationX = _deviceInfo.Sensors.MagnetometerX,
        //                LinearAccelerationY = _deviceInfo.Sensors.MagnetometerY,
        //                LinearAccelerationZ = _deviceInfo.Sensors.MagnetometerZ,
        //                RotationRateX = _deviceInfo.Sensors.GyroscopeRawX,
        //                RotationRateY = _deviceInfo.Sensors.GyroscopeRawY,
        //                RotationRateZ = _deviceInfo.Sensors.GyroscopeRawZ,
        //                MagneticFieldX = _deviceInfo.Sensors.AngleNormalizedX,
        //                MagneticFieldY = _deviceInfo.Sensors.AngleNormalizedY,
        //                MagneticFieldZ = _deviceInfo.Sensors.AngleNormalizedZ,
        //                //following two values copied from Aeonlucid PoGoLib
        //                Status = 3,
        //                MagneticFieldAccuracy = -1,
        //                TimestampSnapshot = (ulong)(_deviceInfo.Sensors.TimeSnapshot - _random.Next(150, 260))
        //            }
        //        },

        //        DeviceInfo = new Signature.Types.DeviceInfo
        //        {
        //            DeviceId = _deviceInfo.DeviceID,
        //            AndroidBoardName = _deviceInfo.AndroidBoardName,
        //            AndroidBootloader = _deviceInfo.AndroidBootloader,
        //            DeviceBrand = _deviceInfo.DeviceBrand,
        //            DeviceModel = _deviceInfo.DeviceModel,
        //            DeviceModelBoot = _deviceInfo.DeviceModelBoot,
        //            DeviceModelIdentifier = _deviceInfo.DeviceModelIdentifier,
        //            FirmwareFingerprint = _deviceInfo.FirmwareFingerprint,
        //            FirmwareTags = _deviceInfo.FirmwareTags,
        //            HardwareManufacturer = _deviceInfo.HardwareManufacturer,
        //            HardwareModel = _deviceInfo.HardwareModel,
        //            FirmwareBrand = _deviceInfo.FirmwareBrand,
        //            FirmwareType = _deviceInfo.FirmwareType
        //        },

        //        ActivityStatus = _deviceInfo.ActivityStatus != null ? new Signature.Types.ActivityStatus()
        //        {
        //            Walking = _deviceInfo.ActivityStatus.Walking,
        //            Automotive = _deviceInfo.ActivityStatus.Automotive,
        //            Cycling = _deviceInfo.ActivityStatus.Cycling,
        //            Running = _deviceInfo.ActivityStatus.Running,
        //            Stationary = _deviceInfo.ActivityStatus.Stationary,
        //            Tilting = _deviceInfo.ActivityStatus.Tilting,
        //        }
        //        : null
        //    };

        //    //if (_deviceInfo.GpsSattelitesInfo.Length > 0)
        //    //{
        //    //    sig.GpsInfo = new Signature.Types.AndroidGpsInfo();
        //    //    //sig.GpsInfo.TimeToFix //currently not filled

        //    //    _deviceInfo.GpsSattelitesInfo.ToList().ForEach(sat =>
        //    //    {
        //    //        sig.GpsInfo.Azimuth.Add(sat.Azimuth);
        //    //        sig.GpsInfo.Elevation.Add(sat.Elevation);
        //    //        sig.GpsInfo.HasAlmanac.Add(sat.Almanac);
        //    //        sig.GpsInfo.HasEphemeris.Add(sat.Emphasis);
        //    //        sig.GpsInfo.SatellitesPrn.Add(sat.SattelitesPrn);
        //    //        sig.GpsInfo.Snr.Add(sat.Snr);
        //    //        sig.GpsInfo.UsedInFix.Add(sat.UsedInFix);
        //    //    });
        //    //}

        //    _deviceInfo.LocationFixes.ToList().ForEach(loc => sig.LocationFix.Add(new Signature.Types.LocationFix
        //    {
        //        Floor = loc.Floor,
        //        Longitude = loc.Longitude,
        //        Latitude = loc.Latitude,
        //        Altitude = loc.Altitude,
        //        LocationType = loc.LocationType,
        //        Provider = loc.Provider,
        //        ProviderStatus = loc.ProviderStatus,
        //        HorizontalAccuracy = loc.HorizontalAccuracy,
        //        VerticalAccuracy = loc.VerticalAccuracy,
        //        Course = loc.Course,
        //        Speed = loc.Speed,
        //        TimestampSnapshot = (ulong)loc.TimeSnapshot

        //    }));

        //    foreach (var request in requestEnvelope.Requests)
        //    {
        //        sig.RequestHash.Add(ReqHash);
        //    }

        //    // Encryption code here
        //    if (UseHashServer)
        //    {
        //        EncSig = ByteString.CopyFrom(Client.Hasher.GetEncryptedSignature(sig.ToByteArray(), (uint)_deviceInfo.TimeSnapshot));
        //    }
        //    else
        //    {
        //        EncSig = ByteString.CopyFrom(PCrypt.encrypt(sig.ToByteArray(), (uint)_deviceInfo.TimeSnapshot));
        //    }

        //    requestEnvelope.PlatformRequests.Add(new PlatformRequest
        //    {
        //        Type = POGOProtos.Networking.Platform.PlatformRequestType.SendEncryptedSignature,
        //        RequestMessage = EncSig
        //    });

        //    foreach (Request request in requestEnvelope.Requests)
        //    {
        //        RequestType requestType = request.RequestType;
        //        if (requestType == RequestType.GetMapObjects || requestType == RequestType.GetPlayer)
        //        {
        //            var plat8Message = new UnknownPtr8Request()
        //            {
        //                Message = "90f6a704505bccac73cec99b07794993e6fd5a12"
        //            };
        //            requestEnvelope.PlatformRequests.Add(new PlatformRequest
        //            {
        //                Type = PlatformRequestType.UnknownPtr8,
        //                RequestMessage = plat8Message.ToByteString()
        //            });
        //            break;
        //        }
        //    }

        //    return requestEnvelope;
        //}

        private ulong GetNextRequestId()
        {
            int rand = 0;
            if (_requestId == 0)
            {
                rand = 0x000041A7; // Initial value for Apple
                _requestId++;
            }
            else
            {
                rand = _random.Next(0, Int32.MaxValue);
            }

            _requestId++;
            return (((uint)rand | ((_requestId & 0xFFFFFFFF) >> 31)) << 32) | _requestId;
        }

        private float GetCourse()
        {
            _course = (float)TRandomDevice.Triangular(0, 359.9, _course);
            return _course;
        }

        private async Task<PlatformRequest> GenerateSignature(RequestEnvelope requestEnvelope, GeoCoordinate currentLocation)
        {
            byte[] ticketBytes = requestEnvelope.AuthTicket != null ? requestEnvelope.AuthTicket.ToByteArray() : requestEnvelope.AuthInfo.ToByteArray();

            // Common device info
            Signature.Types.DeviceInfo deviceInfo = new Signature.Types.DeviceInfo
            {
                DeviceId = _settings.DeviceId,
                DeviceBrand = _settings.DeviceBrand,
                DeviceModel = _settings.DeviceModel,
                DeviceModelBoot = _settings.DeviceModelBoot,
                HardwareManufacturer = _settings.HardwareManufacturer,
                HardwareModel = _settings.HardwareModel,
                FirmwareBrand = _settings.FirmwareBrand,
                FirmwareType = _settings.FirmwareType
            };

            // Android
            if (_client.Platform == Platform.Android)
            {
                deviceInfo.AndroidBoardName = _settings.AndroidBoardName;
                deviceInfo.AndroidBootloader = _settings.AndroidBootloader;
                deviceInfo.DeviceModelIdentifier = _settings.DeviceModelIdentifier;
                deviceInfo.FirmwareTags = _settings.FirmwareTags;
                deviceInfo.FirmwareFingerprint = _settings.FirmwareFingerprint;
            }

            var sig = new Signature
            {
                SessionHash = _sessionHash,
                Unknown25 = ((PokeHashHasher)_client.GetHasher()).Unknown25,
                Timestamp = (ulong)Utils.GetTime(true),
                TimestampSinceStart = (ulong)(Utils.GetTime(true) - _client.StartTime),
                DeviceInfo = deviceInfo
            };

            sig.SensorInfo.Add(new SensorInfo()
            {
                TimestampSnapshot = (ulong)(Utils.GetTime(true) - _client.StartTime - RandomDevice.Next(100, 500)),
                LinearAccelerationX = TRandomDevice.Triangular(-3, 1, 0),
                LinearAccelerationY = TRandomDevice.Triangular(-2, 3, 0),
                LinearAccelerationZ = TRandomDevice.Triangular(-4, 2, 0),
                MagneticFieldX = TRandomDevice.Triangular(-50, 50, 0),
                MagneticFieldY = TRandomDevice.Triangular(-60, 50, -5),
                MagneticFieldZ = TRandomDevice.Triangular(-60, 40, -30),
                MagneticFieldAccuracy = TRandomDevice.Choice(new List<int>(new int[] { -1, 1, 1, 2, 2, 2, 2 })),
                AttitudePitch = TRandomDevice.Triangular(-1.5, 1.5, 0.2),
                AttitudeYaw = GenRandom(-3, 3),
                AttitudeRoll = TRandomDevice.Triangular(-2.8, 2.5, 0.25),
                RotationRateX = TRandomDevice.Triangular(-6, 4, 0),
                RotationRateY = TRandomDevice.Triangular(-5.5, 5, 0),
                RotationRateZ = TRandomDevice.Triangular(-5, 3, 0),
                GravityX = TRandomDevice.Triangular(-1, 1, 0.15),
                GravityY = TRandomDevice.Triangular(-1, 1, -.2),
                GravityZ = TRandomDevice.Triangular(-1, .7, -0.8),
                Status = 3
            });

            Signature.Types.LocationFix locationFix = new Signature.Types.LocationFix
            {
                Provider = TRandomDevice.Choice(new List<string>(new string[] { "network", "network", "network", "network", "fused" })),
                Latitude = (float)currentLocation.Latitude,
                Longitude = (float)currentLocation.Longitude,
                Altitude = (float)currentLocation.Altitude,
                TimestampSnapshot = (ulong)(Utils.GetTime(true) - _client.StartTime - RandomDevice.Next(100, 300)),
                ProviderStatus = 3,
                LocationType = 1
            };

            if (requestEnvelope.Accuracy >= 65)
            {
                locationFix.HorizontalAccuracy = TRandomDevice.Choice(new List<float>(new float[] { (float)requestEnvelope.Accuracy, 65, 65, (int)Math.Round(GenRandom(66, 80)), 200 }));
                if (_client.Platform == Platform.Ios)
                    locationFix.VerticalAccuracy = (float)TRandomDevice.Triangular(35, 100, 65);
            }
            else
            {
                locationFix.HorizontalAccuracy = (float)requestEnvelope.Accuracy;
                if (_client.Platform == Platform.Ios)
                {
                    if (requestEnvelope.Accuracy > 10)
                        locationFix.VerticalAccuracy = (float)TRandomDevice.Choice(new List<double>(new double[] { 24, 32, 48, 48, 64, 64, 96, 128 }));
                    else
                        locationFix.VerticalAccuracy = (float)TRandomDevice.Choice(new List<double>(new double[] { 3, 4, 6, 6, 8, 12, 24 }));
                }
            }

            if (_client.Platform == Platform.Ios)
            {
                sig.ActivityStatus = new ActivityStatus();
                sig.ActivityStatus.Stationary = true;
                if (RandomDevice.NextDouble() > 0.50)
                {
                    sig.ActivityStatus.Tilting = true;
                }

                if (RandomDevice.NextDouble() > 0.95)
                {
                    // No reading for roughly 1 in 20 updates
                    locationFix.Course = -1;
                    locationFix.Speed = -1;
                }
                else
                {
                    // Course is iOS only.
                    locationFix.Course = GetCourse();

                    // Speed is iOS only.
                    locationFix.Speed = (float)TRandomDevice.Triangular(0.2, 4.25, 1);
                }
            }

            sig.LocationFix.Add(locationFix);

            string envelopString = JsonConvert.SerializeObject(requestEnvelope);

            HashRequestContent hashRequest = new HashRequestContent()
            {
                Latitude = currentLocation.Latitude,
                Longitude = currentLocation.Longitude,
                Altitude = requestEnvelope.Accuracy,
                AuthTicket = ticketBytes,
                SessionData = _sessionHash.ToByteArray(),
                Requests = new List<byte[]>(),
                Timestamp = sig.Timestamp
            };


            foreach (var request in requestEnvelope.Requests)
            {
                hashRequest.Requests.Add(request.ToByteArray());
            }

            var res = await _client.GetHasher().RequestHashesAsync(hashRequest);

            foreach (var item in res.RequestHashes)
            {
                sig.RequestHash.Add((unchecked((ulong)item)));
            }
            sig.LocationHash1 = unchecked((int)res.LocationAuthHash);
            sig.LocationHash2 = unchecked((int)res.LocationHash);

            var encryptedSignature = new RequestEnvelope.Types.PlatformRequest
            {
                Type = PlatformRequestType.SendEncryptedSignature,
                RequestMessage = new SendEncryptedSignatureRequest
                {
                    EncryptedSignature = ByteString.CopyFrom(PCrypt.encrypt(sig.ToByteArray(), (uint)_client.StartTime))
                }.ToByteString()
            };

            return encryptedSignature;
        }

        public async Task<RequestEnvelope> GetRequestEnvelopeNecro(params Request[] customRequests)
        {
            // Save the location
            GeoCoordinate currentLocation = new GeoCoordinate(_latitude, _longitude, _accuracy);

            var e = new RequestEnvelope
            {
                StatusCode = 2, //1
                RequestId = (ulong)GetNextRequestId(), //3
                Latitude = currentLocation.Latitude, //7
                Longitude = currentLocation.Longitude, //8
                Accuracy = TRandomDevice.Choice(new List<int>(new int[] { 5, 5, 5, 5, 10, 10, 10, 30, 30, 50, 65, RandomDevice.Next(66, 80) })), //9
                MsSinceLastLocationfix = _random.Next(500, 1000) //12
            };

            e.Requests.AddRange(customRequests);

            if (_authTicket != null)
            {
                e.AuthTicket = _authTicket;
            }
            else
            {
                e.AuthInfo = new AuthInfo
                {
                    Provider = _authType == AuthType.Google ? "google" : "ptc",
                    Token = new AuthInfo.Types.JWT
                    {
                        Contents = _authToken,
                        Unknown2 = 14
                    }
                }; //10
            }

            // Add UnknownPtr8Request.
            // Chat with SLxTnT - this is required for all request and needed before the main envelope.

            var plat8Message = new UnknownPtr8Request()
            {
                Message = _client.Platform8Message
                //Message = "90f6a704505bccac73cec99b07794993e6fd5a12"
            };

            e.PlatformRequests.Add(new RequestEnvelope.Types.PlatformRequest()
            {
                Type = PlatformRequestType.UnknownPtr8,
                RequestMessage = plat8Message.ToByteString()
            });

            e.PlatformRequests.Add(await GenerateSignature(e, currentLocation));

            return e;
        }

        //public RequestEnvelope GetRequestEnvelope(params Request[] customRequests)
        //{
        //    return SetRequestEnvelopeUnknown6(new RequestEnvelope
        //    {
        //        StatusCode = 2, //1

        //        RequestId = GetNextRequestId(), //3
        //        Requests = { customRequests }, //4

        //        //Unknown6 = , //6
        //        Latitude = _latitude, //7
        //        Longitude = _longitude, //8
        //        Accuracy = (int)_accuracy, //9
        //        AuthTicket = _authTicket, //11
        //        MsSinceLastLocationfix = _random.Next(500, 1000) //12
        //    });
        //}

        //public RequestEnvelope GetInitialRequestEnvelope(params Request[] customRequests)
        //{
        //    return SetRequestEnvelopeUnknown6(new RequestEnvelope
        //    {
        //        StatusCode = 2, //1

        //        RequestId = GetNextRequestId(), //3
        //        Requests = { customRequests }, //4

        //        //Unknown6 = , //6
        //        Latitude = _latitude, //7
        //        Longitude = _longitude, //8
        //        Accuracy = (int)_accuracy, //9
        //        AuthInfo = new AuthInfo
        //        {
        //            Provider = _authType == AuthType.Google ? "google" : "ptc",
        //            Token = new AuthInfo.Types.JWT
        //            {
        //                Contents = _authToken,
        //                Unknown2 = 14
        //            }
        //        }, //10
        //        MsSinceLastLocationfix = _random.Next(500, 1000) //12
        //    });
        //}

        public async Task<RequestEnvelope> GetRequestEnvelope(RequestType type, IMessage message)
        {
            return await GetRequestEnvelopeNecro(new Request
            {
                RequestType = type,
                RequestMessage = message.ToByteString()
            });
        }
    }
}