using System;
using System.Collections.Generic;
using System.Windows;

namespace BLEConnecter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        HealthThermometer HealthThermometerService;
        private void button_Click(object sender, RoutedEventArgs e)
        {
            HealthThermometerService = new HealthThermometer();
            HealthThermometerService.Start();
        }

        private void button_Copy_Click(object sender, RoutedEventArgs e)
        {
            HealthThermometerService.Stop();
            HealthThermometerService = null;
        }

        BloodPressure BloodPressureService;
        private void button_BP_Click(object sender, RoutedEventArgs e)
        {
            BloodPressureService = new BloodPressure();
            BloodPressureService.Start();
        }

        private void button_BP_Copy_Click(object sender, RoutedEventArgs e)
        {
            BloodPressureService.Stop();
            BloodPressureService = null;
        }

        WeightScale WeightScaleService;
        private void button_WS_Click(object sender, RoutedEventArgs e)
        {
            WeightScaleService = new WeightScale();
            WeightScaleService.Start();
        }

        private void button_WS_Copy_Click(object sender, RoutedEventArgs e)
        {
            WeightScaleService.Stop();
        }

        private void button_DI_Click(object sender, RoutedEventArgs e)
        {
            DeviceInformationService.CheckDeviceInformation();
        }

    }
}
