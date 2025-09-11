using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.Threading;
using Common;
using Service;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {

            ChannelFactory<IStationService> factory = new ChannelFactory<IStationService>("WeatherService");

            IStationService proxy = factory.CreateChannel();

            string csvPath = @"C:\data\weather.csv";
            string rejectsLog = @"C:\data\rejects.log";
            string extraLog = @"C:\data\extra.log";

            File.WriteAllText(rejectsLog, string.Empty);
            File.WriteAllText(extraLog, string.Empty);

            var samples = ReadFirst100(csvPath, rejectsLog, extraLog);

            Console.WriteLine($"Učitano validnih redova: {samples.Count}");


            try
            {
                var ack = proxy.StartSession(new SessionMeta
                {
                    StationId = "NS-001",
                    StartedAt = DateTime.UtcNow
                });
                Console.WriteLine($"StartSession → {ack.Status} ({ack.Message})");

                foreach (var item in samples)
                {
                    proxy.PushSample(item);
                    Thread.Sleep(250);
                }

                var end= proxy.EndSession();
                Console.WriteLine($"StartSession → {end.Status} ({end.Message})");

                ((IClientChannel)proxy).Close();
                factory.Close();


            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Greška: {ex.Message}");
                ((IClientChannel)proxy).Abort();
                factory.Abort();
            }
        }

        static List<WeatherSample> ReadFirst100(string path, string rejectsLog, string extraLog)
        {
            var list = new List<WeatherSample>();
            if (!File.Exists(path))
            {
                File.AppendAllText(rejectsLog, $"[ERR] CSV ne postoji: {path}{Environment.NewLine}");
                return list;
            }

            using (var reader = new StreamReader(File.OpenRead(path)))
            {
                string header = reader.ReadLine();

                string line;
                int total = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    total++;

                    if (list.Count >= 100)
                    {
                        File.AppendAllText(extraLog, line + Environment.NewLine);
                        continue;
                    }

                    if (TryParseSample(line, out var sample, out string err))
                    {
                        list.Add(sample);
                    }
                    else
                    {
                        File.AppendAllText(rejectsLog, $"[Line {total}] {err} :: {line}{Environment.NewLine}");
                    }
                }
            }

            return list;
        }

        static bool TryParseSample(string line, out WeatherSample s, out string error)
        {
            s = null;
            error = null;

            if (string.IsNullOrWhiteSpace(line))
            {
                error = "Prazna linija";
                return false;
            }

            var parts = line.Split(',');
            if (parts.Length < 8)
            {
                error = $"Premalo kolona ({parts.Length}), očekivano 8";
                return false;
            }

            var ci = CultureInfo.InvariantCulture;
            if (!double.TryParse(parts[0], NumberStyles.Float, ci, out var T)) { error = "Bad T"; return false; }
            if (!double.TryParse(parts[1], NumberStyles.Float, ci, out var Pressure)) { error = "Bad Pressure"; return false; }
            if (!double.TryParse(parts[2], NumberStyles.Float, ci, out var Tpot)) { error = "Bad Tpot"; return false; }
            if (!double.TryParse(parts[3], NumberStyles.Float, ci, out var Tdew)) { error = "Bad Tdew"; return false; }
            if (!double.TryParse(parts[4], NumberStyles.Float, ci, out var VPmax)) { error = "Bad VPmax"; return false; }
            if (!double.TryParse(parts[5], NumberStyles.Float, ci, out var VPdef)) { error = "Bad VPdef"; return false; }
            if (!double.TryParse(parts[6], NumberStyles.Float, ci, out var VPact)) { error = "Bad VPact"; return false; }

            if (!DateTime.TryParse(parts[7], ci, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var Date))
            {
                error = "Bad Date";
                return false;
            }

            if (Pressure <= 0) { error = "Pressure must be > 0"; return false; }
            if (T < -90 || T > 60) { error = "T out of range [-90,60]"; return false; }

            s = new WeatherSample
            {
                T = T,
                Pressure = Pressure,
                Tpot = Tpot,
                Tdew = Tdew,
                VPmax = VPmax,
                VPdef = VPdef,
                VPact = VPact,
                Date = Date
            };
            return true;
        }
    }
}
