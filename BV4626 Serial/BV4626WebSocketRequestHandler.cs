using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Net;

using Alchemy;
using Alchemy.Classes;
using Newtonsoft.Json;


namespace BV4626_Serial
{
    /// <summary>
    /// Expected workflow of this class
    /// Constructed -> open connection to board
    /// WebSocket connect -> Add users to handler 
    ///     bv4626 event triggers, send to users
    /// If user sends a command {RelayA => 1, PinA => 0, etc} -> send to board -> send Ack to websocket
    /// Websocket disconnect, remove user
    /// Desconstucted -> close connection to board
    /// </summary>
    /// <remarks>User handling shamelessly stolen from https://github.com/Olivine-Labs/Alchemy-Websockets-Example/blob/master/Server/src/Program.cs </remarks>
    public class BV4626WebSocketRequestHandler
    {

        #region Fields

        /// <summary>
        /// This holds the currently connected sockets, need these for callbacks
        /// </summary>
        protected ConcurrentDictionary<UserContext, string> OnlineUsers = new ConcurrentDictionary<UserContext, string>();

        public BV4626 Board = null;

        #endregion

        #region Constructor and Destructor

        public BV4626WebSocketRequestHandler(String port)
        {
            Board = new BV4626(port);
            Board.Open();

            Board.PinChanged += PinHandler;

            // Set a fast event poll rate and enable events.
            Board.PollFrequency = 80; // 80ms is awesome
            Board.EventsEnabled = true;
        }

        ~BV4626WebSocketRequestHandler()
        {
            Board.Close();
        }

        #endregion

        #region WebSocket Handlers

        public void OnConnect(UserContext context)
        {
            Console.WriteLine("Client Connection from : " + context.ClientAddress);
            OnlineUsers.TryAdd(context, string.Empty);
        }

        public void OnDisconnect(UserContext context)
        {
            Console.WriteLine("Client Disconnection from : " + context.ClientAddress);

            string temp;
            OnlineUsers.TryRemove(context, out temp);
        }

        /// <summary>
        /// This function actually deserves a comment ;-)
        /// 
        /// When data is received, unparse the json into a throwaway object which should have the format:
        /// { Commands : [{Command : RelayA/etc, Value: on/off/input/output} +] }
        /// 
        /// As json data will come in as a hash, these means we can push lots of commands at once, with an order!
        /// 
        /// Setting a pin to "input or output" changes the pin config accordingly
        /// 
        /// </summary>
        /// <param name="context"></param>
        public void OnReceive(UserContext context)
        {
            Console.WriteLine("Received Data from : " + context.ClientAddress);

            /* This method pretty much will only work with valid json and expected objects.
             * The catch exception is a much needed catch all from wrong types, to malformed errors */
            try
            {
                var json = context.DataFrame.ToString();

                dynamic json_obj = JsonConvert.DeserializeObject(json);

                foreach (dynamic cmd in json_obj.Commands)
                {
                    // Determine if for turning pin on/off, or changing config
                    string value = (string)cmd.Value;

                    bool onoff = value == "on" || value == "off";
                    bool onoff_value = value == "on";

                    bool inoutput = value == "input" || value == "output";
                    bool inoutput_value = value == "input";

                    // Use switch statement to select pin, then do logic at the bottom
                    BV4626.Pins pin = BV4626.Pins.A;
                    bool isPin = true;

                    switch ((string)cmd.Command)
                    {
                        // Relays can only be turned on or off, not configured
                        case "RelayA":
                            if (onoff)
                                Board.RelayA = onoff_value;
                            isPin = false;
                            break;

                        case "RelayB":
                            if (onoff)
                                Board.RelayB = onoff_value;
                            isPin = false;
                            break;

                        case "PinA":
                            pin = BV4626.Pins.A ;
                            break;
                        case "PinB":
                            pin = BV4626.Pins.B ;
                            break;
                        case "PinC":
                            pin = BV4626.Pins.C ;
                            break;
                        case "PinD":
                            pin = BV4626.Pins.D ;
                            break;
                        case "PinE":
                            pin = BV4626.Pins.E ;
                            break;
                        case "PinF":
                            pin = BV4626.Pins.F ;
                            break;
                        case "PinG":
                            pin = BV4626.Pins.G ;
                            break;
                        case "PinH":
                            pin = BV4626.Pins.H ;
                            break;
                    }

                    // If a pin was selected, either turn on or off, or configure
                    if (isPin)
                    {
                        // If changing output value
                        if (onoff)
                        {
                            Board[pin] = onoff_value;
                        }
                            // If configuring
                        else if (inoutput)
                        {
                            Board.SetPinMode(pin, inoutput_value ? BV4626.PinMode.Input : BV4626.PinMode.Output);
                        }
                    }
                    Console.WriteLine("Command: " + cmd.Command + " , Value: " + cmd.Value);
                }
            }
            catch (Exception e) // Json error, Give client feedback
            {
                context.Send(e.Message);
            }
        }

        #endregion

        #region Callbacks

        /// Event handler from board.
        /// Passes event info to websocket clients
        protected void PinHandler(object b, BV4626.Pins pin, bool state)
        {
            // This state is backwards for some reason
            Console.WriteLine("Pin " + pin + " reads " + state);

            foreach (var user in OnlineUsers.Keys)
            {
                user.Send( "{ \"Pin\" : \"" + pin.ToString() + "\" , \"Value\" : \"" + (state ? "on" : "off") + "\" }" );
            }
                    
        }

        #endregion
    }
}
