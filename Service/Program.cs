using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Service
{
    internal class Program
    {
        static void Main(string[] args)
        { 

            WeatherService _transfer= new WeatherService();

            _transfer.OnTransferStarted+=(s,e)=>
            Console.WriteLine($"[Server] Prenos u toku… Station={e.Meta.StationId}, StartedAt={e.Meta.StartedAt:o}");

            _transfer.OnSampleReceived += (s, e) =>
            {
                if (e.ReceivedCount % 10 == 0)
                    Console.WriteLine($"[Server] Primljeno {e.ReceivedCount} uzoraka…");
            };

            _transfer.OnWarningRaised+=(s,e)=>
            Console.WriteLine($"[WARN] {e.Kind} ({e.Direction}) Value={e.Value} Threshold={e.Threshold} :: {e.Message}");

            _transfer.OnTransferCompleted += (s, e) =>
                Console.WriteLine($"[Server] Završен prenos. Ukupno primljeno: {e.TotalReceived}");

            using (var host = new ServiceHost(_transfer))
            {
                host.Open();
                Console.WriteLine("Service is open. Press any key to close.");
                Console.ReadKey();
            }

        }
    }
}
