package com.example.WeTac;

import androidx.annotation.RequiresApi;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.content.ContextCompat;

import android.Manifest;
import android.content.pm.PackageManager;
import android.graphics.drawable.GradientDrawable;
import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.SeekBar;
import android.widget.TextView;
import android.widget.Toast;

import com.github.mikephil.charting.charts.LineChart;
import com.github.mikephil.charting.components.XAxis;
import com.github.mikephil.charting.components.YAxis;
import com.github.mikephil.charting.data.Entry;
import com.github.mikephil.charting.data.LineData;
import com.github.mikephil.charting.data.LineDataSet;
import com.vise.baseble.ViseBle;
import com.vise.baseble.callback.IBleCallback;
import com.vise.baseble.callback.IConnectCallback;
import com.vise.baseble.common.PropertyType;
import com.vise.baseble.core.BluetoothGattChannel;
import com.vise.baseble.core.DeviceMirror;
import com.vise.baseble.core.DeviceMirrorPool;
import com.vise.baseble.exception.BleException;
import com.vise.baseble.model.BluetoothLeDevice;
import com.vise.baseble.utils.HexUtil;
import com.vise.log.ViseLog;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.UUID;

public class MainActivity extends AppCompatActivity {

    /**
     * variables
     */
    private Button btSearch;
    public static LineChart lineChart;
    private List<Entry> list = new ArrayList<>();
    private LineDataSet lineDataSet;
    private LineData lineData;
    private Button buttons[] = new Button[32];
    List<String> freq = Arrays.asList("10","25","30","35","40","45","50","60","100","150","200");
    List<String> duty = Arrays.asList("1","2","3","4","5","6","7","8","9","10","11");
    List<String> intensity2 = Arrays.asList("1","2","3","4","5","6","7","8","9","10","11");


    public static final String BT_UUID = "000018a-0000-1000-8000-00805f9b34fb";//uuid
    public static final String serviceUUID = "0000fff0-0000-1000-8000-00805f9b34fb";
    public static final String characteristicUUID = "0000fff5-0000-1000-8000-00805f9b34fb";
    public static final String descriptorUUID = "00002902-0000-1000-8000-00805f9b34fb";
    public static final String char1UUID = "0000fff1-0000-1000-8000-00805f9b34fb";
    public static final String char2UUID = "0000fff2-0000-1000-8000-00805f9b34fb";
    public static final String char3UUID = "0000fff3-0000-1000-8000-00805f9b34fb";
    public static final String char4UUID = "0000fff4-0000-1000-8000-00805f9b34fb";
    public static final String MAC = "d8:71:4d:0b:dc:14";
    private DeviceMirrorPool mDeviceMirrorPool;
    private DeviceMirror mdevice;

    private boolean flag = false;
    private int j=0;
    private int smooth[] = new int[10];
    private boolean flag_b[] = new boolean[32];
    private String buf;

    SeekBar frequency;
    SeekBar dutyCycle;
    SeekBar intensity;
    TextView freq_t;
    TextView duty_t;
    TextView intensity_t;

