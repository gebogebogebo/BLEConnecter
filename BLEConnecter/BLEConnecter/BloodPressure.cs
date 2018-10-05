using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace BLEConnecter
{
    class BloodPressure
    {
        private GattDeviceService Service;

        const string SERVICE_UUID = "1810";
        private BluetoothLEAdvertisementWatcher advWatcher;

        public async void Start()
        {
            this.advWatcher = new BluetoothLEAdvertisementWatcher();

            this.advWatcher.SignalStrengthFilter.SamplingInterval = TimeSpan.FromMilliseconds(1000);
            this.advWatcher.ScanningMode = BluetoothLEScanningMode.Passive;

            this.advWatcher.Received += this.Watcher_Received;

            // スキャン開始
            this.advWatcher.Start();
        }

        private async void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            this.CheckArgs(args);
        }

        public async void CheckArgs(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Console.WriteLine("★アドバタイズパケットスキャン");

            bool find = false;
            {
                var bleServiceUUIDs = args.Advertisement.ServiceUuids;
                foreach (var uuidone in bleServiceUUIDs) {
                    if (uuidone == Common.CreateFullUUID(SERVICE_UUID)) {
                        // 発見
                        find = true;
                        break;
                    }
                }
            }

            if (find) {
                try {
                    Console.WriteLine($"Service Find！");

                    // スキャンStop
                    this.advWatcher.Stop();

                    BluetoothLEDevice dev = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                    this.Service = dev.GetGattService(Common.CreateFullUUID(SERVICE_UUID));

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

                } catch (Exception ex) {
                    Console.WriteLine($"Exception...{ex.Message})");
                }
            } else {
                Console.WriteLine($"...");
            }
        }

        public async void Stop()
        {
            if (advWatcher != null) {
                this.advWatcher.Stop();
            }
            if (Service != null) {
                Console.WriteLine($"Service Close...{Service.Device.Name}");
                this.Service.Dispose();
            }
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
