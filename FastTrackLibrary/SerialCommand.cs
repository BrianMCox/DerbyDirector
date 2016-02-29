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
using System.Text.RegularExpressions; // added for regular expression matching.

namespace FastTrack
{
    #region Enums 
    /// <summary>
    /// Set of possible lanes. Individual hardare may use few lanes.
    /// </summary>
    /// This enum is provided as a means forcing parameters to be valid.
    public enum Lanes { LaneA = 0, LaneB, LaneC, LaneD, LaneE, LaneF }

    /// <summary>
    /// Specifies a number of lanes.
    /// </summary>
    /// This enum is provided mostly as a means of forcing parameters to use 
    /// a valid set of values.
    public enum LaneCount { None = 0, One, Two, Three, Four, Five, Six }

    /// <summary>
    /// Specifies the race results reponse format.
    /// </summary>
    public enum RaceDataFormat { Old = 0, New = 1 }

    /// <summary>
    /// List of feature flags provided by the device.
    /// </summary>
    public enum DeviceFeatures
    {
        OptionU = 0, // unused
        CountDownClock, // no effect on K1 timers
        LaserReset, // ResetLaserGateCommand
        ForcePrint, // ForceResultsCommand
        EliminatorMode, // Eliminator mode commands
        ReverseLanes, // ReverseLanesCommand
        MaskLane, // MaskLanesCommand and ResetLaneMasksCommand
        SerialData // a given if it works with the computer at all
    }

    #endregion

    #region SerialCommand (base class)
    //##########################################################################
    /// <summary>
    /// Base class for all command objects that encapsulate the input string 
    /// sent to the com port.
    /// </summary>
    /// The base class contains almost all the implementation necessary for a 
    /// simple command that returns a simple acknowledgement. If the command 
    /// expects to receive data in return or uses a more complicated input 
    /// format, the child class will have to override the methods in this class.
    public abstract class SerialCommand : SerialResponse
    {
        // Attributes and Properties
        //**********************************************************************

        // Set by child class / Command description
        //----------------------------------------------------------------------
        /// The values of the following attributes must be set by derived classes. 

        /// <summary>
        /// The actual input sent to the com port.
        /// </summary>
        /// This attribute must be set by the derived class. 
        /// Its value should not be the empty string. The command must represent
        /// a single atomic command.
        protected string commandString;
        /// <summary>
        /// The actual input sent to the com port. Read only.
        /// </summary>
        public virtual string CommandString
        {
            get { return commandString; }
        }

        // Constructor
        //**********************************************************************
        /// <summary>
        /// Base class contructor.
        /// Initializes the response data managed by this class.
        /// </summary>
        /// As an abstract class this method can only be called by a constructor 
        /// of a derived class.
        protected SerialCommand(string commandString,
            string responsePattern, string partialResponsePattern)
            : base(responsePattern, partialResponsePattern, true)
        {
            this.commandString = commandString;
        }

        // Methods
        //**********************************************************************

        /// <summary>
        /// Sends the command to a specified comPort and waits for a response.
        /// After the respone, or indication of failure, is returned the 
        /// response data 
        /// </summary>
        /// <param name="comPort">
        /// Comm port to which the command will be sent.
        /// </param>
        /// <returns>
        /// True if the port sucessfully reponded, otherwise false.
        /// </returns>
        public virtual bool Send(FastTrackSerialComPort comPort)
        {
            return comPort.SendCommand(this);
        }
    }
    #endregion

    #region ForceResultsCommand - RA 
    //##########################################################################
    /// <summary>
    /// Emplements a SerialCommand that causes all timer results to be sent to 
    /// be returned and stop listening for further results. 
    /// </summary>
    /// Command Effect:
    /// If no lane results have been found (i.e. no cars crossed the finish line)
    /// or the start switch is has not been released, the command should only 
    /// return its normal expected response. 
    /// If some but, not all, of the results on all lanes that are not masked 
    /// have be received, then the command response should be sent followed by 
    /// the race results "automatic input".
    /// If cars on all unmasked lanes have finished, then only the command 
    /// acknowledgement should be received.
    public class ForceResultsCommand : SerialCommand
    {
        /// <summary>
        /// Initializes a new ForceResultsCommand
        /// </summary>
        public ForceResultsCommand()
            : base("RA", // commandString
                @"RA\r\n\*\r\n", // responsePattern 
                // partialResponsePattern
                @"(R|RA|RA\r|RA\r\n|RA\r\n\*|RA\r\n\*\r)")
        {
        }
    }
    #endregion

