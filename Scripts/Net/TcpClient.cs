using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;
using Google.Protobuf;
using Pb;
using System.Reflection;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;

public class Method
{
    public UInt16 Id { get; set;}
    public String Name { get; set;}
    public String Request { get; set;}
    public String Response { get; set;}
}

public class TcpClient : MonoBehaviour
{
    Socket s;
    IPAddress ip;
    IPEndPoint ipEnd;
    Method[] Methods;
    byte[] buffer;
 
    void Send(IMessage m)
    {
        byte[] d = Encode(m);
        this.s.Send(d, d.Length, SocketFlags.None);
    }

    /// <summary>
    /// encode
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public byte[] Encode(IMessage msg) {
        for (int i = 0; i < Methods.Length; i++) 
        {
            if (Methods[i].Request != msg.Descriptor.Name)
            {
                continue;
            }
            byte[] payload = msg.ToByteArray();
            byte[] buffer = new byte[0];
            ushort length = Convert.ToUInt16(4+payload.Length);
            buffer = buffer.Concat(BitConverter.GetBytes(length).Reverse()).ToArray();
            buffer = buffer.Concat(BitConverter.GetBytes(Methods[i].Id).Reverse()).ToArray();
            return buffer.Concat(payload).ToArray();
        }
        return null;
    }

    /// <summary>
    /// Decode
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public IMessage Decode(byte[] b) {
        ushort cmd = BitConverter.ToUInt16(b.Skip(2).Take(2).ToArray());
        if (cmd >= Methods.Length) {
            return null;
        }
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == "Pb");
        foreach (var t in types)
        {
            if (t.Name != this.Methods[cmd].Response)
            {
                continue;
            }
            IMessage myInstance = (IMessage)Activator.CreateInstance(t);
            byte[] payload = b.Skip(4).ToArray();
            return myInstance.Descriptor.Parser.ParseFrom(payload);
        }
        return null;
    }
 
    void Close()
    {
        s?.Close();
        print("diconnect");
    }
 
    // Use this for initialization
    void Start()
    {
        Methods = new Method[]{new() {
            Id = 0,
            Name = "Ping",
            Request = "PingRequest",
            Response = "PingResponse",
        }};
        this.buffer = new byte[0];
        ip = IPAddress.Parse("127.0.0.1");
        ipEnd = new IPEndPoint(ip, 9600);
        s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        s.Connect(ipEnd);
    }
 
    // Update is called once per frame
    void Update()
    {
        var request = new PingRequest(){Message = ByteString.CopyFromUtf8("Hello")};
        Send(request);
        byte[] b = new byte[1024];
        int len = s.Receive(b);
        if (len == 0)
        {
            return;
        }
        var newBuffer = buffer.Concat(b.Take(len)).ToArray();
        for (int i = 0; i <= 1000; i++) {
            ushort length = BitConverter.ToUInt16(newBuffer.Take(2).Reverse().ToArray());
            if (length <= 0) 
            {
                return;
            }
            if (length > newBuffer.Length) {
                this.buffer = newBuffer;
                return;
            }
            IMessage iMessage = Decode(newBuffer.Take(length).ToArray());
            Debug.Log("receive" + iMessage);
            newBuffer = newBuffer.Skip(length).ToArray();
            if (newBuffer.Length <= 2) 
            {
                this.buffer = newBuffer;
                return;
            }
        }
    }
 
    void OnApplicationQuit()
    {
        Close();
    }
}