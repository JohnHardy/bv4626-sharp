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
using System.Threading;
using System.IO.Ports;

namespace BV4626_Serial
{
    /// <summary>
    /// A high level wrapper around the BV4626 device.
    /// Details on this device can be found here: http://doc.byvac.com/index.php5?title=Product_BV4626
    /// </summary>
    /// <author>Carl Ellis and John Hardy</author>
    /// <date>Feb 2013</date>
    public class BV4626// : IDisposable
    {
        #region Inner Classes
        /// <summary>
        /// A generic exception for the BV4626 board.
        /// </summary>
        public class BV4626Exception : Exception { public BV4626Exception(String message = "") : base(message) {  } }

        /// <summary>
        /// An exception which is raised when the board self-check fails.
        /// </summary>
        public class SelfCheckFailedException : BV4626Exception { public SelfCheckFailedException(String message = "") : base(message) { } }

        /// <summary>
        /// An exception which is raised when we try to read from an output pin or write to an input pin.
        /// </summary>
        public class PinIOException : BV4626Exception
        {
            internal const String CANNOTREAD  = "Cannot read from output pin";
            internal const String CANNOTWRITE = "Cannot write to input pin";

            public PinIOException(String message) : base(message)
            {
            }
        }

        

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
        /// <summary>
        /// The esc character.
        /// </summary>
        protected const char ESC = '\x001b';

        /// <summary>
        /// The ack character.
        /// </summary>
        protected const char ACK = '*';

        /// <summary>
        /// The thread timeout used when starting up the board.
        /// </summary>
        protected const int DELAY = 200;
        #endregion

        #region Fields

        /// <summary>
        /// The serial port wrapper object.
        /// </summary>
        protected SerialPort Serial = null;

        /// <summary>
        /// The internal IO pin configuration.  Not valid until set.
        /// </summary>
        protected byte _IOConf = (byte)'\x0000';


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
            // Open the port.
            Serial.Open();

            // Handshake.
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

        /// <summary>
        /// Send a command to the device.
        /// </summary>
        /// <param name="command">The command string to send.</param>
        protected void SendCommand(String command)
        {
            // Write the command.
            Serial.Write(ESC + command);
            //System.Threading.Thread.Sleep(DELAY);

            // Read the response from the buffer.
            var ack = Serial.ReadChar();

            // If its not ACK, error.
            if (ack != ACK)
                throw new Exception("Command problem.  Bad ACK.");
        }

