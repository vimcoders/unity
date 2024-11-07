using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using Google.Protobuf;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Text;
using Pb;
using System;

[System.Serializable]
public class Response<T> {
    public int code;
    public string message;
    public T data;
}

[System.Serializable]
public class PassportLoginRequest {
	public string Passport;
	public string Pwd;      
}

[System.Serializable]
public class PassportLoginResponse {
	public string token;
	public Method[] methods;
}

[System.Serializable]
public class Method
{
    public ushort Id;
    public string MethodName;
    public string RequestName;
    public string ResponseName;
    public IMessage Request;
    public IMessage Response;
}

public delegate void OnMessage(IMessage msg);

public class TcpClient : MonoBehaviour
{
    Socket s;
    IPAddress ip;
    IPEndPoint ipEnd;
    Method[] Methods;
    byte[] readBuffer;
    byte[] writeBuffer;
    int r, w;
    Thread t;

    public OnMessage OnMessage;

    /// <summary>
    /// Send
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    int Send(IMessage msg)
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

    // Use this for initialization
    void Start()
    {
        string url = "http://127.0.0.1:9800/api/v1/passport/login";
        string loginRequest = JsonUtility.ToJson(new PassportLoginRequest() {
            Passport = Guid.NewGuid().ToString(),
            Pwd = Guid.NewGuid().ToString(),
        });
        byte[] loginRequestBytes = Encoding.UTF8.GetBytes(loginRequest);
        var httpRequest = (HttpWebRequest)WebRequest.Create(url);
        httpRequest.Method = "POST";
        httpRequest.ContentType = "application/json;charset=UTF-8";
        httpRequest.ContentLength = loginRequestBytes.Length;
        using Stream writer = httpRequest.GetRequestStream();
        writer.Write(loginRequestBytes, 0, loginRequestBytes.Length);
        var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
        using var reader = new StreamReader(httpResponse.GetResponseStream(), Encoding.UTF8);
        string responseJson = reader.ReadToEnd();
        var loginResponse = JsonUtility.FromJson<Response<PassportLoginResponse>>(responseJson);
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == "Pb");
        this.Methods = new Method[0];
        this.readBuffer = new byte[1024];
        this.writeBuffer = new byte[1024];
        Debug.Log("loginResponse"+JsonUtility.ToJson(loginResponse));
        for (int i = 0; i < loginResponse.data.methods.Length; i++)
        {
            var request = types.Where(t => t.Name == loginResponse.data.methods[i].RequestName).First();
            var response = types.Where(t => t.Name == loginResponse.data.methods[i].ResponseName).First();
            Methods = Methods.Append(new Method{
                Id = loginResponse.data.methods[i].Id,
                Request = (IMessage)Activator.CreateInstance(request),
                Response = (IMessage)Activator.CreateInstance(response),
            }).ToArray();
        }
        ip = IPAddress.Parse("127.0.0.1");
        ipEnd = new IPEndPoint(ip, 9600);
        s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        s.Connect(ipEnd);
        t = new Thread(new ThreadStart(StartReceive));
        t.Start();
    }

    /// <summary>
    /// Close
    /// </summary>
    void Close()
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

    // Update is called once per frame
    void Update()
    {
        var n = UnityEngine.Random.Range(1, 10000);
        for (int i = 0; i < n; i++) 
        {
            var request = new PingRequest(){Message = ByteString.CopyFromUtf8("HelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorld")};
            Send(request);
        }
    }

    void OnApplicationQuit()
    {
        Close();
    }
}