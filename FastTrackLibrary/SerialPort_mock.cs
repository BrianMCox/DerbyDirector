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

#if TEST
using System;
using System.IO.Ports;


namespace FastTrack_test
{
    /// <summary>
    /// This class is indented to imitate the essential features of the 
    /// SerialPort class and the Fast Track timer with which it communicates. 
    /// </summary>
    public class SerialPort_mock
    {
        // Properties provided by SerialPort Class
        //**********************************************************************
        public int ReadTimeout;
        public int WriteTimeout;

        public bool IsOpen = false;

        public SerialDataReceivedEventHandler DataReceived;

        // Attributes and properties needed for implementaion.
        //**********************************************************************
        private string buffer = "";
        public int[] mode = new int[10];
        // the features are not modifiable on a real device with this code.
        public int[] Features = new int[8];
        private int serialNumber = 12345;

        public int PhysicalLaneCount = 4;

        // Methods provided by SerialPort Class
        //**********************************************************************
        public SerialPort_mock(string portName, int baudRate, Parity parity,
            int dataBits, StopBits stopBits)
        {
            ResetState();
        }

        public void Open()
        {
            if (IsOpen)
            {
                throw new InvalidOperationException();
            }
            IsOpen = true;
        }

        public void Close()
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException();
            }
            IsOpen = false;
        }

        public void Write(string message)
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException();
            }

            // MA-MF
            if (message.Length == 2 && message[0] == 'M' &&
                (int)message[1] >= 65 && (int)message[1] <= 70)
            {
                if ((int)message[1] - 65 < PhysicalLaneCount)
                {
                    mode[(int)message[1] - 64] = 1;
                }
                buffer += message + "\r\n*\r\n";
            }
            // RL0 - RL6
            else if (message.Length == 3 && message[0] == 'R' && message[1] == 'L'
                && (int)message[2] >= 48 && (int)message[2] <= 54)
            {
                mode[0] = ((int)message[2] - 48);
                if (mode[0] == 0)
                {
                    mode[7] = 0;
                }
                else
                {
                    mode[7] = 1;
                }
                buffer += message + "\r\n*\r\n";
            }
            // LXA-LXP
            else if (message.Length == 3 && message[0] == 'L' && message[1] == 'X'
                && (int)message[2] >= 65 && (int)message[2] <= 80)
            {
                buffer += message + "\r\n*\r\n";
            }
            else
            {
                switch (message)
                {
                    case "MG":
                        for (int i = 1; i < 7; i++)
                        {
                            mode[i] = 0;
                        }
                        buffer += "MG\r\nAC";
                        break;
                    case "LE":
                        mode[8] = 1;
                        buffer += message + "\r\n*\r\n";
                        break;
                    case "RE":
                        mode[8] = 0;
                        buffer += message + "\r\n*\r\n";
                        break;
                    case "RF":
                        buffer += String.Format(
                            "RF\r\n{0}{1}{2}{3} {4}{5}{6}{7}\r\n*\r\n",
                            Features[0], Features[1], Features[2], Features[3],
                            Features[4], Features[5], Features[6], Features[7]);
                        break;
                    case "RS":
                        buffer += String.Format("RS\r\n{0}\r\n", serialNumber);
                        break;
                    case "N0":
                        mode[9] = 0;
                        buffer += message + "\r\n*\r\n";
                        break;
                    case "N1":
                        mode[9] = 1;
                        buffer += message + "\r\n*\r\n";
                        break;
                    case "RM":
                        buffer += String.Format(
                            "RM\r\n{0} {1}{2}{3}{4}{5}{6} {7} {8} {9}\r\n*\r\n",
                            mode[0], mode[1], mode[2], mode[3], mode[4], 
                            mode[5], mode[6], mode[7], mode[8], mode[9]);
                        break;
                    case "RX":
                    case "RA":
                    case "LR":
                        buffer += message + "\r\n*\r\n";
                        break;
                    default:
                        buffer += message + "\r\nX\r\n";
                        break;
                }
            }
            if (DataReceived != null)
            {
                DataReceived(null, null);
            }
        }

        public string ReadExisting()
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException();
            }

            string temp = buffer;
            buffer = "";
            return temp;
        }

        // Methods provided for testing
        //**********************************************************************
        public void ResetState()
        {
            // number of reversed lanes
            mode[0] = 0;
            // lane masks (all off)
            mode[1] = 0;
            mode[2] = 0;
            mode[3] = 0;
            mode[4] = 0;
            mode[5] = 0;
            mode[6] = 0;
            // lanes reversed (off)
            mode[7] = 0;
            // eliminator mode (off)
            mode[8] = 0;
            // result format (new)
            mode[9] = 1;

            for (int i = 0; i < Features.Length; i++)
            {
                Features[i] = 1;
            }

            PhysicalLaneCount = 4;
        }

        public void InitiateResultClearedResponse()
        {
            buffer += "@";
            if (DataReceived != null)
            {
                DataReceived(null, null);
            }
        }

        public void InitialRaceResultResponse(string results)
        {
            buffer += results;
            if (DataReceived != null)
            {
                DataReceived(null, null);
            }
        }
    }
}
#endif