using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace BLEConnecter
{
    public class WeightScale
    {
        private GattDeviceService Service;

        const string SERVICE_UUID = "181D";
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

                //await this.Characteristic_WeightScale_Measurement.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);

                this.Service.Dispose();
            }
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
