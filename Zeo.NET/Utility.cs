using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zeo.NET
{
    public class Utility
    {
        public enum EDataType
        {
            Event,
            SliceEnd,
            Version,
            Waveform,
            FrequencyBins,
            SQI,
            ZeoTimestamp, 
            Impedance,
            BadSignal,
            SleepStage
        }

        public static Dictionary<byte, EDataType> DataTypes = new Dictionary<byte, EDataType>
        { 
            { 0x00, EDataType.Event },
            { 0x02, EDataType.SliceEnd },
            { 0x03, EDataType.Version },
            { 0x80, EDataType.Waveform },
            { 0x83, EDataType.FrequencyBins },
            { 0x84, EDataType.SQI },
            { 0x8A, EDataType.ZeoTimestamp },
            { 0x97, EDataType.Impedance },
            { 0x9C, EDataType.BadSignal },
            { 0x9D, EDataType.SleepStage}
        };
    }
}
