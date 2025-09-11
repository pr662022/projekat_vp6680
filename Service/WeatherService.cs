using System;
using System.ServiceModel;
using Common;



namespace Service
{
    public class WeatherService : IStationService
    {
        private bool _sessionStarted = false;
        private int _received = 0;

        public Ack StartSession(SessionMeta meta)
        {
            if (meta == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault("Meta is null"));

            if (string.IsNullOrWhiteSpace(meta.StationId))
                throw new FaultException<ValidationFault>(new ValidationFault("StationId is required."));

            _received = 0;
            Console.WriteLine("[Server] Prenos u toku…");
            _sessionStarted = true;
            return new Ack { Ok = true, Status = "IN_PROGRESS", Message = "Session started." };
        }

        public Ack PushSample(WeatherSample s)
        {
            if (!_sessionStarted)
                throw new FaultException<ValidationFault>(new ValidationFault("No active session. Call StartSession first."));

            if (s == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault("Sample is null."));

            if (s.Pressure <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault("Pressure must be > 0."));

            if (s.T < -90 || s.T > 60)
                throw new FaultException<ValidationFault>(new ValidationFault("T out of range [-90, 60]."));

            _received++;
            if (_received % 10 == 0)
                Console.WriteLine($"[Server] Primljeno {_received} uzoraka… (prenos u toku)");

            return new Ack { Ok = true, Status = "IN_PROGRESS", Message = "Sample accepted." };
        }

        public Ack EndSession()
        {
            if (!_sessionStarted)
                throw new FaultException<ValidationFault>(new ValidationFault("No active session to end."));

            _sessionStarted = false;
            Console.WriteLine($"[Server] Završен prenos. Ukupno primljeno: {_received}.");
            return new Ack { Ok = true, Status = "COMPLETED", Message = "Session completed." };
        }
    }
}
