using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Threading.Tasks;

namespace BLEConnecter
{
    public class WeightScale
    {
        private GattDeviceService Service;

        const string SERVICE_UUID = "181D";

        public async void Start()
        {
            var devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(Common.CreateFullUUID(SERVICE_UUID)));
            if (devices.Count <= 0) {
                // デバイス無し
                Console.WriteLine("デバイス無し...");
                return;
            }

            this.Service = await GattDeviceService.FromIdAsync(devices.First().Id);
            if (this.Service == null) {
                Console.WriteLine("サービスに接続できない...");
                return;
            }

            // for log
            {
                Console.WriteLine($"Service.Uuid...{Service.Uuid}");
                Console.WriteLine($"Servicev.DeviceId...{Service.DeviceId}");
                Console.WriteLine($"Servicev.Device.Name...{Service.Device.Name}");

                var characteristics = Service.GetAllCharacteristics();
                foreach (var ch in characteristics) {
                    Console.WriteLine($"CharacteristicUUID...{ch.Uuid}");
                    Console.WriteLine($"CharacteristicProperties...{ch.CharacteristicProperties}");
                }
            }

            // Weight Scale Measurement
            {
                var characteristics = Service.GetCharacteristics(Common.CreateFullUUID("2A9D"));
                if (characteristics.Count > 0) {
                    this.Characteristic_WeightScale_Measurement = characteristics.First();
                    if (this.Characteristic_WeightScale_Measurement == null) {
                        Console.WriteLine("Characteristicに接続できない...");
                    } else {
                        if (this.Characteristic_WeightScale_Measurement.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)) {
                            // イベントハンドラ追加
                            this.Characteristic_WeightScale_Measurement.ValueChanged += characteristicChanged_WeightScale_Measurement;

                            // これで有効になる
                            await this.Characteristic_WeightScale_Measurement.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
                        }
                    }
                }
            }

        }

        public async void Stop()
        {
            Console.WriteLine($"Service Close...{Service.Device.Name}");

            await this.Characteristic_WeightScale_Measurement.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);

            this.Service.Dispose();
        }

        // WeightScale_Measurement
        private GattCharacteristic Characteristic_WeightScale_Measurement;
        private void characteristicChanged_WeightScale_Measurement(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
        {
            Console.WriteLine($"characteristicChanged_WeightScale_Measurement...Length={eventArgs.CharacteristicValue.Length}");
            if (eventArgs.CharacteristicValue.Length <= 0) {
                return;
            }

            byte[] data = new byte[eventArgs.CharacteristicValue.Length];
            Windows.Storage.Streams.DataReader.FromBuffer(eventArgs.CharacteristicValue).ReadBytes(data);
            var tmp = BitConverter.ToString(data);
            Console.WriteLine($"characteristicChanged...{tmp}");

            // Parse
            {
                if (eventArgs.CharacteristicValue.Length < 3) {
                    return;
                }
                Console.WriteLine($"flags = {BitConverter.ToString(data, 0, 1)}");
                Console.WriteLine($"C1    = {BitConverter.ToString(data, 1, 2)}");

                byte[] c1 = data.Skip(1).Take(2).ToArray();

                // Weight Scale Measurement(uint16)
                // Unit is in kilograms with a resolution of 0.005, and determined when bit 0 of the Flags field is set to 0.
                // https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.weight_measurement.xml
                var val = BitConverter.ToUInt16(c1, 0)*0.005;

                Console.WriteLine($"WeightScale Measurement    = {val} Kg");
            }

            return;
        }

    }
}
