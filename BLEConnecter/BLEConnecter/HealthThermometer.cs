using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace BLEConnecter
{
    public class HealthThermometer
    {
        private GattDeviceService Service;

        const string SERVICE_UUID = "1809";

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

            // Temperature Measurement
            // Requirement = M , Mandatory Properties = Indicate
            {
                var characteristics = Service.GetCharacteristics(Common.CreateFullUUID("2A1C"));
                if (characteristics.Count > 0) {
                    this.Characteristic_Temperature_Measurement = characteristics.First();
                    if (this.Characteristic_Temperature_Measurement == null) {
                        Console.WriteLine("Characteristicに接続できない...");
                    } else {
                        if (this.Characteristic_Temperature_Measurement.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)) {
                            // イベントハンドラ追加
                            this.Characteristic_Temperature_Measurement.ValueChanged += characteristicChanged_Temperature_Measurement;

                            // これで有効になる
                            await this.Characteristic_Temperature_Measurement.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
                        }
                    }
                }
            }

            // Intermediate Temperature:Notify
            // Requirement = O , Mandatory Properties = Notify
            {
                var characteristics = Service.GetCharacteristics(Common.CreateFullUUID("2A1E"));
                if (characteristics.Count > 0) {
                    this.Characteristic_Intermediate_Temperature = characteristics.First();
                    if (this.Characteristic_Intermediate_Temperature == null) {
                        Console.WriteLine("Characteristicに接続できない...");
                    } else {
                        if (this.Characteristic_Intermediate_Temperature.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)) {
                            this.Characteristic_Intermediate_Temperature.ValueChanged += characteristicChanged_Intermediate_Temperature;
                            await this.Characteristic_Intermediate_Temperature.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        }
                    }
                }
            }
        }

        public async void Stop()
        {
            Console.WriteLine($"Service Close...{Service.Device.Name}");

            await this.Characteristic_Temperature_Measurement.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            await this.Characteristic_Intermediate_Temperature.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            this.Service.Dispose();
        }

        // Temperature_Measurement　体温測定値
        private GattCharacteristic Characteristic_Temperature_Measurement;
        private void characteristicChanged_Temperature_Measurement(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
        {
            Console.WriteLine($"characteristicChanged...Length={eventArgs.CharacteristicValue.Length}");
            if (eventArgs.CharacteristicValue.Length <= 0) {
                return;
            }

            byte[] data = new byte[eventArgs.CharacteristicValue.Length];
            Windows.Storage.Streams.DataReader.FromBuffer(eventArgs.CharacteristicValue).ReadBytes(data);

            // for log
            {
                var tmp = BitConverter.ToString(data);
                Console.WriteLine($"characteristicChanged...{tmp}");
            }

            // Parse
            {
                if (eventArgs.CharacteristicValue.Length < 4) {
                    return;
                }
                Console.WriteLine($"flags = {BitConverter.ToString(data, 0, 1)}");
                Console.WriteLine($"C1    = {BitConverter.ToString(data, 1, 4)}");

                byte[] c1 = data.Skip(1).Take(4).ToArray();
                var temperature = Common.ConvertToFloat(c1, Common.ConvType.IEEE_11073_32bit_float);
                Console.WriteLine($"Temperature    = {temperature}℃");
            }

            return;
        }

        // Intermediate_Temperature　体温の変化通知
        private GattCharacteristic Characteristic_Intermediate_Temperature;
        private void characteristicChanged_Intermediate_Temperature(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
        {
            Console.WriteLine($"characteristicChanged_Intermediate_Temperature...Length={eventArgs.CharacteristicValue.Length}");
            if (eventArgs.CharacteristicValue.Length <= 0) {
                return;
            }

            byte[] data = new byte[eventArgs.CharacteristicValue.Length];
            Windows.Storage.Streams.DataReader.FromBuffer(eventArgs.CharacteristicValue).ReadBytes(data);
            var tmp = BitConverter.ToString(data);
            Console.WriteLine($"characteristicChanged...{tmp}");

            return;
        }

    }
}
