using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zeo.NET;

namespace ZeoMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new PacketListener();
            listener.Start();
        }
    }

    class PacketListener
    {
        private BaseLink _baseLink;
        private Parser _parser;

        public PacketListener()
        {
            _baseLink = new BaseLink("COM4");
            _parser = new Parser();
            _baseLink.PacketReceived += _parser.HandleData;
            _parser.EventReceived += OnEvent;
            _parser.SliceReceived += OnSlice;
        }

        private void OnEvent(object sender, EventHandlerEventArgs args)
        {
            Console.WriteLine("Received event: " + args.EventType);
        }

        private void OnSlice(object sender, SliceHandlerEventArgs args)
        {
            Console.WriteLine("Slice: " + args.Timestamp);
        }

        public void Start()
        {
            _baseLink.StartReading();
        }
    }
}
