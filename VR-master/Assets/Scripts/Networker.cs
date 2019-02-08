using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Text;

public class Networker
{
    private string ip;
    private static int dataBuffer;
    private  bool isListenerRunning;

    private UdpClient client;
    private IPEndPoint RemoteIpEndPoint;
    private Thread udpListenerThread;
    private static int PORT = 13000;
    private static string SENDER_IP = "10.191.148.214";

    static readonly object dataThreadLock = new object();

    public Networker() {
        string hostName = Dns.GetHostName(); // Retrive the Name of HOST  
        // Get the IP  
        ip = Dns.GetHostEntry(hostName).AddressList[1].ToString();
        UnityEngine.Debug.Log("Set Target IP address on EEG computer python script to: " + ip);
        client = new UdpClient(PORT);
        RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        isListenerRunning = false;
    }

    private void receiveFromEEGCPU() {  
        while (true) {      
            try
            {
                // Blocks until a message returns on this socket from a remote host.
                Byte[] receiveBytes = client.Receive(ref RemoteIpEndPoint);
                string returnData = Encoding.UTF8.GetString(receiveBytes);
                if (RemoteIpEndPoint.Address.ToString() == SENDER_IP)
                {
                    // Message from EEG computer
                    lock (dataThreadLock) {
                        dataBuffer = Convert.ToInt32(returnData);
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e.ToString());
                }
               
        }
    }

    public void startListening() {
        if (!isListenerRunning) {
            isListenerRunning = true;
            udpListenerThread = new Thread(new ThreadStart(receiveFromEEGCPU));
            udpListenerThread.IsBackground = true;
            udpListenerThread.Start();
        }
    }

    public void stopListening() {
        if (isListenerRunning)
        {
            isListenerRunning = false;
        }
    }

    public int getData()
    {
        lock (dataThreadLock) {
            return dataBuffer;
        }
    }
}
