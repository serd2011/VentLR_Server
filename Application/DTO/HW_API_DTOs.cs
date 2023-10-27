namespace Application.DTO
{
    public class SystemStatus
    {
        public float Temperature { get; set; }
        public float CapacityOfHeater { get; set; }
        public float TargetTemperature { get; set; }
        public byte SystemState { get; set; }
        public bool Termostat { get; set; }
        public bool FanRelay { get; set; }
        public bool FCError { get; set; }
    }
}
