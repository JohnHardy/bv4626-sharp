/*
 * Copyright (C) 2013 Carl Ellis and John Hardy
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 * 
 * This software is not affiliated with ByVac (byvac.com).
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

using Alchemy;
using Alchemy.Classes;
using Alchemy.Handlers;
using System.Net;

using BV4626_Serial;

namespace BV4626Web
{

    /// <summary>
    /// A simple web socket server which allows web pages (like websocket_client.html) to control the board remotely.
    /// By default it creates a web socket server on port 81.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Store the socket server.
        /// </summary>
        private static WebSocketServer pServer = null;

        /// <summary>
        /// When we start up, create the server and bind some events.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Get a list of the ports.
            var tSerialPorts = SerialPort.GetPortNames();
            var sSerialPort = "";

            // Select what to do based on how many there are.
            switch (tSerialPorts.Length)
            {
                case 0:
                    {
                        Console.WriteLine("No BV4626 plugged in.  Press any key to exit.");
                        Console.ReadKey();
                        return;
                    }
                    break;
                case 1:
                    {
                        sSerialPort = tSerialPorts[0];
                        Console.WriteLine("Automatically selecting BV4626 on " + sSerialPort);
                    }
                    break;
                default:
                    {
                        // Serial prompt.
                        Console.WriteLine("Please select a serial port: ");
                        foreach (var port in tSerialPorts)
                            Console.WriteLine("1) : " + port);

                        // User select a serial port.
                        while (sSerialPort == "")
                        {
                            var key = Console.ReadKey(true);
                            sSerialPort = "COM" + key.KeyChar;
                            if (!tSerialPorts.Contains(sSerialPort))
                            {
                                Console.WriteLine("Bad port.  Try again!");
                                sSerialPort = "";
                            }
                        }
                    }
                    break;
            }


            // Create a web socket handler.  This creates an internal reference to a board.
            BV4626WebSocketRequestHandler pRequestHandler = new BV4626WebSocketRequestHandler(sSerialPort);

            // Create a web socket server that listens on port 81 for any IP.
            pServer = new WebSocketServer(81, IPAddress.Any)
            {
                OnReceive = pRequestHandler.OnReceive,
                OnConnect = pRequestHandler.OnConnect,
                OnDisconnect = pRequestHandler.OnDisconnect,
                TimeOut = new TimeSpan(0, 5, 0)
            };

            // Start that server.
            pServer.Start();

            // Wait fort an exit string.
            var sCommand = string.Empty;
            while (sCommand != "exit")
            {
                sCommand = Console.ReadLine();
                System.Threading.Thread.Yield();
            }

            pServer.Stop();


        }
    }
}



/* Relay board can only handle the curent for 5 relays, which brings our total to 7 */

/* AWKWARD Caveat - plug in the IO header between C-H so A and B are not plugged in. 
 * We can get around this if we power the relay board seperately */
//hand.Board.SetPinMode(BV4626.Pins.A, BV4626.PinMode.Input);
//hand.Board.SetPinMode(BV4626.Pins.B, BV4626.PinMode.Input);
//hand.Board.SetPinMode(BV4626.Pins.C, BV4626.PinMode.Input);
//hand.Board.SetPinMode(BV4626.Pins.D, BV4626.PinMode.Input);
//hand.Board.SetPinMode(BV4626.Pins.E, BV4626.PinMode.Input);
//hand.Board.SetPinMode(BV4626.Pins.F, BV4626.PinMode.Output);
//hand.Board.SetPinMode(BV4626.Pins.G, BV4626.PinMode.Output);
//hand.Board.SetPinMode(BV4626.Pins.H, BV4626.PinMode.Output);

//hand.Board.SetPinValue(BV4626.Pins.F, 255);
//hand.Board.SetPinValue(BV4626.Pins.G, 255);
//hand.Board.SetPinValue(BV4626.Pins.H, 255);