using System;
using System.ServiceModel;
using Common;



namespace Service
{
    public class WeatherService : IStationService
    {
        private bool _sessionStarted = false;

        public Ack StartSession(SessionMeta meta)
        {
            if (meta == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault("Meta is null"));

            if (string.IsNullOrWhiteSpace(meta.StationId))
                throw new FaultException<ValidationFault>(new ValidationFault("StationId is required."));

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


            return new Ack { Ok = true, Status = "IN_PROGRESS", Message = "Sample accepted." };
        }

        public Ack EndSession()
        {
            if (!_sessionStarted)
                throw new FaultException<ValidationFault>(new ValidationFault("No active session to end."));

            _sessionStarted = false;
            return new Ack { Ok = true, Status = "COMPLETED", Message = "Session completed." };
        }
    }
}
