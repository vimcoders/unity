using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using Google.Protobuf;
using System.Threading;
using System;


public class UdpClient 
{
    readonly Socket s;
    readonly Method[] Methods;
    byte[] readBuffer;
    byte[] writeBuffer;
    Thread Thread;

    public UdpClient(IPEndPoint ipEnd, Method[] Methods)
    {
        this.Methods = Methods;
        this.readBuffer = new byte[1024];
        this.writeBuffer = new byte[1024];
        s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        s.Bind(ipEnd);
        Thread = new Thread(new ThreadStart(StartReceive));
        Thread.Start();
    }

    public OnMessage OnMessage;

    /// <summary>
    /// Send
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public int Send(IMessage msg)
    {
        var size = msg.CalculateSize();
        ushort length = Convert.ToUInt16(4 + size);
        Method method = Methods.Where(t => t.Request.Descriptor.Name == msg.Descriptor.Name).First();
        writeBuffer[0] = Convert.ToByte(length >> 8);
        writeBuffer[1] = Convert.ToByte(length);
        writeBuffer[2] = Convert.ToByte(method.Id >> 8);
        writeBuffer[3] = Convert.ToByte(method.Id);
        msg.WriteTo(new Span<byte>(writeBuffer, 4, size));
        var ip = IPAddress.Parse("127.0.0.1");
        var ipEnd = new IPEndPoint(ip, 39600);
        return s.SendTo(writeBuffer, writeBuffer.Length, SocketFlags.None, ipEnd);
    }

    /// <summary>
    /// Receive
    /// </summary>
    void Receive()
    {
        int len = s.Receive(readBuffer, 0, readBuffer.Length, SocketFlags.None);
        if (len == 0)
        {
            return;
        }
        ushort length = Convert.ToUInt16((readBuffer[0]<<8)|readBuffer[1]);;
        if (readBuffer.Length < length) 
        {
            return;
        }
        ushort cmd = Convert.ToUInt16((readBuffer[2]<<8)|readBuffer[3]);
        if (cmd >= Methods.Length) {
            return;
        }
        IMessage iMessage = Methods[cmd].Response.Descriptor.Parser.ParseFrom(readBuffer, 4, length-4);
        OnMessage?.Invoke(iMessage);
    }

    /// <summary>
    /// Close
    /// </summary>
    public void Close()
    {
        s?.Close();
        s?.Dispose();
        Thread?.Join();
    }

    /// <summary>
    /// StartReceive
    /// </summary>
    void StartReceive()
    {
        while (true)
        {
            try
            {
                Receive();
            }
            catch (SocketException ex)
            {
                int err = ex.ErrorCode;
                Debug.Log("StartReceive"+err.ToString());
                return;
            }
        }
    }
}