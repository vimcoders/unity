using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;
using Google.Protobuf;
using Pb;
using System.Reflection;
using System.Threading;

public class Method
{
    public ushort Id { get; set;}
    public string MethodName { get; set;}
    public string RequestName { get; set;}
    public string ResponseName { get; set;}
    public IMessage Request { get; set;}
    public IMessage Response { get; set;}
}

public class TcpClient : MonoBehaviour
{
    Socket s;
    IPAddress ip;
    IPEndPoint ipEnd;
    Method[] Methods;
    byte[] readBuffer;
    byte[] writeBuffer;
    int count;
    Thread t;

    void Send(IMessage msg)
    {
        byte[] payload = msg.ToByteArray();
        ushort length = Convert.ToUInt16(4 + payload.Length);
        Method method = Methods.Where(t => t.Request.Descriptor.Name == msg.Descriptor.Name).First();
        writeBuffer[0] = Convert.ToByte(length >> 8);
        writeBuffer[1] = Convert.ToByte(length);
        writeBuffer[2] = Convert.ToByte(method.Id >> 8);
        writeBuffer[3] = Convert.ToByte(method.Id);
        Buffer.BlockCopy(payload, 0, writeBuffer, 4, payload.Length);
        s.Send(writeBuffer, length, SocketFlags.None);
    }

    void Close()
    {
        s?.Shutdown(SocketShutdown.Both);
        s?.Close();
    }

    // Use this for initialization
    void Start()
    {
        count = 0;
        readBuffer = new byte[1024];
        writeBuffer = new byte[1024];
        Methods = new Method[]{new() {
            Id = 0,
            MethodName = "Ping",
            RequestName = "PingRequest",
            ResponseName = "PingResponse",
        }};
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == "Pb");
        for (int i = 0; i < Methods.Length; i++)
        {
            var request = types.Where(t => t.Name == Methods[i].RequestName).First();
            var response = types.Where(t => t.Name == Methods[i].ResponseName).First();
            Methods[i].Request = (IMessage)Activator.CreateInstance(request);
            Methods[i].Response = (IMessage)Activator.CreateInstance(response);
        }
        ip = IPAddress.Parse("127.0.0.1");
        ipEnd = new IPEndPoint(ip, 9600);
        s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        s.Connect(ipEnd);
        t = new Thread(new ThreadStart(StartReceive));
        t.Start();
    }

    /// <summary>
    /// StartReceive
    /// </summary>
    void StartReceive()
    {
        while (true)
        {
           Receive();
        }
    }


    /// <summary>
    /// Receive
    /// </summary>
    void Receive()
    {
        int len = s.Receive(readBuffer, count, readBuffer.Length-count, SocketFlags.None);
        if (len == 0)
        {
            return;
        }
        count += len;
        while (count >= 4) {
            ushort length = Convert.ToUInt16((readBuffer[0]<<8)|readBuffer[1]);;
            if (count < length) 
            {
                return;
            }
            ushort cmd = Convert.ToUInt16((readBuffer[2]<<8)|readBuffer[3]);
            if (cmd >= Methods.Length) {
                return;
            }
            IMessage iMessage = Methods[cmd].Response.Descriptor.Parser.ParseFrom(readBuffer, 4, length-4);
            Buffer.BlockCopy(readBuffer, length, readBuffer, 0, length);
            count -= length;
        }
    }

    // Update is called once per frame
    void Update()
    {
        var request = new PingRequest(){Message = ByteString.CopyFromUtf8("Hello")};
        Send(request);
    }

    void OnApplicationQuit()
    {
        Close();
    }
}