/*******************************************************************************
Copyright 2010 Ben Mechling

This file is part of Fast Track Timer Library.

Fast Track Timer Library is free software: you can redistribute it and/or modify
it under the terms of the Lesser GNU General Public License as published by the 
Free Software Foundation, either version 3 of the License, or (at your option) 
any later version.

Fast Track Timer Library is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or 
FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more 
details.

You should have received a copy of the GNU General Public License along with 
Fast Track Timer Library.  If not, see <http://www.gnu.org/licenses/>.
*******************************************************************************/

// Default namespaces used.
using System;
using System.Collections.Generic;
using System.Text; // for StringBuilder Class
// Added namespaces.
using System.IO.Ports; // for SerialPort Class
using System.Text.RegularExpressions; // Regex Class
using System.Threading; // ManualResetEvent Class

namespace FastTrack
{
    public class FastTrackSerialComPort
    {
        #region Static Attributes and Properties 
        //**********************************************************************

        /// <summary>
        /// Default baud rate assigned to new instances.
        /// </summary>
        private static int defaultBaudRate = 9600;
        /// <summary>
        /// Default baud rate assigned to new instances.
        /// </summary>
        public static int DefaultBaudRate
        {
            get { return FastTrackSerialComPort.defaultBaudRate; }
            set { defaultBaudRate = value; }
        }

        /// <summary>
        /// Default parity assigned to new instances.
        /// </summary>
        private static Parity defaultParity = Parity.None;
        /// <summary>
        /// Default parity assigned to new instances.
        /// </summary>
        public static Parity DefaultParity
        {
            get { return FastTrackSerialComPort.defaultParity; }
            set { defaultParity = value; }
        }

        /// <summary>
        /// Default number of data bits assigned to new instances.
        /// </summary>
        private static int defaultDataBits = 8;
        /// <summary>
        /// Default number of data bits assigned to new instances.
        /// </summary>
        public static int DefaultDataBits
        {
            get { return FastTrackSerialComPort.defaultDataBits; }
            set { defaultDataBits = value; }
        }

        /// <summary>
        /// Default stop bits assigned to new instances.
        /// </summary>
        private static StopBits defaultStopBits = StopBits.One;
        /// <summary>
        /// Default stop bits assigned to new instances. 
        /// </summary>
        public static StopBits DefaultStopBits
        {
            get { return FastTrackSerialComPort.defaultStopBits; }
            set { defaultStopBits = value; }
        }

        /// <summary>
        /// Default timeout for read operations. 
        /// </summary>
        private static int defaultReadTimeout = 500;
        /// <summary>
        /// Default timeout for read operations for the underlying SerialPort
        /// object.
        /// </summary>
        public static int DefaultReadTimeout
        {
            get { return FastTrackSerialComPort.defaultReadTimeout; }
            set { defaultReadTimeout = value; }
        }

        /// <summary>
        /// Default timeout for write operations.
        /// </summary>
        private static int defaultWriteTimeout = 500;
        /// <summary>
        /// Default timeout for write operations for the underlying SerialPort
        /// object.
        /// </summary>
        public static int DefaultWriteTimeout
        {
            get { return FastTrackSerialComPort.defaultWriteTimeout; }
            set { defaultWriteTimeout = value; }
        }

        /// <summary>
        /// Default timeout waiting for a response from a command after is sent.
        /// </summary>
        /// If the response string to a command is not received, identified, and
        /// returned within this time period, the command will be considered to
        /// have failed and the response will be ignored if it is received.
        private static int defaultResponseTimeout = 1000;
        /// <summary>
        /// Default timeout waiting for a response from a command after is sent.
        /// </summary>
        public static int DefaultResponseTimeout
        {
            get { return FastTrackSerialComPort.defaultResponseTimeout; }
            set { FastTrackSerialComPort.defaultResponseTimeout = value; }
        }
        
        /// <summary>
        /// Regular expression for validating a com port name.
        /// </summary>
        /// Expression parts:
        ///     ^               string starts with expression
        ///     [Cc][Oo][Mm]    "com" where case is ignored 
        ///     [0-9]{1,3}      port number where the number 1 to 3 digits
        ///     $               string ends with this expression 
        /// Thus, the entire string must match the expression.
        private static string ComPortRegexPattern_validate = 
            @"^[Cc][Oo][Mm][0-9]{1,3}$";

        /// <summary>
        /// Regular expression for finding a comport name in a larger string.
        /// </summary>
        /// Mostly the same as the ComPortRegexPattern_validate string except
        /// that it allows for characters on either end.
        /// This expression is intended to allow a Regex.Match method to extract
        /// a com port name from a larger string.
        private static string ComPortRegexPatter_parse =
            @"[Cc][Oo][Mm][0-9]{1,3}";
        #endregion

