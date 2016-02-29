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

using System;

namespace FastTrack
{
    public struct ComPortInfo
    {
        /// <summary>
        /// Com port name.
        /// </summary>
        private string portName;

        /// <summary>
        /// Com port name.
        /// </summary>
        public string PortName
        {
            get { return portName; }
            set
            {
                portName = FastTrackSerialComPort.SanitizePortName(value);
            }
        }

        /// <summary>
        /// Human understandable description for users.
        /// </summary>
        private string description;
        /// <summary>
        /// Human understandable description for users.
        /// </summary>
        public string Description
        {
            get { return description; }
            set
            {
                if (value.Trim() != "")
                {
                    this.description = value;
                }
                else
                {
                    this.description = "(Unknown)";
                }
            }
        }

        /// <summary>
        /// Device Instance Path
        /// </summary>
        /// <example>
        /// USB\VID_067B&PID_2303\5&8d342b5&0&1
        /// </example>
        /// The device instance path should be made up of three parts:
        /// Enumerator\DeviceId\HardwareKey
        ///     e.g. Enumerator  = USB
        ///     e.g. DeviceID    = VID_067B&PID_2303
        ///     e.g. HardwareKey = 5&8d342b5&0&1
        /// This format also forms the regisitry path to the device information
        /// starting from "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum".
        /// http://msdn.microsoft.com/en-us/library/dd568017.aspx
        private string deviceInstancePath;

        /// <summary>
        /// Device Instance Path
        /// </summary>
        /// <example>
        /// USB\VID_067B&PID_2303\5&8d342b5&0&1
        /// </example>
        public string DeviceInstancePath
        {
            get { return deviceInstancePath; }
            set { deviceInstancePath = value; }
        }

        public override string ToString()
        {
            return this.PortName;
        }
    }
}
