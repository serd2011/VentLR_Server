using Application.DTO;

namespace Application.Services.Impl
{
    public class HW_API : I_HW_API
    {
        Infrastructure.IVentLRSerialProtocol _ventLRSerialProtocol;

        public HW_API(Infrastructure.IVentLRSerialProtocol ventLRSerialProtocol)
        {
            _ventLRSerialProtocol = ventLRSerialProtocol;
        }

        public SystemStatus GetStatus()
        {
            return _ventLRSerialProtocol.GetStatus();
        }

        public void SendEnable(bool enable)
        {
            _ventLRSerialProtocol.SendEnable(enable);
        }

        public void SendTemperature(float temperature)
        {
            _ventLRSerialProtocol.SendTemperature(temperature);
        }
    }
}