        #region Instance Attributes and Properties 
        //**********************************************************************

        /// <summary>
        /// Encaplulated SerialPort object
        /// </summary>
        private SerialPort comPort;

        /// <summary>
        /// Encaplulated SerialPort object
        /// </summary>
        public SerialPort ComPort
        {
            get { return comPort; }
        }

        /// <summary>
        /// Amount of time allowed for a device to response after a command is
        /// sent.
        /// </summary>
        private int responseTimeout;
        /// <summary>
        /// Amount of time allowed for a device to response after a command is
        /// sent.
        /// </summary>
        public int ResponseTimeout
        {
            get { return responseTimeout; }
            set { responseTimeout = value; }
        }

        private List<SerialResponse> expectedResponses;

        public List<SerialResponse> ExpectedResponses
        {
            get { return expectedResponses; }
            set { expectedResponses = value; }
        }

        public bool IsOpen
        {
            get { return (this.comPort != null && this.comPort.IsOpen); }
        }

        // Timeout Events and Associated Delegates
        //----------------------------------------------------------------------

        /// <summary>
        /// Represents the method(s) that will handle the response timeout 
        /// event for this object.
        /// </summary>
        public delegate void ResponseTimeoutExpiredEventHandler();

        /// <summary>
        /// Event triggered when a reponse times out.
        /// </summary>
        public event ResponseTimeoutExpiredEventHandler ResponseTimeoutEvent;

        /// <summary>
        /// Represents the method(s) that will handle the WriteTimeoutEvent.
        /// </summary>
        public delegate void WriteTimeoutEventHandler();

        /// <summary>
        /// Event triggered when a write call to the encapsulated SerialPort 
        /// times out.
        /// </summary>
        public event WriteTimeoutEventHandler WriteTimeoutEvent;

        // Internal variables for thread control and communication.
        //----------------------------------------------------------------------

        private object activeCommandLock = new object();
        private object responseInfoLock = new object();

        // only use with a lock on the activeCommandLock
        private ManualResetEvent responseEvent = new ManualResetEvent(false);

        // longest single response should be about 85 charaters 
        private StringBuilder responseBuffer = new StringBuilder("", 100);
        #endregion

        #region Exceptions 
        //**********************************************************************

        /// <summary>
        /// Custom exception thrown when a communication error occurres that can
        /// not be indicated easily another way.
        /// </summary>
        public class InvalidPortNameException : Exception { }
        #endregion

        #region Constructors
        //**********************************************************************

        /// <summary>
        /// Initializes a new instance of the class. 
        /// </summary>
        /// <param name="portName">COM port name (e.g. "COM99")</param>
        public FastTrackSerialComPort(string portName)
            : this(portName, null)
        { }

        /// <summary>
        /// Initializes a new instance of the class. 
        /// </summary>
        /// <param name="portName">COM port name (e.g. "COM99")</param>
        /// <param name="initialExpectedResponses">
        /// List of serial responses to be expected. </param>
        public FastTrackSerialComPort(string portName, 
            List<SerialResponse> initialExpectedResponses)
        {
            if (!IsPortNameFormatValid(portName) ||
                !PortExist(portName))
            {
                throw new InvalidPortNameException();
            }

            // create new SerialPort object.
            this.comPort = new SerialPort(portName, defaultBaudRate,
                defaultParity, defaultDataBits, defaultStopBits);

            // Set timeouts 
            this.comPort.ReadTimeout = defaultReadTimeout;
            this.comPort.WriteTimeout = defaultWriteTimeout;
            this.responseTimeout = defaultResponseTimeout;

            // Add initial expected responses to the list.
            if (initialExpectedResponses == null)
            {
                this.expectedResponses = new List<SerialResponse>();
            }
            else
            {
                this.expectedResponses = initialExpectedResponses;
            }
        }

        #endregion

        #region Instance Methods
        //**********************************************************************

        // Public
        //----------------------------------------------------------------------

