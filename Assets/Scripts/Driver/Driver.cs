using Google.Protobuf;

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
