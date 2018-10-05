package com.example.gebo.bleconnecterforandroid;

import android.Manifest;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothGatt;
import android.bluetooth.BluetoothGattCallback;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothGattDescriptor;
import android.bluetooth.BluetoothGattService;
import android.bluetooth.BluetoothManager;
import android.bluetooth.BluetoothProfile;
import android.bluetooth.le.BluetoothLeScanner;
import android.bluetooth.le.ScanCallback;
import android.bluetooth.le.ScanFilter;
import android.bluetooth.le.ScanResult;
import android.bluetooth.le.ScanSettings;
import android.content.Context;
import android.content.pm.PackageManager;
import android.os.ParcelUuid;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.TextView;

import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.List;
import java.util.UUID;

import static android.bluetooth.BluetoothGattCharacteristic.FORMAT_FLOAT;
import static android.bluetooth.BluetoothGattCharacteristic.FORMAT_SFLOAT;
import static android.bluetooth.BluetoothGattCharacteristic.FORMAT_UINT16;

public class MainActivity extends AppCompatActivity {

    private BluetoothAdapter mBluetoothAdapter;
    private BluetoothLeScanner mBluetoothLeScanner;
    private ScanCallback mScanCallback;
    private BluetoothGatt mBluetoothGatt;
    private BluetoothGattCallback mGattCallback;
    private BluetoothGattCharacteristic mBleCharacteristic;

    private static final String SERVICE_Health_Thermometer = "00001809-0000-1000-8000-00805F9B34FB";
    private static final String SERVICE_Blood_Pressure = "00001810-0000-1000-8000-00805F9B34FB";
    private static final String SERVICE_Weight_Scale = "0000181D-0000-1000-8000-00805F9B34FB";

    private static final String CHARACTERISTIC_CONFIG_UUID = "00002902-0000-1000-8000-00805F9B34FB";
    private static final String CHARACTERISTIC_Temperature_Measurement = "00002A1C-0000-1000-8000-00805F9B34FB";
    private static final String CHARACTERISTIC_Blood_Pressure_Measurement = "00002A35-0000-1000-8000-00805F9B34FB";
    private static final String CHARACTERISTIC_Weight_Scale_Measurement = "00002A9D-0000-1000-8000-00805F9B34FB";

