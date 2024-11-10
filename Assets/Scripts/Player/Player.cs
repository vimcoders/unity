using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Google.Protobuf;
using UnityEngine;
using Pb;

public class Player : MonoBehaviour
{
    string url;
    Method[] Methods;
    IPAddress ip;
    IPEndPoint ipEnd;
    //ProtobufTcpClient tcpClient;
    UdpClient udpClient;

    void  Start() 
    {
        Methods = new Method[0];
        url = "http://127.0.0.1:9800/api/v1/passport/login";
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
        var responseBytes = reader.ReadToEnd();
        Debug.Log("responseBytes"+responseBytes);
        var loginResponse = JsonUtility.FromJson<Response<PassportLoginResponse>>(responseBytes);
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == "Pb");
        Debug.Log("loginResponse"+JsonUtility.ToJson(loginResponse));
        for (int i = 0; i < loginResponse.Data.Methods.Length; i++)
        {
            var request = types.Where(t => t.Name == loginResponse.Data.Methods[i].RequestName).First();
            var response = types.Where(t => t.Name == loginResponse.Data.Methods[i].ResponseName).First();
            Methods = Methods.Append(new Method{
                Id = loginResponse.Data.Methods[i].Id,
                Request = (IMessage)Activator.CreateInstance(request),
                Response = (IMessage)Activator.CreateInstance(response),
            }).ToArray();
        }
        //ip = IPAddress.Parse("127.0.0.1");
        ipEnd = new IPEndPoint(IPAddress.Any, 49600);
        //tcpClient = new ProtobufTcpClient(ipEnd, Methods);
        this.udpClient = new UdpClient(ipEnd, Methods);
        udpClient.OnMessage += OnMessage;
        Debug.Log(this.udpClient);
    }

    // Update is called once per frame
    void Update()
    {
        var n = UnityEngine.Random.Range(1, 10000);
        for (int i = 0; i < n; i++) 
        {
            var request = new PingRequest(){Message = ByteString.CopyFromUtf8("HelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorldHelloWorld")};
            udpClient.Send(request);
        }
    }

    void OnMessage(IMessage msg) 
    {
    }

    void OnApplicationQuit()
    {
        udpClient?.Close();
    }
}
