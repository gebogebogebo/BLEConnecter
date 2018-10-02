using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace BLEConnecter
{
    public class HealthThermometer2: HealthThermometer
    {
        private BluetoothLEAdvertisementWatcher advWatcher;

        override public async void Start()
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

            // Health Thermometerサービスを検索
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

                } catch (Exception ex) {
                    Console.WriteLine($"Exception...{ex.Message})");
                }
            } else {
                Console.WriteLine($"...");
            }


        }

    }
}
