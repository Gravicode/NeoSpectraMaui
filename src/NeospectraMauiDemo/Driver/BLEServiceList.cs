using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.PortableExecutable;

namespace NeospectraMauiDemo.Driver
{
    public class BLEServiceUID
    {
        public Guid UID { get; set; }

        public List<Guid> CharacteristicUIDs { get; set; }
        public BLEServiceUID(Guid UID)
        {
            this.UID = UID;
            CharacteristicUIDs = new List<Guid>();
        }
    }
    public class BLEServiceList
    {
        public bool IsReady { get; set; } = false;
        List<BLEServiceUID> BLEServiceUIDs;
        public BLEServiceList(IDevice device)
        {
            this.BLEServiceUIDs = new List<BLEServiceUID>();
            if (device != null)
            {
                if (device.State == DeviceState.Connected)
                {
                    Setup(device);
                }
            }
        }

        async void Setup(IDevice IDevice)
        {
            var services = await IDevice.GetServicesAsync();
            foreach (var service in services)
            {

                var svc = new BLEServiceUID(service.Id);


                // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only 
                // and the new Async functions to get the characteristics of unpaired devices as well. 
                var characteristics = await service.GetCharacteristicsAsync();

                foreach (var characteristic in characteristics)
                {
                    svc.CharacteristicUIDs.Add(characteristic.Id);
                }
                BLEServiceUIDs.Add(svc);
            }
            IsReady = true;

        }

        public Guid? GetServiceUIDByCharacteristicUID(Guid UID)
        {
            foreach(var service in BLEServiceUIDs)
            {
                if(service.CharacteristicUIDs.Any(x=>x.Equals(UID)))
                {
                    return service.UID;
                }
            }
            return null;
        }
    }
}