    /**
     * BLE callback
     */
    private IConnectCallback periodScanCallback = new IConnectCallback() {
        @Override
        public void onConnectSuccess(DeviceMirror deviceMirror) {
            mdevice = deviceMirror;
//            btSearch.setText("Disconnect");
            flag = false;
            btSearch.setBackgroundTintList(MainActivity.this.getResources().getColorStateList(R.color.light_green));
//            Toast.makeText(MainActivity.this, "BLE Connected!", Toast.LENGTH_SHORT).show();
            /**
             * Register Notification
             */
            BluetoothGattChannel bluetoothGattChannel = new BluetoothGattChannel.Builder()
                    .setBluetoothGatt(deviceMirror.getBluetoothGatt())
                    .setPropertyType(PropertyType.PROPERTY_NOTIFY)
                    .setServiceUUID(UUID.fromString(serviceUUID))
                    .setCharacteristicUUID(UUID.fromString(characteristicUUID))
                    .setDescriptorUUID(UUID.fromString(descriptorUUID))
                    .builder();
            deviceMirror.bindChannel(new IBleCallback() {
                @Override
                public void onSuccess(byte[] data, BluetoothGattChannel bluetoothGattChannel, BluetoothLeDevice bluetoothLeDevice) {
                    deviceMirror.setNotifyListener(bluetoothGattChannel.getGattInfoKey(), new IBleCallback() {
                        @Override
                        public void onSuccess(byte[] data, BluetoothGattChannel bluetoothGattChannel, BluetoothLeDevice bluetoothLeDevice) {
                            ViseLog.i("notify success:" + HexUtil.encodeHexStr(data));
                            try {
                                updateLineChart(HexUtil.encodeHexStr(data)); //process received data
                            } catch (InterruptedException e) {
                                e.printStackTrace();
                            }
                        }

                        @Override
                        public void onFailure(BleException exception) {

                        }
                    });
                }

                @Override
                public void onFailure(BleException exception) {

                }
            }, bluetoothGattChannel);
            deviceMirror.registerNotify(false);

        }

        @Override
        public void onConnectFailure(BleException exception) {
//            btSearch.setText("Search BLE");
            flag = false;
            btSearch.setBackgroundTintList(MainActivity.this.getResources().getColorStateList(R.color.red_50));
            Toast.makeText(MainActivity.this, "Connect failure!", Toast.LENGTH_SHORT).show();
            flag = false;
        }

        @Override
        public void onDisconnect(boolean isActive) {
//            btSearch.setText("Search BLE");
            flag = false;
            btSearch.setBackgroundTintList(MainActivity.this.getResources().getColorStateList(R.color.red_50));
            Toast.makeText(MainActivity.this, "Disconnected!", Toast.LENGTH_SHORT).show();
            flag = false;
        }
    };

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        checkPermission();//Check permissions
        setContentView(R.layout.activity_main);

        //Bluetooth related configuration
        ViseBle.config()
                .setScanTimeout(10 * 1000)
                .setConnectTimeout(10 * 1000)
                .setOperateTimeout(5 * 1000)
                .setConnectRetryCount(3)
                .setConnectRetryInterval(1000)
                .setOperateRetryCount(3)
                .setOperateRetryInterval(1000)
                .setMaxConnectCount(1);
       //Init BLE
        ViseBle.getInstance().init(this);
        mDeviceMirrorPool = ViseBle.getInstance().getDeviceMirrorPool();
        btSearch = findViewById(R.id.search);
        lineChart = findViewById(R.id.curveChart);

        buttons[0] = (Button) findViewById(R.id.button1);
        /* button1.setBackgroundColor(Color.rgb(255,134,66)); */
        buttons[1] = (Button) findViewById(R.id.button2);
        //button2.setBackgroundColor(Color.rgb(198,13,25));
        buttons[2] = (Button) findViewById(R.id.button3);
        buttons[3] = (Button) findViewById(R.id.button4);
        buttons[4] = (Button) findViewById(R.id.button5);
        buttons[5] = (Button) findViewById(R.id.button6);
        buttons[6] = (Button) findViewById(R.id.button7);
        buttons[7] = (Button) findViewById(R.id.button8);
        buttons[8] = (Button) findViewById(R.id.button9);
        buttons[9] = (Button) findViewById(R.id.button10);
        buttons[10] = (Button) findViewById(R.id.button11);
        buttons[11] = (Button) findViewById(R.id.button12);
        buttons[12] = (Button) findViewById(R.id.button13);
        buttons[13] = (Button) findViewById(R.id.button14);
        buttons[14] = (Button) findViewById(R.id.button15);
        buttons[15] = (Button) findViewById(R.id.button16);
        buttons[16] = (Button) findViewById(R.id.button17);
        buttons[17] = (Button) findViewById(R.id.button18);
        buttons[18] = (Button) findViewById(R.id.button19);
        buttons[19] = (Button) findViewById(R.id.button20);
        buttons[20] = (Button) findViewById(R.id.button21);
        buttons[21] = (Button) findViewById(R.id.button22);
        buttons[22] = (Button) findViewById(R.id.button23);
        buttons[23] = (Button) findViewById(R.id.button24);
        buttons[24] = (Button) findViewById(R.id.button25);
        buttons[25] = (Button) findViewById(R.id.button26);
        buttons[26] = (Button) findViewById(R.id.button27);
        buttons[27] = (Button) findViewById(R.id.button28);
        buttons[28] = (Button) findViewById(R.id.button29);
        buttons[29] = (Button) findViewById(R.id.button30);
        buttons[30] = (Button) findViewById(R.id.button31);
        buttons[31] = (Button) findViewById(R.id.button32);
        frequency = (SeekBar) findViewById(R.id.FQ);
        dutyCycle = (SeekBar) findViewById(R.id.DC);
        intensity = (SeekBar) findViewById(R.id.IT);
        freq_t = (TextView) findViewById(R.id.freq);
        duty_t = (TextView) findViewById(R.id.duty);
        intensity_t = (TextView) findViewById(R.id.intensity2);

