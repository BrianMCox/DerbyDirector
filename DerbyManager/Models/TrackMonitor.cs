using DerbyManager.Hubs;
using FastTrack;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DerbyManager.Models
{
    public static class TrackMonitor
    {
        private static K1Timer _timer;
        private static ResultsHub _hub = new ResultsHub();
        
        public static ComPortInfo Initialize()
        {
            ComPortInfo port;
            var portList = ProlificUsbSerialConverter.GetProlificComPorts().ToList<ComPortInfo>();
            int laneCount;
            bool canCommunicate;

            if (portList.Count > 0)
            {
                port = portList[0];
                _timer = new K1Timer(port.PortName);
                canCommunicate = TestComPort(port.PortName, out laneCount);
                _timer.NewRaceResultsEvent += new K1Timer.NewRaceResultsEventHandler(_timer_NewRaceResultsEvent);
            }

            return port;
        }

        public static bool TestConnection()
        {
            return _timer.TestDeviceCommunication();
        }

        public static ComPortInfo NewConnection()
        {
            if (_timer != null && _timer.ComPort.IsOpen)
            {
                _timer.ComPort.Close();
            }

            return Initialize();
        }

        public static bool EndRace()
        {
            return _timer.EndRace();
        }

        public static bool ClearRace()
        {
            return _timer.ClearRace();
        }

        private static void _timer_NewRaceResultsEvent(K1Timer sender, RaceResult results)
        {
            _hub.Send(results);
        }

        /// <summary>
        /// Tests comport communication. To avoid rapidly connecting and
        /// disconnecting from ports comport objects will be stored and reused 
        /// for each test if they can be created. 
        /// </summary>
        /// <param name="portName">Name of the com port (e.g. COM99)</param>
        /// <param name="laneCount">
        /// Will be set to the number of lanes found if successfull or 0 on error.
        /// </param>
        /// <returns>True if the test succeeded. False if not.</returns>
        private static bool TestComPort(string portName, out int laneCount)
        {
            laneCount = 0;

            // if no timer object has already been found ... 
            if (_timer == null)
            {
                try
                { // Attempt to open a new one and add it to the list.
                    _timer = new K1Timer(portName);
                }
                catch
                { // If the attempt fails, then so does the test.
                    return false;
                }
            }

            // If the above code is successful, try to run the timers test 
            // function. Continue if it works. Otherwise, return failure.
            if (!_timer.TestDeviceCommunication())
            {
                return false;
            }

            // Get the lane count from the timer and return a success.
            laneCount = _timer.LastDetectedPhysicalLaneCount;
            return true;
        }
    }
}