using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Zeo.NET;


namespace ZeoUI
{
    class ZeoControllerServiceLocator
    {
        private static ZeoController _controller;

        public static ZeoController Controller
        {
            get
            {
                if (_controller == null)
                {
                    _controller = new ZeoController();
                    _controller.Connect("COM4");
                }
                return _controller;
            }
        }
    }

    class ZeoController
    {
        private BaseLink _baseLink;
        private Parser _parser;
        private Thread _worker;

        public Parser Parser { get { return _parser; } }

        public void Connect(string comPort)
        {
            _baseLink = new BaseLink(comPort);
            _parser = new Parser();
            _baseLink.PacketReceived += _parser.HandleData;

            _worker = new Thread(_baseLink.StartReading);
            _worker.Start();
        }
    }
}
