using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
public class ArduinoTest : MonoBehaviour
{
    public ControllerInfo inputs = new ControllerInfo();
    private List<int> _dataList = new List<int>();
    private SerialPort _port;
    private ConcurrentQueue<string> _lineQueue = new ConcurrentQueue<string>();
    
    private Thread _serialThread;
    private bool _isThreadRunning = false;

    void Start()
    {
        OpenPort();
    }

    public void OpenPort()
    {
        var ports = SerialPort.GetPortNames();
        if (ports.Length == 0) return;
        string targetPort = System.Array.Find(ports, name => name.Contains("usb") || name.Contains("modem")) ?? ports[^1];
    
        
        _port = new SerialPort(targetPort, 9600);
        
        // Critical for thread cleanup later so it doesn't hang the editor
        _port.ReadTimeout = 200; 
        _port.WriteTimeout = 200;
        
        try
        {
            _port.Open();   
            _isThreadRunning = true;
            _serialThread = new Thread(ReadSerialLoop);
            _serialThread.Start();
            Debug.Log($"Successfully opened {targetPort}");

            // Start your background thread here...
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to open port {targetPort}: {e.Message}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        while (_lineQueue.TryDequeue(out string freshValue))
        {
            parseString(freshValue);
            var dataString = "";
            foreach (var data in _dataList)
            {
                dataString += data + ";";
            }
            TranslateToUnityInputs();
        }
        
    }

    void ReadSerialLoop()
    {
        while (_isThreadRunning && _port != null && _port.IsOpen)
        {
            try
            {
                string value = _port.ReadLine();
                if (!string.IsNullOrEmpty(value))
                {
                    // Safely pass the data string over to the queue
                    _lineQueue.Enqueue(value);
                }
            }
            catch (System.TimeoutException e)
            {
                
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Serial read error: {e.Message}");
            }
        }
    }

    private void parseString(string inputs)
    {
        _dataList = new List<int>();
        var data = inputs;
        data = data.Replace(" ", "");
        data = data.Replace("\n", "");
        data = data.Replace("\r", "");
        if (data.IndexOf("#")> data.IndexOf("@"))
        {
            data = data.Substring(data.IndexOf("@") + 1, data.IndexOf("#")-1);
            var subs = data.Split(';');
            foreach (var sub in subs)
            {
                if (sub != "" && sub !=" ")
                {
                    var temp = int.Parse(sub);
                    _dataList.Add(temp);
                }
            }
        }
    }

    private void TranslateToUnityInputs()
    {
        inputs.stickL = new Vector2((float)ControllerToUnityValues(0), (float)ControllerToUnityValues(1));
        inputs.stickR = new Vector2((float)ControllerToUnityValues(2), (float)ControllerToUnityValues(3));
        inputs.b1 = _dataList[4] == 0;
        inputs.b2 = _dataList[5] == 0;
        inputs.l = _dataList[6] == 0;
        inputs.r = _dataList[7] == 0;
    }

    private double ControllerToUnityValues(int index)
    {
        var temp= (_dataList[index] - 512f)/(512f);
        if (Mathf.Abs((float)temp) > 0.1f)
        {
            return temp;
        }
        else
        {
            return 0;
        }
    }

    public void ClosePort()
    {
        _port.Close();
    }
    
}

public class ControllerInfo
{
    public Vector2 stickL;
    public Vector2 stickR;
    public bool b1;
    public bool b2;
    public bool l;
    public bool r;
}
