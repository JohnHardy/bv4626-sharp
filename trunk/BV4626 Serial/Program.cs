using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace BV4626_Serial
{
    class Program
    {

        const int DELAY = 100;

        static void Main(string[] args)
        {
            String buf;

            BV4626 board = new BV4626("COM7");

            try
            {
                board.Open();

                board.RelayA = true;
                board.RelayA = false;
                board.RelayB = true;
                board.RelayB = false;

                board.IOConf = board.AdjustAndReturnIOConf(BV4626.Pins.A, BV4626.PinMode.Input);
                board.IOConf = board.AdjustAndReturnIOConf(BV4626.Pins.H, BV4626.PinMode.Output);
                
                board[BV4626.Pins.H] = true;

                while (!board[BV4626.Pins.A]) 
                {
                    Console.WriteLine(board[BV4626.Pins.H].ToString() + " -> " + board[BV4626.Pins.A].ToString() );
                };

                Console.WriteLine("Button");
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
