using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zeo.NET
{
    public class EventHandlerBase
    {
        public DateTime Timestamp { get; set; }
        public double TimestampSubsecond { get; set; }
    }

    public class EventHandlerEventArgs : EventHandlerBase
    {
        public UInt32 EventType { get; set; }
    }

    public class SliceHandlerEventArgs : EventHandlerBase
    {
        public Slice Slice { get; set; }
    }

    public delegate void EventHandler(object sender, EventHandlerEventArgs args);
    public delegate void SliceHandler(object sender, SliceHandlerEventArgs args);

    public class Slice
    {
        public ICollection<double> Data { get; set; }
        public double[] FrequencyBins { get; set; }
        public UInt32 BadData { get; set; }
        public UInt32 SQI { get; set; }
        public double Impedance { get; set; }
        public UInt32 SleepStage { get; set; }

        public Slice()
        {
            Data = new List<double>();
            FrequencyBins = new double[8];
        }
    }

    public class Parser
    {
        public event EventHandler EventReceived;
        public event SliceHandler SliceReceived;

        private Slice _slice;

        public Parser()
        {
            _slice = new Slice();
        }

        public void HandleData(object sender, PacketReceivedEventArgs args)
        {
            if(args.Version != 3)
            {
                Console.WriteLine("Unsupport raw data output version: " + args.Version);
                return;
            }

            var timestamp = new DateTime(1970, 1, 1, 0, 0, 0, 0) + TimeSpan.FromSeconds(args.Timestamp);
            
            if(args.DataType == Utility.EDataType.Event)
            {
                if(EventReceived != null)
                {
                    var eventArgs = new EventHandlerEventArgs
                    {
                        Timestamp = timestamp,
                        TimestampSubsecond = args.TimestampSubsecond,
                        EventType = BitConverter.ToUInt32(args.Data, 1)
                    };
                }
                return;
            }

            if(args.DataType == Utility.EDataType.SliceEnd)
            {
                var sliceArgs = new SliceHandlerEventArgs
                {
                    Slice = _slice
                };

                if(SliceReceived != null)
                    SliceReceived(this, sliceArgs);

                _slice = new Slice();
                return;
            }

            if(args.DataType == Utility.EDataType.Waveform)
            {
                for(int i=1; i <= 256*2; i += 2)
                {
                    Int16 intValue = BitConverter.ToInt16(args.Data, i);
                    double realValue = (double)(intValue*315) / (double)0x8000;
                    _slice.Data.Add(realValue);
                }

                // todo: consider filtering 60Hz interference out
                return;
            }
            

            if(args.DataType == Utility.EDataType.FrequencyBins)
            {
                for(int i=0; i < 8; ++i)
                {
                    UInt16 intValue = BitConverter.ToUInt16(args.Data, 1 + (i*2));
                    double realValue = (double)intValue / (double)0x8000;
                    _slice.FrequencyBins[i] = realValue;
                }
                return;
            }

            if(args.DataType == Utility.EDataType.BadSignal)
            {
                _slice.BadData = BitConverter.ToUInt32(args.Data, 1);
                return;
            }

            if(args.DataType == Utility.EDataType.SleepStage)
            {
                _slice.SleepStage = BitConverter.ToUInt32(args.Data, 1);
                return;
            }

            if(args.DataType == Utility.EDataType.Impedance)
            {
                UInt32 intValue = BitConverter.ToUInt32(args.Data, 1);
                UInt32 inPhase = (intValue & 0x0000ffff) - 0x00008000;
                UInt32 quadrature = ((intValue & 0xffff0000) >> 16) - 0x00008000;
                if(inPhase != 0x7FFF) // 32767 indicates the impedance is bad
                {
                    double impSquared = (inPhase * inPhase) + (quadrature * quadrature);
                    _slice.Impedance = Math.Sqrt(impSquared);
                }
                return;
            }

            if(args.DataType == Utility.EDataType.SQI)
            {
                _slice.SQI = BitConverter.ToUInt32(args.Data, 1);
                return;
            }

        }

    }
}
