//using Org.Apache.Http.Authentication;
//using Plugin.BLE.Android;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
//using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
//using static Android.Icu.Text.CaseMap;
//using Windows.Devices.Bluetooth;
//using Windows.Devices.Bluetooth.GenericAttributeProfile;
//using Windows.Devices.Enumeration;
//using Windows.Devices.Geolocation;
//using Windows.Devices.Radios;
//using Windows.Security.Cryptography;
//using Windows.Storage.Streams;
//using Windows.UI.Core;
using static NeospectraMauiDemo.Driver.GlobalVariables;

namespace NeospectraMauiDemo.Driver
{
    public class SWS_P3ConnectionServices
    {
        static private List<SWS_P3BLEDevice> bleDevices;
        private static String TAG = "P3_Connection";
        #region Error Codes
        readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion

        bool mHeaderPacketDone = false;
        private bool mHeaderMemPacketDone = false;
        private bool mHeaderSysPacketDone = false;
        private byte[] scanBytes;
        private int scanBytesIterator;
        private int packetsType = 0;
        int mNumberOfPackets = 0;
        int mDataLength = 0;
        int mReceivedPacketsCounter = 0;
        SWS_P3PacketResponse mPacketResponse;
        bool still_fail = true;
        bool isConnectedToGatt = false;
        int status;

        // flag to connection process
        public enum ConnectionStatus
        {
            ready,
            findingChannel,
            failedToGetChannel,
            gotChannel,
            connecting,
            failedToConnect,
            connected,
            disconnected
        }

        public ConnectionStatus connectionStatus = ConnectionStatus.ready;

        public ConnectionStatus getConnectionStatus()
        {
            return connectionStatus;
        }

        public void setConnectionStatus(ConnectionStatus connectionStatus)
        {
            this.connectionStatus = connectionStatus;
        }

        // =====================================================================
        // RxAndroidBLE related variables
        //private RxBleClient mRxBleClient;
        private IDevice mRxBleDevice;
        //private Disposable scanSubscription;
        private ObservableCollection<IDeviceDisplay> mRxBleConnection;



        BLEServiceList ServiceList = null;
        List<SWS_P3BLEDevice> DevicesList = new List<SWS_P3BLEDevice>();

        List<String> DevicesMacAddress = new List<string>();
        List<Guid> serviceGuidsList = new List<Guid>();
        List<Guid> characteristicGuidsList = new List<Guid>();
        List<Guid> descriptorGuidsList = new List<Guid>();

        // ============================================================================================================
        public static int MY_PERMISSIONS_REQUEST_ACCESS_FINE_LOCATION = 1;
        private static readonly int LOCATION_PERMISSION_REQUEST_CODE = 1;
        // ============================================================================================================

        //Macros
        public static readonly Guid TX_POWER_Guid = new Guid("00001804-0000-1000-8000-00805f9b34fb");
        public static readonly Guid TX_POWER_LEVEL_Guid = new Guid("00002a07-0000-1000-8000-00805f9b34fb");
        public static readonly Guid CCCD = new Guid("00002902-0000-1000-8000-00805f9b34fb");
        public static readonly Guid FIRMWARE_REVISON_Guid = new Guid("00002a26-0000-1000-8000-00805f9b34fb");
        public static readonly Guid DIS_Guid = new Guid("0000180a-0000-1000-8000-00805f9b34fb");

        public static readonly Guid P3_SERVICE_Guid = new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
        public static readonly Guid SYS_STAT_SERVICE_Guid = new Guid("B100B100-B100-B100-B100-B100B100B100");
        public static readonly Guid MEM_SERVICE_Guid = new Guid("C100C100-C100-C100-C100-C100C100C100");
        public static readonly Guid OTA_SERVICE_Guid = new Guid("D100D100-D100-D100-D100-D100D100D100");
        public static readonly Guid BATTERY_SERVICE_Guid = new Guid("E100E100-E100-E100-E100-E100E100E100");

