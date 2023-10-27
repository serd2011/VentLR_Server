using System.ComponentModel.DataAnnotations;

namespace API.v1.Models.Controller
{
    public class EnableRequest
    {
        public bool enable { get; set; }
    }

    public class TragetTemperatureRequest
    {
        [Range(0, 60)]
        public float temperature { get; set; }
    }

    public class StatusResponse
    {
        public struct Failures
        {
            public bool heater { get; set; }
            public bool fan { get; set; }
            public bool frequencyConverter { get; set; }
        }
        public byte state { get; set; }
        public float targetTemperature { get; set; }
        public float airTemperature { get; set; }
        public float heaterCapacity { get; set; }
        public Failures failures { get; set; }
    }
}
