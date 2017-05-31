using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System;
using System.Text;

public class TestTcp : MonoBehaviour {

    TcpListener listener;
    TcpClient clientSend;
    TcpClient clientReceive;

    bool shouldTcpListen;
    bool shouldRead1;
    bool shouldRead2;

    public ushort port;

	// Use this for initialization
	void Start () {
        listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
        listener.Start();
        clientSend = new TcpClient();
        shouldTcpListen = true;
        shouldRead1 = false;
        shouldRead2 = false;
    }


    void BeginConversationCallback(IAsyncResult result)
    {
        TcpClient c = (TcpClient)result.AsyncState;
        c.EndConnect(result);
        byte[] buffer = new byte[22];
        string s = "hello this is XX";
        c.GetStream().Write(Encoding.ASCII.GetBytes(s), 0, s.Length);
        shouldRead1 = true;
    }
    string report = "Enter Port";
    void OnGUI()
    {
        
        report = GUI.TextField(new Rect(20, 50, 80, 20), report);
        if(GUI.Button(new Rect(20, 20, 80, 20), "Connect"))
        {
            ushort remotePort = ushort.Parse(report);
            clientSend.BeginConnect(IPAddress.Parse("127.0.0.1"), remotePort, new AsyncCallback(BeginConversationCallback), clientSend);
        }
    }

    void tcpListenCallback(IAsyncResult result)
    {
        clientReceive = listener.EndAcceptTcpClient(result);
        string s = "I Accepted your request";
        clientReceive.GetStream().Write(Encoding.ASCII.GetBytes(s), 0, s.Length);
        shouldTcpListen = true;
        shouldRead2 = true;
    }

	// Update is called once per frame
	void Update () {
		if(shouldTcpListen)
        {
            shouldTcpListen = false;
            listener.BeginAcceptTcpClient(new AsyncCallback(tcpListenCallback), listener);
        }
        if (shouldRead1)
        {
            if (clientSend.GetStream().DataAvailable)
            {
                byte[] buffer = new byte[256];
                int bytes = clientSend.GetStream().Read(buffer, 0, buffer.Length);
                Debug.Log("1read " + bytes + " bytes and content is : " + Encoding.Default.GetString(buffer));
            }
        }
        if(shouldRead2)
        {
            if(clientReceive.GetStream().DataAvailable)
            {
                byte[] buffer = new byte[256];
                int bytes = clientReceive.GetStream().Read(buffer, 0, buffer.Length);
                Debug.Log("2read " + bytes + " bytes and content is : " + Encoding.Default.GetString(buffer));
            }
        }
	}
}
