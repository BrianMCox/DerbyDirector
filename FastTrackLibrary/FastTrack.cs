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
// Added namespaces
using System.Threading; // necessary for lanuching new threads

namespace FastTrack
{
    public class K1Timer
    {
        #region Attributes and Properties 
        //**********************************************************************

        // Publicly Accessible (including encapsulated private attributes)
        //----------------------------------------------------------------------

        /// <summary>
        /// The custom com port communication object used to send to and receive 
        /// messages from the device.
        /// </summary>
        private FastTrackSerialComPort comPort;
        /// <summary>
        /// The custom com port communication object used to send to and receive 
        /// messages from the device.
        /// </summary>
        public FastTrackSerialComPort ComPort
        {
            get { return comPort; }
            set { comPort = value; }
        }

        // Result Evaluation Properties
        //......................................................................

        /// <summary>
        /// The default value for the Offset Results for Ties option.
        /// </summary>
        public const bool OffsetResultsForTiesDefault = true;

        /// <summary>
        /// Indicates whether the results of a race should be offset for ties.
        /// </summary>
        private bool offsetResultsForTies;
        /// <summary>
        /// Get or set whether the results will be offset for ties.
        /// </summary>
        public bool OffsetResultsForTies
        {
            get { return offsetResultsForTies; }
            set { offsetResultsForTies = value; }
        }

        /// <summary>
        /// Indicates whether the Eliminator Mode is enabled by default.
        /// </summary>
        public const bool EliminatorModeDefault = false;

        /// <summary>
        /// Indicates whether the device indicated that the last results were
        /// cleared. 
        /// </summary>
        private bool lastResultsCleared;
        /// <summary>
        /// Indicates whether the device indicated that the last results were
        /// cleared. 
        /// </summary>
        public bool LastResultsCleared
        {
            get { return lastResultsCleared; }
        }

        // Lane Counts
        //......................................................................

        /// <summary>
        /// The number of logical lanes acknowledged by the device.
        /// </summary>
        public static int MaxLogicalLaneCount
        {
            get { return RaceResultsResponse.MaxLogicalLaneCount; }
        }

        /// <summary>
        /// The last detected number of physically lanes available to the device.
        /// </summary>
        private int lastDetectedPhysicalLaneCount;
        /// <summary>
        /// The last detected number of physically lanes available to the device.
        /// </summary>
        public int LastDetectedPhysicalLaneCount
        {
            get { return lastDetectedPhysicalLaneCount; }
        }

        // Reversed Lanes Info
        /// <summary>
        /// Inidicates whether the lanes are reversed. 
        /// </summary>
        public bool AreLanesReversed
        {
            get { return mode_LanesReversed; }
        }

        /// <summary>
        /// Indicates the number of lanes reversed. 
        /// </summary>
        public LaneCount NumberOfReversedLanes
        {
            get { return mode_ReversedLaneCount; }
        }

        // Automatic Reset
        //......................................................................

        /// <summary>
        /// The automatic reset minimum timeout in seconds. 
        /// </summary>
        public static double AutomaticResetMinDelay
        {
            get { return SetAutomaticResetTimeCommand.MinTimeout; }
        }

        /// <summary>
        /// The automatic reset maximum timeout in seconds.
        /// </summary>
        public static double AutomaticResetMaxDelay
        {
            get { return SetAutomaticResetTimeCommand.MaxTimeout; }
        }

        /// <summary>
        /// The amount of time between each increment of the automatic reset 
        /// option.
        /// </summary>
        public static double AutomaticResetTimeIncrement
        {
            get { return SetAutomaticResetTimeCommand.TimeIncrement; }
        }

        /// <summary>
        /// The last value set for the automatic reset option.
        /// </summary>
        private double automaticResetLastValueSet;
        /// <summary>
        /// The last value set for the automatic reset option.
        /// </summary>
        public double AutomaticResetLastValueSet
        {
            get { return automaticResetLastValueSet; }
        }

        // Features
        //......................................................................

        /// <summary>
        /// The number of device features possible. 
        /// </summary>
        /// Proviced mostly as a upper bound for iteration. 
        public static int FeatureCount
        {
            get { return ReturnFeaturesCommand.FeatureCount; }
        }

