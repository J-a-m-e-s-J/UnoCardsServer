using System.Data;
using Exceptions;

// using Exceptions;

namespace UnoCardsServer;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data.SQLite;

public static class UnoCardsServer
{
    private static Socket _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private static byte[] _buffer = new byte[1024];
    private static List<Socket> _userList = new List<Socket>();
    private static List<string> _funcs = new List<string>() { "exit", "log", "sqlite", "" };
    private static bool _isRunning = true;
    private static SQLiteConnection _connection = new SQLiteConnection(@"Data Source=F:\unity\UnoCards\UnoCardsServer\UserInfo.sqlite");
    private static SQLiteCommand _sqLiteCommand = new SQLiteCommand();

    public static void Main(string[] args)
    {
        Init();
        
    }

    static void SendMsg(string message)
    {
        lock (_userList)
        {
            foreach (Socket client in _userList)
            {
                if (client == _server) continue;
                SendMsg(message, client);
            }
        }
    }

    static void SendMsg(string message, Socket client)
    {
        client.Send(Encoding.UTF8.GetBytes(message));
    }

    static void StartAccept()
    {
        _server.BeginAccept(AcceptCallback, null);
    }

    static void AcceptCallback(IAsyncResult iar)
    {
        Socket client = _server.EndAccept(iar);
        lock (_userList)
        {
            _userList.Add(client);
            Console.WriteLine($"[Main Thread {DateTime.Now:hh:mm:ss}]\t\tNew user connected\tIp: {((IPEndPoint)client.RemoteEndPoint!).Address}\tPort: {((IPEndPoint)client.RemoteEndPoint!).Port}");
            Console.WriteLine($"[Main Thread {DateTime.Now:hh:mm:ss}]\t\tUser count: {_userList.Count}");
        }

        StartRecieve(client);
        StartAccept();
    }

