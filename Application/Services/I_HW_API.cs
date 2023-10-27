using Application.DTO;

namespace Application.Services
{
    public interface I_HW_API
    {
        public void SendEnable(bool enable);
        public void SendTemperature(float temperature);
        public SystemStatus GetStatus();
    }
}
