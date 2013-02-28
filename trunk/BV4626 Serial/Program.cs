using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace BV4626_Serial
{
    class Program
    {
        static void Main(string[] args)
        {
            SerialPort port = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);

            port.DtrEnable = false;
            port.RtsEnable = false;

            port.Open();

            
            port.Write("\r");


            Console.WriteLine("reading");
            int buf = port.ReadByte();
            if (buf == '*')
                Console.WriteLine("Boom!");

            port.Close();
            
            /*
            // Write available ports.
            var tPorts = SerialPort.GetPortNames();
            foreach (var sPort in tPorts)
                Console.WriteLine("Port: " + sPort);

            //Console.Read();
            var device = new BV4626("COM4");//, 9600);
            device.Open();
            
            device.RelayA = true;
            System.Threading.Thread.Sleep(100);

            device.RelayA = false;
            System.Threading.Thread.Sleep(100);

            device.Close();
            */
        }
    }
}
