using System;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class WeatherSample
    {
        public WeatherSample() { }

        public WeatherSample(DateTime date, double pressure, double t, double tpot, double tdew, double vPmax, double vPact, double vPdef)
        {
            Date = date;
            Pressure = pressure;
            T = t;
            Tpot = tpot;
            Tdew = tdew;
            VPmax = vPmax;
            VPact = vPact;
            VPdef = vPdef;
        }

        [DataMember] public DateTime Date { get; set; }
        [DataMember] public double Pressure { get; set; }
        [DataMember] public double T { get; set; }
        [DataMember] public double Tpot { get; set; }
        [DataMember] public double Tdew { get; set; }
        [DataMember] public double VPmax { get; set; }
        [DataMember] public double VPact { get; set; }
        [DataMember] public double VPdef { get; set; }

    }

    [DataContract]
    public class SessionMeta
    {
        [DataMember] public string StationId { get; set; }
        [DataMember] public DateTime StartedAt { get; set; }
    }

    [DataContract]
    public class Ack
    {
        [DataMember] public bool Ok { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public string Message { get; set; }
    }

    [DataContract]
    public class DataFormatFault
    {
        public DataFormatFault() { }
        public DataFormatFault(string msg) { Message = msg; }

        [DataMember] public string Message { get; set; }
    }

    [DataContract]
    public class ValidationFault
    {
        public ValidationFault() { }
        public ValidationFault(string msg) { Message = msg; }

        [DataMember] public string Message { get; set; }
    }
}
