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

// Default namespaces used
using System;
// Added namespaces
using System.Text.RegularExpressions; // used to match response strings
using System.ComponentModel; // used to launch events on a new thread

namespace FastTrack
{
    /// <summary>
    /// Set of possible finishing places. 
    /// </summary>
    /// NoPlace is set to 7 instead of 0 to simplify comparision. In this form 
    /// the greater the value the worse the place. 
    public enum FinishingPlaces 
    { 
        First = 1, 
        Second = 2, 
        Third = 3, 
        Forth = 4, 
        Fifth = 5, 
        Sixth = 6,
        NoPlace = 7
    }

    #region SerialResponse 
    //##########################################################################
    /// <summary>
    /// The SeiralResponse Class is a base class designed to handle specified 
    /// strings sent to the computer from the timer device. Each separate 
    /// response type should be handled by a separate derived class. 
    /// </summary>
    public abstract class SerialResponse
    {
        // Attributes and Properties
        //**********************************************************************

        /// <summary>
        /// Signifies that an acceptable response has bee received and that any 
        /// data members designed to hold information returned are uptodate. 
        /// </summary>
        protected bool isResponseSet;
        /// <summary>
        /// Signifies that an acceptable response has bee received and that any 
        /// data members designed to hold information returned are uptodate. 
        /// (Read only)
        /// </summary>
        public virtual bool IsResponseSet
        {
            get { return isResponseSet; }
        }

        /// <summary>
        /// A regular expression string that should match all forms of a 
        /// complete response from the device. 
        /// </summary>
        /// This string is provided by any class that implements this class. 
        protected string responsePattern;
        /// <summary>
        /// A regular expression string that should match all forms of a 
        /// complete response from the device. (Read only)
        /// </summary>
        public virtual string ResponsePattern
        {
            get { return responsePattern; }
        }

        /// <summary>
        /// A regular expression string that should match any incomplete form of 
        /// a response from the device. The string is used to identify partial 
        /// strings so that they are not discarded. 
        /// </summary>
        /// This string is provided by any class that implements this class. 
        protected string partialResponsePattern;
        /// <summary>
        /// A regular expression string that should match any incomplete form of 
        /// a response from the device. The string is used to identify partial 
        /// strings so that they are not discarded. (Read only)
        /// </summary>
        public virtual string PartialResponsePattern
        {
            get { return partialResponsePattern; }
        }

        /// <summary>
        /// Indicates that the no the response is only expected once and should 
        /// not be looked for after it is received. 
        /// </summary>
        protected bool expiresOnUse;
        /// <summary>
        /// Indicates that the no the response is only expected once and should 
        /// not be looked for after it is received. 
        /// </summary>
        public bool ExpiresOnUse
        {
            get { return expiresOnUse; }
            set { expiresOnUse = value; }
        }

        // Constructor
        //**********************************************************************
        /// <summary>
        /// Initializes a new instance of the response class. 
        /// </summary>
        /// <param name="responsePattern">
        /// Regular expressions string that matches a complete respone string.
        /// </param>
        /// <param name="partialResponsePattern">
        /// Regular expressions string that matches a partially complete respone 
        /// string.
        /// </param>
        /// <param name="expiresOnUse">
        /// Indicates whether only one response is expected.
        /// </param>
        protected SerialResponse(string responsePattern,
            string partialResponsePattern, bool expiresOnUse)
        {
            // Set parameters
            this.responsePattern = responsePattern;
            this.partialResponsePattern = partialResponsePattern;
            this.expiresOnUse = expiresOnUse;
            // Set response data to defaults
            this.ResetResponseData();
        }

        // Methods
        //**********************************************************************

        /// <summary>
        /// Submits a string as a response to from the device. If it is valid, 
        /// the objects response data members will be set from the information 
        /// it contains. In the base class that would only be the IsResponseSet
        /// member, but derived classes should overload this method if more 
        /// response information is expected. 
        /// </summary>
        /// <param name="response">response string from the device</param>
        public virtual void SetResponseData(string response)
        {
            // reset data
            this.ResetResponseData();
            // check validity/set data
            this.isResponseSet = Regex.IsMatch(response, 
                "^" + this.responsePattern + "$");
        }

        /// <summary>
        /// Clears and invalidates all the response data in the object. 
        /// This base class method is trivial but derived classes should 
        /// over load this method to do clear any additional response data added.
        /// </summary>
        public virtual void ResetResponseData()
        {
            this.isResponseSet = false;
        }
    }
    #endregion

