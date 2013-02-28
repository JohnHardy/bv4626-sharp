using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO.Ports;


namespace BV4626_Serial
{
    /// <summary>
    /// 
    /// </summary>
    /// <author></author>
    /// <date>27 Feb 2013</date>
    public class BV4626// : IDisposable
    {
        #region Inner Classes
        /// <summary>
        /// Define a pin mode.
        /// </summary>
        public enum PinMode
        {
            /// <summary>
            /// The pin is an output pin.
            /// </summary>
            Output = 0,

            /// <summary>
            /// The pin is an input pin.
            /// </summary>
            Input = 1,
        }

        /// <summary>
        /// An enumeration of all digital IO pins.
        /// </summary>
        public enum Pins
        {
            A = 0,
            B = 1,
            C = 2,
            D = 3,
            E = 4,
            F = 5,
            G = 6,
            H = 7,
        }
        #endregion

        /// <summary>
        /// The serial port wrapper object.
        /// </summary>
        protected SerialPort Serial = null;

        //protected const char CR = 0x015;
        protected const char ESC = '\x033';
        protected const char ACK = '\x006';


        /// <summary>
        /// Construct a new BV4626 class which talks to a device on a specified COM port.
        /// </summary>
        /// <param name="port">The COM port to talk on.  e.g. "COM3"</param>
        public BV4626(String port, int baudrate = 115200, Parity parity = Parity.None)
        {
            // Create the serial port.
            Serial = new SerialPort(port);//, baudrate, parity);//, 8, StopBits.One);
        }

        /// <summary>
        /// Open the serial communications with the BV4626.
        /// </summary>
        public void Open()
        {
            //Serial.DtrEnable = true;
            //Serial.RtsEnable = true;

            //Serial.DtrEnable = true;
            //Serial.DtrEnable = false;
            //System.Threading.Thread.Sleep(50);

            // Open it.
            Serial.Open();

            // Handshake.
            Handshake();
        }

        /// <summary>
        /// Close the serial communication with the BV4626.
        /// </summary>
        public void Close()
        {
            // Close the serial port.
            Serial.Close();
        }

        /// <summary>
        /// Is this serial port open and ready to communicate.
        /// </summary>
        public bool IsOpen { get { return Serial.IsOpen; } }

        #region Helper Methods
        /// <summary>
        /// Perfrom a handshake.
        /// </summary>
        /// <remarks>For this device, '*' is sent back only once so a second handshake attempt will produce an error.</remarks>
        protected void Handshake()
        {
            /*
            //Serial.BaseStream.Flush();
            Serial.Write(new byte[] {13}, 0, 1);
            //System.Threading.Thread.Sleep(500);

            // Read in.
            var count = Serial.Read(tBuffer, 0, 1);
            if (count == 0)
                throw new Exception("No data");


            if (tBuffer[0] != '*')
                throw new Exception("Error in first time handshake.");
            */

            Serial.Write("\r");

            var b = new byte[512];
            var count = Serial.Read(b, 0, 1);
            if (count == 0)
                throw new Exception("WWdfjslkjf");
            if (b[0] == (byte)'*')
                Console.WriteLine("boom");
        }

        protected void SendCommand(String command)
        {
            Serial.Write(ESC + command);
            ReadToACK();
        }


        protected byte[] tBuffer = new byte[512];

        protected string SendCommandRead(String command)
        {
            // Push the command.
            Serial.Write(ESC + command);

            // Output buffer.
            var sb = new StringBuilder();

            // While we have not recieved an ack, read.
            while (true)
            {
                // Read in.
                var count = Serial.Read(tBuffer, 0, 1);
                if (count == 0)
                    throw new Exception("No data");

                // Break out on ack.
                if (tBuffer[0] == ACK)
                    break;

                // Append if not an ack.
                sb.Append((char)tBuffer[0]);
            }

            // Return the result.
            return sb.ToString();
        }

        protected void ReadToACK()
        {
            // Read the response from the buffer.
            var ack = Serial.ReadChar();

            // If its not ACK, error.
            if (ack != ACK)
                throw new Exception("Command problem.  Bad ACK.");
        }
        #endregion

        #region Public Interface
        #region Relays
        /// <summary>
        /// Read the state or Turn ON/OFF RelayA.  
        /// </summary>
        public bool RelayA
        {
            get
            {
                return _RelayA;
            }
            set
            {
                // If we want to turn the relay on.
                if (value)
                {
                    SendCommand("[1A");
                    _RelayA = value;
                }
                else
                {
                    SendCommand("[0A");
                    _RelayA = value;
                }
            }
        }
        protected bool _RelayA = false;


        /// <summary>
        /// Read the state or Turn ON/OFF RelayB.  
        /// </summary>
        public bool RelayB
        {
            get
            {
                return _RelayB;
            }
            set
            {
                // If we want to turn the relay on.
                if (value)
                {
                    SendCommand("[1B");
                    _RelayB = value;
                }
                else
                {
                    SendCommand("[0B");
                    _RelayB = value;
                }
            }
        }
        protected bool _RelayB = false;
        #endregion

        #region Device
        /// <summary>
        /// Return the device ID.
        /// </summary>
        public String ID { get { return SendCommandRead("[?31d"); } }

