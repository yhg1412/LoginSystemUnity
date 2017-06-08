using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Security.Cryptography;

public class ChatClient : MonoBehaviour
{
    public class UserItem
    {
        public IPAddress ip;
        public ushort port;
        public string userName;
        public GameObject userButton;
        public Conversation convers;
        public ChatClient chatClient;
        public bool chatStarted;

        public UserItem(IPAddress _ip, ushort _port, string _userName, ChatClient _chatClient)
        {
            ip = _ip;
            port = _port;
            userName = _userName;
            chatClient = _chatClient;
            userButton = null;
            chatStarted = false;
        }

        public void startChat()
        {
            if (!chatStarted)
            {
                chatStarted = true;
                chatClient.BeginConversation(this);
            }
            else
            {
                chatClient.displayController.disPlayDialog(convers);
            }
        }

    }

    public class Conversation
    {
        public TcpClient tcpClient;
        public NetworkStream stream;
        public UserItem userItem;
        public string remoteName;
        public ushort remotePort;
        public bool UICreated;
        public ChatClient chatClient;
        public DialogControl dialog;
        public toggleControl toggle;

        public Conversation(TcpClient c, ChatClient _chatClient)
        {
            UICreated = false;
            tcpClient = c;
            stream = c.GetStream();
            userItem = null;
            chatClient = _chatClient;
            dialog = null;
            toggle = null;
        }

        public Conversation(TcpClient c, string _remoteName, ushort _remotePort, ChatClient _chatClient)
        {
            UICreated = false;
            tcpClient = c;
            remoteName = _remoteName;
            remotePort = _remotePort;
            userItem = null;
            chatClient = _chatClient;
            dialog = null;
            toggle = null;
        }

        public void GetConfirmMsg()
        {
            byte[] buffer = new byte[20];

            string s = "I accept your connection";
            stream.Write(Encoding.ASCII.GetBytes(s), 0, s.Length);
            
            //This call blocks;
            if(stream.DataAvailable)
            {
                int bytes = stream.Read(buffer, 0, buffer.Length);
                //remotePort = System.BitConverter.ToUInt16(buffer, 0);
                remoteName = Encoding.Default.GetString(buffer, 0, 20);
                
                Debug.Log("ConfirmMsg from : " + " RemoteEP:" + tcpClient.Client.RemoteEndPoint.ToString() 
                     + " ListenPort: " + remotePort  +" says " + remoteName);
            }
        }

        public void SetupUI()
        {
            UICreated = true;
            if(userItem == null)
            {
                try
                {
                    userItem = chatClient.users[remoteName];
                    userItem.convers = this;
                    userItem.chatStarted = true;
                }
                catch(Exception e)
                {
                    Debug.Log("can't find " + remoteName + " in user list " + e.ToString());
                    return;
                }
            }
            chatClient.displayController.createDialog(this);
        }

        void SendCallbcak(IAsyncResult result)
        {
            try
            {
                stream.EndWrite(result);
            }
            catch(Exception e)
            {
                Debug.Log("EndWrite Failed : " + e.ToString());
            }
            
            Debug.Log("End Write");
        }

        public void Send(string s)
        {
            byte[] buffer = Encoding.Default.GetBytes(s);
            try
            {
                stream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(SendCallbcak), stream);
            }
            catch(Exception e)
            {
                Debug.Log("BeginWrite Failed : " + e.ToString());
                return;
            }
            
            Debug.Log("Send "   + s + " at " + System.DateTime.Now + " : " + remoteName   );
        }

