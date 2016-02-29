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
using Microsoft.Win32; // necessary for using the registry

namespace FastTrack
{
    public static class ProlificUsbSerialConverter
    {
        #region Static Attributes and Properties
        //**********************************************************************
        /// <summary>
        /// Prolific driver service name.
        /// </summary>
        /// The registry contains a list of services including driver services.
        /// The registry key contains subkey "Enum" that enumerates the device
        /// instance path of all the devices it manages. 
        /// 
        /// The device path can be used to fined the proper com port that is used to 
        /// communicate with the device.
        private static string driverServiceName = @"Ser2pl";
        /// <summary>
        /// Prolific driver service name. Read only.
        /// </summary>
        public static string DriverServiceName
        {
            // Read only. 
            get { return ProlificUsbSerialConverter.driverServiceName; }
        }

        /// <summary>
        /// Windows upper level filter driver service name.
        /// Secondary location that might recognize the hardware if the 
        ///     Prolific driver service does not.
        /// </summary>
        /// http://msdn.microsoft.com/en-us/library/aa939503%28WinEmbedded.5%29.aspx
        /// The Communications Port between two computers component provides 
        /// support for a communications port. This component supplies the 
        /// Serenum Filter Driver in the serenum.sys file. The Serenum Filter 
        /// Driver is an upper-level device filter driver that implements the 
        /// Serenum service. This component also supplies the Serial port driver 
        /// in the serial.sys file. The Serial driver implements the Serial 
        /// Service. This component supplies the msports.inf file, which is the 
        /// system-supplied INF (information) file for the port device class.
        private static string filterDriverServiceName = @"Serenum";
        /// <summary>
        /// Windows upper level filter driver service name.
        /// Secondary location that might recognize the hardware if the 
        ///     Prolific driver service does not.
        /// Read only.
        /// </summary>
        public static string FilterDriverServiceName
        {
            get { return ProlificUsbSerialConverter.filterDriverServiceName; }
        }

        /// <summary>
        /// Root registry path for the computer's serice list.
        /// </summary>
        /// Appending the service name to this path will provide the full path 
        /// to the service's registry information.
        private static string registryServicePath
            = @"SYSTEM\CurrentControlSet\Services";
        /// <summary>
        /// Root registry path for the computer's serice list. Read only.
        /// </summary>
        public static string RegistryServicePath
        {
            get { return ProlificUsbSerialConverter.registryServicePath; }
        }

        /// <summary>
        /// Root registry path for the computer's device list.
        /// </summary>
        /// The device name given by the device service is a relative path 
        /// starting at this location. Appending the its device instance path to 
        /// this path (with a '\') will provide the complete path to the specific 
        /// device's information. A subkey ("Device Parameters") of this combined 
        /// path will contain the all-important com port name.
        private static string registryDevicePath
            = @"SYSTEM\CurrentControlSet\Enum";
        /// <summary>
        /// Root registry path for the computer's device list. Read only.
        /// </summary>
        public static string RegistryDevicePath
        {
            get { return ProlificUsbSerialConverter.registryDevicePath; }
        }
        #endregion

        #region GetComPorts Methods
        /// <summary>
        /// Gets information about all devices managed by the Prolific driver.
        /// </summary>
        /// <returns>
        /// Array of ComPortInfo structs for all ports found. 
        /// Empty array if none are found.
        /// Null if an error ocurrs such as the driver information is not found.
        /// </returns>
        public static ComPortInfo[] GetProlificComPorts()
        {
            return GetComPortsFromService(driverServiceName);
        }

        /// <summary>
        /// Gets information about all devices managed by the filter driver.
        /// </summary>
        /// <returns>
        /// Array of ComPortInfo structs for all ports found. 
        /// Empty array if none are found.
        /// Null if an error ocurrs such as the driver information is not found.
        /// </returns>
        public static ComPortInfo[] GetFilterDriverComPorts()
        {
            return GetComPortsFromService(filterDriverServiceName);
        }

        /// <summary>
        /// Gets information about all devices managed by a specified driver 
        /// service.
        /// </summary>
        /// <returns>
        /// Array of ComPortInfo structs for all ports found. 
        /// Empty array if none are found.
        /// Null if an error ocurrs such as the driver information is not found.
        /// </returns>
        /// The number of ports found is expected to be 1 if the device the device
        /// is plugged in and 0 if it is not. It cannot be assumed, however, that 
        /// there is not more than one device connected. Furthermore, it is 
        /// possible that a different driver has been used and the expected 
        /// registry information does not exist.
        public static ComPortInfo[] GetComPortsFromService(string service)
        {
            // Check that the service exists.
            RegistryKey key = Registry.LocalMachine.OpenSubKey(
                registryServicePath + @"\" + service);
            if (key == null)
            {
                return null;
            }

            // Check that the sub key containing the device list exists.
            key = key.OpenSubKey("Enum");
            if (key == null)
            {
                // The subkey does not exist; thus, the service data contains 
                // no devices information. 
                return new ComPortInfo[0];
            }

            int deviceCount = (int)key.GetValue("Count", 0);

            ComPortInfo[] portList = new ComPortInfo[deviceCount];
            for (int i = 0; i < deviceCount; i++)
            {
                portList[i] = new ComPortInfo();

                // DeviceInstancePath
                portList[i].DeviceInstancePath = 
                    (string)key.GetValue("0", "error");
                // Description
                key = Registry.LocalMachine.OpenSubKey(
                    registryDevicePath + @"\" + portList[i].DeviceInstancePath);
                if (key == null)
                {
                    continue;
                }
                portList[i].Description = (string)key.GetValue("FriendlyName");
                // PortName
                key = key.OpenSubKey("Device Parameters");
                if (key != null)
                {
                    portList[i].PortName = (string)key.GetValue("PortName");
                }
            }
            return portList;
        }
        #endregion
    }
}