    #region ResetTimerCommand - RX 
    //##########################################################################
    /// <summary>
    /// Emplements the SerialCommand that causes timer results, if any, from the 
    /// last race to be returned and resets the finish lights and timers.
    /// </summary>
    /// Command results: 
    /// Essentially does the same thing as pressing and releasing the start 
    /// switch. 
    /// If the start switch is pressed, the command returns an acknowledgement
    /// and does nothing.
    /// If the start switch is released, the command operates in the same way 
    /// as the ForceResultsCommand, but also resets the finish lights and 
    /// restarts clocks on each lane. 
    public class ResetTimerCommand : SerialCommand
    {
        /// <summary>
        /// Initializes a new ResetTimerCommand
        /// </summary>
        public ResetTimerCommand() 
            : base("RX", // commandString
                @"RX\r\n\*\r\n", // responsePattern
                // partialResponsePattern
                @"(R|RX|RX\r|RX\r\n|RX\r\n\*|RX\r\n\*\r)")
        { }
    }
    #endregion

    #region MaskLaneCommand - MA, MB, MC, MD, ME, MF 
    //##########################################################################
    /// <summary>
    /// Emplements the SerialCommand that causes a specifid lane to be ignored
    /// when determining the results of a race.
    /// </summary>
    /// Command Results:
    /// The resulting I/O is only the command acknowledgement.
    /// If the masked lane is not physically available, the command will have no
    /// effect. The response will be the same. Masking lanes E or F on a 4 lane 
    /// device will not change anything on the device. 
    /// If a lane is not physially available, it will be ignored automaticlly,
    /// but the reported mode for the lane will never be marked as masked.
    /// If the lane exists, the results of the following ReadModeCommand will be 
    /// changed immediately. The lane will not be masked till the start of the 
    /// next race. Masking a lane that is already masked will have no effect.
    /// The order inwhich lanes are identified by the lane letter is not 
    /// dependent state of the ReverseLanes feature.
    /// It is possible to mask all available lanes.
    /// 
    /// This class really manages 6 different command string, but the commands 
    /// are so close in format and function that they can be handled as one 
    /// command that takes a parameter. The command's responses are specific the 
    /// lane being masked.
    public class MaskLaneCommand : SerialCommand
    {
        /// <summary>
        /// Initializes a new MaskLaneCommand
        /// </summary>
        /// <param name="lane">Specifies the lane to mask.</param>
        public MaskLaneCommand(Lanes lane) 
            : base(GetCommandString(lane),
                GetResponsePattern(lane), 
                GetPartialResponsePattern(lane))
        {
        }

        /// <summary>
        /// Changes what lane is masked by the command. 
        /// This action will reset any current response data.
        /// </summary>
        /// <param name="lane">Specifies the new lane to mask.</param>
        /// The function appears to change the command's parameter, but really 
        /// changes the whole command and response indentification.
        public void SetLane(Lanes lane)
        {
            base.commandString = GetCommandString(lane);
            base.responsePattern = GetResponsePattern(lane);
            base.partialResponsePattern = GetPartialResponsePattern(lane);

            base.ResetResponseData();
        }

        /// <summary>
        /// Gets the currently specified lane to be masked by the command.
        /// </summary>
        /// <returns>The currently specified lane.</returns>
        public Lanes GetLane()
        {
            /// The lane information is parsed directly from the commandString
            /// rather than storing it as a data member to ensure that the 
            /// information returned is an uptodate representation of what the 
            /// command will do.
            switch (base.commandString)
            {
                case "MA":
                    return Lanes.LaneA;
                case "MB":
                    return Lanes.LaneB;
                case "MC":
                    return Lanes.LaneC;
                case "MD":
                    return Lanes.LaneD;
                case "ME":
                    return Lanes.LaneE;
                case "MF":
                    return Lanes.LaneF;
                default: // This should not happen.
                    // Reset the command to something valid.
                    this.SetLane(Lanes.LaneF);
                    /// To avoid leaving the command in an invalid state, the 
                    /// last lane is selected. As most tracks do not have 6 
                    /// lanes, this is the least likely to have a impact if it
                    /// is sent.
                    return Lanes.LaneF;
            }
        }

        /// <summary>
        /// Returns the part of the command string that specifies the lane.
        /// </summary>
        /// <param name="lane">Specified lane to be masked</param>
        /// <returns>
        /// The part of the command string that specifies the lane.
        /// </returns>
        private static string GetCommandString(Lanes lane)
        {
            switch (lane)
            {
                case Lanes.LaneA:
                    return "MA";
                case Lanes.LaneB:
                    return "MB";
                case Lanes.LaneC:
                    return "MC";
                case Lanes.LaneD:
                    return "MD";
                case Lanes.LaneE:
                    return "ME";
                case Lanes.LaneF:
                    return "MF";
                default: // This should be impossible.
                    /// It is better to return a value that can be used to 
                    /// construct a valid command then allow the an invalid 
                    /// command to be built.
                    return "MF";
            }
        }

        /// <summary>
        /// Builds and returns a regular expression for a complete response.
        /// </summary>
        /// <param name="lane">Lane to be masked</param>
        /// <returns>Regular expression for a complete respone</returns>
        private static string GetResponsePattern(Lanes lane)
        {
            return GetCommandString(lane) + @"\r\n\*\r\n";
        }

