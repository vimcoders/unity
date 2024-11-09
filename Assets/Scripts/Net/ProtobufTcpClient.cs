using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using Google.Protobuf;
using System.Threading;
using System;


public class ProtobufTcpClient 
{
    readonly Socket s;
    readonly Method[] Methods;
    byte[] readBuffer;
    byte[] writeBuffer;
    int r, w;
    Thread t;

    public ProtobufTcpClient(IPAddress ip, IPEndPoint ipEnd, Method[] Methods)
    {
        this.Methods = Methods;
        this.readBuffer = new byte[1024];
        this.writeBuffer = new byte[1024];
        ip = IPAddress.Parse("127.0.0.1");
        ipEnd = new IPEndPoint(ip, 9600);
        s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        s.Connect(ipEnd);
        t = new Thread(new ThreadStart(StartReceive));
        t.Start();
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
        return s.Send(writeBuffer, length, SocketFlags.None);
    }

    /// <summary>
    /// Receive
    /// </summary>
    void Receive()
    {
        int len = s.Receive(readBuffer, w, readBuffer.Length-w, SocketFlags.None);
        if (len == 0)
        {
            return;
        }
        w += len;
        while (w-r >= 4) {
            ushort length = Convert.ToUInt16((readBuffer[r]<<8)|readBuffer[r+1]);;
            if (w-r < length) 
            {
                break;
            }
            ushort cmd = Convert.ToUInt16((readBuffer[r+2]<<8)|readBuffer[r+3]);
            if (cmd >= Methods.Length) {
                break;
            }
            IMessage iMessage = Methods[cmd].Response.Descriptor.Parser.ParseFrom(readBuffer, r+4, length-4);
            OnMessage?.Invoke(iMessage);
            r += length;
        }
        if (w > r)
        {
            Buffer.BlockCopy(readBuffer, r, readBuffer, 0, w-r);
        }
        w -= r;
        r = 0;
    }

    /// <summary>
    /// Close
    /// </summary>
    public void Close()
    {
        s?.Close();
        s?.Dispose();
        t?.Join();
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