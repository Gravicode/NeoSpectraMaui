using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Windows.Devices.Bluetooth;
//using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace NeospectraMauiDemo.Driver
{
    public class SWS_P3BLEDevice
    {

        private String DeviceName;
        private String DeviceMacAddress;
        private int DeviceRSSI;
        private DeviceCandidate DeviceInstance;
        public bool connected = false;


        private DeviceCandidate gattDeviceInstance = null; // The mi band
        private bool isConnectedToGatt = false; // the gatt connection
        private IService myGatBand = null;

        //SWS_P3BLEDevice(String mName, String mMacAddress, int mRSSI, RxBleDevice mInstance)
        //{
        //    DeviceName = mName;
        //    DeviceMacAddress = mMacAddress;
        //    DeviceRSSI = mRSSI;
        //    DeviceInstance = mInstance;
        //}

        public SWS_P3BLEDevice(String mName, String mMacAddress, int mRSSI, DeviceCandidate mInstance)
        {
            DeviceName = mName;
            DeviceMacAddress = mMacAddress;
            DeviceRSSI = mRSSI;
            gattDeviceInstance = mInstance;
            isConnectedToGatt = false;
        }

        public String getDeviceName()
        {
            return DeviceName;
        }

        public void setDeviceName(String deviceName)
        {
            DeviceName = deviceName;
        }

        public String getDeviceMacAddress()
        {
            return DeviceMacAddress;
        }

        public void setDeviceMacAddress(String deviceMacAddress)
        {
            DeviceMacAddress = deviceMacAddress;
        }

        public int getDeviceRSSI()
        {
            return DeviceRSSI;
        }

        public void setDeviceRSSI(int deviceRSSI)
        {
            DeviceRSSI = deviceRSSI;
        }

        public DeviceCandidate getDeviceInstance()
        {
            return DeviceInstance;
        }

        public void setDeviceInstance(DeviceCandidate deviceInstance)
        {
            DeviceInstance = deviceInstance;
        }

        public DeviceCandidate getGattDeviceInstance()
        {
            return gattDeviceInstance;
        }

        public void setGattDeviceInstance(DeviceCandidate gattDeviceInstance)
        {
            this.gattDeviceInstance = gattDeviceInstance;
        }
        public IService getMyGatBand()
        {
            return myGatBand;
        }

        public void setMyGatBand(IService myGatBand)
        {
            this.myGatBand = myGatBand;
        }

        public bool IsConnectedToGatt()
        {
            return isConnectedToGatt;
        }
        public void setConnectedToGatt(bool connectedToGatt)
        {
            isConnectedToGatt = connectedToGatt;
        }

    }

}