        /// <summary>
        /// Builds and returns a regular expression for a partial response.
        /// </summary>
        /// <param name="lane">Lane to be masked</param>
        /// <returns>Regular expression for a partial</returns>
        private static string GetPartialResponsePattern(Lanes lane)
        {
            string commandString = GetCommandString(lane);
            return @"(M|" + commandString +
                @"|" + commandString + @"\r" +
                @"|" + commandString + @"\r\n" +
                @"|" + commandString + @"\r\n\*" +
                @"|" + commandString + @"\r\n\*\r)";
        }
    }
    #endregion

    #region ResetLaneMasksCommand - MG 
    //##########################################################################
    /// <summary>
    /// SerialCommand that resets (clears) all lane masks. 
    /// </summary>
    /// Command Results:
    /// The command will set turn off the mask for all six lanes.
    /// The command's acknowledgement is the only exprect response.
    /// The mode of the device will be changed immediately, but the change in 
    /// which lanes are ignored will take effect the next time a race is started.
    public class ResetLaneMasksCommand : SerialCommand
    {
        /// <summary>
        /// Initializes a new ResetLaneMasksCommand.
        /// </summary>
        public ResetLaneMasksCommand() 
            : base("MG", @"MG\r\nAC", @"(M|MG|MG\r|MG\r\n|MG\r\nA)")
        { }
    }
    #endregion

    #region ReverseLanesCommand - RL0, RL1, RL2, RL3, RL4, RL5, RL6 
    //##########################################################################
    /// <summary>
    /// Serial Command that reverses the order in which race results are 
    /// associated with the lane lables for a specified number of lanes.
    /// </summary>
    /// Command Result:
    /// The command only effects the way that results are return. 
    /// Setting the number of reversed lanes to 0 will set the system back to 
    /// normal.
    /// Consider the following results string returned in normal (non-reversed)
    /// mode:
    ///     A=1.111! B=2.222" C=3.333# D=4.444$ E=5.555% F=6.666&
    /// If 4 lanes are reversed the response would be as follows:
    ///     A=4.444$ B=3.333# C=2.222" D=1.111! E=0.000  F=0.000
    /// The specified number of lanes is intended to be equal to the number of 
    /// physical lanes available. 
    /// The result for all lanes not reversed is returned as "0.000".
    /// The result will still wait until all the results from all unmasked lanes
    /// are found before returning even if the lane's result will be ignored 
    /// because it was not one of the reversed lanes.
    /// 
    /// This class manages 7 distinct command strings, but their format and 
    /// and function are close enought to handle as one command with a parameter.
    public class ReverseLanesCommand : SerialCommand
    {
        /// <summary>
        /// Initializes a new instance of the ReverseLanesCommand.
        /// </summary>
        /// <param name="laneCount">Number of lanes to reverse</param>
        public ReverseLanesCommand(LaneCount laneCount) 
            : base(GetCommandString(laneCount),
                GetResponsePattern(laneCount),
                GetPartialReponsePattern(laneCount))
        {
        }

        /// <summary>
        /// Changes the number of lanes reversed by the command.
        /// Changing the command will reset (clear) the response data.
        /// </summary>
        /// <param name="laneCount">Number of lanes to reverse</param>
        public void SetLaneCount(LaneCount laneCount)
        {
            base.commandString = GetCommandString(laneCount);
            base.responsePattern = GetResponsePattern(laneCount);
            base.partialResponsePattern = GetPartialReponsePattern(laneCount);

            base.ResetResponseData();
        }

        /// <summary>
        /// Gets the number of lanes the command is currently set to reverse.
        /// </summary>
        /// <returns>Number of lanes the command is set to reverse</returns>
        public LaneCount GetLaneCount()
        {
            return (LaneCount)(Convert.ToInt32(base.commandString[2]) - 48);
        }

        /// <summary>
        /// Builds and returns the command string.
        /// </summary>
        /// <param name="laneCount">number of lanes to reverse.</param>
        /// <returns>command string</returns>
        private static string GetCommandString(LaneCount laneCount)
        {
            return "RL" + ((int)laneCount).ToString();
        }

        /// <summary>
        /// Builds and returns the response pattern.
        /// </summary>
        /// <param name="laneCount">number of lanes to reverse</param>
        /// <returns>response pattern</returns>
        private static string GetResponsePattern(LaneCount laneCount)
        {
            return GetCommandString(laneCount) + @"\r\n\*\r\n";
        }

