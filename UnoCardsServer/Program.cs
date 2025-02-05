namespace UnoCardsServer;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data.SQLite;

public static class UnoCardsServer
{
    private static Socket _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private static byte[] _buffer = new byte[1024];
    private static string _func = null!;
    private static string _funcInput = null!;
    private static string _parameter = null!;
    private static List<string> _parameters = new List<string>();
    private static List<Socket> _userList = new List<Socket>();
    private static List<string> _funcs = new List<string>() { "exit", "log", "" };
    private static bool _isRunning = true;
    private static object _lock = new object();
    
    public static void Main(string[] args)
    {
        // _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _server.Bind(new IPEndPoint(IPAddress.Any, 25565));
        _server.Listen();
        StartAccept();
        Thread handleFunc = new Thread(HandleFuncs);
        // Thread handleFuncInput = new Thread(HandleInputFuncs);
        handleFunc.Start();
        // handleFuncInput.Start();
    }

    static void SendMsg(Socket client, byte[] buffer)
    {
        client.Send(buffer);
    }

    static void SendMsg(Socket client)
    {
        client.Send(_buffer);
    }

    static void StartAccept()
    {
        _server.BeginAccept(AcceptCallback, null);
    }

    static void AcceptCallback(IAsyncResult iar)
    {
        Socket client = _server.EndAccept(iar);
        _userList.Add(client);
        StartRecieve(client);
        StartAccept();
    }

    static void StartRecieve(Socket client)
    {
        client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, RecieveCallback, client);
    }

    static void RecieveCallback(IAsyncResult iar)
    {
        Socket client = (Socket)iar.AsyncState!;
        int len = client.EndReceive(iar);
        if (len == 0)
        {
            _userList.Remove(client);
            return;
        }
        string message = Encoding.UTF8.GetString(_buffer, 0, len);
        // Console.WriteLine(message);
        HandleMessage(message);
        // client.Close();
        StartRecieve(client);
    }
    
    static void HandleMessage(string message)
    {
        HandleMessage(message, new []{ ' ' });
    }
    
    static void HandleMessage(string message, char separator)
    {
        HandleMessage(message, new []{ separator });
    }

    static void HandleMessage(string message, char[] separators)
    {
        lock (_lock)
        {
            List<string> parts = message.Split(separators).ToList();
            _func = parts[0];

            if (!_funcs.Contains(_func))
            {
                Console.WriteLine("Invalid function");
                return;
            }

            if (_func == "")
            {
                return;
            }
            
            _parameter = parts[1];
            _parameters = _parameter.Split('(', ')', ' ').ToList();
        }
        Console.WriteLine(message);
        // Console.WriteLine("msg:" + message);
    }

    static void HandleFuncs()
    {
        Task.Run(() =>
        {
            while (_isRunning)
            {
                lock (_lock)
                {
                    _funcInput = Console.ReadLine()!;

                    try
                    {
                        HandleMessage(_funcInput);
                        SwitchFunc(_funcInput, _parameters);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Wrong input!\nInput parameters!");
                        _func = "";
                        // Console.WriteLine(e);
                        continue;
                    }
                }
            }
        });

        Task.Run(() =>
        {
            while (_isRunning)
            {
                lock (_lock)
                {
                    SwitchFunc(_func, _parameters);
                }
            }
        });

        while (_isRunning)
        {
            continue;
        }
    }

    static void SwitchFunc(string func, List<string> parameters)
    {
        lock (_lock)
        {
            switch (func)
            {
                case "exit":
                    Console.WriteLine("服务端已退出");
                    _isRunning = false;
                    break;

                case "log":
                    Console.WriteLine(func);
                    string messageLog = parameters[0];
                    break;
                
                case "":
                    break;
            }
        }
    }
}