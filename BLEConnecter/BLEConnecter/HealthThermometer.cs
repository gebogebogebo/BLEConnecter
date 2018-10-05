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
    public class HealthThermometer
    {
        protected GattDeviceService Service;

        protected const string SERVICE_UUID = "1809";

        public virtual async void Start1()
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
                    Console.WriteLine($"Characteristic...");
                    Console.WriteLine($"...AttributeHandle=0x{ch.AttributeHandle.ToString("X2")}");
                    Console.WriteLine($"...Properties={ch.CharacteristicProperties}");
                    Console.WriteLine($"...ProtectionLevel={ch.ProtectionLevel}");
                    Console.WriteLine($"...UUID={ch.Uuid}");
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

        private BluetoothLEAdvertisementWatcher advWatcher;
        public async void Start2()
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

                    // for log
                    {
                        Console.WriteLine($"Service.Uuid...{Service.Uuid}");
                        Console.WriteLine($"Servicev.DeviceId...{Service.DeviceId}");
                        Console.WriteLine($"Servicev.Device.Name...{Service.Device.Name}");

                        var characteristics = Service.GetAllCharacteristics();
                        foreach (var ch in characteristics) {
                            Console.WriteLine($"Characteristic...");
                            Console.WriteLine($"...AttributeHandle=0x{ch.AttributeHandle.ToString("X2")}");
                            Console.WriteLine($"...Properties={ch.CharacteristicProperties}");
                            Console.WriteLine($"...ProtectionLevel={ch.ProtectionLevel}");
                            Console.WriteLine($"...UUID={ch.Uuid}");
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

                //if (Characteristic_Temperature_Measurement != null) {
                //    await this.Characteristic_Temperature_Measurement.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                //}
                //if (Characteristic_Intermediate_Temperature != null) {
                //    await this.Characteristic_Intermediate_Temperature.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                //}

                this.Service.Dispose();
            }
        }

        // Temperature_Measurement　体温測定値
        protected GattCharacteristic Characteristic_Temperature_Measurement;
        protected void characteristicChanged_Temperature_Measurement(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
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
        protected GattCharacteristic Characteristic_Intermediate_Temperature;
        protected void characteristicChanged_Intermediate_Temperature(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
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
