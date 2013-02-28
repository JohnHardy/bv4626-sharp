using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

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

        public class SelfCheckFailedException : Exception { } ;

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

        #region Constants

        //protected const char CR = 0x015;
        protected const char ESC = '\x001b';
        protected const char ACK = '*';
        protected const int DELAY = 100;

        #endregion

        #region Fields

        /// <summary>
        /// The serial port wrapper object.
        /// </summary>
        protected SerialPort Serial = null;

        protected byte ioConf = (byte)'\x0000';


        #endregion

        #region Constructors

        /// <summary>
        /// Construct a new BV4626 class which talks to a device on a specified COM port.
        /// </summary>
        /// <param name="port">The COM port to talk on.  e.g. "COM3"</param>
        public BV4626(String port, int baudrate = 115200, Parity parity = Parity.None)
        {
            // Create the serial port.
            Serial = new SerialPort(port, baudrate, parity,  8, StopBits.Two);
        }

        #endregion



        #region Serial Communication

        /// <summary>
        /// Open the serial communications with the BV4626.
        /// </summary>
        public void Open()
        {

            Serial.Open();

            Handshake();

        }

        /// <summary>
        /// Close the serial communication with the BV4626.
        /// </summary>
        public void Close()
        {
            Serial.Close();
        }

        /// <summary>
        /// Is this serial port open and ready to communicate.
        /// </summary>
        public bool IsOpen { get { return Serial.IsOpen; } }

        /// <summary>
        /// Perform and handshake and set the ACK to a * as a self test.
        /// Will take at least 1.2 seconds due to the delay bug
        /// </summary>
        /// <remarks>For this device, '*' is sent back only once so a second handshake attempt will produce an error.</remarks>
        protected void Handshake()
        {
            String buf = "";
            bool selfCheck = false;

            /* Perform a board reset - wait for rhe board to come alive again */
            Serial.DtrEnable = true;
            Serial.DtrEnable = false;
            System.Threading.Thread.Sleep(8*DELAY);

            /* Write a carriage return in order to auto set the baud */
            Serial.Write(new char[] { '\x000D' }, 0, 1);
            System.Threading.Thread.Sleep(3*DELAY);
            if(Serial.BytesToRead > 0)
            {
                buf = Serial.ReadExisting();
                if (buf == "*")
                {
                    /* First time hand shake. */
                }

            }

            /* As a self check, set the ACK character to a * */
            Serial.Write(new char[]{'\x001b', '[', '4','2', 'E'},0,5);
            System.Threading.Thread.Sleep(2*DELAY);

            /* The above call returns nothing, check the firmware to check the ACK character */
            Serial.Write(new char[]{'\x001b', '[', '?', '3','1', 'f'},0,6);
            System.Threading.Thread.Sleep(2*DELAY);
            if (Serial.BytesToRead > 0)
            {
                buf = Serial.ReadExisting();
                if (buf == "12*")
                {
                    selfCheck = true;
                }
            }

            // If the self check 
            if (!selfCheck)
            {
                throw new SelfCheckFailedException();
            }
        }

        protected void SendCommand(String command)
        {
            Serial.Write(ESC + command);
            System.Threading.Thread.Sleep(DELAY);
            ReadToACK();
        }


        protected byte[] tBuffer = new byte[512];

        protected string SendCommandRead(String command)
        {
            // Push the command.
            Serial.Write(ESC + command);
            System.Threading.Thread.Sleep(DELAY);

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

        #region IO pins

        /// <summary>
        /// Get/Set individual pin data
        /// For example: myDevice[BV4626.Pins.A] = T/F;
        /// </summary>
        /// <param name="index">The index of the pin you want to index.</param>
        /// <returns>The value of that pin</returns>
        public bool this[Pins index]
        {
            get
            {
                // Read the latest values from the device.
                var pins = byte.Parse(SendCommandRead("[r"));

                // Return the value of the pin we are interested in.
                return (pins & (1<<((int)index))) == 1 ? false : true;

            }
            set
            {
                // Push to the device.
                SendCommand("[" + ((value)? "255" : "0") + index.ToString().ToLower());

            }
        }

        /// <summary>
        /// Get IO Byte
        /// </summary>
        public byte IO
        {
            get { return byte.Parse(SendCommandRead("[r")); }
        }

        /// <summary>
        /// Get/Set the IO Conf byte
        /// </summary>
        public byte IOConf
        {
            get { return ioConf; }
            set
            {

                // Push to the device.
                SendCommand("[" + ((int)value).ToString() + "s");
                ioConf = value;

            }
        }

        /// <summary>
        /// Get an updated configuration byte from the current byte
        /// </summary>
        /// <param name="pin"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public byte AdjustAndReturnIOConf(Pins pin, PinMode mode)
        {
            byte conf = IOConf;

            conf |= (byte)((int)mode << (int)pin);

            return conf;
        }

        #endregion
    }


    #endregion  
}