    static void StartRecieve(Socket client)
    {
        lock (_userList)
        {
            client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, RecieveCallback, client);
        }
    }

    static void RecieveCallback(IAsyncResult iar)
    {
        Socket client = null!;
        try
        {
            client = (Socket)iar.AsyncState!;
            int len;
            lock (_userList)
            {
                foreach (Socket user in _userList.ToList())
                {
                    if (!user.Connected)
                    {
                        _userList.Remove(user);
                        Console.WriteLine($"[Main Thread {DateTime.Now:hh:mm:ss}]\t\tUser disconnected\tIp: {((IPEndPoint)user.RemoteEndPoint!).Address}\tPort: {((IPEndPoint)user.RemoteEndPoint!).Port}");
                        Console.WriteLine($"[Main Thread {DateTime.Now:hh:mm:ss}]\t\tUser count: {_userList.Count}");
                        user.Close();
                        break;
                    }
                }

                len = client.EndReceive(iar);
                if (len == 0)
                {
                    _userList.Remove(client);
                    Console.WriteLine($"[Main Thread {DateTime.Now:hh:mm:ss}]\t\tUser disconnected\tIp: {((IPEndPoint)client.RemoteEndPoint!).Address}\tPort: {((IPEndPoint)client.RemoteEndPoint!).Port}");
                    Console.WriteLine($"[Main Thread {DateTime.Now:hh:mm:ss}]\t\tUser count: {_userList.Count}");
                    client.Close();
                    return;
                }
            }

            string message = Encoding.UTF8.GetString(_buffer, 0, len);
            // Console.WriteLine(message);
            HandleMessage(message);
            message = "";
            StartRecieve(client);
        }
        catch (SocketException)
        {
            lock (_userList)
            {
                if (_userList.Contains(client))
                {
                    _userList.Remove(client);
                    Console.WriteLine($"[Main Thread {DateTime.Now:hh:mm:ss}]\t\tUser disconnected\tIp: {((IPEndPoint)client.RemoteEndPoint!).Address}\tPort: {((IPEndPoint)client.RemoteEndPoint!).Port}");
                    Console.WriteLine($"[Main Thread {DateTime.Now:hh:mm:ss}]\t\tUser count: {_userList.Count}");
                    client.Close();
                }
            }
        }
    }

    static void HandleMessage(string message)
    {
        HandleMessage(message, new[] { '-' });
    }

    static void HandleMessage(string message, char separator)
    {
        HandleMessage(message, new[] { separator });
    }

    static void HandleMessage(string message, char[] separators)
    {
        List<string> parts = message.Split(separators).ToList();
        string func, parameter, ip, port;
        try
        {
            func = parts[0];
            parameter = parts[1];
        }
        catch (Exception)
        {
            Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tInvalid message");
            return;
        }

        if (!_funcs.Contains(func))
        {
            Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tInvalid function");
            return;
        }

        if (func == "") return;

        List<string> parameters = parameter.Split('(', ')', ' ').ToList();
        parameters.Remove(parameters[0]);
        parameters.Remove(parameters[^1]);
        
        try
        {
            ip = parts[2];
            port = parts[3];
        }
        catch (Exception)
        {
            SwitchFunc(func, parameters);
            return;
        }

        SwitchFunc(func, parameters, ip, port);
    }

    static void HandleFuncs()
    {
        // 线称启动成功提示
        Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tFunction Handler started successfully!");
        
        while (_isRunning)
        {
            string msgInput = Console.ReadLine()!;

            try
            {
                HandleMessage(msgInput);
            }
            catch (Exception e)
            {
                // Console.WriteLine(e);
                Console.WriteLine("An Exception Occured: " + e.GetType() + "\n" + e.Message);
                continue;
            }
        }
    }

    static void SwitchFunc(string func, List<string> parameters, string ip = "", string port = "")
    {
        switch (func)
        {
            case "exit":
                Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tServer exited");
                _isRunning = false;
                break;

            case "log":
                Console.Write($"[Function Handler {DateTime.Now:hh:mm:ss}]\tServer log: ");

                foreach (string parameter in parameters)
                {
                    Console.Write(parameter + " ");
                }
                Console.WriteLine("");
                break;

            case "sqlite":
                string operation = parameters[0];
                SQLiteDataReader reader;
                switch (operation)
                {
                    case "update":
                        switch (parameters[1])
                        {
                            case "username":
                                RunSqliteCommand(
                                    $"UPDATE user_info SET password = '{parameters[3]}' WHERE username = '{parameters[2]}'");
                                Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tFunction executed successfully");
                                break;

                            case "password":
                                RunSqliteCommand(
                                    $"UPDATE user_info SET username = '{parameters[3]}' WHERE password = '{parameters[2]}'");
                                Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tFunction executed successfully");
                                break;
                        }

                        break;

                    case "insert":
                        _sqLiteCommand.CommandText = "SELECT * FROM user_info";
                        reader = _sqLiteCommand.ExecuteReader();
                        
                        // 检查用户名是否已存在
                        while (reader.Read())
                        {
                            string username = reader.GetString(0);
                            if (username == parameters[1])
                            {
                                lock (_userList)
                                {
                                    if (ip == "" || port == "")
                                    {
                                        // SendMsg("Username already exists");
                                        throw new IpMissingException("missing ip");
                                    }
                                    else
                                    {
                                        SendMsg("Username already exists",
                                            _userList.Find(client =>
                                                ((IPEndPoint)client.RemoteEndPoint!).Address.ToString() == ip &&
                                                ((IPEndPoint)client.RemoteEndPoint!).Port.ToString() == port)!);
                                    }
                                }
                                Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tUsername '{username}' already exists");
                                reader.Close();
                                return;
                            }
                        }
                        reader.Close();
                        
                        // 检查密码是否包含空格
                        if (parameters.Count > 3)
                        {
                            lock (_userList)
                            {
                                if (ip == "" && port == "")
                                {
                                    // SendMsg("Password contains space");
                                    throw new IpMissingException("missing ip");
                                }
                                else
                                {
                                    SendMsg("Password contains space",
                                        _userList.Find(client =>
                                            ((IPEndPoint)client.RemoteEndPoint!).Address.ToString() == ip &&
                                            ((IPEndPoint)client.RemoteEndPoint!).Port.ToString() == port)!);
                                }
                            }
                            Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tPassword contains space");
                            return;
                        }
                        
                        // 注册成功
                        lock (_userList)
                        {
                            if (ip == "" && port == "")
                            {
                                // SendMsg("Register success");
                                throw new IpMissingException("missing ip");
                            }
                            else
                            {
                                SendMsg("Register success",
                                    _userList.Find(client =>
                                        ((IPEndPoint)client.RemoteEndPoint!).Address.ToString() == ip &&
                                        ((IPEndPoint)client.RemoteEndPoint!).Port.ToString() == port)!);
                            }
                        }

                        RunSqliteCommand(
                            $"INSERT INTO user_info (username, password) VALUES ('{parameters[1]}', '{parameters[2]}')");
                        Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tFunction executed successfully");
                        break;
                    
                    case "login":
                        _sqLiteCommand.CommandText = "SELECT * FROM user_info";
                        reader = _sqLiteCommand.ExecuteReader();
                        
                        // 检查密码是否包含空格
                        if (parameters.Count > 3)
                        {
                            lock (_userList)
                            {
                                if (ip == "" && port == "")
                                {
                                    // SendMsg("Password contains space");
                                    throw new IpMissingException("missing ip");
                                }
                                else
                                {
                                    SendMsg("Password contains space",
                                        _userList.Find(client =>
                                            ((IPEndPoint)client.RemoteEndPoint!).Address.ToString() == ip &&
                                            ((IPEndPoint)client.RemoteEndPoint!).Port.ToString() == port)!);
                                }
                            }
                            Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tPassword contains space");
                            reader.Close();
                            return;
                        }
                        
                        // 登录成功
                        while (reader.Read())
                        {
                            string username = reader.GetString(0);
                            string password = reader.GetString(1);
                            if (username == parameters[1] && password == parameters[2])
                            {
                                lock (_userList)
                                {
                                    if (ip == "" && port == "")
                                    {
                                        // SendMsg("Login success");
                                        throw new IpMissingException("missing ip");
                                    }
                                    else
                                    {
                                        SendMsg("Login success",
                                            _userList.Find(client =>
                                                ((IPEndPoint)client.RemoteEndPoint!).Address.ToString() == ip &&
                                                ((IPEndPoint)client.RemoteEndPoint!).Port.ToString() == port)!);
                                    }
                                }
                                Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tLogin success\tUsername: {username}\tPassword: {password}");
                                reader.Close();
                                return;
                            }
                        }
                        
                        // 登录失败
                        lock (_userList)
                        {
                            if (ip == "" && port == "")
                            {
                                // SendMsg("Login failed");
                                throw new IpMissingException("missing ip");
                            }
                            else
                            {
                                SendMsg("Login failed",
                                    _userList.Find(client =>
                                        ((IPEndPoint)client.RemoteEndPoint!).Address.ToString() == ip &&
                                        ((IPEndPoint)client.RemoteEndPoint!).Port.ToString() == port)!);
                            }
                        }
                        Console.WriteLine($"[Function Handler {DateTime.Now:hh:mm:ss}]\tLogin failed");
                        
                        reader.Close();
                        break;
                }
                
                break;

            case "":
                break;
        }
    }

    static void Init()
    {
        // 服务器初始化
        _server.Bind(new IPEndPoint(IPAddress.Any, 25565));
        _server.Listen();
        StartAccept();
        Thread handleFunc = new Thread(HandleFuncs); 
        handleFunc.Start();

        // 建立与数据库连接
        try
        {
            _connection.Open();
            _sqLiteCommand.Connection = _connection;
            
        }
        catch (SQLiteException e)
        {
            Console.WriteLine(e);
        }
        
        // 服务器启动成功提示
        Console.WriteLine($"[Main Thread {DateTime.Now:hh:mm:ss}]\t\t"+"Server started successfully!");
    }

    static void RunSqliteCommand(string sqliteQuery)
    {
        _sqLiteCommand.CommandText = sqliteQuery;
        _sqLiteCommand.ExecuteNonQuery();
    }
}