        public static readonly Guid P3_RX_CHAR_Guid = new Guid("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
        public static readonly Guid P3_TX_CHAR_Guid = new Guid("6e400003-b5a3-f393-e0a9-e50e24dcca9e");
        public static readonly Guid SYS_STAT_TX_CHAR_Guid = new Guid("B101B101-B101-B101-B101-B101B101B101");
        public static readonly Guid SYS_STAT_RX_CHAR_Guid = new Guid("B102B102-B102-B102-B102-B102B102B102");
        public static readonly Guid MEM_TX_CHAR_Guid = new Guid("C101C101-C101-C101-C101-C101C101C101");
        public static readonly Guid MEM_RX_CHAR_Guid = new Guid("C102C102-C102-C102-C102-C102C102C102");
        public static readonly Guid OTA_TX_CHAR_Guid = new Guid("D101D101-D101-D101-D101-D101D101D101");
        public static readonly Guid OTA_RX_CHAR_Guid = new Guid("D102D102-D102-D102-D102-D102D102D102");
        // ======================================================= =====================================================
        public ObservableCollection<DeviceCandidate> KnownDevices { set; get; } = new ObservableCollection<DeviceCandidate>();
        
        public bool IsServiceReady()
        {
            return ServiceList != null && ServiceList.IsReady;
        }
        // ============================================================================================================
        // Constructor
        public async void enableBluetooth()
        {

            var btAdapter = BleSvc.Adapter;
            if (btAdapter == null)
                return;
            //var permission = await BleSvc.RequestBluetoothPermissions();
        }

        public async void disableBluetooth()
        {
            setConnecting("disableBluetooth()", false);
            //do nothing
        }
        public async Task<bool> isBluetoothEnabled()
        {

            IAdapter btAdapter = BleSvc.Adapter;
            if (btAdapter == null)
                return false;
            return BleSvc.BluetoothLE.IsOn;
        }

       

        private bool Not(bool value) => !value;

        public async Task<bool> askForLocationPermissions()
        {
            MethodsFactory.LogMessage("bluetooth", "Ask for permission");

            GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));

            var _cancelTokenSource = new CancellationTokenSource();

            Location location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
            return location != null;
        }
    

    
        BluetoothLEService BleSvc { set; get; }
        public SWS_P3ConnectionServices(BluetoothLEService service)
        {
            this.BleSvc = service;
            //StartWatcher();
        }

        public IDevice getmRxBleDevice()
        {
            return mRxBleDevice;
        }

        public void setmRxBleDevice(IDevice mRxBleDevice)
        {
            if (this.mRxBleDevice != mRxBleDevice)
            {
                this.mRxBleDevice = mRxBleDevice;
                this.ServiceList = new BLEServiceList(mRxBleDevice);
            }

        }


        // ============================================================================================================

        
    public async Task< List<SWS_P3BLEDevice>> ScanBTDevices()
        {
            DevicesList = new List<SWS_P3BLEDevice>();
            
            foreach(var item in this.BleSvc.DeviceCandidateList)
            {
            
                DevicesList.Add(new SWS_P3BLEDevice(item.Name, "",0, item ));
            }
            return DevicesList;
        }


        public bool isConnecting { set; get; }

        public void setConnecting(String from, bool connecting)
        {
            isConnecting = connecting;
        }

        public void ConnectToP3()
        {
            setConnecting("ConnectToP3()", true);
            broadcastNotificationconnected("connection established!"); //heba added

            //StartWatcher(true);
        }

        public void DisconnectFromP3()
        {
            setConnecting("DisconnectFromP3()", false);
            //StartWatcher(false);
            //disconnectTriggerSubject.onNext(true);
        }
        async Task<ICharacteristic> GetCharacteristic(IService service, Guid CharacterUUID)
        {
            if (BleSvc.Device.State == DeviceState.Connected && service!=null)
            {
                var res = await service.GetCharacteristicAsync(CharacterUUID);
                return res;
            }
            
            Debug.WriteLine($"Characteristic for {CharacterUUID} is not found");
            return default;
        }
        async Task<IService> GetGattService(Guid ServiceUUID)
        {
            if (BleSvc.Device.State == DeviceState.Connected)
            {
                var res = await this.mRxBleDevice.GetServiceAsync(ServiceUUID);
                return res;
            }
            Debug.WriteLine($"GATT Service for {ServiceUUID} is not found");
            return default;
        }

        Dictionary<Guid,ICharacteristic> CharacteristicList = new Dictionary<Guid,ICharacteristic>();
        async Task<ICharacteristic> GetCharacteristic(Guid CharacterUUID)
        {
            if (CharacteristicList.ContainsKey(CharacterUUID)) return CharacteristicList[CharacterUUID];
            var serviceuid = this.ServiceList.GetServiceUIDByCharacteristicUID(CharacterUUID);
            if (serviceuid == null) throw new Exception("Service is not exists");
            var service = await this.mRxBleDevice.GetServiceAsync(serviceuid.Value);




            var SelectedCharacteristic = await service.GetCharacteristicAsync(CharacterUUID);

            if (!CharacteristicList.ContainsKey(CharacterUUID))
                CharacteristicList.Add(CharacterUUID, SelectedCharacteristic);
            return SelectedCharacteristic;

        }

