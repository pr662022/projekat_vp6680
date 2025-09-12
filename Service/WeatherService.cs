using Common;
using System;
using System.IO;
using System.ServiceModel;
using System.Configuration;
using System.Collections.Generic;
using System.Globalization;

namespace Service
{
    public class WeatherService : IStationService
    {
        private bool _sessionStarted = false;
        private int _received = 0;
        private string _measurementsPath;
        private string _rejectsPath;
        private WeatherSample _previousSample;
        private double _pAvg = 0;
        private List<double> _pValues = new List<double>();
        private readonly double P_threshold;
        private readonly double VPact_threshold;
        private readonly double VPdef_threshold;

        public WeatherService()
        {
            P_threshold = double.Parse(ConfigurationManager.AppSettings["P_threshold"], CultureInfo.InvariantCulture);
            VPact_threshold = double.Parse(ConfigurationManager.AppSettings["VPact_threshold"], CultureInfo.InvariantCulture);
            VPdef_threshold = double.Parse(ConfigurationManager.AppSettings["VPdef_threshold"], CultureInfo.InvariantCulture);
        }

        public Ack StartSession(SessionMeta meta)
        {
            if (meta == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault("Meta is null"));

            if (string.IsNullOrWhiteSpace(meta.StationId))
                throw new FaultException<ValidationFault>(new ValidationFault("StationId is required."));

            Directory.CreateDirectory("Data");

            _measurementsPath = Path.Combine("Data", "measurements_session.csv");
            _rejectsPath = Path.Combine("Data", "rejects.csv");

            using (var ms = new StreamWriter(_measurementsPath, false))
                ms.WriteLine("Date,Pressure,T,Tpot,Tdew,VPmax,VPact,VPdef");

            using (var rs = new StreamWriter(_rejectsPath, false))
                rs.WriteLine("Date,Pressure,T,Tpot,Tdew,VPmax,VPact,VPdef,Error");

            _received = 0;
            _pValues.Clear();
            _pAvg = 0;
            _previousSample = null;
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

            if (s.Pressure <= 0 || s.T < -90 || s.T > 60)
            {
                string error = s.Pressure <= 0 ? "Pressure <= 0" : "T out of range [-90,60]";
                AppendReject(s, error);
                throw new FaultException<ValidationFault>(new ValidationFault(error));
            }

            

            AppendMeasurement(s);

            _previousSample = s;
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
            Console.WriteLine($"[Server] Završen prenos. Ukupno primljeno: {_received}.");
            return new Ack { Ok = true, Status = "COMPLETED", Message = "Session completed." };
        }

        private void AppendMeasurement(WeatherSample s)
        {
            using (var fs = new FileStream(_measurementsPath, FileMode.Append, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(fs))
            {
                writer.WriteLine($"{s.Date},{s.Pressure},{s.T},{s.Tpot},{s.Tdew},{s.VPmax},{s.VPact},{s.VPdef}");
            }
        }

        private void AppendReject(WeatherSample s, string error)
        {
            using (var fs = new FileStream(_rejectsPath, FileMode.Append, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(fs))
            {
                string line = s != null
                    ? $"{s.Date},{s.Pressure},{s.T},{s.Tpot},{s.Tdew},{s.VPmax},{s.VPact},{s.VPdef},{error}"
                    : $"NULL,,,,,,,,{error}";
                writer.WriteLine(line);
            }
        }
    }
}
