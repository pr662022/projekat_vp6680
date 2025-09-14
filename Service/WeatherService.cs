using Common;
using System;
using System.IO;
using System.ServiceModel;
using System.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Policy;

namespace Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]

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

        public event EventHandler<TransferStartedEventArgs> OnTransferStarted;
        public event EventHandler<SampleReceivedEventArgs> OnSampleReceived;
        public event EventHandler<TransferCompletedEventArgs> OnTransferCompleted;
        public event EventHandler<WarningRaisedEventArgs> OnWarningRaised;


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

            OnTransferStarted?.Invoke(this, new TransferStartedEventArgs { Meta = meta });
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

            if (_previousSample != null)
            {
                double deltaP = Math.Abs(s.Pressure - _previousSample.Pressure);
                if (deltaP > P_threshold)
                    Console.WriteLine($"[Event] PressureSpike: {deltaP:+0.00;-0.00} hPa");

                double deltaVPact = Math.Abs(s.VPact - _previousSample.VPact);
                if (deltaVPact > VPact_threshold)
                    Console.WriteLine($"[Event] VPActSpike: {deltaVPact:+0.00;-0.00}");

                double deltaVPdef = Math.Abs(s.VPdef - _previousSample.VPdef);
                if (deltaVPdef > VPdef_threshold)
                    Console.WriteLine($"[Event] VPdefSpike: {deltaVPdef:+0.00;-0.00}");
            }

            _pValues.Add(s.Pressure);
            _pAvg = 0;
            foreach (var v in _pValues) _pAvg += v;
            _pAvg /= _pValues.Count;

            if (s.Pressure < 0.75 * _pAvg || s.Pressure > 1.25 * _pAvg)
                Console.WriteLine($"[Warning] OutOfBandPressure: {s.Pressure} hPa (mean={_pAvg:0.00})");

            AppendMeasurement(s);

            _previousSample = s;
            _received++;
            if (_received % 10 == 0)
                Console.WriteLine($"[Server] Primljeno {_received} uzoraka… (prenos u toku)");

            OnSampleReceived?.Invoke(this, new SampleReceivedEventArgs { Sample = s, ReceivedCount = _received });
            return new Ack { Ok = true, Status = "IN_PROGRESS", Message = "Sample accepted." };
        }

        public Ack EndSession()
        {
            if (!_sessionStarted)
                throw new FaultException<ValidationFault>(new ValidationFault("No active session to end."));

            
            _sessionStarted = false;
            Console.WriteLine($"[Server] Završen prenos. Ukupno primljeno: {_received}.");

            OnTransferCompleted?.Invoke(this, new TransferCompletedEventArgs { TotalReceived = _received });
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

    public class TransferStartedEventArgs : EventArgs { public SessionMeta Meta { get; set; } }
    public class SampleReceivedEventArgs : EventArgs { public WeatherSample Sample { get; set; } public int ReceivedCount { get; set; } }
    public class TransferCompletedEventArgs : EventArgs { public int TotalReceived { get; set; } }
    public class WarningRaisedEventArgs : EventArgs { public string Kind { get; set; } public string Direction { get; set; } public double Value { get; set; } public double Threshold { get; set; } public string Message { get; set; } }
}
