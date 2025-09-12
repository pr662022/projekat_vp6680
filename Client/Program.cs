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

            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);
            string csvPath = Path.Combine(dataDir, "weather.csv");
            string rejectsLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rejects.log");
            string extraLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "extra.log"); ;

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

                var end = proxy.EndSession();
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
            if (parts.Length < 9)
            {
                error = $"Premalo kolona ({parts.Length}), očekivano 9";
                return false;
            }

            var ci = CultureInfo.InvariantCulture;
            if (!DateTime.TryParse(parts[0], ci, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var Date))
            {
                error = "Bad Date";
                return false;
            }
            if (!double.TryParse(parts[1], NumberStyles.Float, ci, out var Pressure)) { error = "Bad Pressure"; return false; }
            if (!double.TryParse(parts[2], NumberStyles.Float, ci, out var T)) { error = "Bad T"; return false; }
            if (!double.TryParse(parts[3], NumberStyles.Float, ci, out var Tpot)) { error = "Bad Tpot"; return false; }
            if (!double.TryParse(parts[4], NumberStyles.Float, ci, out var Tdew)) { error = "Bad Tdew"; return false; }
            if (!double.TryParse(parts[6], NumberStyles.Float, ci, out var VPmax)) { error = "Bad VPmax"; return false; }
            if (!double.TryParse(parts[7], NumberStyles.Float, ci, out var VPact)) { error = "Bad VPact"; return false; }
            if (!double.TryParse(parts[8], NumberStyles.Float, ci, out var VPdef)) { error = "Bad VPdef"; return false; }



            if (Pressure <= 0) { error = "Pressure must be > 0"; return false; }
            if (T < -90 || T > 60) { error = "T out of range [-90,60]"; return false; }

            s = new WeatherSample
            {
                Date = Date,
                Pressure = Pressure,
                T = T,
                Tpot = Tpot,
                Tdew = Tdew,
                VPmax = VPmax,
                VPact = VPact,
                VPdef = VPdef
            };
            return true;
        }
    }
}