    private int mBLEDevType = 0;       // 1:Health Thermometer , 2:Bood Presshure , 3 :Weight Scale

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        // Ready - Clickイベント
        findViewById(R.id.button_ready).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if( onClickReady(v) == true ) {
                    ((TextView) findViewById(R.id.text1)).setText("Ready-OK!");
                } else {
                    ((TextView) findViewById(R.id.text1)).setText("Ready-Error");
                }
            }
        });

        // Start - Clickイベント
        {
            mScanCallback = new ScanCallback() {

                // スキャンしたとき
                @Override
                public void onScanResult(int callbackType, ScanResult result) {
                    super.onScanResult(callbackType, result);
                    onScanResultMethod(callbackType,result);
                }

                // スキャンしたとき（こっちもあるっぽいがどんなときに発生するか不明）
                @Override
                public void onBatchScanResults(List<ScanResult> results) {
                    super.onBatchScanResults(results);
                    Log.d("", "？？？onBatchScanResults");
                }

                @Override
                public void onScanFailed(int errorCode) {
                    super.onScanFailed(errorCode);
                    Log.d("", "？？？onScanFailed");
                }
            };

            findViewById(R.id.button_HT_start).setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    mBLEDevType = 1;
                    if( onClickStart() == true ) {
                        ((TextView) findViewById(R.id.text1)).setText("Start-OK!");
                    } else {
                        ((TextView) findViewById(R.id.text1)).setText("Start-Error");
                    }
                }
            });

            findViewById(R.id.button_BP_start).setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    mBLEDevType = 2;
                    if( onClickStart() == true ) {
                        ((TextView) findViewById(R.id.text1)).setText("Start-OK!");
                    } else {
                        ((TextView) findViewById(R.id.text1)).setText("Start-Error");
                    }
                }
            });

            findViewById(R.id.button_WS_start).setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    mBLEDevType = 3;
                    if( onClickStart() == true ) {
                        ((TextView) findViewById(R.id.text1)).setText("Start-OK!");
                    } else {
                        ((TextView) findViewById(R.id.text1)).setText("Start-Error");
                    }
                }
            });

        }

        // Stop - Clickイベント
        findViewById(R.id.button_HT_stop).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                ((TextView) findViewById(R.id.text1)).setText("Stop");
                if( mBluetoothLeScanner != null ) {
                    mBluetoothLeScanner.stopScan(mScanCallback);
                }
                mBLEDevType = 0;
            }
        });
    }

    // Ready
    private boolean onClickReady(View v) {
        // 6.0以降はコメントアウトした処理をしないと初回はパーミッションがOFFになっています。
        if (checkSelfPermission(Manifest.permission.ACCESS_COARSE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
            requestPermissions(new String[]{Manifest.permission.ACCESS_COARSE_LOCATION}, 0);
        }

        BluetoothManager manager = (BluetoothManager) getSystemService(Context.BLUETOOTH_SERVICE);
        mBluetoothAdapter = manager.getAdapter();
        mBluetoothLeScanner = mBluetoothAdapter.getBluetoothLeScanner();
        if( mBluetoothLeScanner == null ){
            return(false);
        }

        return(true);
    }

    // Start
    private boolean onClickStart(){

        if( mBluetoothLeScanner == null ){
            return(false);
        }

        ScanFilter scanFilter =
                new ScanFilter.Builder()
                        //.setDeviceName("Health")
                        .build();
        ArrayList scanFilterList = new ArrayList();
        scanFilterList.add(scanFilter);

        ScanSettings scanSettings = new ScanSettings.Builder().setScanMode(ScanSettings.SCAN_MODE_BALANCED).build();

        mBluetoothLeScanner.startScan(scanFilterList, scanSettings, mScanCallback);

        return(true);
    }

    // Scan
    private void onScanResultMethod(int callbackType, ScanResult result) {
        Log.d("","アドバタイズパケットスキャン中...");

        boolean isFindService = false;

        List<ParcelUuid> serviceUuids = result.getScanRecord().getServiceUuids();
        if( serviceUuids != null ) {
            for (ParcelUuid uuid : serviceUuids) {
                if( mBLEDevType == 1 ) {
                    // Health Thermometerかどうかのチェック
                    if (SERVICE_Health_Thermometer.equals(uuid.toString().toUpperCase())) {
                        isFindService = true;
                    }
                } else if( mBLEDevType == 2 ) {
                    if (SERVICE_Blood_Pressure.equals(uuid.toString().toUpperCase())) {
                        isFindService = true;
                    }
                } else if( mBLEDevType == 3 ) {
                    if (SERVICE_Weight_Scale.equals(uuid.toString().toUpperCase())) {
                        isFindService = true;
                    }

                }
            }
        }

        if( isFindService == true){
            // 発見→スキャン停止→コネクト
            mBluetoothLeScanner.stopScan(mScanCallback);

            setText1("BLE Device Find!");
            Log.d("","★アドバタイズパケットスキャン");
            Log.d("", "callbackType = " + callbackType);
            Log.d("", "BluetoothAddress = " + result.getDevice().getAddress());
            Log.d("", "RSSI = " + String.format(("%d"),result.getRssi()));
            Log.d("", "Name = " + result.getDevice().getName());

            this.connect(this,result.getDevice());
        }

    }

    public void connect(Context context, BluetoothDevice device) {
        mGattCallback = new BluetoothGattCallback() {
            @Override
            public void onConnectionStateChange(BluetoothGatt gatt, int status, int newState) {
                super.onConnectionStateChange(gatt, status, newState);

                // 接続成功し、サービス取得
                if (newState == BluetoothProfile.STATE_CONNECTED) {
                    if (mBluetoothGatt != null) {
                        setText1("BLE Device Connect");
                        mBluetoothGatt.discoverServices();
                        // -> onServicesDiscovered
                    }
                } else if (newState == BluetoothGatt.STATE_DISCONNECTED) {
                    // ペリフェラルとの接続が切れた時点でオブジェクトを空にする
                    if (mBluetoothGatt != null) {
                        setText1("BLE Device Disconnect");
                        mBluetoothGatt.close();
                        mBluetoothGatt = null;
                    }
                }
            }

            @Override
            public void onServicesDiscovered(BluetoothGatt gatt, int status) {
                super.onServicesDiscovered(gatt, status);
                if (status == BluetoothGatt.GATT_SUCCESS) {
                    mBluetoothGatt = gatt;
                    Log.d("","START - onServicesDiscovered!");
                    getGATTService(gatt.getServices());
                    Log.d("","END - onServicesDiscovered!");
                }
            }

            @Override
            public void onCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
            {
                // キャラクタリスティックのUUIDをチェック
                if (CHARACTERISTIC_Temperature_Measurement.equals(characteristic.getUuid().toString().toUpperCase())) {
                    // Health Thermometer Service - Temperature Measurement Characteristic
                    setText1("Get Data - Health Thermometer Service - Temperature Measurement Characteristic");
                    parseTemperatureMeasurementData(characteristic);
                } else if (CHARACTERISTIC_Blood_Pressure_Measurement.equals(characteristic.getUuid().toString().toUpperCase())){
                    // Blood Pressure Service - Blood Pressure Measurement Characteristic
                    setText1("Get Data - Blood Pressure Service - Blood Pressure Measurement Characteristic");
                    parseBloodPressureMeasurementData(characteristic);
                } else if (CHARACTERISTIC_Weight_Scale_Measurement.equals(characteristic.getUuid().toString().toUpperCase())){
                    // Weight Scale Measurement Service -Weight Scale Measurement Characteristic
                    setText1("Get Data - Weight Scale Measurement Service -Weight Scale Measurement Characteristic");
                    parseWeightScaleMeasurementData(characteristic);
                }
            }

        };

        mBluetoothGatt = device.connectGatt(context, false, mGattCallback);
        mBluetoothGatt.connect();
        // -> onConnectionStateChange
    }

    private void getGATTService(List<BluetoothGattService> serviceList){

        // log
        Log.d("", String.format("Service Num = %d",serviceList.size()));
        /*
        for log
        for (BluetoothGattService s : serviceList) {
            Log.d("", "<Service>");
            Log.d("", String.format("-> Service UUID = %s",s.getUuid()));
            Log.d("", String.format("-> Service Type = %d",s.getType()));
            List<BluetoothGattCharacteristic> characteristics = s.getCharacteristics();
            Log.d("", String.format("-> Characteristics Num = %d",characteristics.size()));
            for(BluetoothGattCharacteristic c: characteristics){
                Log.d("", "-><Characteristics>");
                Log.d("", String.format("->-> Characteristics UUID = %s",c.getUuid()));
            }
        }
        */

        String searchServiceUUID = "";
        String searchChUUID = "";
        if( mBLEDevType == 1 ) {
            // Temperature Measurement
            // Requirement = M , Mandatory Properties = Indicate
            searchServiceUUID = SERVICE_Health_Thermometer;
            searchChUUID = CHARACTERISTIC_Temperature_Measurement;
        }else if( mBLEDevType == 2 ) {
            searchServiceUUID = SERVICE_Blood_Pressure;
            searchChUUID = CHARACTERISTIC_Blood_Pressure_Measurement;
        }else if( mBLEDevType == 3 ) {
            searchServiceUUID = SERVICE_Weight_Scale;
            searchChUUID = CHARACTERISTIC_Weight_Scale_Measurement;
        }

        if(searchServiceUUID.length() > 0 ) {
            BluetoothGattService service = mBluetoothGatt.getService(UUID.fromString(searchServiceUUID));
            if (service != null) {
                mBleCharacteristic = service.getCharacteristic(UUID.fromString(searchChUUID));

                if (mBleCharacteristic != null) {
                    // キャラクタリスティックが見つかったら、Notificationをリクエスト.
                    boolean registered = mBluetoothGatt.setCharacteristicNotification(mBleCharacteristic, true);

                    // Characteristic の Notificationを有効化する.
                    BluetoothGattDescriptor descriptor = mBleCharacteristic.getDescriptor(
                            UUID.fromString(CHARACTERISTIC_CONFIG_UUID));

                    descriptor.setValue(BluetoothGattDescriptor.ENABLE_INDICATION_VALUE);
                    mBluetoothGatt.writeDescriptor(descriptor);
                    // 接続が完了したらデータ送信を開始する.
                    //mIsBluetoothEnable = true;
                    Log.d("", "Notify-START");
                }
            }
        }

    }

    private void parseTemperatureMeasurementData(BluetoothGattCharacteristic characteristic){
        Log.d("", "Notify-Event");
        // Peripheralで値が更新されたらNotificationを受ける.
        byte[] val = characteristic.getValue();
        if( val != null ){
            if( val.length <= 0 ){
                Log.d("", "Notify-Data=0");
                return;
            } else {
                Log.d("", String.format("Notify-Data=%s", convByteToHexString(val)));
            }
        } else {
            Log.d("", "Notify-Data=null");
            return;
        }

        // C1
        Float floatValue = characteristic.getFloatValue(FORMAT_FLOAT, 1);
        Log.d("", String.format("体温 = %.1f ℃",floatValue));
        setText2(String.format("体温 = %.1f℃",floatValue));
    }

    private void parseBloodPressureMeasurementData(BluetoothGattCharacteristic characteristic){
        Log.d("", "Notify-Event");
        // Peripheralで値が更新されたらNotificationを受ける.
        byte[] val = characteristic.getValue();
        if( val != null ){
            if( val.length <= 0 ){
                Log.d("", "Notify-Data=0");
                return;
            } else {
                Log.d("", String.format("Notify-Data=%s", convByteToHexString(val)));
            }
        } else {
            Log.d("", "Notify-Data=null");
            return;
        }

        // C1
        Float floatValue1 = characteristic.getFloatValue(FORMAT_SFLOAT, 1);
        Float floatValue2 = characteristic.getFloatValue(FORMAT_SFLOAT, 3);
        String text = String.format("最低血圧 = %.1f mmHg , 最高血圧 = %.1f mmHg",floatValue1,floatValue2);
        Log.d("", text);
        setText2(text);
    }

    private void parseWeightScaleMeasurementData(BluetoothGattCharacteristic characteristic){
        Log.d("", "Notify-Event");
        // Peripheralで値が更新されたらNotificationを受ける.
        byte[] val = characteristic.getValue();
        if( val != null ){
            if( val.length <= 0 ){
                Log.d("", "Notify-Data=0");
                return;
            } else {
                Log.d("", String.format("Notify-Data=%s", convByteToHexString(val)));
            }
        } else {
            Log.d("", "Notify-Data=null");
            return;
        }

        // C1
        int intValue = characteristic.getIntValue(FORMAT_UINT16, 1);
        float floatValue = (float)(intValue * 0.005);
        Log.d("", String.format("体重 =  %.1f Kg",floatValue));
        setText2(String.format("体重 =  %.1f Kg",floatValue));
    }

    // Common
    private static String convByteToHexString(byte[] data) {
        String ret = "";
        for (byte b : data) {
            ret = ret + String.format("%02x-", b);
        }
        if (ret.length() > 0) {
            ret = ret.substring(0, ret.length() - 1);
        }
        return(ret);
    }

    String mText1Val = "";
    public void setText1(String val) {
        mText1Val = val;
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                ((TextView) findViewById(R.id.text1)).setText(mText1Val);
            }
        });
    }

    String mText2Val = "";
    public void setText2(String val) {
        mText2Val = val;
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                ((TextView) findViewById(R.id.text2)).setText(mText2Val);
            }
        });
    }

}