    #region RaceClearedResponse
    //##########################################################################
    /// <summary>
    /// Child class of the Serial Response class intended to recognize the 
    /// race cleared response string. This response object is intended to raise
    /// an event on when set, but continue to remain on the list of expected 
    /// responses. 
    /// </summary>
    public class RaceClearedResponse : SerialResponse
    {
        // Event and Delegates
        //**********************************************************************

        /// <summary>
        /// Event handler delegate for the RaceClearedEvent.
        /// </summary>
        public delegate void RaceClearedEventHandler();

        /// <summary>
        /// Event raised when a race is cleared and the response is set.
        /// </summary>
        public event RaceClearedEventHandler RaceClearedEvent;

        // Constructor
        //**********************************************************************

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public RaceClearedResponse()
            : base("@", "@", false)
        {}

        // Methods
        //**********************************************************************
        public override void SetResponseData(string response)
        {
            /// The value of the isResponseSet does not mean much beyond this 
            /// funciton, but it serves to convey the result of the base class
            /// validation.
            base.SetResponseData(response);
            // Send event to all subscribed methods if they exist.
            if (base.isResponseSet && RaceClearedEvent != null)
            {
                /// The event is setup to be launched from a new thread. It is 
                /// likely that this method will be run from a secondary thread
                /// lauched by a response from the device. It may be necessary 
                /// for the event handlers subscribed to this event to run some
                /// function on the main thread. If they attempt to do so during
                /// while the response handler is blocking the main thread, the 
                /// program will deadlock. Using a new thread will allow the 
                /// event handler to wait for the main thread without blocking 
                /// this one.
                BackgroundWorker workerThread = new BackgroundWorker();
                workerThread.DoWork += 
                    new DoWorkEventHandler(RaiseEvent);
                workerThread.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Do work event handler for the worker thread used in the 
        /// SetResponseData method.
        /// </summary>
        private void RaiseEvent(object sender, DoWorkEventArgs e)
        {
            // Run event handlers.
            RaceClearedEvent();
        }
    }
    #endregion

    #region RaceResultsResponse
    //##########################################################################
    /// <summary>
    /// This class is designed to handle race results responses from the device.
    /// It is derived from the SerialResponse class.
    /// </summary>
    public class RaceResultsResponse : SerialResponse
    {
        // Attributes
        //**********************************************************************

        /// <summary>
        /// Event handler delegate for the RaceResultsReceivedEvent.
        /// </summary>
        /// <param name="response">copy of the sending response object</param>
        public delegate void RaceResultReceivedEventHandler(
            RaceResultsResponse response);

        /// <summary>
        /// Event raised when new results are received. 
        /// </summary>
        public event RaceResultReceivedEventHandler RaceResultsReceiveEvent;

        /// <summary>
        /// Finishing places for each of the logical lanes.
        /// </summary>
        private FinishingPlaces[] response_FinshingPlaces;

        /// <summary>
        /// Finishing times for each of the logical lanes.
        /// </summary>
        private double[] response_FinishingTimes;

        /// <summary>
        /// Number of logical lanes in the device result responses.
        /// </summary>
        public const int MaxLogicalLaneCount = 6;

        // Constructor
        //**********************************************************************
        public RaceResultsResponse()
            : base( // response pattern
            "A=([0-9]\\.[0-9]{3})([ !\"#\\$%&]) " +
            "B=([0-9]\\.[0-9]{3})([ !\"#\\$%&]) " +
            "C=([0-9]\\.[0-9]{3})([ !\"#\\$%&]) " +
            "D=([0-9]\\.[0-9]{3})([ !\"#\\$%&]) " +
            "E=([0-9]\\.[0-9]{3})([ !\"#\\$%&]) " +
            "F=([0-9]\\.[0-9]{3})([ !\"#\\$%&]) " +
            "\r\n"
            , // partial response pattern
            "(([A-E]=[0-9]\\.[0-9]{3}[ !\"#\\$%&] ){0,5}[A-F]=?" +
            "|([A-E]=[0-9]\\.[0-9]{3}[ !\"#\\$%&] ){0,5}[A-F]=[0-9]\\.?" +
            "|([A-E]=[0-9]\\.[0-9]{3}[ !\"#\\$%&] ){0,5}[A-F]=[0-9]\\.[0-9]{0,3}" +
            "|([A-E]=[0-9]\\.[0-9]{3}[ !\"#\\$%&] )" + 
             "{0,5}[A-F]=[0-9]\\.[0-9]{3}[ !\"#\\$%&]" +
            "|([A-F]=[0-9]\\.[0-9]{3}[ !\"#\\$%&] ){1,6}\r?)"
            ,
            false) // exprire on use
        {
            this.ResetResponseData();
        }

        // Public Methods
        //**********************************************************************

        /// <summary>
        /// Resets all the response data for this object to default values and 
        /// marks them as invalid.
        /// </summary>
        public override void ResetResponseData()
        {
            // Call the base class method to reset its data members. 
            base.ResetResponseData();

            // Clear the finishing times.
            this.response_FinishingTimes = new double[MaxLogicalLaneCount];
            for (int i = 0; i < this.response_FinishingTimes.Length; i++)
            {
                this.response_FinishingTimes[i] = 0;
            }
            // Clear the finishing places.
            this.response_FinshingPlaces = new FinishingPlaces[MaxLogicalLaneCount];
            for (int i = 0; i < this.response_FinshingPlaces.Length; i++)
            {
                this.response_FinshingPlaces[i] = FinishingPlaces.NoPlace;
            }
        }

        /// <summary>
        /// Determines if a given string if a valid response and if so, extracts 
        /// the information from it.
        /// </summary>
        /// <param name="response">
        /// response string received from the device
        /// </param>
        public override void SetResponseData(string response)
        {
            GroupCollection groups;
            
            // Set the base data members. 
            base.SetResponseData(response);
            // if the string is recognized as valid by the base class...
            if (base.isResponseSet)
            {
                // Get information groups out of the match.
                groups = Regex.Match(response, responsePattern).Groups;

                for (int i = 0; i < this.response_FinishingTimes.Length; i++)
                {
                    // Set time for lane i.
                    this.response_FinishingTimes[i] = 
                        Convert.ToDouble(groups[2 * i + 1].Value);
                    // Get the finishing place from the character indicator.
                    this.response_FinshingPlaces[i] = 
                        GetPlaceFromSymbol(groups[2 * i + 2].Value[0]);
                }
            }

            // if there are subscribed to the event...
            if (RaceResultsReceiveEvent != null)
            {
                // make a copy of this object and thus its response data
                RaceResultsResponse copy = 
                    (RaceResultsResponse)this.MemberwiseClone();
                // Setup a secondary thread to run the event methods.
                BackgroundWorker secondThread = new BackgroundWorker();
                secondThread.DoWork += new DoWorkEventHandler(RaiseEvent);
                // Start thread.
                secondThread.RunWorkerAsync(copy);
            }
        }

        /// <summary>
        /// Do work event handler for the worker thread used in the 
        /// SetResponseData method. 
        /// </summary>
        void RaiseEvent(object sender, DoWorkEventArgs e)
        {
            // Call subscribed methods and pass the result as an argument. 
            RaceResultsReceiveEvent((RaceResultsResponse)e.Argument);
        }

        /// <summary>
        /// Gets the finishing time returned for a specified lane.
        /// </summary>
        /// <param name="lane">Lane of interest.</param>
        /// <returns>The time that corresponds to the specified lane.</returns>
        public double Response_GetTime(Lanes lane)
        {
            return this.response_FinishingTimes[(int)lane];
        }

        /// <summary>
        /// Gets the finishing place for a specified lane.
        /// </summary>
        /// <param name="lane">Lane of interest.</param>
        /// <returns>
        /// The finishing place associated with the specified lane.
        /// </returns>
        public FinishingPlaces Response_GetPlace(Lanes lane)
        {
            return this.response_FinshingPlaces[(int)lane];
        }

        // Private Methods
        //----------------------------------------------------------------------

        /// <summary>
        /// Gets the finishing place associated with a particular character from
        /// the response string.
        /// </summary>
        /// <param name="symbol">character</param>
        /// <returns>Finishing Place</returns>
        private FinishingPlaces GetPlaceFromSymbol(char symbol)
        {
            switch (symbol)
            {
                case '!':
                    return FinishingPlaces.First;
                case '\"':
                    return FinishingPlaces.Second;
                case '#':
                    return FinishingPlaces.Third;
                case '$':
                    return FinishingPlaces.Forth;
                case '%':
                    return FinishingPlaces.Fifth;
                case '&':
                    return FinishingPlaces.Sixth;
                default: // ' ' or anything else:
                    return FinishingPlaces.NoPlace;
            }
        }
    }
    #endregion
}