        /// <summary>
        /// Builds and returns the partial response pattern.
        /// </summary>
        /// <param name="laneCount">number of lanes to reverse</param>
        /// <returns>partial response pattern</returns>
        private static string GetPartialReponsePattern(LaneCount laneCount)
        {
            string commandString = GetCommandString(laneCount);
            return @"(R|RL|" + commandString +
                @"|" + commandString + @"\r" +
                @"|" + commandString + @"\r\n" +
                @"|" + commandString + @"\r\n\*" +
                @"|" + commandString + @"\r\n\*\r)";
        }
    }
    #endregion

    #region EnableEliminatorModeCommand - LE 
    //##########################################################################
    /// <summary>
    /// SerialCommand that turns on eliminator mode.
    /// </summary>
    /// Command Result:
    /// In eliminator mode the lanes are scored in pairs. Lanes A and B, C and D,
    /// E and F are pair together. Each pair is given one first place and one 
    /// second place. 
    /// I am not sure how a track with an odd number of lanes is handled. I 
    /// assume that the odd lane is ignored but it may also be given an 
    /// uncontended first place.
    /// The flag in the devices reported mode will be set immediately.
    /// The change in the behavior of the lights and results will not happen 
    /// till the start of the next race. A race that is in progress will not be
    /// altered. 
    public class EnableEliminatorModeCommand : SerialCommand
    {
        /// <summary>
        /// Initializes a new EnableEliminatorModeCommand object.
        /// </summary>
        public EnableEliminatorModeCommand() 
            : base("LE", @"LE\r\n\*\r\n",
                @"(L|LE|LE\r|LE\r\n|LE\r\n\*|LE\r\n\*\r)")
        { }
    }
    #endregion

    #region DisableEliminatorModeCommand - RE 
    //##########################################################################
    /// <summary>
    /// SerialCommand that disables eliminator mode.
    /// </summary>
    /// Command Result:
    /// Eliminator mode is turned off and normal scoring is resumed.
    /// The command is not exprected to cause any other response than the 
    /// acknowledgement that it matches. 
    /// The flag in the devices reported mode will be set immediately.
    /// The change in the behavior of the lights and results will not happen 
    /// till the start of the next race. A race that is in progress will not be
    /// altered. 
    public class DisableEliminatorModeCommand : SerialCommand
    {
        /// <summary>
        /// Initializes a new DisableEliminatorModeCommand object.
        /// </summary>
        public DisableEliminatorModeCommand()
            : base("RE", @"RE\r\n\*\r\n",
                @"(R|RE|RE\r|RE\r\n|RE\r\n\*|RE\r\n\*\r)")
        { }
    }
    #endregion

    #region ReturnFeaturesCommand - RF 
    //##########################################################################
    /// <summary>
    /// SerialCommand that gets the list of features the device supports.
    /// </summary>
    /// Command Result:
    /// The command is not exprected to cause any other response than the 
    /// acknowledgement that it matches. 
    /// The response data is in the form of a list of 8 flags. Each is set to 
    /// "1" if the feature is supported and "0" if not. The order of the flags 
    /// is as follows:
    ///     U: Unused
    ///         For future use.
    ///     C: Count Down Clock
    ///         Does nothing on the K1 timer.
    ///     L: Laser Reset
    ///         Allows a laser gate to be reset from the computer.
    ///         If a laser gate is not used, it does nothing.
    ///         Enables the ResetLaserGateCommand.
    ///     F: Force Print
    ///         Enables the ForceResultsCommand
    ///     E: Eliminator Mode
    ///         Enables the EnableEliminatorModeCommand and the 
    ///         DisableEliminatorModeCommand.
    ///     R: Reverse Lanes
    ///         Enables the ReverseLanesCommand.
    ///     M: Mask Lanes
    ///         Enables the MaskLaneCommand and ResetLaneMaskCommand.
    ///     S: Serial Data 
    ///         Allows the computer to communicate with device.
    public class ReturnFeaturesCommand : SerialCommand
    {
        // Attributes and Properties
        //**********************************************************************

        /// <summary>
        /// Array of boolean values indicateding whether each feature was 
        /// reported to be enabled.
        /// </summary>
        private bool[] response_FeatureArray;

        /// <summary>
        /// Number of features.
        /// </summary>
        /// Also used for initializing the feature array.
        public const int FeatureCount = 8;

        // Constructor
        //**********************************************************************

        /// <summary>
        /// Initializes an new ReturnFeaturesCommand object.
        /// </summary>
        public ReturnFeaturesCommand() 
            : base("RF", // commandString
                // responsePattern
                @"RF\r\n(0|1)(0|1)(0|1)(0|1) (0|1)(0|1)(0|1)(0|1)\r\n\*\r\n",
                // partialResponsePattern
                @"(R|RF|RF\r|RF\r\n(0|1){0,4}" +
                @"|RF\r\n(0|1){4} (0|1){0,4}" +
                @"|RF\r\n(0|1){4} (0|1){4}\r" +
                @"|RF\r\n(0|1){4} (0|1){4}\r\n" +
                @"|RF\r\n(0|1){4} (0|1){4}\r\n\*" +
                @"|RF\r\n(0|1){4} (0|1){4}\r\n\*\r)")
        {
            this.ResetFeatureArray();
        }

