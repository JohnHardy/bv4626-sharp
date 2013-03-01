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

namespace BV4626_Serial
{
    class Program
    {
        static void Main(string[] args)
        {
            // List the ports.
            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                Console.WriteLine("Available: " + port);
            }

            // If we have one serial port, use that - otherwise prompt.
            var sPort = ports.Length == 1 ? ports[0] : "";
            while (sPort == "")
            {
                var key = Console.ReadKey(true);
                sPort = "COM" + key.KeyChar;
                if (!ports.Contains(sPort))
                {
                    Console.WriteLine("Bad port.  Try again!");
                    sPort = "";
                }

            }


            Console.WriteLine("Connecting on port " + sPort);
            BV4626 board = new BV4626(sPort);  //new BV4626("COM3");

            try
            {
                // Handshake and print details.
                board.Open();
                Console.WriteLine("Device ID: " + board.DeviceID);
                Console.WriteLine("Firmware: " + board.FirmwareVersion);

                /**/
                // Click the relays.
                for (int i = 0; i < 4; ++i)
                {
                    board.RelayA = true;
                    System.Threading.Thread.Sleep(100);
                    board.RelayB = true;
                    System.Threading.Thread.Sleep(100);
                    
                    board.RelayA = false;
                    System.Threading.Thread.Sleep(100);
                    board.RelayB = false;
                    System.Threading.Thread.Sleep(100);
                }
                board.RelayA = false;
                board.RelayB = false;
                
                /*
                // Click the relays quickly.
                board.RelayA = true;
                System.Threading.Thread.Sleep(100);
                board.RelayA = false;
                System.Threading.Thread.Sleep(100);
                board.RelayB = true;
                System.Threading.Thread.Sleep(100);
                board.RelayB = false;
                */

                // Make it so we read off pin A.
                board.SetPinMode(BV4626.Pins.A, BV4626.PinMode.Input);

                // Make pin H live and set it active.
                board.SetPinMode(BV4626.Pins.H, BV4626.PinMode.Output);
                board[BV4626.Pins.H] = true;


                // Listen to pin change events.
                board.PinChanged += (object b, BV4626.Pins pin, bool state) =>
                    {
                        Console.WriteLine("Pin " + pin + " reads " + state);

                        if (pin == BV4626.Pins.A) board.RelayA = state;
                    };

                // Set a fast event poll rate and enable events.
                board.PollFrequency = 250; // 80ms is awesome
                board.EventsEnabled = true;

                // Wait for the app to die.
                while (true)
                {
                    System.Threading.Thread.Sleep(100); 
                }
            }
            catch (BV4626.SelfCheckFailedException e)
            {
                Console.WriteLine("Self Check Failed");
            }



            board.Close();
            Console.ReadLine();
        }
    }
}