        // Internally mantained only
        //----------------------------------------------------------------------
        // Persistant Response Objects
        private RaceClearedResponse raceClearedResponseObj;
        private RaceResultsResponse raceResultsResponseObj;

        // Mode Values
        private LaneCount mode_ReversedLaneCount = LaneCount.None;
        private bool[] mode_MaskValues;
        private bool mode_LanesReversed = false;
        private bool mode_EliminatorMode = false;
        private RaceDataFormat mode_DataFormat = RaceDataFormat.New;

        // Features
        private ReturnFeaturesCommand featuresCommand;

        #endregion

        #region Events and Exceptions 
        // Events
        //**********************************************************************

        /// <summary>
        /// Delegate for the NewRaceResultsEvent. 
        /// </summary>
        /// <param name="sender">
        /// The timer object responsible for raising the event.
        /// </param>
        /// <param name="results">
        /// The race results. 
        /// </param>
        public delegate void NewRaceResultsEventHandler(
            K1Timer sender, RaceResult results);

        /// <summary>
        /// Event raised when a new set of race results are receive from the 
        /// device.
        /// </summary>
        public event NewRaceResultsEventHandler NewRaceResultsEvent;

        /// <summary>
        /// Delegate for the ResultsClearedEvent.
        /// </summary>
        /// <param name="sender">
        /// The timer object responsible for raising the event.
        /// </param>
        public delegate void ResultsClearedEventHandler(K1Timer sender);

        /// <summary>
        /// Event raised when the device reports that the results have been 
        /// cleared. 
        /// </summary>
        public event ResultsClearedEventHandler ResultsClearedEvent;

        // Exceptions
        //**********************************************************************
        public class DeviceCommunicationException : Exception { }

        #endregion

        #region Constructors
        //**********************************************************************

        /// <summary>
        /// Initializes a new Timer object and sets up a connection with the 
        /// timer device on the specified COM port. 
        /// </summary>
        /// <param name="portName">COM port name (e.g. "COM99")</param>
        public K1Timer(string portName) : this(portName, true, false) { }

        /// <summary>
        /// Initializes a new Timer object and sets up a connection with the 
        /// timer device on the specified COM port. 
        /// </summary>
        /// <param name="portName">COM port name (e.g. "COM99")</param>
        /// <param name="offsetResultsForTies">
        /// Indicate whether the results should be offset for ties.</param>
        /// <param name="useEliminatorMode">
        /// Indicate whether the Eliminator Mode option is desired.</param>
        public K1Timer(string portName, bool offsetResultsForTies,
            bool useEliminatorMode)
        {
            // initialize automatic response objects
            raceClearedResponseObj = new RaceClearedResponse();
            raceClearedResponseObj.RaceClearedEvent +=
                new RaceClearedResponse.RaceClearedEventHandler(
                RaceClearedEventHandler);

            raceResultsResponseObj = new RaceResultsResponse();
            raceResultsResponseObj.RaceResultsReceiveEvent +=
                new RaceResultsResponse.RaceResultReceivedEventHandler(
                RaceResultReceivedEventHandler);

            // setup com port
            this.comPort = new FastTrackSerialComPort(portName);
            this.comPort.ExpectedResponses.Add(raceClearedResponseObj);
            this.comPort.ExpectedResponses.Add(raceResultsResponseObj);

            // Open port
            if (!this.comPort.IsOpen && !this.comPort.Open())
            {
                throw new DeviceCommunicationException();
            }

            // Cache feature list
            this.featuresCommand = new ReturnFeaturesCommand();
            if (UpdateFeatureList())
            {
                throw new DeviceCommunicationException();
            }

            // Set misc. parameters
            this.offsetResultsForTies = offsetResultsForTies;
            this.SetEliminatorMode(useEliminatorMode);

            if (!RestoreDefaults() || // RestoreDefaults will reread the mode.
                (DetectNumberOfDeviceLanes() < 0) ||
                !ClearRace()) // Make the lights blink
            {
                throw new DeviceCommunicationException();
            }
        }

        #endregion

        #region Public Methods
        //**********************************************************************

