using System;
using System.IO;
using Common;

namespace Common
{
    public static class DisposalProof
    {
        public static void SimulateTransferWithCrash(string path)
        {
            try
            {
                using (var tm = new TextManipulation(path))
                {
                    tm.AddTextToFile("header: demo");
                    for (int i = 0; i < 5; i++)
                    {
                        tm.AddTextToFile($"line {i}");
                        if (i == 2) throw new IOException("Simulirani prekid veze.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sim] Greška tokom prenosa: {ex.Message}");
            }

            try
            {
                using (var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    Console.WriteLine("[Sim] Fajl je slobodan → resursi su zatvoreni (Dispose OK).");
                }
            }
            catch (IOException)
            {
                Console.WriteLine("[Sim] Fajl je i dalje zaključan → negde resurs nije zatvoren!");
            }
        }
    }
}