        // Methods
        //**********************************************************************

        /// <summary>
        /// Gets the reponse value for a specified feature.
        /// </summary>
        /// <param name="feature">Specified feature</param>
        /// <returns>True if enabled. False if not.</returns>
        public bool Response_GetFeature(DeviceFeatures feature)
        {
            return response_FeatureArray[(int)feature];
        }

        /// <summary>
        /// Evaluates the response sucess and response string and sets the 
        /// response datat accordingly.
        /// </summary>
        /// <param name="deviceResponded">
        /// True if the device responded. False if a reponse was not received.
        /// </param>
        /// <param name="response">
        /// String received from the device.
        /// </param>
        public override void SetResponseData(string response)
        {
            base.SetResponseData(response);
            if (base.isResponseSet)
            {
                GroupCollection groups = Regex.Match(response,
                    base.responsePattern).Groups;
                for (int i = 0; i < response_FeatureArray.Length; i++)
                {
                    this.response_FeatureArray[i] =
                        (groups[i + 1].ToString() == "1");
                }
            }
        }

        /// <summary>
        /// Clears the command's response data.
        /// </summary>
        public override void ResetResponseData()
        {
            base.ResetResponseData();
            this.ResetFeatureArray();
        }

        /// <summary>
        /// Sets all of the values in the feature array to false.
        /// </summary>
        private void ResetFeatureArray()
        {
            response_FeatureArray = new bool[FeatureCount];
            for (int i = 0; i < response_FeatureArray.Length; i++)
            {
                response_FeatureArray[i] = false;
            }
        }
    }
    #endregion

    #region ReturnSerialNumberCommand - RS 
    //##########################################################################
    /// <summary>
    /// SerialCommand that returns the timer hardware's serial number.
    /// </summary>
    /// Command Results:
    /// The command does not effect the device in any known way.
    /// The only io exprected is the response that contains the serial number.
    public class ReturnSerialNumberCommand : SerialCommand
    {
        // Attributes and Properties
        //**********************************************************************

        /// <summary>
        /// Serial number read from the response string.
        /// </summary>
        private int response_SerialNumber;
        /// <summary>
        /// Serial number read from the response string.
        /// Read only.
        /// </summary>
        public int Response_SerialNumber
        {
            get { return response_SerialNumber; }
        }

        // Constructor
        //**********************************************************************

        /// <summary>
        /// Initailizes a new instance of a ReturnSerialNumberCommand.
        /// </summary>
        public ReturnSerialNumberCommand()
            : base("RS", @"RS\r\n([0-9]{5})\r\n",
                @"(R|RS|RS\r|RS\r\n[0-9]{0,5}|RS\r\n[0-9]{5}\r)")
        {
            this.response_SerialNumber = 0;
        }

        // Methods
        //**********************************************************************

        /// <summary>
        /// Evaluates the response sucess and response string and sets the 
        /// response datat accordingly.
        /// </summary>
        /// <param name="response">
        /// String received from the device.
        /// </param>
        public override void SetResponseData(string response)
        {
            base.SetResponseData(response);
            if (base.isResponseSet)
            {
                this.response_SerialNumber = Convert.ToInt32(Regex.Match(
                    response, base.responsePattern).Groups[1].ToString());
            }
        }

        /// <summary>
        /// Clears the command's response data.
        /// </summary>
        public override void ResetResponseData()
        {
            base.ResetResponseData();
            response_SerialNumber = 0;
        }
    }
    #endregion

    #region ResetLaserGateCommand - LR 
    //##########################################################################
    /// <summary>
    /// SerialCommand that resets the laser gate for the next race if a laser
    /// gate is used.
    /// </summary>
    /// Command Results:
    /// (I do not have a laser gate, so this is partly speculation.)
    /// This command does nothing if a laser gate is not used or the feature is 
    /// not supported. 
    /// The command is assumed to work the same way that pressing the start 
    /// switch. When the cars or start gate crosses the laser it is the 
    /// equivalent of releasing the start switch.
    public class ResetLaserGateCommand : SerialCommand
    {
        /// <summary>
        /// Initializes a new ResetLaserGateCommand instance.
        /// </summary>
        public ResetLaserGateCommand()
            : base("LR", @"LR\r\n\*\r\n",
                @"(L|LR|LR\r|LR\r\n|LR\r\n\*|LR\r\n\*\r)")
        { }
    }
    #endregion

