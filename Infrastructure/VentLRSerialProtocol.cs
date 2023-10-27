using System.IO.Ports;
using System.Text;
using System.Threading;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Infrastructure
{
    public class VentLRSerialProtocol : Application.Infrastructure.IVentLRSerialProtocol
    {
        private SerialPort _serialPort;

        public VentLRSerialProtocol()
        {
            _serialPort = new SerialPort("/dev/ttyUSB1", 115200, Parity.None, 8, StopBits.One); // This should be extracted into config
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            _serialPort.Open();

            configureCommandHandlers();
        }

        private Mutex requestMutex = new Mutex();
        private ManualResetEvent AckEvent = new ManualResetEvent(false);
        private ManualResetEvent RequestStatusEvent = new ManualResetEvent(false);

        public void SendEnable(bool enable)
        {
            if (!requestMutex.WaitOne(5000))
                throw new Exception("Failed to acquire the mutex in specified time");


            var res = TryCommand(Commands.StartStop, BitConverter.GetBytes((int)(enable ? 1 : 0)), 5);
            requestMutex.ReleaseMutex();
            if (!res)
                throw new Exception("Failed to enable");
        }
        public void SendTemperature(float temperature)
        {
            if (!requestMutex.WaitOne(5000))
                throw new Exception("Failed to acquire the mutex in specified time");

            var res = TryCommand(Commands.SetTempature, BitConverter.GetBytes(temperature), 5);
            requestMutex.ReleaseMutex();
            if (!res)
                throw new Exception("Failed to set tempature");
        }
        public Application.DTO.SystemStatus GetStatus()
        {
            if (!requestMutex.WaitOne(1000))
                throw new Exception("Failed to acquire the mutex in specified time");

            RequestStatusEvent.Reset();
            var res = TryCommand(Commands.RequestStatus, BitConverter.GetBytes((int)0), 5);
            if (!res)
            {
                requestMutex.ReleaseMutex();
                throw new Exception("Failed to request system status");
            }

            res = RequestStatusEvent.WaitOne(1000);
            requestMutex.ReleaseMutex();
            if (!res)
                throw new Exception("Timed out waiting for system status");

            return systemStatus;
        }

        private bool TryCommand(Commands cmd, byte[] data, int retriesCount)
        {
            int count = 0;

            while (count < retriesCount)
            {
                count++;

                AckEvent.Reset();
                Send(cmd, data);

                var res = AckEvent.WaitOne(100);
                if (res && ackResponse)
                    return true;

                if (count >= retriesCount)
                    return false;
            }

            return false;
        }

        // RECIEVED PACKET HANDLING

        bool ackResponse;
        Application.DTO.SystemStatus systemStatus = new Application.DTO.SystemStatus();

        private enum Commands : byte
        {
            Ack = 0x61,
            StatusData = 0x64,
            RequestStatus = 0x72,
            StartStop = 0x73,
            SetTempature = 0x74
        };

        Dictionary<byte, Tuple<int, Action<byte[], int>>> commandHandlers = new Dictionary<byte, Tuple<int, Action<byte[], int>>>();

        private void configureCommandHandlers()
        {
            commandHandlers.Add((byte)Commands.Ack, new Tuple<int, Action<byte[], int>>(1, HandleAck));
            commandHandlers.Add((byte)Commands.StatusData, new Tuple<int, Action<byte[], int>>(13, HandleStatusData));
        }
        private void Send(Commands cmd, byte[] data)
        {
            byte[] buf = new byte[5];
            buf[0] = (byte)cmd;
            Array.Copy(data, 0, buf, 1, 4);
            Send(buf, 5);
        }

        private void HandlePacket(byte[] data, int len)
        {

            if (!commandHandlers.ContainsKey(data[0]))
            {
                Console.WriteLine($"Unknown packet 0x{Convert.ToHexString(data, 0, 1)}");
                return;
            }

            (int expectedDataSize, var handler) = commandHandlers[data[0]];

            if (expectedDataSize != (len - 1))
            {
                Console.WriteLine($"Malformed 0x{Convert.ToHexString(data, 0, 1)} packet recieved. Size {len - 1}, expected {expectedDataSize}");
                return;
            }

            handler(data, len);
        }

        private void HandleAck(byte[] data, int len)
        {
            ackResponse = (data[1] == 1);
            AckEvent.Set();
        }

        private void HandleStatusData(byte[] data, int len)
        {
            systemStatus.TargetTemperature = System.BitConverter.ToSingle(data, 1); // first byte is command
            systemStatus.Temperature = System.BitConverter.ToSingle(data, 5);
            systemStatus.CapacityOfHeater = System.BitConverter.ToSingle(data, 9);
            systemStatus.SystemState = (Byte)((data[13] & 0x38) >> 3);
            systemStatus.Termostat = System.Convert.ToBoolean((data[13] & 0x4) >> 2);
            systemStatus.FanRelay = System.Convert.ToBoolean((data[13] & 0x2) >> 1);
            systemStatus.FCError = System.Convert.ToBoolean((data[13] & 0x1));

            RequestStatusEvent.Set();
        }

        // SERIAL PORT STUFF

        const int bufferSize = 1024;
        byte[] recieveBuf = new byte[bufferSize];
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            int bytesToRead = Math.Min(_serialPort.BytesToRead, bufferSize);
            int bytesRead = _serialPort.Read(recieveBuf, 0, bytesToRead);
            Parse(recieveBuf, bytesRead);
        }

        private void Send(byte[] data, int len)
        {
            PrepareData(data, len);
            _serialPort.Write(sendBuf, 0, sendSize);
        }

        //  LOW LEVEL PROTOCOL STUFF

        private enum SpecialBytes : byte
        {
            Start = 0xAA,
            Stop = 0xFF,
            Escape = 0x55
        };

        private byte calculate_crc8(byte[] pcBlock, int len)
        {
            byte crc = 0xFF;
            byte i;
            int j = 0;
            while (j < len)
            {
                crc ^= pcBlock[j++];

                for (i = 0; i < 8; i++)
                    crc = ((crc & 0x80) == 0x80) ? (Byte)((crc << 1) ^ 0x31) : (Byte)(crc << 1);
            }
            return crc;
        }

        int sendSize = 0;
        byte[] sendBuf = new byte[bufferSize];

        private void PrepareData(byte[] data, int len)
        {
            sendSize = 1;
            sendBuf[0] = (byte)SpecialBytes.Start;
            for (int i = 0; i < len; i++)
            {
                if (sendSize >= bufferSize - 5) // 2 for data byte and possible escape, 2 for crc and possible escape, 1 for stop
                    throw new Exception("Data was to large");

                if (Enum.IsDefined(typeof(SpecialBytes), data[i]))
                {
                    sendBuf[sendSize++] = (byte)SpecialBytes.Escape;
                    sendBuf[sendSize++] = (byte)(data[i] ^ 0x20);
                }
                else
                {
                    sendBuf[sendSize++] = data[i];
                }
            }
            byte crc = calculate_crc8(data, len);
            if (Enum.IsDefined(typeof(SpecialBytes), crc))
            {
                sendBuf[sendSize++] = (byte)SpecialBytes.Escape;
                sendBuf[sendSize++] = (byte)(crc ^ 0x20);
            }
            else
            {
                sendBuf[sendSize++] = crc;
            }
            sendBuf[sendSize++] = (byte)SpecialBytes.Stop;
        }

        // Protocol parser

        bool escaped = false;
        bool active = false;

        int recieverCount = 0;
        byte[] recieverData = new byte[bufferSize];

        private void Parse(byte[] buf, int len)
        {
            for (int i = 0; i < len; i++)
            {

                if (buf[i] == (byte)SpecialBytes.Start)
                {
                    recieverCount = 0;
                    active = true;
                    escaped = false;
                    continue;
                }

                if (buf[i] == (byte)SpecialBytes.Stop)
                {
                    active = false;
                    escaped = false;

                    if (recieverCount < 2)
                        continue; // we must have at least 1 byte of data and 1 byte of crc

                    byte crc = calculate_crc8(recieverData, (recieverCount - 1)); // there is 1 byte of crc in data
                    if (crc != recieverData[recieverCount - 1])
                        continue; // crc check failed. Discarding data                    

                    HandlePacket(recieverData, recieverCount - 1); // -1 cuz of crc

                    continue;
                }

                if (buf[i] == (byte)SpecialBytes.Escape)
                {
                    escaped = true;
                    continue;
                }

                // Protect against buffer overflow
                if (recieverCount >= bufferSize)
                {
                    active = true;
                }

                if (active)
                {                    
                    recieverData[recieverCount++] = (escaped ? (byte)(buf[i] ^ 0x20) : buf[i]);
                    escaped = false;
                }
            }
        }
    }
}