        /// <summary>
        /// Updates this object's local copy of the track's feature list. 
        /// Returns true if the device call and following conditions succeed, 
        /// and false otherwise.
        /// (Number of device calls: 1)
        /// </summary>
        /// <returns>True if successful. Otherwise, false.</returns>
        public bool UpdateFeatureList()
        {
            return !featuresCommand.Send(comPort);
        }

        /// <summary>
        /// Indicates whether a specified feature is available or not.
        /// Note: This function uses cached values not a new device call.
        /// (Number of device calls: 0)
        /// </summary>
        /// <param name="feature">device feature in question</param>
        /// <returns>True if the feature is available. Otherwise, false.</returns>
        public bool IsFeatureAvailable(DeviceFeatures feature)
        {
            return featuresCommand.Response_GetFeature(feature);
        }

        /// <summary>
        /// Gets the device's 5-digit serial number if successful. Returns -1 if
        /// an error occurred.
        /// (Number of device calls: 1)
        /// </summary>
        /// <returns>Success: Device serial number. Error: -1.</returns>
        public int GetSerialNumber()
        {
            ReturnSerialNumberCommand cmd = new ReturnSerialNumberCommand();
            if (!cmd.Send(comPort) || !cmd.IsResponseSet)
            {
                return -1;
            }
            return cmd.Response_SerialNumber;
        }

        /// <summary>
        /// Detects the number of lanes physically supported by the device. 
        /// (Number of device calls: 9)
        /// </summary>
        /// <returns>Number of lanes on the device</returns>
        /// The implementation of this method relies on an quark in the way the
        /// device mode is recorded and reported. If a physical lane exists, the 
        /// ReadModeCommand will indicate that the lane is masked. If the lane 
        /// does not exist, the mode will not change.
        public int DetectNumberOfDeviceLanes()
        {
            if (!featuresCommand.Response_GetFeature(DeviceFeatures.MaskLane))
            {
                return MaxLogicalLaneCount;
            }
            // Get the current mode
            ReadModeCommand initialMode = new ReadModeCommand();
            if (!initialMode.Send(comPort) || !initialMode.IsResponseSet)
            {
                return -1;
            }

            // Set all lane masks
            MaskLaneCommand maskLane = new MaskLaneCommand(Lanes.LaneA);
            for (int i = 0; i < MaxLogicalLaneCount; i++)
            {
                if (!initialMode.Response_IsLaneMasked((Lanes)i))
                {
                    maskLane.SetLane((Lanes)i);
                    if (!maskLane.Send(comPort))
                    {
                        return -1; ;
                    }
                }
            }

            // Get the resulting mode
            ReadModeCommand testResultMode = new ReadModeCommand();
            if (!testResultMode.Send(comPort) || !testResultMode.IsResponseSet)
            {
                return -1;
            }

            // Find the number of masks that were set.
            int lane = 5;
            while (lane > 0 && !testResultMode.Response_IsLaneMasked((Lanes)lane))
            {
                lane--;
            }
            lane++;

            // Clear mask test values
            if (!new ResetLaneMasksCommand().Send(comPort))
            {
                return -1;
            }

            // Restore original mask
            for (int i = 0; i < 6; i++)
            {
                if (initialMode.Response_IsLaneMasked((Lanes)i))
                {
                    maskLane.SetLane((Lanes)i);
                    if (!maskLane.Send(comPort))
                    {
                        return -1;
                    }
                }
            }
            this.lastDetectedPhysicalLaneCount = lane;
            return lane;
        }

        /// <summary>
        /// Tests whether the software can still communicate with the device by
        /// sending a command that will not effect the device's settings and 
        /// waiting for a response. 
        /// (Number of device calls: 1)
        /// </summary>
        /// <returns>True if successful. False if not.</returns>
        public bool TestDeviceCommunication()
        {
            return RereadMode();
        }

        /// <summary>
        /// Forces a race to terminate. All results collected will be return 
        /// shortly. This function does not clear the finish lights on the device.
        /// (Number of device calls: 1)
        /// </summary>
        /// <returns>True if successful. False if not.</returns>
        public bool EndRace()
        {
            return featuresCommand.Response_GetFeature(
                DeviceFeatures.ForcePrint) &&
                new ForceResultsCommand().Send(comPort);
        }