        /// <summary>
        /// Return the firmware version.
        /// </summary>
        public String FirmwareVersion { get { return SendCommandRead("[?31f"); } }
        #endregion

        #region ADC
        /// <summary>
        /// Get the value of the specified channel of the Analogue to Digital converter.
        /// </summary>
        /// <param name="channel">An integer value of range 0 to 3.</param>
        /// <returns></returns>
        public String ADC(int channel)
        {
            switch (channel)
            {
                case 0: return SendCommandRead("[0D");
                case 1: return SendCommandRead("[1D");
                case 2: return SendCommandRead("[2D");
                //case 3: return SendCommandRead("[3D");
            }
            throw new ArgumentOutOfRangeException("ADC channel range is 0 to 3.");
        }

        /// <summary>
        /// Set the voltage reference for the ADC.
        /// 0 = 1.02V, 1 = 2.048V, 2 = 4.096V
        /// </summary>
        /// <param name="setting">The voltage setting.  Range 0 to 3.  0 = 1.02V, 1 = 2.048V, 2 = 4.096V</param>
        public void ADCVoltage(int setting)
        {
            switch (setting)
            {
                case 0: { SendCommand("[0V"); break; }
                case 1: { SendCommand("[1V"); break; }
                case 2: { SendCommand("[2V"); break; }
            }
            throw new ArgumentOutOfRangeException("ADC channel range is 0 to 3.");
        }
        #endregion

        #region DAC
        /// <summary>
        /// Set the X channel on the DAC (Digital to Analogue Converter).
        /// This should be a value between 0 and 63.
        /// </summary>
        public int DACX
        {
            get
            {
                return _DACX;
            }
            set
            {
                // Check it is in range.
                if (value < 0 || value >= 63)
                    throw new ArgumentOutOfRangeException("DAC range is 0 to 63");

                // Send the command.
                SendCommand("[" + value + "X");

                // Store the value.
                _DACX = value;
            }
        }
        private int _DACX = 0;

        /// <summary>
        /// Set the Y channel on the DAC (Digital to Analogue Converter).
        /// This should be a value between 0 and 63.
        /// </summary>
        public int DACY
        {
            get
            {
                return _DACY;
            }
            set
            {
                // Check it is in range.
                if (value < 0 || value >= 63)
                    throw new ArgumentOutOfRangeException("DAC range is 0 to 63");

                // Send the command.
                SendCommand("[" + value + "Y");

                // Store the value.
                _DACY = value;
            }
        }
        private int _DACY = 0;
        #endregion

        /*
        /// <summary>
        /// Access the IO pins.  This always executes a serial call.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte this[Pins index]
        {
            get
            {
                // Read the pin.
                //int pinvalue;
                //if (!int.TryParse(SendCommandRead("[r"), out pinvalue))
                //    throw new Exception("Unable to read pin values.");

                // Convert it to a byte array.
                //bool[] b = new bool[8];
                //for (int i = 0; i < 8; ++i)
                //    b[i] = (pinvalue & (1<<i)) != 0;

                // Return the bit of the byte array that we want.
                return pinvalue & (1<<((int)index));
            }
            set
            {
                // Check that the pin is in output mode.
                //if (_PinMode[(int)index] == PinMode.Input)
                //    throw new Exception("Cannot write to pin "+index+" when in input mode.");

                // Write to the pin.
                SendCommand("[" + ((int)index).ToString() + "s");
            }
        }

        /// <summary>
        /// Read or write the value of all the pin modes in a single byte.
        /// In binary, 0 = PinMode.Output and 1 = PinMode.Input.
        /// To set channels 1-4 (a,b,c,d) as input and the rest as output
        /// the byte value required would be 00001111 (0x0F in hex and 15 in dec).
        /// </summary>
        public byte PinModes
        {
            get
            {
                return byte.Parse(SendCommandRead("[r"));
            }
            set
            {
                // Push to the device.
                SendCommand("[" + value + "s");
            }
        }
        */

        /// <summary>
        /// Set individual pins to be either input or output.
        /// For example: myDevice[BV4626.Pins.A] = BV4626.PinMode.Input;
        /// </summary>
        /// <param name="index">The index of the pin you want to set.</param>
        /// <returns>The mode of the pin at that value.</returns>
        public PinMode this[Pins index]
        {
            get
            {
                // Read the latest values from the device.
                var pins = byte.Parse(SendCommandRead("[r"));

                // Return the value of the pin we are interested in.
                return (pins & (1<<((int)index))) != 0 ? PinMode.Input : PinMode.Output;

                // Write them 
                //return _PinMode[(int)index];
            }
            set
            {
                // Read the latest values from the device.
                var pins = byte.Parse(SendCommandRead("[r"));

                // Compute the pin set.
                //int pinset = 0;
                //for (int i = 0; i < 8; ++i)
                //    if (_PinMode[i] == PinMode.Input)
                //        pinset |= 1 << i;
                pins |= (byte)(1 << (int)index);

                // Push to the device.
                SendCommand("[" + pins + "s");

                // Set the value.
                //_PinMode[(int)index] = value;
            }
        }
        //protected PinMode[] _PinMode = new PinMode[8];
    }


    #endregion  
}
