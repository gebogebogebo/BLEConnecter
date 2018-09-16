using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace BLEConnecter
{
    class BloodPressure
    {
        private GattDeviceService Service;

        const string SERVICE_UUID = "1810";

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

            // Blood Pressure Measurement
            // Requirement = M , Mandatory Properties = Indicate
            {
                var characteristics = Service.GetCharacteristics(Common.CreateFullUUID("2A35"));
                if (characteristics.Count > 0) {
                    this.Characteristic_Blood_Pressure_Measurement = characteristics.First();
                    if (this.Characteristic_Blood_Pressure_Measurement == null) {
                        Console.WriteLine("Characteristicに接続できない...");
                    } else {
                        if (this.Characteristic_Blood_Pressure_Measurement.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)) {
                            // イベントハンドラ追加
                            this.Characteristic_Blood_Pressure_Measurement.ValueChanged += characteristicChanged_Blood_Pressure_Measurement;

                            // これで有効になる
                            await this.Characteristic_Blood_Pressure_Measurement.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
                        }
                    }
                }
            }

            // Blood Pressure Feature
            // Requirement = M , Mandatory Properties = Read
            {
                var characteristics = Service.GetCharacteristics(Common.CreateFullUUID("2A49"));
                if (characteristics.Count > 0) {
                    var chara = characteristics.First();
                    if (chara == null) {
                        Console.WriteLine("Characteristicに接続できない...");
                    } else {
                        if (chara.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read)) {
                            GattReadResult result = await chara.ReadValueAsync();
                            if (result.Status == GattCommunicationStatus.Success) {
                                var reader = Windows.Storage.Streams.DataReader.FromBuffer(result.Value);
                                byte[] input = new byte[reader.UnconsumedBufferLength];
                                reader.ReadBytes(input);

                                var tmp = BitConverter.ToString(input);
                                Console.WriteLine($"Blood Pressure Feature...{tmp}");
                            }
                        }
                    }
                }
            }

        }

        public async void Stop()
        {
            Console.WriteLine($"Service Close...{Service.Device.Name}");

            await this.Characteristic_Blood_Pressure_Measurement.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            this.Service.Dispose();
        }

        // Blood Pressure Measurement　血圧測定値
        private GattCharacteristic Characteristic_Blood_Pressure_Measurement;
        private void characteristicChanged_Blood_Pressure_Measurement(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
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
                if (eventArgs.CharacteristicValue.Length < 7) {
                    return;
                }
                Console.WriteLine($"flags = {BitConverter.ToString(data, 0, 1)}");
                Console.WriteLine($"C1    = {BitConverter.ToString(data, 1, 6)}");

                {
                    byte[] c1 = data.Skip(1).Take(2).ToArray();
                    var val = Common.ConvertToFloat(c1, Common.ConvType.IEEE_11073_16bit_float);
                    Console.WriteLine($"Blood Pressure Measurement Compound Value - Systolic(最高血圧)    = {val}mmHg");
                }
                {
                    byte[] c1 = data.Skip(3).Take(2).ToArray();
                    var val = Common.ConvertToFloat(c1, Common.ConvType.IEEE_11073_16bit_float);
                    Console.WriteLine($"Blood Pressure Measurement Compound Value - Diastolic(最低血圧)   = {val}mmHg");
                }
                {
                    byte[] c1 = data.Skip(5).Take(2).ToArray();
                    var val = Common.ConvertToFloat(c1, Common.ConvType.IEEE_11073_16bit_float);
                    Console.WriteLine($"Mean Arterial Pressure(平均)                                      = {val}mmHg");
                }
            }

            return;
        }

    }
}