        private async Task<bool> WriteCharacteristic(ICharacteristic selectedCharacteristic, string ValueStr)
        {
            if (!String.IsNullOrEmpty(ValueStr))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(ValueStr);
                return await selectedCharacteristic.WriteAsync(bytes);
            }
            else
            {
                Debug.WriteLine("No data to write to device");
            }
            return false;
        }

        private async Task<bool> WriteCharacteristicInt(ICharacteristic selectedCharacteristic, int ValueInt)
        {
            try
            {
                var bytes = BitConverter.GetBytes(ValueInt);
                return await selectedCharacteristic.WriteAsync(bytes);

            }
            catch (Exception)
            {
                Debug.WriteLine("No data to write to device");
                
            }
         
            return false;


        } 
        
        private async Task<bool> WriteCharacteristicBytes(ICharacteristic selectedCharacteristic, byte[] ValueBytes)
        {
            try
            {
                return await selectedCharacteristic.WriteAsync(ValueBytes);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }

           

        }


        public async void ReadFromP3()
        {
            if (isConnected())
            {
                try
                {
                    var item = await GetCharacteristic(P3_TX_CHAR_Guid);
                    if (item.CanRead)
                    {
                        var res = await item.ReadAsync();

                        onReadSuccess(res);
                    }
                }
                catch (Exception ex)
                {

                    onReadFailure("P3 Read Error => " + ex);
                    Debug.WriteLine("Please! Ensure that you have a connected device firstly");

                }

            }
            else
            {
                onReadFailure("P3 Read Error => not connected" );
                Debug.WriteLine("Please! Ensure that you have a connected device firstly");
            }

        }


        private void onReadFailure(string throwable)
        {
            Debug.WriteLine( "Read error: " + throwable);
            broadcastNotificationFailure(throwable + " onReadFailure", "read_failure", 0);
        }

        private void onReadSuccess(byte[] bytes)
        {
            Debug.WriteLine("P3 Read => Read Success!");

        }

        public async void WriteToP3( byte[] byteArray)
        {
            if (isConnected())
            {
                var item = await GetCharacteristic(P3_RX_CHAR_Guid);
                var res = await WriteCharacteristicBytes(item, byteArray);
                Debug.WriteLine($"Write to P3_RX_CHAR_Guid => {res}");
            }
            else
            {
                Debug.WriteLine("Please! Ensure that you have a connected device firstly");
            }
            
        }
        public bool isConnected()
        {
            if (mRxBleDevice == null)
            {
                return false;
            }

            setConnecting("isConnected()", false);
            return mRxBleDevice.State ==  DeviceState.Connected;
        }

        public async void writeData(byte[] data)
        {

            if (isConnected())
            {
                var myGatService = await GetGattService(OTA_SERVICE_Guid);

                
                if (myGatService != null)
                {
                    Debug.WriteLine( "* Getting gatt Characteristic. UUID: " + OTA_RX_CHAR_Guid);

                    var myGatChar = await GetCharacteristic (myGatService, OTA_RX_CHAR_Guid /*Consts.UUID_START_HEARTRATE_CONTROL_POINT*/);
                    if (myGatChar != null)
                    {
                        Debug.WriteLine( "* Writing trigger");
                    
                        var res = await WriteCharacteristicBytes(myGatChar, data);
                        Debug.WriteLine($"Write to OTA_RX_CHAR_Guid => {res}");
                        //boolean status = myGatBand.writeCharacteristic(myGatChar);
                       Debug.WriteLine(TAG + "* Writting trigger status :" + res);
                    }
                }              
            }
            else
            {
                Debug.WriteLine("Please! Ensure that you have a connected device firstly");
            }
        }

        public async void WriteToMemoryService( byte[] byteArray)
        {
            if (isConnected())
            {
                var item = await GetCharacteristic(MEM_RX_CHAR_Guid);
                var res = await WriteCharacteristicBytes(item, byteArray);
                Debug.WriteLine($"Write to MEM_RX_CHAR_Guid => {res}");
            }
            else
            {
                Debug.WriteLine("Please! Ensure that you have a connected device firstly");
            }
           
        }

        public async void WriteToSystemService( byte[] byteArray)
        {
            if (isConnected())
            {
                var item = await GetCharacteristic(SYS_STAT_RX_CHAR_Guid);
                var res = await WriteCharacteristicBytes(item, byteArray);
                Debug.WriteLine($"Write to SYS_STAT_RX_CHAR_Guid => {res}");
            }
            else
            {
                Debug.WriteLine("Please! Ensure that you have a connected device firstly");
            }
          
        }
        public async void WriteOTAService( byte[] byteArray)
        {
            if (isConnected())
            {
                var item = await GetCharacteristic(OTA_RX_CHAR_Guid);
                var res = await WriteCharacteristicBytes(item, byteArray);
                Debug.WriteLine($"Write to OTA_RX_CHAR_Guid => {res}");
            }
            else
            {
                Debug.WriteLine("Please! Ensure that you have a connected device firstly");
            }

        }
      
        bool SubscribeNotification = false;
        private void broadcastWriteFailure(String msg)
        {
            Dictionary<string, object> iWriteData = new Dictionary<string, object>();
            //iWriteData.setAction(GlobalVariables.INTENT_ACTION);
            iWriteData.Add("iName", "sensorWriting");
            iWriteData.Add("isWriteSuccess", false);
            iWriteData.Add("err", msg);
            BroadcastReceived?.Invoke(this, new BroadcastEventArgs() { Created = DateTime.Now, iGotData = iWriteData });

        }

        private void broadcastWriteSuccess()
        {
            Dictionary<string, object> iWriteData = new Dictionary<string, object>();
            //iWriteData.setAction(GlobalVariables.INTENT_ACTION);
            iWriteData.Add("iName", "sensorWriting");
            iWriteData.Add("isWriteSuccess", true);
            BroadcastReceived?.Invoke(this, new BroadcastEventArgs() { Created = DateTime.Now, iGotData = iWriteData });

        }

        public async Task<bool> SetNotificationOnTXInP3()
        {
            if (isConnected())
            {
                var NotificationCharacteristic = await GetCharacteristic(P3_TX_CHAR_Guid);
                if (SubscribeNotification)
                {
                    // Need to clear the CCCD from the remote device so we stop receiving notifications
                    await NotificationCharacteristic.StopUpdatesAsync();

                    NotificationCharacteristic.ValueUpdated -= Notification_ValueChanged;

                }

                try
                {
                    if (NotificationCharacteristic != null && NotificationCharacteristic.CanUpdate)
                    {
                        SubscribeNotification = true;
                        NotificationCharacteristic.ValueUpdated += Notification_ValueChanged;
                        await NotificationCharacteristic.StartUpdatesAsync();
                        MethodsFactory.LogMessage("Successfully subscribed P3 for value changes", "Status");
                        return true;

                        //rootPage.NotifyUser("Successfully subscribed for value changes", NotifyType.StatusMessage);
                    }
                    else
                    {
                        MethodsFactory.LogMessage($"Error registering P3 for value changes: {status}", "ErrorMessage");
                        //rootPage.NotifyUser($"Error registering for value changes: {status}", NotifyType.ErrorMessage);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    // This usually happens when a device reports that it support indicate, but it actually doesn't.
                    //rootPage.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                    MethodsFactory.LogMessage(ex.Message, "ErrorMessage");
                }
            }
            return false;
        }

        private async void Notification_ValueChanged(object sender, CharacteristicUpdatedEventArgs e)
        { 
            var data = e.Characteristic.Value;
            onNotificationReceived(data);
        }
        private void onNotificationReceived( byte[] bytes)
        {
            if (!mHeaderPacketDone)
            {
                // get the error code and check its value
                if (bytes[0] == 0)
                {
                    Debug.WriteLine( "%--> NO Error in Received Packet.");

                    int doublesCount = 0;
                    // No Error, compute the length of the frame
                    int length1 = 0x000000FF & (bytes[1]);
                    int length2 = 0x000000FF & (bytes[2]);
                    mDataLength = (length1) + ((length2) << 8);

                    Debug.WriteLine("%--> Length: " + mDataLength + "  mInterpolationEnabled = " + gIsInterpolationEnabled);

                    if (mDataLength == 1 || mDataLength == 2)
                    {
                        mNumberOfPackets = 1;
                    }
                    else
                    {
                        if (gIsInterpolationEnabled)
                        {
                            mNumberOfPackets = (int)Math.Ceiling((mDataLength + 2) * 8.0 / 20);
                        }
                        else
                        {
                            mNumberOfPackets = (int)Math.Ceiling((mDataLength * 2) * 8.0 / 20);
                        }

                    }

                    Debug.WriteLine("%--> Packets: " + mNumberOfPackets);


                    // create a new response packet
                    mPacketResponse = new SWS_P3PacketResponse(mNumberOfPackets, mDataLength, gIsInterpolationEnabled);

                    // initialize the packet response
                    mPacketResponse.PrepareArraySize();

                    // set iteration parameters
                    mHeaderPacketDone = true;
                }
                else
                {
                    // Error
                    Debug.WriteLine( "#--> Error in Received Packet.");
                    broadcastNotificationFailure("#--> Error in Received Packet.", "packet_failure", (int)bytes[0]);
                }
            }
            else
            {

                // send the received bytes to the packet response instance
                mPacketResponse.AddNewResponse(bytes);

                // increment the received packets counter
                mReceivedPacketsCounter++;

                Debug.WriteLine("#--> Received packets: " + mReceivedPacketsCounter);

                Debug.WriteLine("mDataLength = " + mDataLength + ", mNumberOfPackets = " + mNumberOfPackets);
                if (mNumberOfPackets == mReceivedPacketsCounter)
                {
                    mReceivedPacketsCounter = 0;
                    Debug.WriteLine("#--> Received packets: Done !!");
                    Debug.WriteLine("-------------------------------------------------------------------------------------");

                    // initiate the interpretation process
                    mPacketResponse.InterpretByteStream();

                    double[] mRecDoubles = mPacketResponse.GetInterpretedPacketResponse();

                    broadcastNotificationData(mRecDoubles, mPacketResponse.ConvertByteArrayToString());
                    Debug.WriteLine("A PACKET IS BROADCASTED");
                    // fix iterator parameters for next packet
                    mHeaderPacketDone = false;
                }
            }

            Debug.WriteLine("Notification Received - " + bytes.Length);
        }
        
        bool SubscribeMemTx = false;
        public async Task<bool> SetNotificationOnMemTx()
        {
            if (isConnected())
            {
                var MemTxCharacteristic = await GetCharacteristic(MEM_TX_CHAR_Guid);
                if (SubscribeMemTx)
                {
                    // Need to clear the CCCD from the remote device so we stop receiving notifications
                    await MemTxCharacteristic.StopUpdatesAsync();

                    MemTxCharacteristic.ValueUpdated -= MemTx_ValueChanged;

                }

                try
                {
                    if (MemTxCharacteristic != null && MemTxCharacteristic.CanUpdate)
                    {
                        SubscribeMemTx = true;
                        MemTxCharacteristic.ValueUpdated += MemTx_ValueChanged;
                        await MemTxCharacteristic.StartUpdatesAsync();
                        MethodsFactory.LogMessage("Successfully subscribed MemTx for value changes", "Status");
                        return true;

                        //rootPage.NotifyUser("Successfully subscribed for value changes", NotifyType.StatusMessage);
                    }
                    else
                    {
                        MethodsFactory.LogMessage($"Error registering MemTx for value changes: {status}", "ErrorMessage");
                        //rootPage.NotifyUser($"Error registering for value changes: {status}", NotifyType.ErrorMessage);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    // This usually happens when a device reports that it support indicate, but it actually doesn't.
                    //rootPage.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                    MethodsFactory.LogMessage(ex.Message, "ErrorMessage");
                }
            }
            return false;
        }
        private async void MemTx_ValueChanged(object sender, CharacteristicUpdatedEventArgs e)
        {
            byte[] data = e.Characteristic.Value;
            onMemNotificationReceived(data);
        }
        private void onMemNotificationReceived(byte[] bytes)
        {
            if (!mHeaderMemPacketDone)
            {
                status = bytes[0];


                mDataLength = ((bytes[1] & 0xFF) << 0) | ((bytes[2] & 0xFF) << 8);

                scanBytes = new byte[mDataLength];
                scanBytesIterator = 0;

                mHeaderMemPacketDone = true;
            }
            else
            {

                if (mDataLength == 1 && status == 0)
                {
                    broadcastHomeNotification(0, "OperationDone");
                    mHeaderMemPacketDone = false;
                    return;
                }
                else if (mDataLength == 1 && status != 0)
                {
                    broadcastHomeNotification(0, "Error");
                    mHeaderMemPacketDone = false;
                    return;
                }

                if (scanBytesIterator == 0)
                {
                    long type = ((bytes[0] & 0xFFL) << 0) |
                            ((bytes[1] & 0xFFL) << 8) |
                            ((bytes[2] & 0xFFL) << 16) |
                            ((bytes[3] & 0xFFL) << 24);                //long to support 32-bin unsigned
                    packetsType = (int)type;
                }

                switch (packetsType)
                {
                    case MEMORY_STATUS_REQUEST:

                        long memory = ((bytes[4] & 0xFFL) << 0) |
                                ((bytes[5] & 0xFFL) << 8) |
                                ((bytes[6] & 0xFFL) << 16) |
                                ((bytes[7] & 0xFFL) << 24);                //long to support 32-bin unsigned

                        long FWVersion = ((bytes[8] & 0xFFL) << 0) |
                                ((bytes[9] & 0xFFL) << 8) |
                                ((bytes[10] & 0xFFL) << 16) |
                                ((bytes[11] & 0xFFL) << 24);              //long to support 32-bin unsigned

                        broadcastHomeNotification(memory, "Memory");
                        broadcastHomeNotification(FWVersion, "FWVersion");

                        mHeaderMemPacketDone = false;
                        break;

                    case MEMORY_GET_SCANS_REQUEST:
                        if (scanBytesIterator == 0)
                        {
                            Array.Copy(bytes, 4, scanBytes, scanBytesIterator, 16);
                            scanBytesIterator = scanBytesIterator + 16;
                        }
                        else if (scanBytesIterator + 20 > (mDataLength - 4))
                        {
                            Array.Copy(bytes, 0, scanBytes, scanBytesIterator,
                                    mDataLength - scanBytesIterator - 4);
                            scanBytesIterator = mDataLength - 4;
                        }
                        else
                        {
                            Array.Copy(bytes, 0, scanBytes, scanBytesIterator, 20);
                            scanBytesIterator = scanBytesIterator + 20;
                        }

                        if (scanBytesIterator == (mDataLength - 4))
                        {
                            /*
                            ByteBuffer buffer = ByteBuffer.wrap(scanBytes);
                            buffer.order(ByteOrder.LITTLE_ENDIAN);
                            double[] doubleValues = new double[scanBytes.length / 8];
                            for(int i = 0; i < scanBytes.length / 16; i++) {
                                doubleValues[i] = buffer.getLong(i * 8) / Math.pow(2, 33);
                                doubleValues[i + doubleValues.length/2] =
                                        buffer.getLong((i + doubleValues.length/2) * 8) /
                                                Math.pow(2, 30);
                            }
                            */
                            ByteBuffer buffer = new ByteBuffer(scanBytes);
                            double[] doubleValues = new double[scanBytes.Length / 8];
                            for (int i = 0; i < scanBytes.Length / 16; i++)
                            {
                              
                                doubleValues[i] = buffer.GetLong(i * 8) / Math.Pow(2, 33);
                                doubleValues[i + doubleValues.Length / 2] =
                                        buffer.GetLong((i + doubleValues.Length / 2) * 8) /
                                                Math.Pow(2, 30);
                            }
                            mHeaderMemPacketDone = false;
                            broadcastNotificationMemoryData(doubleValues);
                        }
                        break;

                    case MEMORY_CLEAR_REQUEST:
                        break;
                }
                long Fnum = ((bytes[0] & 0xFFL) << 0) |
                        ((bytes[1] & 0xFFL) << 8) |
                        ((bytes[2] & 0xFFL) << 16) |
                        ((bytes[3] & 0xFFL) << 24);
                Debug.WriteLine("===========" + Fnum + "=========" + scanBytesIterator + "==========");

            }

            Debug.WriteLine("Notification Received - " + (bytes.Length));
        }

        
        bool SubscribeBatTx = false;
        public async Task<bool> SetNotificationOnBatTx()
        {
            if (isConnected())
            {
                var BatTxCharacteristic = await GetCharacteristic(SYS_STAT_TX_CHAR_Guid);
                if (SubscribeBatTx)
                {
                    // Need to clear the CCCD from the remote device so we stop receiving notifications
                    await BatTxCharacteristic.StopUpdatesAsync();

                    BatTxCharacteristic.ValueUpdated -= BatTx_ValueChanged;

                }

                try
                {
                    if (BatTxCharacteristic != null && BatTxCharacteristic.CanUpdate)
                    {
                        SubscribeBatTx = true;
                        BatTxCharacteristic.ValueUpdated += BatTx_ValueChanged;
                        await BatTxCharacteristic.StartUpdatesAsync();
                        MethodsFactory.LogMessage("Successfully subscribed BatTx for value changes", "Status");
                        return true;

                        //rootPage.NotifyUser("Successfully subscribed for value changes", NotifyType.StatusMessage);
                    }
                    else
                    {
                        MethodsFactory.LogMessage($"Error registering BatTx for value changes: {status}", "ErrorMessage");
                        //rootPage.NotifyUser($"Error registering for value changes: {status}", NotifyType.ErrorMessage);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    // This usually happens when a device reports that it support indicate, but it actually doesn't.
                    //rootPage.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                    MethodsFactory.LogMessage(ex.Message, "ErrorMessage");
                }
            }
            return false;

        }

        private async void BatTx_ValueChanged(object sender, CharacteristicUpdatedEventArgs e)
        {
            byte[] data = e.Characteristic.Value;
            onSystemNotificationReceived(data);
        }

        private void onSystemNotificationReceived(byte[] bytes)
        {
            if (!mHeaderSysPacketDone)
            {
                //ToDo: Check the status
                mHeaderSysPacketDone = true;
            }
            else
            {
                long type = ((bytes[0] & 0xFFL) << 0) |
                            ((bytes[1] & 0xFFL) << 8) |
                            ((bytes[2] & 0xFFL) << 16) |
                            ((bytes[3] & 0xFFL) << 24);

                switch ((int)type)
                {
                    case SYSTEM_BATTERY_REQUEST:
                        {
                            long capacity = ((bytes[4] & 0xFFL) << 0) |
                                            ((bytes[5] & 0xFFL) << 8) |
                                            ((bytes[6] & 0xFFL) << 16) |
                                            ((bytes[7] & 0xFFL) << 24);                //long to support 32-bin unsigned

                            int charging = ((bytes[8] & 0xFF) << 0);

                            broadcastHomeNotification(capacity, "BatCapacity");
                            broadcastHomeNotification((long)charging, "ChargingStatus");
                            break;
                        }
                    case SYSTEM_P3_ID_REQUEST:
                        {
                            long P3ID = ((bytes[4] & 0xFFL) << 0) |
                                        ((bytes[5] & 0xFFL) << 8) |
                                        ((bytes[6] & 0xFFL) << 16) |
                                        ((bytes[7] & 0xFFL) << 24) |
                                        ((bytes[8] & 0xFFL) << 32) |
                                        ((bytes[9] & 0xFFL) << 40) |
                                        ((bytes[10] & 0xFFL) << 48) |
                                        ((bytes[11] & 0xFFL) << 56);
                            broadcastHomeNotification(P3ID, "P3_ID");

                            break;
                        }
                    case SYSTEM_TEMPERATURE_REQUEST:
                        {
                            long temperature = ((bytes[4] & 0xFFL) << 0) |
                                    ((bytes[5] & 0xFFL) << 8) |
                                    ((bytes[6] & 0xFFL) << 16) |
                                    ((bytes[7] & 0xFFL) << 24);
                            broadcastHomeNotification(temperature, "Temperature");

                            break;
                        }
                    case SYSTEM_BATTERY_INFO:
                        {
                            int v = ((bytes[4] & 0xFF) << 0) |
                                    ((bytes[5] & 0xFF) << 8);
                            int i = ((bytes[6] & 0xFF) << 0) |
                                    ((bytes[7] & 0xFF) << 8);
                            int c = ((bytes[8] & 0xFF) << 0) |
                                    ((bytes[9] & 0xFF) << 8);
                            int fcc = ((bytes[10] & 0xFF) << 0) |
                                    ((bytes[11] & 0xFF) << 8);
                            int t = ((bytes[12] & 0xFF) << 0) |
                                    ((bytes[13] & 0xFF) << 8);
                            int v1 = ((bytes[14] & 0xFF) << 0) |
                                    ((bytes[15] & 0xFF) << 8);
                            int v2 = ((bytes[16] & 0xFF) << 0) |
                                    ((bytes[17] & 0xFF) << 8);
                            int cc = ((bytes[18] & 0xFF) << 0) |
                                    ((bytes[19] & 0xFF) << 8);

                            String batteryInfo = "Battery Voltage =  " + v + " mv" +
                                                 "\nBattery Current =  " + i + " mA" +
                                                 "\nBattery ChargingCurrent =  " + cc + " mA" +
                                                 "\nBattery Capacity =  " + c + " mAhr" +
                                                 "\nBattery Full Capacity =  " + fcc + " mAhr" +
                                                 "\nBattery Temperature =  " + t + " cel" +
                                                 "\nBattery CellVoltage1 =  " + v1 + " mv" +
                                                 "\nBattery CellVoltage2 =  " + v2 + " mv";


                            broadcastHomeNotification(batteryInfo, "Battery_info");

                            break;
                        }
                }

                mHeaderSysPacketDone = false;
            }

            Debug.WriteLine("Notification Received - " + (bytes.Length));
        }

        public event EventHandler<BroadcastEventArgs> BroadcastReceived;
        public class BroadcastEventArgs : EventArgs
        {
            public Dictionary<string, object> iGotData { get; set; }
            public DateTime Created { get; set; }
        }
        private void broadcastHomeNotification(long data, String iName)
        {
            Debug.WriteLine(TAG + "INSIDE BROADCAST NOTIFICATION DATA");
            Dictionary<string, object> iGotData = new Dictionary<string, object>();
            //iGotData.setAction(GlobalVariables.HOME_INTENT_ACTION);
            iGotData.Add("iName", iName);
            iGotData.Add("isNotificationSuccess", true);  //false heba change
            iGotData.Add("data", data);
            iGotData.Add("reason", "gotData");
            iGotData.Add("from", "broadcastHomeNotification");
            BroadcastReceived?.Invoke(this, new BroadcastEventArgs() { Created = DateTime.Now, iGotData = iGotData });
            //sendBroadCast(this.HomeActivityContext, iGotData);
        }
        private void broadcastHomeNotification(String input, String iName)
        {
            Debug.WriteLine(TAG+ "INSIDE BROADCAST NOTIFICATION DATA");
            Dictionary<string, object> iGotData = new Dictionary<string, object>();
            //Intent iGotData = new Intent();
            //iGotData.setAction(GlobalVariables.HOME_INTENT_ACTION);
            iGotData.Add("iName", iName);
            iGotData.Add("isNotificationSuccess", true);  //false heba change
            iGotData.Add("data", input);
            iGotData.Add("reason", "gotData");
            iGotData.Add("from", "broadcastHomeNotification");
            BroadcastReceived?.Invoke(this, new BroadcastEventArgs() { Created = DateTime.Now, iGotData = iGotData });
            //sendBroadCast(this.HomeActivityContext, iGotData);
        }
        
        private void broadcastNotificationData(double[] mDoubles, string streamByte)
        {
            Debug.WriteLine(TAG + "INSIDE BROADCAST NOTIFICATION DATA");
           
            Dictionary<string, object> iGotData = new Dictionary<string, object>();
            //iGotData.setAction(GlobalVariables.INTENT_ACTION);
            iGotData.Add("iName", "sensorNotification_data");
            iGotData.Add("isNotificationSuccess", true);  //false heba change
            iGotData.Add("data", mDoubles);
            iGotData.Add("stream", streamByte);
            iGotData.Add("reason", "gotData");
            iGotData.Add("from", "broadcastNotificationData");
            BroadcastReceived?.Invoke(this, new BroadcastEventArgs() { Created = DateTime.Now, iGotData = iGotData });
            //sendBroadCast(getMainActivityContext(), iGotData);
        }

        private void broadcastNotificationMemoryData(double[] mDoubles)
        {
            Debug.WriteLine(TAG+ "INSIDE BROADCAST NOTIFICATION Memory DATA");
            //Intent iGotData = new Intent();
            Dictionary<string, object> iGotData = new Dictionary<string, object>();
            //iGotData.setAction(GlobalVariables.HOME_INTENT_ACTION);
            iGotData.Add("iName", "MemoryScanData");
            iGotData.Add("isNotificationSuccess", true);  //false heba change
            iGotData.Add("data", mDoubles);
            iGotData.Add("reason", "gotData");
            iGotData.Add("from", "broadcastNotificationData");
            BroadcastReceived?.Invoke(this, new BroadcastEventArgs() { Created = DateTime.Now, iGotData = iGotData });
            //sendBroadCast(this.HomeActivityContext, iGotData);
        }
        private void broadcastNotificationFailure(String msg, String reason, int errorCode)
        {
            Debug.WriteLine(TAG + "inside broadcastNotificationFailure");
            Dictionary<string, object> iGotData = new Dictionary<string, object>();
            //iGotData.setAction(GlobalVariables.INTENT_ACTION);
            iGotData.Add("iName", "sensorNotification_failure");
            iGotData.Add("isNotificationSuccess", false);
            iGotData.Add("err", msg);
            iGotData.Add("reason", reason);
            iGotData.Add("data", errorCode);
            BroadcastReceived?.Invoke(this, new BroadcastEventArgs() { Created = DateTime.Now, iGotData = iGotData });
            //sendBroadCast(getMainActivityContext(), iGotData);
        }

        private void broadcastNotificationconnected(String msg)
        {
            Debug.WriteLine(TAG + "inside broadcastNotificationconnected " + msg);
            Dictionary<string, object> iGotData = new Dictionary<string, object>();
            //iGotData.Addn(GlobalVariables.INTENT_ACTION);
            iGotData.Add("iName", "sensorNotification_connection");
            iGotData.Add("isNotificationSuccess", true);
            iGotData.Add("err", msg);
            iGotData.Add("reason", "connected");
            BroadcastReceived?.Invoke(this, new BroadcastEventArgs() { Created = DateTime.Now, iGotData = iGotData });
            //sendBroadCast(getMainActivityContext(), iGotData);
        }

        public void broadcastdisconnectionNotification()
        {
            Debug.WriteLine(TAG + "inside broadcastdisconnectionNotification");
            //System.out.println("inside broadcastdisconnectionNotification");
            //Intent iGotData = new Intent();
            Dictionary<string, object> iGotData = new Dictionary<string, object>();
            //iGotData.setAction(GlobalVariables.INTENT_ACTION);
            iGotData.Add("iName", "Disconnection_Notification");
            iGotData.Add("reason", "disconnected");
            BroadcastReceived?.Invoke(this, new BroadcastEventArgs() { Created = DateTime.Now, iGotData = iGotData });
            //sendBroadCast(getMainActivityContext(), iGotData);

            iGotData = new Dictionary<string, object>();
            //iGotData.setAction(GlobalVariables.HOME_INTENT_ACTION);
            iGotData.Add("iName", "Disconnection_Notification");
            iGotData.Add("reason", "disconnected");
            BroadcastReceived?.Invoke(this, new BroadcastEventArgs() { Created = DateTime.Now, iGotData = iGotData });
            //sendBroadCast(this.HomeActivityContext, iGotData);
        }
       
       
    }


}