        /// <summary>
        /// Forces a race to terminate, clear the lights and return any results 
        /// collected so far. 
        /// (Number of device calls: 2)
        /// </summary>
        /// If the manual reset switch is pressed, the timers will not start
        /// running but they will if it is released and a laser gate is not used
        /// instead. The command will also reset the laser gate incase it exists.
        public bool ClearRace()
        {
            // Reset Timer
            bool retVal = new ResetTimerCommand().Send(comPort);
            // Reset laser gate if the feature is available
            return (!featuresCommand.Response_GetFeature(DeviceFeatures.LaserReset)
                || new ResetLaserGateCommand().Send(comPort)) && retVal;
        }

        /// <summary>
        /// Restores the defaults settingd for the device and software settings. 
        /// (Number of device calls: 3-6)
        /// </summary>
        /// <returns>True if successful. False if not.</returns>
        public bool RestoreDefaults()
        {
            /* The return value has been calculated separately for each command
             to attempt to execute all of them. Calculating the value as a 
             single boolean expression would stop executing if one of them 
             failed.*/

            // Offset Results for Ties
            offsetResultsForTies = OffsetResultsForTiesDefault;

            // Set format to new
            bool retVal = new NewFormatCommand().Send(comPort);

            // Automatic reset = off
            retVal = new DisableAutomaticResetCommand().Send(comPort)
                && retVal;
            this.automaticResetLastValueSet = 0;

            // Eliminator mode = default
            retVal = this.SetEliminatorMode(EliminatorModeDefault)
                && retVal;

            // Reverse lanes = off
            retVal = (!IsFeatureAvailable(DeviceFeatures.ReverseLanes)
                || new ReverseLanesCommand(LaneCount.None).Send(comPort))
                && retVal;

            // Clear lane masks 
            retVal = (!IsFeatureAvailable(DeviceFeatures.MaskLane) ||
                (new ResetLaneMasksCommand().Send(comPort))) 
                && retVal;

            // Reset local copy of mode to reflect changes
            return RereadMode() && retVal;
        }

        /// <summary>
        /// Sets which lanes will be ignored by the device for during the next 
        /// race. 
        /// (Number of device calls: 8)
        /// </summary>
        /// <param name="useA">True if lane A should be ignored.</param>
        /// <param name="useB">True if lane B should be ignored.</param>
        /// <param name="useC">True if lane C should be ignored.</param>
        /// <param name="useD">True if lane D should be ignored.</param>
        /// <param name="useE">True if lane E should be ignored.</param>
        /// <param name="useF">True if lane F should be ignored.</param>
        /// <returns>True if commands sent successfully. Otherwise, false.</returns>
        public bool SetLaneMasks(bool useA, bool useB, bool useC, bool useD,
            bool useE, bool useF)
        {
            // Setup lane masks
            if (this.featuresCommand.Response_GetFeature(DeviceFeatures.MaskLane))
            {
                // Clear current masks
                if (!new ResetLaneMasksCommand().Send(comPort))
                {
                    return false;
                }

                bool[] masks = new bool[6];
                masks[0] = useA;
                masks[1] = useB;
                masks[2] = useC;
                masks[3] = useD;
                masks[4] = useE;
                masks[5] = useF;

                // Make specified lanes masked
                for (int i = 0; i < 6; i++)
                {
                    this.mode_MaskValues[i] = masks[i];
                    if (masks[i] && !new MaskLaneCommand((Lanes)i).Send(comPort))
                    {
                        return false;
                    }
                }
            }
            return RereadMode();
        }

        /// <summary>
        /// Returns true if a speicfied lane is currently set as masked. 
        /// Otherwise false.
        /// (Number of device calls: 0)
        /// </summary>
        /// <param name="lane">specifed lane</param>
        /// <returns>
        /// True if the lane is currently set as masked. Otherwise false.
        /// </returns>
        public bool IsMaskSet(Lanes lane)
        {
            return mode_MaskValues[(int)lane];
        }

        /// <summary>
        /// Enables the automatic reset option. 
        /// (Number of device calls: 1)
        /// </summary>
        /// <param name="seconds">The number of seconds for the timeout.</param>
        /// <returns>
        /// The number of seconds to which the time out was set.
        /// </returns>
        public double EnableAutomaticReset(double seconds)
        {
            SetAutomaticResetTimeCommand cmd =
                new SetAutomaticResetTimeCommand(seconds);
            seconds = cmd.GetResetTime();
            if (!cmd.Send(comPort))
            {
                return -1;
            }
            this.automaticResetLastValueSet = seconds;
            return seconds;
        }