    #region SetAutomaticResetTimeCommand - LXA to LXO 
    //##########################################################################
    /// <summary>
    /// SerialCommand that turns on an automatic reset timer and sets the timeout.
    /// </summary>
    /// Command Results:
    /// The command turns on the automatic reset timer mode.
    /// The command will be reflected in the mode flags immediately but will not 
    /// have an affect till the the next race is started.
    /// The reset timer will start after receiving the first car crosses the 
    /// finish line. Each time car crosses the finish line it will restart the 
    /// reset timer each time a car cross the line. If the set time elapses 
    /// before the last recer crosses the finish line the result will be sent 
    /// and the subsequent finishers will be ingnored. After the results are 
    /// sent, either by timeout or all of the cars crosse the line, the timer 
    /// will start again. After it elapses, the lights and lane times will be
    /// reset. If the start switch is released, the times for the next race will
    /// start running. I suspect that if the a laser gate is used it will also 
    /// be reset and will wait to be tripped to start the next race.
    /// 
    /// The set of possible times is one of 14 descrete value. The values are 
    /// indicated by the letters A through O. The associated times are have 
    /// been experimentally determined to be multiple of 1.65 seconds. 
    /// LXA = 1 * 1.65 = 1.65 sec.
    /// LXB = 2 * 1.65 = 3.30 sec.
    /// ...
    /// LXO = 15 * 1.65 = 24.75 sec.
    public class SetAutomaticResetTimeCommand : SerialCommand
    {
        // Constants 
        //**********************************************************************

        /// <summary>
        /// Minimum timeout.
        /// </summary>
        private const int minMultipule = 1;
        /// <summary>
        /// Minimum timeout. Read only.
        /// </summary>
        public static double MinTimeout
        {
            get { return (double)minMultipule * TimeIncrement; }
        }


        /// <summary>
        /// Maximum timeout not including disabling the automatic reset.
        /// </summary>
        private const int maxMultiple = 15;
        /// <summary>
        /// Maximum timeout not including disabling the automatic reset.
        /// Read only.
        /// </summary>
        public static double MaxTimeout
        {
            get { return (double)maxMultiple * TimeIncrement; }
        }

        /// <summary>
        /// Time Increment between between set values. 
        /// This time is an estimate.
        /// </summary>
        public const double TimeIncrement = 1.65;

        // Constructor
        //**********************************************************************
        /// <summary>
        /// Initializes a new instance of the class with a specified timeout.
        /// The timeout will be rounded to the nearest multiple of the 
        /// TimeIncrement within the min and max values.
        /// </summary>
        /// <param name="time">target timeout in seconds</param>
        public SetAutomaticResetTimeCommand(double time)
            : base(GetCommandString(time),
                GetResponsePattern(time),
                GetPartialResponsePattern(time))
        {
        }

        /// <summary>
        /// Gets the current timeout to which the command is set.
        /// This time is an estimate.
        /// </summary>
        /// <returns>The the timout in seconds.</returns>
        public double GetResetTime()
        {
            return (double)(Convert.ToInt32(base.commandString[2]) - 64) 
                * TimeIncrement;
        }

        /// <summary>
        /// Sets the reset timeout. The input time will be rounded to the nearest 
        /// multiple of the TimeIncrement that is between the max and min.
        /// </summary>
        /// <param name="time">desired timeout</param>
        public void SetResetTime(double time)
        {
            base.commandString = GetCommandString(time);
            base.responsePattern = GetResponsePattern(time);
            base.partialResponsePattern = GetPartialResponsePattern(time);
            base.ResetResponseData();
        }

        /// <summary>
        /// Builds and returns the command string.
        /// </summary>
        /// <param name="time">autoreset timeout</param>
        /// <returns>command string</returns>
        private static string GetCommandString(double time)
        {
            // round the time to the nearest whole multiple of the time
            // increment. 
            int level = (int)(time / TimeIncrement + .5);
            // set to min if lower than min
            if (level < minMultipule)
            {
                level = minMultipule;
            }
            else if (level > maxMultiple)
            { // set to max if greater than max
                level = maxMultiple;
            }
            // convert to a character (1 = 'A')
            return "LX" + Convert.ToChar(level + 64);
        }

        /// <summary>
        /// Builds and returns the response pattern.
        /// </summary>
        /// <param name="time">autoreset timeout</param>
        /// <returns>response pattern</returns>
        private static string GetResponsePattern(double time)
        {
            return GetCommandString(time) + @"\r\n\*\r\n";
        }

        /// <summary>
        /// Builds and returns the partial response pattern.
        /// </summary>
        /// <param name="time">autoreset timeout</param>
        /// <returns>partial response pattern</returns>
        private static string GetPartialResponsePattern(double time)
        {
            string commandString = GetCommandString(time);
            return @"(L|LX|" + commandString +
                @"|" + commandString + @"\r" +
                @"|" + commandString + @"\r\n" +
                @"|" + commandString + @"\r\n\*" +
                @"|" + commandString + @"\r\n\*\r)";
        }
    }
    #endregion

