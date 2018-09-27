using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Threading.Tasks;

namespace BLEConnecter
{
    static public class DeviceInformationService
    {
        static public async Task<bool> CheckDeviceInformation()
        {
            Console.WriteLine($"★Start CheckDeviceInformation");

            var devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(Common.CreateFullUUID("180A")));
            Console.WriteLine($"devices.Count...{devices.Count}");
            if (devices.Count <= 0) {
                // デバイス無し
                Console.WriteLine("デバイス無し...");
                return (false);
            }

            foreach (var dev in devices) {
                Console.WriteLine($"dev.Id...{dev.Id}");
                var service = await GattDeviceService.FromIdAsync(dev.Id);
                if (service == null) {
                    Console.WriteLine("サービスに接続できない...");
                    continue;
                }

                // for log
                {
                    Console.WriteLine($"Service.Uuid...{service.Uuid}");
                    Console.WriteLine($"Servicev.DeviceId...{service.DeviceId}");
                    Console.WriteLine($"Servicev.Device.Name...{service.Device.Name}");

                    var characteristics = service.GetAllCharacteristics();
                    foreach (var ch in characteristics) {
                        Console.WriteLine($"Characteristic...");
                        Console.WriteLine($"...AttributeHandle=0x{ch.AttributeHandle.ToString("X2")}");
                        Console.WriteLine($"...Properties={ch.CharacteristicProperties}");
                        Console.WriteLine($"...ProtectionLevel={ch.ProtectionLevel}");
                        Console.WriteLine($"...UUID={ch.Uuid}");

                        if (ch.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read)) {
                            // Readプロパティがあるのに、読もうとするとExceptionがでるプロパティが時々あるなんなんだ（トラップ）
                            try {
                                GattReadResult result = await ch.ReadValueAsync();
                                if (result.Status == GattCommunicationStatus.Success) {
                                    var reader = Windows.Storage.Streams.DataReader.FromBuffer(result.Value);
                                    if (reader.UnconsumedBufferLength > 0) {
                                        byte[] input = new byte[reader.UnconsumedBufferLength];
                                        reader.ReadBytes(input);

                                        var tmp = BitConverter.ToString(input);
                                        var strvalue = System.Text.Encoding.ASCII.GetString(input);
                                        Console.WriteLine($"Characteristic Data...{tmp}...({strvalue})");
                                    }
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"ReadValueAsync() Exception...{ex.Message})");
                            }
                        }

                    }
                }
            }

            Console.WriteLine($"★End CheckDeviceInformation");

            return (true);
        }

    }
}