        /// <summary>
        /// Disables the automatic reset action.
        /// (Number of device calls: 1)
        /// </summary>
        /// <returns>True if command is successful. Otherwise, false.</returns>
        public bool DisableAutomaticReset()
        {
            if (!new DisableAutomaticResetCommand().Send(comPort))
            {
                return false;
            }
            this.automaticResetLastValueSet = 0;
            return true;
        }

        /// <summary>
        /// Reverses a specified number of lanes starting at the first lane.
        /// The rest are ignored. 
        /// (Number of device calls: 1)
        /// </summary>
        /// <param name="lanecount">number of lanes to reverse</param>
        /// <returns>True if the command succeeds. Otherwise, false.</returns>
        public bool ReverseLanes(LaneCount lanecount)
        {
            return featuresCommand.Response_GetFeature(
                DeviceFeatures.ReverseLanes) &&
                new ReverseLanesCommand(lanecount).Send(comPort)
                && RereadMode();
        }

        /// <summary>
        /// Sets the eliminator mode to enabled or disabled. 
        /// (Number of device calls: 1)
        /// </summary>
        /// <param name="value">True to enable. False to diable.</param>
        /// <returns>True if the command succeeds. Otherwise, false.</returns>
        public bool SetEliminatorMode(bool value) 
        {
            return 
                IsFeatureAvailable(DeviceFeatures.EliminatorMode) && 
                (
                    value && new EnableEliminatorModeCommand().Send(comPort)
                    ||
                    !value && new DisableEliminatorModeCommand().Send(comPort)
                )
                && RereadMode();
        }
        
        /// <summary>
        /// Indicates whether the eliminator mode option is enabled. 
        /// </summary>
        /// <returns>True if enabled. False if disabled.</returns>
        public bool IsEliminatorModeEnabled()
        {
            return this.mode_EliminatorMode;
        }

        #endregion
        
        #region Private Methods
        //**********************************************************************

        /// <summary>
        /// Gets the mode from the device and updates the local copy of the mode.
        /// (Number of device calls: 1)
        /// </summary>
        /// <returns>True if device call is successful. False if not.</returns>
        private bool RereadMode()
        {
            ReadModeCommand cmd = new ReadModeCommand();
            if (!cmd.Send(comPort) || !cmd.IsResponseSet)
            {
                return false;
            }

            this.mode_ReversedLaneCount = cmd.Response_ReversedLaneCount;
            this.mode_MaskValues = new bool[MaxLogicalLaneCount];
            for (int i = 0; i < MaxLogicalLaneCount; i++)
            {
                this.mode_MaskValues[i] = cmd.Response_IsLaneMasked((Lanes)i);
            }
            this.mode_LanesReversed = cmd.Response_LanesReversed;
            this.mode_EliminatorMode = cmd.Response_EliminatorMode;
            this.mode_DataFormat = cmd.Response_DataFormat;
            return true;
        }

        // Event Handlers
        //----------------------------------------------------------------------

        /// <summary>
        /// Handels the RaceResultsReceiveEvent thrown by the device response 
        /// object and raies this.RaceResultReceivedEvent. 
        /// </summary>
        /// <param name="response">Device response object</param>
        private void RaceResultReceivedEventHandler(RaceResultsResponse response)
        {
            // Ensure that the response object is not null.
            if (!response.IsResponseSet)
            {
                return; // should not happen
            }

            RaceResult results = new RaceResult(response, mode_MaskValues, 
                offsetResultsForTies, this.IsEliminatorModeEnabled());

            this.lastResultsCleared = false;

            if (this.NewRaceResultsEvent != null)
            {
                this.NewRaceResultsEvent(this, results);
            }
        }

        /// <summary>
        /// Handels the race cleared event raised by the RaceClearedResponse 
        /// object for this track.
        /// </summary>
        private void RaceClearedEventHandler()
        {
            lastResultsCleared = true;

            if (this.ResultsClearedEvent != null)
            {
                this.ResultsClearedEvent(this);
            }
        }

        #endregion
    }
}