    #region DisableAutomaticResetCommand - LXP 
    //##########################################################################
    /// <summary>
    /// SerialCommand that clears the automatic reset timeout.
    /// </summary>
    /// Command Result:
    /// Another way to think of it is setting the automatic reset timeout to 
    /// infinity. 
    /// The command appears to take effect whenever the timeout timer is reset.
    /// (See the SetAutomaticResetTimeCommand explanation.)
    /// No reponse is directly expected except the matched response.
    public class DisableAutomaticResetCommand : SerialCommand
    {
        public DisableAutomaticResetCommand()
            : base("LXP", @"LXP\r\n\*\r\n",
                @"(L|LX|LXP|LXP\r|LXP\r\n|LXP\r\n\*|LXP\r\n\*\r)")
        { }
    }
    #endregion
    
    #region OldFormatCommand - N0 
    //##########################################################################
    /// <summary>
    /// Serial Command that sets the format of the race results to the lagecy 
    /// style. Unless the software using this library is dependant on this 
    /// format it is better to use the new format.
    /// </summary>
    /// The old format only marks the first place with a token. All other places
    /// must be infered from the times recorded for each. 
    /// The old format is as follows:
    ///     "A=3.001! B=3.002  C=3.003  D=3.004  E=3.005  F=3.006  \r\n"
    /// If the lane is first it is follows by "!" and one space.
    /// If it is not first it is followed by two spaces.
    /// As this class does not need this format it uses the new format 
    /// exclusively. Other than for testing purposes this command will probably 
    /// not be used.
    public class OldFormatCommand : SerialCommand
    {
        /// <summary>
        /// Initializes a new instance of the OldFormatCommand.
        /// </summary>
        public OldFormatCommand()
            : base("N0", @"N0\r\n\*\r\n", 
                @"(N|N0|N0\r|N0\r\n|N0\r\n\*|N0\r\n\*\r)")
        { }
    }
    #endregion

    #region NewFormatCommand - N1 
    //##########################################################################
    /// <summary>
    /// Serial Command that sets the format of the race results to the new style. 
    /// </summary>
    /// The new format uses a different token to identify all the places on 
    /// lanes that have not been ignored. The addvantage of this method is that
    /// the places will be identified even if the lane times have reached the 
    /// upper limit. The times and tokens also serve to varify one another.
    /// The new format is as follows:
    ///     "A=3.001! B=3.002" C=3.003# D=3.004$ E=3.005% F=3.006& \r\n"
    /// If the lane was ignored, the token is replaced by a space as a 
    /// placeholder.
    public class NewFormatCommand : SerialCommand
    {
        /// <summary>
        /// Initializes a new instance of the NewFormatCommand.
        /// </summary>
        public NewFormatCommand()
            : base("N1", @"N1\r\n\*\r\n", 
                @"(N|N1|N1\r|N1\r\n|N1\r\n\*|N1\r\n\*\r)")
        { }
    }
    #endregion

    #region ReadModeCommand - RM 
    //##########################################################################
    /// <summary>
    /// SerialCommand that reads the mode of the device. 
    /// </summary>
    /// The mode of the device is contains a current state of the following 
    /// options:
    ///     L: The number of reversed lanes. 
    ///         value = 0-6
    ///     A-F: Which lanes are masked.
    ///         value = 0-1
    ///     R: Whether lanes have been reversed.
    ///         value = 0-1
    ///     X: Whether eliminator mode has been enabled.
    ///         value = 0-1
    ///     N: Which results format is being used.
    ///         value = 0-1
    /// The format of the data portion of the response is below. The letters 
    /// uses a place holders and the possible values they represent are listed
    /// above.
    ///     "L ABCDEF R X N"
    /// The command has no effect on the state of the device.
    /// The only reponse exprected is the matched response. 
    public class ReadModeCommand : SerialCommand
    {
        // Attributes and Properties
        //**********************************************************************

        /// <summary>
        /// The number of lanes that have been reversed. 
        /// </summary>
        private LaneCount response_ReversedLaneCount;
        /// <summary>
        /// The number of lanes that have been reversed. 
        /// Read only.
        /// </summary>
        public LaneCount Response_ReversedLaneCount
        {
            get { return response_ReversedLaneCount; }
        }

        /// <summary>
        /// True if the lane is masked. False if not.
        /// </summary>
        private bool[] response_MaskValues;

        /// <summary>
        /// Indicates if lanes have been reversed. True if so. False if not.
        /// </summary>
        private bool response_LanesReversed;
        /// <summary>
        /// Indicates if lanes have been reversed. True if so. False if not.
        /// Read only.
        /// </summary>
        public bool Response_LanesReversed
        {
            get { return response_LanesReversed; }
        }

        /// <summary>
        /// True if eliminiator mode is enabled. False if not.
        /// </summary>
        private bool response_EliminatorMode;
        /// <summary>
        /// True if eliminator mode is enabled. False if not.
        /// Read only.
        /// </summary>
        public bool Response_EliminatorMode
        {
            get { return response_EliminatorMode; }
        }