        /// <summary>
        /// Attempts to open a connection on the underlying SerialPort object.
        /// </summary>
        /// <returns>True if successful. Otherwise, false.</returns>
        public bool Open()
        {
            // setup an event handler to receive communications from the device.
            this.comPort.DataReceived +=
                new SerialDataReceivedEventHandler(this.SerialDataResponseHandler);

            if (!this.comPort.IsOpen)
            {
                try
                {
                    // Attempt to open.
                    this.comPort.Open();
                }
                catch (Exception ex)
                {
                    // Indicate failure.
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Sends a Serial Command and waits for a response or a timeout.
        /// </summary>
        /// <param name="command">command to send</param>
        /// <returns>True if the command was successfully sent and the response
        /// was received. Otherwise, false.</returns>
        public bool SendCommand(SerialCommand command)
        {
            bool responseReceived = false;
            
            // Get a lock to prevent any other commands from being sent.
            lock (activeCommandLock)
            {
                // Setup for send
                lock (responseInfoLock)
                {
                    expectedResponses.Add(command);
                    // enable blocking
                    responseEvent.Reset();
                }
                // Send command
                if (SendRawCommand(command.CommandString))
                { // Send successfully
                    // wait
                    responseReceived = responseEvent.WaitOne(this.responseTimeout);
                }
                // Remove the command as an expected response. 
                lock (responseInfoLock)
                {
                    // if the event timed out
                    if (!responseReceived)
                    {
                        // Set the respone event so that execution will nolonger
                        // block.
                        responseEvent.Set();
                    }
                    // remove command from the list, if not already gone.
                    expectedResponses.Remove(command);
                    if (!responseReceived && ResponseTimeoutEvent != null)
                    {
                        ResponseTimeoutEvent();
                    }
                }
            }

            if (!responseReceived)
            {
                command.ResetResponseData();
            }
            return responseReceived;
        }

        /// <summary>
        /// Closes the underlying SerialPort connection.
        /// </summary>
        /// <returns>True if successfully closed. Otherwise, false.</returns>
        public bool Close()
        {
            if (comPort.IsOpen)
            {
                comPort.Close();
                return true;
            }
            else
            {
                return false;
            }
        }

        // Private
        //----------------------------------------------------------------------

        /// <summary>
        /// Sends a string to the COM port.
        /// </summary>
        /// <param name="cmd">command string</param>
        /// <returns>True if the command is sent successfully. Otherwise, false.
        /// </returns>
        private bool SendRawCommand(string cmd)
        {
            if (cmd == null || // avoid ArgumentNullException
                !this.comPort.IsOpen) // avoid InvalidOperationException
            {
                return false;
            }

            try
            {
                this.comPort.Write(cmd);
            }
            catch // TimeoutException, only other possible exception
            {
                // sent event to whom it may concern
                if (WriteTimeoutEvent != null)
                {
                    WriteTimeoutEvent();
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Method that handles the SerialDataResponseEvent from the SerialPort
        /// object, distinguish responses, and send them to the associated 
        /// SerialResponse object.
        /// </summary>
        /// <param name="sender">object responsible for raising the event</param>
        /// <param name="e">Event arguments.</param>
        private void SerialDataResponseHandler(
            object sender, SerialDataReceivedEventArgs e)
        {
            lock (responseInfoLock)
            {
                // Read input into buffer 
                responseBuffer.Append(comPort.ReadExisting());

                int firstMatchingResponseIndex = -1;
                int firstMatchBufferIndex = -1;
                Match match;

                // find all complete matches
                do
                {
                    // find the first complete match
                    firstMatchingResponseIndex = -1;
                    firstMatchBufferIndex = -1;
                    for (int i = 0; i < expectedResponses.Count; i++)
                    {
                        // Get match
                        match = Regex.Match(responseBuffer.ToString(),
                            expectedResponses[i].ResponsePattern);
                        if (match.Success &&
                            (match.Index < firstMatchBufferIndex
                            || firstMatchBufferIndex < 0))
                        {
                            // Mark index in response string
                            firstMatchBufferIndex = match.Index;
                            // Mark the corresponding response object
                            firstMatchingResponseIndex = i;
                        }
                        // if the match starts the string, stop looking.
                        if (firstMatchBufferIndex == 0)
                        {
                            break;
                        }
                    }
                    // if a match was found
                    if (firstMatchBufferIndex >= 0)
                    {
                        // Get the first match found.
                        match = Regex.Match(responseBuffer.ToString(),
                            expectedResponses[firstMatchingResponseIndex]
                            .ResponsePattern);
                        // Remove any characters prior to the first match.
                        responseBuffer.Remove(0, firstMatchBufferIndex);
                        // Call the SerialResponse object's response handler.
                        expectedResponses[firstMatchingResponseIndex]
                            .SetResponseData(match.Value);
                        // Consume the response.
                        responseBuffer.Remove(0, match.Length);
                        // If the response belongs to a command, 
                        if (expectedResponses[firstMatchingResponseIndex]
                            .ExpiresOnUse)
                        {
                            // revome the command from the list.
                            expectedResponses.RemoveAt(firstMatchingResponseIndex);
                            // let the send command know that the command is done
                            this.responseEvent.Set();
                        }
                    }
                    // look for more complete responses
                } while (firstMatchBufferIndex >= 0);

                if (responseBuffer.Length > 0)
                {
                    // Check whether the remaining characters are a patial match.
                    firstMatchingResponseIndex = -1;
                    firstMatchBufferIndex = -1;
                    // find the partial response that starts closes to the 
                    // beginning of the buffer. 
                    for (int i = 0; i < expectedResponses.Count; i++)
                    {
                        // check for a match at the end of the buffer.
                        match = Regex.Match(responseBuffer.ToString(),
                            expectedResponses[i].PartialResponsePattern + "$");
                        if (match.Success &&
                            (match.Index < firstMatchBufferIndex
                            || firstMatchBufferIndex < 0))
                        {
                            firstMatchBufferIndex = match.Index;
                            firstMatchingResponseIndex = i;
                        }
                        if (firstMatchBufferIndex == 0)
                        {
                            break;
                        }
                    }
                    // If a match was found
                    if (firstMatchBufferIndex >= 0)
                    {
                        // remove all characters before it
                        responseBuffer.Remove(0, firstMatchBufferIndex);
                    }
                    else
                    { // no match found
                        // empty the buffer
                        responseBuffer.Remove(0, responseBuffer.Length);
                    }
                }
            } // release lock
        }
        #endregion

        #region Static Methods
        //**********************************************************************

        // Public
        //----------------------------------------------------------------------

        /// <summary>
        /// Gets and corrects the list of port names supplied by 
        /// SerialPort.GetPortNames method.
        /// </summary>
        /// <returns>string array of COM port names</returns>
        /// Something is a little wierd with Windows, or at least my installation.
        /// The SerialPort.GetPortNames method returns the list of port names 
        /// with an strang extra character at the end that seems to be trash. 
        /// The documentation says that the names are taken from the registry. 
        /// HKEY_LOCAL_MACHINE\HARDWARE\DEVICEMAP\SERIALCOMM
        /// A direct query of the registry values gets the same results. 
        /// I settled for extracted the proper name from the string returned. 
        public static string[] GetPortNames()
        {
            // Get the offical list.
            string[] portNames = SerialPort.GetPortNames();
            // Sanitize each port name in the list and keep only the ones 
            // that are not entirely removed.
            List<string> properPortNames = new List<string>();
            for (int i = 0; i < portNames.Length; i++)
            {
                portNames[i] = SanitizePortName(portNames[i]);
                if (portNames[i] != "")
                {
                    properPortNames.Add(portNames[i]);
                }
            }
            // Build array from remaining list.
            string[] newNameArray = new string[properPortNames.Count];
            for (int i = 0; i < properPortNames.Count; i++)
            {
                newNameArray[i] = properPortNames[i];
            }

            return newNameArray;
        }

        /// <summary>
        /// Checks the COM part name against a regular expression for validation.
        /// </summary>
        /// <param name="portName">port name to check</param>
        /// <returns>True if valid. Otherwise, false</returns>
        public static bool IsPortNameFormatValid(string portName)
        {
            return Regex.IsMatch(portName, ComPortRegexPattern_validate);
        }

        /// <summary>
        /// Checks whether a port name exist in the list of ports.
        /// </summary>
        /// <param name="portName">Port name for which to check</param>
        /// <returns>True if it exists. Otherwise, false.</returns>
        public static bool PortExist(string portName)
        {
#if TEST
            // If a mock port is used, the name does not really matter. 
            return true;
#else
            string[] ports = GetPortNames();
            for (int index = 0; index < ports.Length; index++)
            {
                if (ports[index].ToLower() == portName.ToLower())
                {
                    return true;
                }
            }
            return false;
#endif
        }

        /// <summary>
        /// Extracts the first identifyible COM Port name from a string. If no
        /// name is found an empty string is returned.
        /// </summary>
        /// <param name="portName">string containing a port name</param>
        /// <returns>Valid port name if found. Empty string if not.</returns>
        public static string SanitizePortName(string portName)
        {
            Regex r = new Regex(ComPortRegexPattern_validate);
            if (!IsPortNameFormatValid(portName) &&
                !Regex.Match(portName, ComPortRegexPatter_parse).Success)
            { // not valid and can not be found in the string
                return "";
            }
            // else the string can be salvaged 
            return Regex.Match(portName, ComPortRegexPatter_parse).ToString();
        }

        #endregion
    }
}
