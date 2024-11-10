using Google.Protobuf;
using System;
using System.Text.Json;

[System.Serializable]
public class Response<T> {
    public int Code;
    public string Message;
    public T Data;
}

[System.Serializable]
public class PassportLoginRequest {
	public string Passport;
	public string Pwd;
}

[System.Serializable]
public class PassportLoginResponse {
	public string Token;
	public Method[] Methods;
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