        /// <summary>
        /// Indicates if the old results data format is used.
        /// </summary>
        private RaceDataFormat response_DataFormat;
        /// <summary>
        /// Indicates if the old results data format is used.
        /// Read only.
        /// </summary>
        public RaceDataFormat Response_DataFormat
        {
            get { return response_DataFormat; }
        }

        // Constructor
        //**********************************************************************

        /// <summary>
        /// Iniatializes a new instance of the ReadModeCommand.
        /// </summary>
        public ReadModeCommand()
            : base("RM", 
                @"RM\r\n([0-6]) (0|1)(0|1)(0|1)(0|1)(0|1)(0|1) " +
                @"(0|1) (0|1) (0|1)\r\n\*\r\n",
                @"(R|RM|RM\r" +
                @"|RM\r\n([0-6])?" +
                @"|RM\r\n([0-6]) (0|1){0,6}" +
                @"|RM\r\n([0-6]) (0|1){6} (0|1)?" +
                @"|RM\r\n([0-6]) (0|1){6} (0|1) (0|1)?" +
                @"|RM\r\n([0-6]) (0|1){6} (0|1) (0|1) (0|1)?" +
                @"|RM\r\n([0-6]) (0|1){6} (0|1) (0|1) (0|1)\r" +
                @"|RM\r\n([0-6]) (0|1){6} (0|1) (0|1) (0|1)\r\n" +
                @"|RM\r\n([0-6]) (0|1){6} (0|1) (0|1) (0|1)\r\n\*" +
                @"|RM\r\n([0-6]) (0|1){6} (0|1) (0|1) (0|1)\r\n\*\r)")
        {
            this.ResetResponseData();
        }

        // Methods
        //**********************************************************************

        /// <summary>
        /// Clears the response data last received.
        /// </summary>
        public override void ResetResponseData()
        {
            base.ResetResponseData();

            this.response_ReversedLaneCount = LaneCount.None;

            this.response_MaskValues = new bool[6];
            for (int i = 0; i < this.response_MaskValues.Length; i++)
            {
                this.response_MaskValues[i] = false;
            }

            this.response_LanesReversed = false;
            this.response_EliminatorMode = false;
            this.response_DataFormat = RaceDataFormat.New;
        }

        /// <summary>
        /// Evaluates the response sucess and response string and sets the 
        /// response data accordingly.
        /// </summary>
        /// <param name="deviceResponded">
        /// True if the device responded. False if a reponse was not received.
        /// </param>
        /// <param name="response">
        /// String received from the device.
        /// </param>
        public override void SetResponseData(string response)
        {
            // set new values
            base.SetResponseData(response);
            if (!base.isResponseSet)
            { // nothing to do 
                return;
            }

            GroupCollection datagroups = 
                Regex.Match(response, this.responsePattern).Groups;
            // datagroups[0] is the whole matched string.
            // The subgroups with the data I want, start at 1.
            this.response_ReversedLaneCount =
                (LaneCount)Convert.ToInt32(datagroups[1].ToString());
            // Lane Masks [2 - 7]
            for (int i = 0; i < response_MaskValues.Length; i++)
            {
                response_MaskValues[i] = (datagroups[2 + i].ToString() == "1");
            }

            this.response_LanesReversed = 
                (Convert.ToInt32(datagroups[8].ToString()) == 1);
            this.response_EliminatorMode = 
                (Convert.ToInt32(datagroups[9].ToString()) == 1);
            this.response_DataFormat = 
                (RaceDataFormat)Convert.ToInt32(datagroups[10].ToString());
        }

        /// <summary>
        /// Returns a true if the specified lane is masked. 
        /// </summary>
        /// <param name="lane">Specified lane</param>
        /// <returns>True if masked. False if not.</returns>
        public bool Response_IsLaneMasked(Lanes lane)
        {
            return response_MaskValues[(int)lane];
        }
    }
    #endregion

    #region BootLoadCommand - BA (not implemented)
    //##########################################################################
    /// Not Implemented
    /// The command cannot be tested with the information I have. 
    #endregion

    #region LoadFeatureCommand - LF (not implemented)
    //##########################################################################
    /// Not Implemented
    /// The command cannot be tested without risk of messing up the device.
    #endregion

    #region AboutCommand - RV (not implemented)
    //##########################################################################
    /// Not Implemented
    /// This command is not published. The command has no know functional value.
    /// Its reponse looks like the following:
    /// "RV\r\nCopyright (C) 2004 Micro Wizard\n\rK1 Version 1.09D Serial Number 27752\r\n\r\n"
    /// The exact format is not know, and, therefore, may be difficult to identify.
    /// A similar stirng is also received when the device is first powered on, 
    /// saving that it does not start with the command string.
    #endregion
}
