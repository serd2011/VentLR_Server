namespace Application.Infrastructure
{
    public interface IVentLRSerialProtocol
    {
        public void SendEnable(bool enable);
        public void SendTemperature(float temperature);
        public DTO.SystemStatus GetStatus();
    }
}