        /// <summary>
        /// Send a command to the device and read from the input buffer.
        /// </summary>
        /// <param name="command">The command to send. e.g. "[r"</param>
        /// <returns></returns>
        protected string SendCommandRead(String command)
        {
            // Push the command.
            Serial.Write(ESC + command);
            //System.Threading.Thread.Sleep(DELAY);

            // Output buffer.
            var sb = new StringBuilder();

            // While we have not recieved an ack, read.
            while (true)
            {
                // Read the response from the buffer.
                var c = Serial.ReadChar();

                // Break out on on ack.
                if (c == ACK)
                    break;

                // Append if not an ack.
                sb.Append((char)c);
            }

            // Return the result.
            return sb.ToString();
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
        /// Return the device ID.  In this case, it should be 4626.
        /// </summary>
        public String DeviceID { get { return SendCommandRead("[?31d"); } }

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
        /// Returns true if the pin is configured to be an input pin, false if not.
        /// </summary>
        /// <param name="pin">The pin of interest.</param>
        /// <returns>True if the specified pin is an input pin, false if not.</returns>
        public bool IsInputPin(Pins pin)
        {
            return (_IOConf & (1 << (int)pin)) != 0;
        }

        /// <summary>
        /// Returns true if the pin is configured to be an output pin, false if not.
        /// </summary>
        /// <param name="pin">The pin of interest.</param>
        /// <returns>True if the specified pin is an output pin, false if not.</returns>
        public bool IsOutputPin(Pins pin)
        {
            return !IsInputPin(pin);
        }

        /// <summary>
        /// Set an individual pin to be input or output.
        /// </summary>
        /// <param name="pin">The pin of interest.</param>
        /// <param name="mode">Do we want this pin to be an input or an output pin.</param>
        public void SetPinMode(Pins pin, PinMode mode)
        {
            IOConf = AdjustIOConfByte(pin, mode, IOConf);
        }

        /// <summary>
        /// Set the value of a specific pin.  This can be a value between 0 and 255.
        /// </summary>
        /// <param name="pin">The pin of interest.</param>
        /// <param name="value">A value between 0 and 255.</param>
        public void SetPinValue(Pins pin, byte value)
        {
            SendCommand("[" + value.ToString() + pin.ToString().ToLower());
        }


        #region Basic Pin IO
        /// <summary>
        /// Read or write a boolean value to Pin A (the digital IO pins).
        /// </summary>
        public bool A
        {
            get { return this[Pins.A]; }
            set { this[Pins.A] = value; }
        }
        /// <summary>
        /// Read or write a boolean value to Pin B (the digital IO pins).
        /// </summary>
        public bool B
        {
            get { return this[Pins.B]; }
            set { this[Pins.B] = value; }
        }
        /// <summary>
        /// Read or write a boolean value to Pin C (the digital IO pins).
        /// </summary>
        public bool C
        {
            get { return this[Pins.C]; }
            set { this[Pins.C] = value; }
        }
        /// <summary>
        /// Read or write a boolean value to Pin D (the digital IO pins).
        /// </summary>
        public bool D
        {
            get { return this[Pins.D]; }
            set { this[Pins.D] = value; }
        }
        /// <summary>
        /// Read or write a boolean value to Pin E (the digital IO pins).
        /// </summary>
        public bool E
        {
            get { return this[Pins.E]; }
            set { this[Pins.E] = value; }
        }
        /// <summary>
        /// Read or write a boolean value to Pin F (the digital IO pins).
        /// </summary>
        public bool F
        {
            get { return this[Pins.F]; }
            set { this[Pins.F] = value; }
        }
        /// <summary>
        /// Read or write a boolean value to Pin G (the digital IO pins).
        /// </summary>
        public bool G
        {
            get { return this[Pins.G]; }
            set { this[Pins.G] = value; }
        }
        /// <summary>
        /// Read or write a boolean value to Pin H (the digital IO pins).
        /// </summary>
        public bool H
        {
            get { return this[Pins.H]; }
            set { this[Pins.H] = value; }
        }
        #endregion

        /// <summary>
        /// Get/Set individual pin data
        /// For example: myDevice[BV4626.Pins.A] = T/F;
        /// </summary>
        /// <param name="index">The index of the pin you want to index.</param>
        /// <returns>The value of that pin</returns>
        /// <remarks>The full on value of the digital output pin is 255.</remarks>
        public bool this[Pins index]
        {
            get
            {
                // If we are set to be an output pin, bail.
                if (IsOutputPin(index))
                    throw new PinIOException(PinIOException.CANNOTREAD); 

                // Read the latest values from the device.
                var pins = byte.Parse(SendCommandRead("[r"));

                // Return the value of the pin we are interested in.
                return (pins & (1<<((int)index))) == 1 ? false : true;

            }
            set
            {
                // If we are set to be an input pin, bail.
                if (IsInputPin(index))
                    throw new PinIOException(PinIOException.CANNOTWRITE); 

                // Push to the device.
                SetPinValue(index, value ? (byte)255 : (byte)0);
                //SendCommand("[" + ((value)? "255" : "0") + index.ToString().ToLower());
            }
        }

        /// <summary>
        /// Get IO Byte.  This is composed of the on/off values of the pins a-h.
        /// </summary>
        public byte IOByte
        {
            get {
                String b = SendCommandRead("[r");
                return (b != "") ? byte.Parse(b) : (byte)0; 
            }
        }

        /// <summary>
        /// Get or set the IO configuration byte.  This is composed of the input/output states for the pins a-h.
        /// </summary>
        public byte IOConf
        {
            get { return _IOConf; }
            set
            {
                // Push to the device.
                SendCommand("[" + ((int)value).ToString() + "s");
                _IOConf = value;
            }
        }

        /// <summary>
        /// Modify a configuration byte to set a pin into a given mode.  
        /// Once you are finished making changes to the byte, pass it to the IOConf variable to push the changes.
        /// </summary>
        /// <param name="pin">The pin of interest.</param>
        /// <param name="mode">The mode we want this pin to be.</param>
        /// <returns></returns>
        public static byte AdjustIOConfByte(Pins pin, PinMode mode, byte ioconf)
        {
            byte conf = ioconf;

            if (mode == PinMode.Input)
            {
                conf = (byte)(conf | (1 << (int)pin));
            }
            else
            {
                conf = (byte)(conf & ~(1 << (int)pin));
            }
            
            return conf;
        }

        #endregion

        #region Threaded Button Events
        /// <summary>
        /// Triggered when an input pin changes state.
        /// </summary>
        public event Action<object, Pins, bool> PinChanged;

        /// <summary>
        /// Triggered when one or more of the input pins change state.
        /// </summary>
        public event Action<object, byte> PinsChanged;

        /// <summary>
        /// Get or set the rate at which the digital IO pins are polled in milliseconds.  Default = 500ms.
        /// </summary>
        /// <remarks>From a HCI point of view, humans asociate actions with reactions if both events happen within 80ms of each other.</remarks>
        public int PollFrequency
        {
            get
            {
                return _PollFrequency;
            }
            set
            {
                if (_PollFrequency < 50)
                    throw new ArgumentOutOfRangeException("Poll frequency must be larger than 50ms.  Small values result in out of sync serial IO.");
                _PollFrequency = value;
                if (PollTimer != null)
                    PollTimer.Change(100, _PollFrequency);
            }
        }
        protected int _PollFrequency = 500;

        /// <summary>
        /// A timer which polls the pins to check for event changes.
        /// </summary>
        protected Timer PollTimer = null;

        /// <summary>
        /// The last known pin state.  Only up to date after each event frame has completed.
        /// </summary>
        protected byte _LastPins = 0;

        /// <summary>
        /// Is the polling event timer enabled.  Setting true will activate the PinChanged and PinsChanged events.  False will dislable.
        /// </summary>
        public bool EventsEnabled
        {
            get
            {
                return _EventsEnabled;
            }
            set
            {
                // Bail if nothing to do.
                if (_EventsEnabled == value)
                    return;

                //  Update the value.
                _EventsEnabled = value;

                // Enable the timer from a disabled state.
                if (value)
                {
                    // Update the pin state.
                    _LastPins = this.IOByte;

                    // Create the timer.
                    PollTimer = new System.Threading.Timer(EventPoll, null, 100, _PollFrequency);
                }
                else
                {
                    PollTimer = null;
                }
            }
        }
        protected bool _EventsEnabled = false;

        /// <summary>
        /// Handler for the timer tick event.
        /// </summary>
        /// <param name="stateInfo"></param>
        protected void EventPoll(Object stateInfo)
        {
            // Bail if events have been disabled.
            if (!_EventsEnabled)
                return;

            // Update the pin state.
            var pins = this.IOByte;

            // If the states are different, trigger the general event.
            if (pins != _LastPins)
            {
                if (PinsChanged != null)
                    PinsChanged(this, pins);
            }

            // If we have individual pin events.
            if (this.PinChanged != null)
            {
                //  Trigger events for each pin that's new.
                for (int pin = 0; pin < 8; ++pin)
                {
                    bool bitNew = (pins & (1 << pin)) == 0;
                    bool bitOld = (_LastPins & (1 << pin)) == 0;
                    if (bitNew != bitOld)
                    {
                        this.PinChanged(this, (Pins)pin, bitNew);
                    }
                }
            }

            // Store the updated pins value.
            _LastPins = pins;

        }
        #endregion

        #endregion
    }
}