        public void Read()
        {
            byte[] buffer = new byte[256];
            int bytes = stream.Read(buffer, 0, buffer.Length);
            string s = Encoding.Default.GetString(buffer);
            dialog.readMessage(s);
            Debug.Log("Read from Remote user : " +  s + " at " + System.DateTime.Now + " says: " +remoteName);
        }
    }

    public string userName;
    public string remoteUdpAddress;
    public ushort remoteUdpPort;
    public ushort localTcpListenPort;
    private string password;

    private string pubKey = "<RSAKeyValue><Modulus>pDG3M3tqtkzx17ayJaLEa7LM1lBIEoDI6q23BZlXqDc2ckU5QStq7VC/LhdBrUv8pNMHgstKcezn4f1hWBMEUq/K6HoTUuG3BD/yLErM8vIVxSMp5kLiFbgvw18dr0m17Z43UTIjMrBqss8jP/Eqz6yvfcxWsb6jhBn8WgLYxD0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
    public TcpClient myTcpClient;
    public NetworkStream netStream;
    CryptoStream CryptStreamRead;
    CryptoStream CryptStreamWrite;
    StreamReader SReader;
    StreamWriter SWriter;
    RijndaelManaged RM;
    byte[] myKey;
    byte[] myIV;

    //Awaiting server to send back connection Confirmation
    private bool phase1;
    
    private bool phase2;

    public InputField Uname;
    public InputField Pwd;
    public InputField ServerIP;
    public InputField ServerPort;
    public InputField ListenPort;

    public DisplayController displayController;

    public List<Conversation> conversations;
    public Dictionary<string, UserItem> users;
    UdpClient udpClient;
    TcpListener tcpListener;
    private bool shouldReceive;
    private bool udpMessageReceived;
    private bool shouldTcpListen;

    byte[] udpReceiveBuffer;

    void PrintError(string e)
    {
        Debug.Log(e);
    }

    void UdpReceiveCallback(IAsyncResult result)
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        udpReceiveBuffer = udpClient.EndReceive(result, ref ep);

        udpMessageReceived = true;
        shouldReceive = true;
    }

    void LoginRequestCallback(IAsyncResult result)
    {
        try
        {
            udpClient.EndSend(result);
        }
        catch(Exception e)
        {
            Debug.Log("udpClient.EndSend failed: " + e.ToString());
        }
        shouldReceive = true;
    }

    int Init()
    {
        userName = Uname.text;
        password = Pwd.text;
        remoteUdpAddress = ServerIP.text;
        remoteUdpPort = ushort.Parse(ServerPort.text);
        localTcpListenPort = ushort.Parse(ListenPort.text);
        
        try
        {
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), localTcpListenPort);
            tcpListener.Start();
        }
        catch(Exception e)
        {
            Debug.Log("Can't start local listening: " + e.ToString());
            return -1;
        }

        shouldTcpListen = true;

        return 0;
    }

    public void Login()
    {
        if (Init() != 0)
        {
            Debug.Log("Init error");
            return;
        }

        shouldReceive = false;
        AsyncCallback callback = new AsyncCallback(LoginRequestCallback);
        try
        {
//            udpClient.Connect(remoteUdpAddress, remoteUdpPort);
        }
        catch(ObjectDisposedException e)
        {
            PrintError("UdpClient is closed, please reboot system and try again: " + e.ToString());
            return;
        }
        catch(SocketException e)
        {
            PrintError("Error Socket: " + e.ToString());
            return;
        }

        Debug.Log("Begin make message");

        byte[] msg = new byte[24];
        msg[0] = 0x01;
        byte[] port = new byte[2];
        byte[] usr = Encoding.ASCII.GetBytes(userName);
        port = System.BitConverter.GetBytes(localTcpListenPort);
        port.CopyTo(msg, 1);
        try
        {
            usr.CopyTo(msg, 3);
            msg[usr.Length + 3] = 0x00;
        }
        catch (ArgumentOutOfRangeException e)
        {
            Debug.Log("user name is too long, please change it and try again." + e.ToString());
            return;
        }

        Debug.Log("Begin UdpSend");

        try
        {
            IPAddress ipAddress = IPAddress.Any;
            foreach (var add in Dns.GetHostEntry(remoteUdpAddress).AddressList)
            {
                if (add.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = add;
                    break;
                }
            }
            IPEndPoint ipRemoteEndPoint = new IPEndPoint(ipAddress, remoteUdpPort);
            udpClient.BeginSend(msg, 23, ipRemoteEndPoint, callback, udpClient);

            //            udpClient.BeginSend(msg, 23, "localhost", 7777, callback, udpClient);
            //            udpClient.BeginSend(msg, 23, callback, udpClient);
        }
        catch (Exception e)
        {
            Debug.Log("udpClient.BeginSend failed." + e.ToString());
            return;
        }
        Debug.Log("Send UdpServer a login request: " + Encoding.ASCII.GetString(msg));
    }

    public void SignUp()
    {

    }

    void QuitRequsetCallback(IAsyncResult result)
    {
        udpClient.EndSend(result);
        Debug.Log("Send UdpServer a quit request");
    }

    public void Quit()
    {
        shouldReceive = false;
        shouldTcpListen = false;
        byte[] msg = new byte[1];
        msg[0] = 0x03;
        IPAddress ipAddress = IPAddress.Any;
        foreach (var add in Dns.GetHostEntry(remoteUdpAddress).AddressList)
        {
            if (add.AddressFamily == AddressFamily.InterNetwork)
            {
                ipAddress = add;
                break;
            }
        }
        IPEndPoint ipRemoteEndPoint = new IPEndPoint(ipAddress, remoteUdpPort);
        udpClient.BeginSend(msg, 1, ipRemoteEndPoint, new AsyncCallback(QuitRequsetCallback), udpClient);
    } 

    void ProcessUdpMessage()
    {
        udpMessageReceived = false;

        //Perfect solution for thread sychronization
        byte[] udpBuffer = udpReceiveBuffer;
        Debug.Log("udpReceived a msg, Length is : " + udpBuffer.Length);

        if (udpBuffer[0] == 0x02)
        {
            //Login Confirm;
            int userNum = System.BitConverter.ToInt32(udpBuffer, 1);
            for(int i = 0; i < userNum; i++)
            {
                byte[] iptemp = new byte[4];
                for(int j = 0; j < 4; j++)
                {
                    //Little endian
                    //iptemp[j] = udpBuffer[j + 5 + 26 * i];

                    //Big endian
                    iptemp[3 - j] = udpBuffer[j + 5 + 26 * i];
                }
                IPAddress ip = new IPAddress(iptemp);
                ushort tcpPort = System.BitConverter.ToUInt16(udpBuffer, 5 + 26 * i + 4);
                string name = Encoding.Default.GetString(udpBuffer, 5 + 26 * i + 6, 20);
                users.Add(name.Trim(), new UserItem(ip, tcpPort, name, this));
                Debug.Log("User " + i + " : " + ip.ToString() + " tcpPort: " + tcpPort + " userName: " + name);
            }
            try
            {
                displayController.LoadChatRoom();
            }
            catch(Exception e)
            {
                Debug.Log("LoadChatRoom failed : " + e.ToString());
            }
        }
        else if(udpBuffer[0] == 0x05)
        {
            int userNum = System.BitConverter.ToInt32(udpBuffer, 1);
            if (userNum < users.Count)
            {
                foreach(var u in users)
                {
                    bool remove = true;
                    for (int i = 0; i < userNum; i++)
                    {
                        string name = Encoding.Default.GetString(udpBuffer, 5 + 26 * i + 6, 20);
                        if (name == u.Key)
                        {
                            remove = false;
                            break;
                        }
                    }
                    if(remove)
                    {
                        users.Remove(u.Key);
                        displayController.UndisplayUser(u.Value);
                        Debug.Log("Remove user " + " : " + u.Value.ip.ToString() + " tcpPort: " + u.Value.port + " userName: " + u.Key);
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < userNum; i++)
                {
                    byte[] iptemp = new byte[4];
                    for (int j = 0; j < 4; j++)
                    {
                        //Little endian
                        //iptemp[j] = udpBuffer[j + 5 + 26 * i];

                        //Big endian
                        iptemp[3 - j] = udpBuffer[j + 5 + 26 * i];
                    }
                    IPAddress ip = new IPAddress(iptemp);
                    ushort tcpPort = System.BitConverter.ToUInt16(udpBuffer, 5 + 26 * i + 4);
                    string name = Encoding.Default.GetString(udpBuffer, 5 + 26 * i + 6, 20);

                    if(!users.ContainsKey(name))
                    {
                        UserItem u = new UserItem(ip, tcpPort, name, this);
                        users.Add(name.Trim(), u);
                        displayController.DisplayUser(u);
                        Debug.Log("Add user " + " : " + ip.ToString() + " tcpPort: " + tcpPort + " userName: " + name);
                        break;
                    }
                }
            }
        }
    }

    void AcceptTcpCallback(IAsyncResult result)
    {
        TcpClient c = tcpListener.EndAcceptTcpClient(result);
        Conversation conversation = new Conversation(c, this);

        conversation.GetConfirmMsg();
        lock(conversations)
        {
            conversations.Add(conversation);
        }
        shouldTcpListen = true;
    }

    void BeginConversationCallback(IAsyncResult result)
    {
        Conversation conversation = (Conversation)result.AsyncState; 
        conversation.tcpClient.EndConnect(result);
        byte[] buffer = new byte[22];

        //To cope with the new request, we don't need the following line
        //System.BitConverter.GetBytes(localTcpListenPort).CopyTo(buffer, 0);


        Encoding.Default.GetBytes(userName).CopyTo(buffer, 0);
        conversation.stream = conversation.tcpClient.GetStream();

        //The new version of this homework doesn't require this initial request which would cause an error in the client benchmark, which is provided by TA.
        conversation.stream.Write(buffer, 0, 20);

        lock (conversations)
        {
            conversations.Add(conversation);
        }
    }

    void BeginConversation(UserItem peer)
    {
        TcpClient c = new TcpClient();
        Conversation conversation = new Conversation(c, peer.userName, peer.port, this);
        conversation.userItem = peer;
        conversation.userItem.convers = conversation;
        c.BeginConnect(peer.ip, peer.port, new AsyncCallback(BeginConversationCallback), conversation);
    }

    // Use this for initialization
    void Start ()
    {
        conversations = new List<Conversation>();
        users = new Dictionary<string, UserItem>();
        udpClient = new UdpClient();

        Uname.text = "YuHugang";
        ServerIP.text = "localhost";
        ServerPort.text = "7777";
        ListenPort.text = "11000";

        shouldTcpListen = false;
        shouldReceive = false;
        udpMessageReceived = false;
        phase1 = false;
        phase2 = false;
        receiveBuffer = new byte[512];

        receiveCount = 0;
	}

    public static byte[] SymetricEncrypt(byte[] data, int dataLength, byte[] Key, byte[] IV)
    {
        RijndaelManaged RMCrypto = new RijndaelManaged();
        MemoryStream memoryStream = new MemoryStream();
        CryptoStream cryptoStream = new CryptoStream(memoryStream, RMCrypto.CreateEncryptor(Key, IV), CryptoStreamMode.Write);
        cryptoStream.Write(data, 0, dataLength);
        cryptoStream.FlushFinalBlock();
        return memoryStream.ToArray();
    }

    public static byte[] SymetricDecrypt(byte[] data, int dataLength, byte[] Key, byte[] IV)
    {
        RijndaelManaged RMCrypto = new RijndaelManaged();
        MemoryStream memoryStream = new MemoryStream();
        CryptoStream cryptoStream = new CryptoStream(memoryStream, RMCrypto.CreateDecryptor(Key, IV), CryptoStreamMode.Write);
        cryptoStream.Write(data, 0, dataLength);
        cryptoStream.FlushFinalBlock();
        return memoryStream.ToArray();
    }

    void ConnectServer()
    {
        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(pubKey);

        RM = new RijndaelManaged();
        myKey = RM.Key;
        myIV = RM.IV;
        byte[] EncryptedSymmetricKey;
        byte[] EncryptedSymmetricIV;
        EncryptedSymmetricKey = rsa.Encrypt(myKey, false);
        EncryptedSymmetricIV = rsa.Encrypt(myIV, false);
        myTcpClient = new TcpClient("localhost", 11000);
        netStream = myTcpClient.GetStream();

        byte[] prefix = System.BitConverter.GetBytes((int)19950114);
        byte[] keyLength = System.BitConverter.GetBytes(myKey.Length);
        byte[] IVLength = System.BitConverter.GetBytes(myIV.Length);
        byte[] EnKeyLength = System.BitConverter.GetBytes(EncryptedSymmetricKey.Length);
        byte[] EnIVLength = System.BitConverter.GetBytes(EncryptedSymmetricIV.Length);
        byte[] message = new byte[1024];
        prefix.CopyTo(message, 0);
        keyLength.CopyTo(message, 4);
        IVLength.CopyTo(message, 8);
        EnKeyLength.CopyTo(message, 12);
        EnIVLength.CopyTo(message, 16);
        EncryptedSymmetricKey.CopyTo(message, 20);
        EncryptedSymmetricIV.CopyTo(message, 20 + EncryptedSymmetricKey.Length);
        Debug.Log("KL " + myKey.Length + ", IVL " + myIV.Length + ", enKL " + EncryptedSymmetricKey.Length + ", enIVL " + EncryptedSymmetricIV.Length);
        netStream.Write(message, 0, message.Length);
        Debug.Log("Assymetric key sent");
        phase1 = true;
    }

    void CreateCryptoStream()
    {
        //byte[] Key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        //byte[] IV = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };

        //CryptStreamRead = new CryptoStream(
        //    netStream,
        //    RM.CreateDecryptor(Key, IV),
        //    CryptoStreamMode.Read);
        //CryptStreamWrite = new CryptoStream(netStream,
        //    RM.CreateEncryptor(Key, IV),
        //    CryptoStreamMode.Write);

        CryptStreamRead = new CryptoStream(
            netStream,
            RM.CreateDecryptor(RM.Key, RM.IV),
            CryptoStreamMode.Read);
        CryptStreamWrite = new CryptoStream(netStream,
            RM.CreateEncryptor(RM.Key, RM.IV),
            CryptoStreamMode.Write);


        SReader = new StreamReader(CryptStreamRead);
        SWriter = new StreamWriter(CryptStreamWrite);

        phase1 = true;
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(20, 20, 80, 20), "Connect"))
        {
            ConnectServer();
        }
        if (GUI.Button(new Rect(20, 50, 80, 20), "CryptStream"))
        {
            CreateCryptoStream();
        }
    }

    //void OnGUI()
    //{
    //    if(GUI.Button(new Rect(20, 20, 80, 20), "Login"))
    //    {
    //        Login();
    //    }
    //    if(GUI.Button(new Rect(20, 50, 80, 20), "Conversation"))
    //    {
    //        foreach(var u in users)
    //        {
    //            if (u.Value.port != localTcpListenPort)
    //            {
    //                Debug.Log("This is " + userName + " trying to begin a conversation with " +
    //                    u.Value.ip.ToString() + " : " + u.Value.port + " : " + u.Value.userName);
    //                BeginConversation(u.Value);
    //                break;
    //            }
    //        }
    //    }
    //    if(GUI.Button(new Rect(20, 80, 80, 20), "Say something"))
    //    {
    //        foreach(var c in conversations)
    //        {
    //            c.Send("Hello this is Hugang speaking!" + ccount++.ToString());
    //        }
    //    }
    //    if(GUI.Button(new Rect(20, 110, 80, 20), "Check Users"))
    //    {
    //        int i = 0;
    //        foreach(var u in users)
    //        {
    //            Debug.Log("user : " + i + " user.Key: " + u.Key + 
    //                "userEP : " + u.Value.ip.ToString() + " : " + u.Value.port);
    //            i++;
    //        }
    //    }
    //    if(GUI.Button(new Rect(20, 140, 80, 20), "Quit"))
    //    {
    //        Quit();
    //    }
    //}

    private int receiveCount;
    byte[] receiveBuffer;

    void sReadCallback(IAsyncResult result)
    {
        int numRead = CryptStreamRead.EndRead(result);
        string s = Encoding.ASCII.GetString(receiveBuffer);
        Debug.Log("Read " + numRead + " bytes of security data: " + s);
        phase2 = true;
    }

    // Update is called once per frame
    void Update ()
    {
        if(phase1)
        {
            Debug.Log("Awaiting Server to send back connection confirmation");
            if (netStream.DataAvailable)
            {
                Debug.Log("netStream Data Available");
                int numRead = netStream.Read(receiveBuffer, 0, receiveBuffer.Length);
                byte[] decryptedMessage = SymetricDecrypt(receiveBuffer, numRead, myKey, myIV);
                string s = Encoding.ASCII.GetString(decryptedMessage);
                Debug.Log("Read " + numRead + " bytes of security data: " + s);
                if (string.Equals(s, "Connection Permitted") == true)
                {
                    Debug.Log("Connection Success!");
                    phase1 = false;
                    phase2 = true;
                }
                else
                {
                    Debug.Log("Connection Denied: " + s);
                    phase1 = false;
                }
            }
        }
        if(false)
        {
            phase2 = false;
            CryptStreamRead.BeginRead(receiveBuffer, 0, receiveBuffer.Length, new AsyncCallback(sReadCallback), CryptStreamRead);
        }


  //      Debug.Log("shouldTcpReceive" + shouldTcpListen);
  //      Debug.Log("Conversations.count : " + conversations.Count);
  //	  if(shouldReceive)
  //      {
  //          udpClient.BeginReceive(new AsyncCallback(UdpReceiveCallback), udpClient);
  //          shouldReceive = false;
  //      }
  //      if(udpMessageReceived)
  //      {
  //          ProcessUdpMessage();
  //      }
  //      if(shouldTcpListen)
  //      {
  //          Debug.Log("TcpLisnter BeginAccept times: " + ++receiveCount);
  //          tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpCallback), tcpListener);
  //          shouldTcpListen = false;
  //      }
        
  //      foreach(var c in conversations)
  //      {
  //          if(!c.UICreated)
  //          {
  //              c.SetupUI();
  //          }
  //          if(c.stream.DataAvailable)
  //          {
  //              c.Read();
  //          }
  //      }
	}
}