        initLineChart();// Init line chart
        initLineDataSet("Sweat", getResources().getColor(R.color.pink_700));


        String deviceName = "Tactile 32Channel";
//        btSearch.setText("Connecting");
        btSearch.setBackgroundTintList(MainActivity.this.getResources().getColorStateList(R.color.red_50));
        Toast.makeText(MainActivity.this, "Searching!", Toast.LENGTH_SHORT).show();
        ViseBle.getInstance().connectByName(deviceName, periodScanCallback);
        flag = true;

        // Button search BLE
        btSearch.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if (flag)
                {
//                    btSearch.setText("Search BLE");
                    mdevice.disconnect();
                    btSearch.setBackgroundTintList(MainActivity.this.getResources().getColorStateList(R.color.red_50));
                    flag = false;
                }
                else
                {
                    //该方式是扫到指定设备就停止扫描
                    String deviceName = "Tactile 32Channel";
//                    btSearch.setText("Connecting");
                    btSearch.setBackgroundTintList(MainActivity.this.getResources().getColorStateList(R.color.red_50));
                    Toast.makeText(MainActivity.this, "Searching!", Toast.LENGTH_SHORT).show();
                    ViseBle.getInstance().connectByName(deviceName, periodScanCallback);
                    flag = true;
                }

            }
        });

        //SeekBar for modify frequency
        frequency.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                freq_t.setText( freq.get(progress) + "Hz");
                buf = freq.get(progress);
            }

            @Override
            public void onStartTrackingTouch(SeekBar seekBar) {
            }

            @Override
            public void onStopTrackingTouch(SeekBar seekBar) {
                write(mdevice, char2UUID, (byte)(Integer.parseInt(buf)));
            }
        });

        //SeekBar for modify duty cycle
        dutyCycle.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                duty_t.setText( duty.get(progress) + "%");
                buf = duty.get(progress);
            }

            @Override
            public void onStartTrackingTouch(SeekBar seekBar) {
            }

            @Override
            public void onStopTrackingTouch(SeekBar seekBar) {
                write(mdevice, char3UUID, (byte)(Integer.parseInt(buf)));
            }
        });

        //SeekBar for modify the current
        intensity.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
                intensity_t.setText( intensity2.get(progress));
                buf = intensity2.get(progress);
            }

            @Override
            public void onStartTrackingTouch(SeekBar seekBar) {
            }

            @Override
            public void onStopTrackingTouch(SeekBar seekBar) {
                write(mdevice, char4UUID, (byte)(Integer.parseInt(buf)*20));
            }
        });
    }


    /**
     * Buttons to control different channels
     * @param v
     */
    public void onClick(View v) {
        int i;
        switch (v.getId()) {
            case R.id.button1:
                i = 0;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);// open channel 1
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_green));// change the button color
                    flag_b[i]=true;// mark this button has been pressed down
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));// close channel 1
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_green_30));
                    flag_b[i]=false;// mark this button has been released
                }
                break;
            case R.id.button2:
                i = 1;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button3:
                i = 2;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button4:
                i = 3;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button5:
                i = 4;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button6:
                i = 5;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button7:
                i = 6;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button8:
                i = 7;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button9:
                i = 8;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button10:
                i = 9;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button11:
                i = 10;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button12:
                i = 11;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button13:
                i = 12;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button14:
                i = 13;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button15:
                i = 14;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button16:
                i = 15;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button17:
                i = 16;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button18:
                i = 17;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button19:
                i = 18;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button20:
                i = 19;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_green));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_green_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button21:
                i = 20;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button22:
                i = 21;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button23:
                i = 22;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button24:
                i = 23;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_green));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_green_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button25:
                i = 24;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button26:
                i = 25;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button27:
                i = 26;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button28:
                i = 27;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_green));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_green_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button29:
                i = 28;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.pink_A200_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button30:
                i = 29;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button31:
                i = 30;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_blue_A400_30));
                    flag_b[i]=false;
                }
                break;
            case R.id.button32:
                i = 31;
                if(!flag_b[i])
                {
                    write(mdevice, char1UUID, (byte)i);
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_green));
                    flag_b[i]=true;
                }
                else
                {
                    write(mdevice, char1UUID, (byte)(0x20+i));
                    buttons[i].setBackgroundTintList(v.getResources().getColorStateList(R.color.light_green_30));
                    flag_b[i]=false;
                }
                break;
        }
    }

    /**
     * Init line chart
     */
    private void initLineChart() {
        //lineChart.setOnChartGestureListener(this);
        lineChart.getDescription().setEnabled(false);//不显示描述
//        lineChart.setBackgroundColor(Color.WHITE);
        lineChart.setTouchEnabled(true);
        lineChart.setDragEnabled(true);
        lineChart.setScaleEnabled(true);//允许缩放
        lineChart.setPinchZoom(true);
        lineChart.getLegend().setEnabled(false);
//        lineChart.getLegend().setXOffset(40);
//        lineChart.setNoDataText("No data available");
//        lineChart.setNoDataTextColor(Color.GRAY);

        //自定义适配器，适配于X轴
        XAxis xAxis = lineChart.getXAxis();
        xAxis.setPosition(XAxis.XAxisPosition.BOTTOM);
        //xAxis.setTypeface(mTfLight);
//        xAxis.setGranularity(1f);
//        xAxis.enableGridDashedLine(10f, 10f, 0f);
        xAxis.setDrawGridLines(true);
        xAxis.setDrawAxisLine(true);
//        xAxis.setTextColor(Color.BLACK);
        xAxis.setTextSize(12);
//        xAxis.setTextColor(Color.TRANSPARENT);

        YAxis leftAxis = lineChart.getAxisLeft();
        //leftAxis.setTypeface(mTfLight);
        leftAxis.setLabelCount(3, false);
        leftAxis.setPosition(YAxis.YAxisLabelPosition.OUTSIDE_CHART);
//        leftAxis.setAxisMinimum(261.94f);//(4.2f);
//        leftAxis.setAxisMaximum(261.95f);//(4.8f);
        leftAxis.setSpaceTop(15f);
//        leftAxis.setTextColor(getResources().getColor(R.color.purple_A100));
        leftAxis.setTextSize(12);
        leftAxis.setDrawGridLines(true);
        leftAxis.enableGridDashedLine(10f, 10f, 0f);
        leftAxis.setEnabled(true);

        lineChart.getAxisRight().setEnabled(false);
        lineChart.getAxisRight().setDrawAxisLine(false);
        lineChart.animateX(2000);
    }

    /**
     * Init line dataset
     * @param name
     * @param color
     */
    private void initLineDataSet(String name, int color) {

        lineDataSet = new LineDataSet(null, "ECG");
        lineDataSet.setLineWidth(2f);
        //lineDataSet_na.setCircleRadius(1.5f);
        lineDataSet.setDrawValues(false);
        lineDataSet.setDrawCircles(false);
        lineDataSet.setColor(getResources().getColor(R.color.orange_200));
        //lineDataSet_na.setCircleColor(color);
        lineDataSet.setHighLightColor(getResources().getColor(R.color.orange_200));
        //设置曲线填充
        lineDataSet.setDrawFilled(true);
        lineDataSet.setFillDrawable(new GradientDrawable(GradientDrawable.Orientation.TOP_BOTTOM,
                new int[]{getResources().getColor(R.color.amber_200_30), getResources().getColor(R.color.transparent)}));
        lineDataSet.setAxisDependency(YAxis.AxisDependency.LEFT);
        lineDataSet.setValueTextSize(10f);
        lineDataSet.setMode(LineDataSet.Mode.CUBIC_BEZIER);
//        lineDataSets.add(lineDataSet_na);

        //添加一个空的 LineData
        lineData = new LineData();
        lineChart.setData(lineData);
        lineChart.invalidate();
    }

    /**
     * Update line chart, process data received
     * @param data
     * @throws InterruptedException
     */
    public void updateLineChart(String data) throws InterruptedException {

        int d=Integer.parseInt(data, 16);
        smooth[j%10] = d;
        float sum=0;
        if(j>10)
        {
            for(int a=0;a<10;a++){
                sum += (float)(smooth[a]);
            }
            sum = sum/10;
        }

        if (lineDataSet.getEntryCount() == 0) {
            lineData.addDataSet(lineDataSet);
        }
        lineData.addEntry(new Entry((float) (j / 20.0), (float) (sum*16/4.096)), 0);
        if (j > 10000) {
            lineDataSet.removeFirst();
        }

        j++;
        lineChart.setData(lineData);
        lineData.notifyDataChanged();
        lineChart.notifyDataSetChanged();
        lineChart.setVisibleXRangeMaximum(10);
        lineChart.moveViewToX(lineData.getEntryCount() - 10);
//            lineChart.invalidate();

    }


    /**
     * Receive data callback
     */
    private IBleCallback receiveCallback = new IBleCallback() {
        @Override
        public void onSuccess(final byte[] data, BluetoothGattChannel bluetoothGattInfo, BluetoothLeDevice bluetoothLeDevice) {
            if (data == null) {
                return;
            }
        }

        @Override
        public void onFailure(BleException exception) {
            if (exception == null) {
                return;
            }
            ViseLog.i("notify fail:" + exception.getDescription());
        }
    };

    /**
     * Write command to BLE device
     * @param deviceMirror
     * @param characteristicUUID1
     * @param data
     */
    public void write(DeviceMirror deviceMirror, String characteristicUUID1, byte data) {
        deviceMirror = mdevice;
        BluetoothGattChannel bluetoothGattChannel = new BluetoothGattChannel.Builder()
                .setBluetoothGatt(deviceMirror.getBluetoothGatt())
                .setPropertyType(PropertyType.PROPERTY_WRITE)
                .setServiceUUID(UUID.fromString(serviceUUID))
                .setCharacteristicUUID(UUID.fromString(characteristicUUID1))
                .setDescriptorUUID(UUID.fromString(descriptorUUID))
                .builder();
        deviceMirror.bindChannel(new IBleCallback() {
            @Override
            public void onSuccess(byte[] data, BluetoothGattChannel bluetoothGattChannel, BluetoothLeDevice bluetoothLeDevice) {

                if (data == null) {
                    return;
                }
                DeviceMirror deviceMirror = mDeviceMirrorPool.getDeviceMirror(bluetoothLeDevice);
                if (deviceMirror != null) {
                    deviceMirror.setNotifyListener(bluetoothGattChannel.getGattInfoKey(), receiveCallback);
                }
            }

            @Override
            public void onFailure(BleException exception) {

            }
        }, bluetoothGattChannel);
        byte[] data1=new byte[1];
        data1[0]=data;
        deviceMirror.writeData(data1);
        deviceMirror.unbindChannel(bluetoothGattChannel);

    }

    /**
     * Check all the permission needed
     */
    private void checkPermission(){
        if((ContextCompat.checkSelfPermission(MainActivity.this,Manifest.permission.BLUETOOTH)!=
                PackageManager.PERMISSION_GRANTED)||(ContextCompat.checkSelfPermission(MainActivity.this,Manifest.permission.BLUETOOTH_ADMIN)!=
                PackageManager.PERMISSION_GRANTED)||(ContextCompat.checkSelfPermission(MainActivity.this,Manifest.permission.ACCESS_FINE_LOCATION)!=
                PackageManager.PERMISSION_GRANTED)){
            if(Build.VERSION.SDK_INT>=Build.VERSION_CODES.M){
                makeAnExtraRequest();
            }
        }
    }

    @RequiresApi(api = Build.VERSION_CODES.M)
    private void makeAnExtraRequest(){
        int code = 100;
        String[] permissions = {
                Manifest.permission.BLUETOOTH,
                Manifest.permission.BLUETOOTH_ADMIN,
                Manifest.permission.ACCESS_FINE_LOCATION
        };
        for(String string:permissions){
            if(this.checkSelfPermission(string)!=PackageManager.PERMISSION_GRANTED){
                this.requestPermissions(permissions,code);
                code++;
                return;
            }
        }
    }

}