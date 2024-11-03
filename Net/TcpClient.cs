using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Linq;

public class TcpClient : MonoBehaviour
{
    Socket s;
    IPAddress ip;
    IPEndPoint ipEnd;
 
    void Send(byte[] data)
    {
        byte[] d = Encode(data);
        this.s.Send(d, d.Length, SocketFlags.None);
    }

    /// <summary>
    /// encode
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public byte[] Encode(byte[] data) {
        byte[] buffer = new byte[0];
        UInt16 length = Convert.ToUInt16(4);
        UInt16 cmd = Convert.ToUInt16(0);
        buffer = buffer.Concat(BitConverter.GetBytes(length).Reverse()).ToArray();
        buffer = buffer.Concat(BitConverter.GetBytes(cmd).Reverse()).ToArray();
        return buffer.Concat(data).ToArray();
    }
 
    void Close()
    {
        this.s?.Close();
        print("diconnect");
    }
 
    // Use this for initialization
    void Start()
    {
        ip = IPAddress.Parse("127.0.0.1");
        ipEnd = new IPEndPoint(ip, 9600);
        s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        s.Connect(ipEnd);
    }
 
    // Update is called once per frame
    void Update()
    {
        byte[] buffer = new byte[0];
        Send(buffer);
        byte[] recvData = new byte[1024];
        int recvLen = s.Receive(recvData);
        if (recvLen == 0)
        {
            return;
        }
        //Debug.Log("receive" + BitConverter.ToString(recvData));
    }
 
    void OnApplicationQuit()
    {
        Close();
    }
}