using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * 
BaseLink
--------

Listens to the data coming from the serial port connected to Zeo.

The serial port is set at baud 38400, no parity, one stop bit.
Data is sent Least Significant Byte first.

The serial protocol is: 
    * AncllLLTttsid
    
        * A  is a character starting the message
        * n  is the protocol "version", ie "4"
        * c  is a one byte checksum formed by summing the identifier byte and all the data bytes
        * ll is a two byte message length sent LSB first. This length includes the size of the data block plus the identifier.
        * LL is the inverse of ll sent for redundancy. If ll does not match ~LL, we can start looking for the start of the next block immediately, instead of reading some arbitrary number of bytes, based on a bad length.
        * T  is the lower 8 bits of Zeo's unix time.
        * tt is the 16-bit sub-second (runs through 0xFFFF in 1second), LSB first.
        * s  is an 8-bit sequence number.
        * i  is the datatype
        * d  is the array of binary data

The incoming data is cleaned up into packets containing a timestamp,
the raw data output version, and the associated data.

External code can be sent new data as it arrives by adding 
themselves to the callback list using the addCallBack function.
It is suggested, however, that external code use the ZeoParser to
organize the data into events and slices of data.
*/

using System.IO.Ports;

namespace Zeo.NET
{
    public class PacketReceivedEventArgs
    {
        public byte[] Data { get; private set; }
        public UInt32 Timestamp { get; private set; }
        public double TimestampSubsecond { get; private set; }
        public UInt32 Version { get; private set; }
        public Utility.EDataType DataType { get; private set; }

        public PacketReceivedEventArgs(UInt32 version, UInt32 timestamp, double subsecond, Utility.EDataType dataType, byte[] data)
        {
            Version = version;
            Timestamp = timestamp;
            TimestampSubsecond = subsecond;
            DataType = dataType;
            Data = data;
        }
    }

    public delegate void PacketRecievedHandler(object sender, PacketReceivedEventArgs e);

    public class BaseLink
    {
        private SerialPort _port;
        private UInt32 _timestamp;
        private UInt32 _version;

        public event PacketRecievedHandler PacketReceived;

        public BaseLink(string portName)
        {
            _port = new SerialPort(portName, 38400, Parity.None, 8);
            _port.ReadTimeout = 2000;
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            var port = (SerialPort)sender;
            var data = port.ReadExisting();
        }

        private bool ReadByte(out byte value)
        {
            var c = _port.ReadByte();
            if(c == -1)
            {
                value = 0;
                return false;
            }

            value = (byte)c;
            return true;
        }

        private bool ReadInt16(out Int16 value)
        {
            var buffer = new byte[2];
            if(_port.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                value = 0;
                return false;
            }
            value = BitConverter.ToInt16(buffer, 0);
            return true;
        }

        private bool ReadUInt32(out UInt32 value)
        {
            var buffer = new byte[4];
            if(_port.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                value = 0;
                return false;
            }
            value = BitConverter.ToUInt32(buffer, 0);
            return true;
        }

        public void StartReading()
        {
            _port.Open();

            while (true)
            {
                WaitForHeader();

                try
                {
                    ReadPacket();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to read packet: " + e);
                }
            }
        }

        private void WaitForHeader()
        {
            char[] header = new char[2];

            while (true)
            {
                try
                {
                    if (_port.Read(header, 0, header.Length) != header.Length)
                        continue;

                    if (header[0] == 'A' && header[1] == '4')
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occured while waiting for the header: " + e);
                }
            }
        }

        private void ReadPacket()
        {
            var checksum = _port.ReadByte();
            if (checksum == -1)
            {
                throw new Exception("Failed to read checksum.");
            }

            Int16 dataLength = 0;
            if (!ReadInt16(out dataLength))
            {
                throw new Exception("Failed to read data length");
            }

            Int16 invDataLength = 0;
            if (!ReadInt16(out invDataLength))
            {
                throw new Exception("Failed to read inverse data length");
            }

            Int16 invInvDataLength = (Int16)~invDataLength;
            if (dataLength != invInvDataLength)
            {
                throw new Exception("Ignoring package with corrupt length values");
            }

            byte timestampLow = 0;
            if (!ReadByte(out timestampLow))
            {
                throw new Exception("Failed to read timstamp seconds");
            }

            Int16 timestampSubseconds = 0;
            if (!ReadInt16(out timestampSubseconds))
            {
                throw new Exception("Failed to read timestamp");
            }

            double subseconds = timestampSubseconds / 65535.0;

            byte sequenceNumber = 0;
            if (!ReadByte(out sequenceNumber))
            {
                throw new Exception("Failed to read sequence number.");
            }

            byte[] data = new byte[dataLength];
            if (_port.Read(data, 0, dataLength) != dataLength)
            {
                throw new Exception("Failed to read data");
            }

            int sum = data.Sum(x => x) % 256;
            if (sum != checksum)
            {
                throw new Exception("Corrupt data detected.");
            }

            if (!Utility.DataTypes.ContainsKey(data[0]))
            {
                throw new Exception("Invalid data type");
            }

            var dataType = Utility.DataTypes[data[0]];

            if (dataType == Utility.EDataType.ZeoTimestamp)
            {
                _timestamp = BitConverter.ToUInt32(data, 1);
            }

            if (dataType == Utility.EDataType.Version)
            {
                _version = BitConverter.ToUInt32(data, 1);
            }

            if (_timestamp == 0 || _version == 0)
            {
                throw new Exception("Skipping " + dataType + " packet until version and timestamps arrive");
            }

            // Construct the full timestamp from the most recently received RTC
            // value in seconds, and the lower 8 bits of the RTC value as of
            // when this object was sent.
            UInt32 timestamp = 0;
            if ((_timestamp & 0xff) == timestampLow)
                timestamp = _timestamp;
            else if (((_timestamp - 1) & 0xff) == timestampLow)
                timestamp = _timestamp - 1;
            else if (((_timestamp + 1) & 0xff) == timestampLow)
                timestamp = _timestamp + 1;
            else
                // Something doesn't line up. Maybe unit was reset.
                timestamp = _timestamp;

            if (PacketReceived != null)
                PacketReceived(this, new PacketReceivedEventArgs(_version, timestamp, timestampSubseconds, dataType, data));
        }
    }
}
