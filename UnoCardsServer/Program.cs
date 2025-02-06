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
    // private static string _sqliteQuery = "";
    private static SQLiteCommand _sqLiteCommand = new SQLiteCommand();
    
    public static void Main(string[] args)
    {
        Init();
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
        StartRecieve(client);
    }
    
    static void HandleMessage(string message)
    {
        HandleMessage(message, new []{ '(' });
    }
    
    static void HandleMessage(string message, char separator)
    {
        HandleMessage(message, new []{ separator });
    }

    static void HandleMessage(string message, char[] separators)
    {
        List<string> parts = message.Split(separators).ToList();
        string func = parts[0];

        if (!_funcs.Contains(func))
        {
            Console.WriteLine("Invalid function");
            return;
        }

        if (func == "")
            return;
        
        string parameter = parts[1];
        List<string> parameters = parameter.Split('(', ')', ' ').ToList();
        
        SwitchFunc(func, parameters);
        // Console.WriteLine(message);
        // Console.WriteLine("msg:" + message);
    }

    static void HandleFuncs()
    {
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

    static void SwitchFunc(string func, List<string> parameters)
    {
        switch (func)
        {
            case "exit":
                Console.WriteLine("[Input Thread]Server exited");
                _isRunning = false;
                break;

            case "log":
                // Console.WriteLine(func);
                string messageLog = parameters[0];
                Console.WriteLine(messageLog);
                break;
            
            case "sqlite":
                string operation = parameters[0];
                switch (operation)
                {
                    case "update":
                        switch (parameters[1])
                        {
                            case "username":
                                RunSqliteCommand($"UPDATE user_info SET password = '{parameters[3]}' WHERE username = '{parameters[2]}'");
                                Console.WriteLine("[Input Thread] Function executed successfully");
                                break;
                            
                            case "password":
                                RunSqliteCommand($"UPDATE user_info SET username = '{parameters[3]}' WHERE password = '{parameters[2]}'");
                                Console.WriteLine("[Input Thread] Function executed successfully");
                                break;
                        }
                        break;
                    
                    case "insert":
                        RunSqliteCommand($"INSERT INTO user_info (username, password) VALUES ('{parameters[1]}', '{parameters[2]}')");
                        Console.WriteLine("[Input Thread] Function executed successfully");
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
        // Thread handleFuncInput = new Thread(HandleInputFuncs);
        handleFunc.Start();
        // handleFuncInput.Start();
        
        // 建立与数据库连接
        try
        {
            _connection.Open();
        }
        catch (SQLiteException e)
        {
            Console.WriteLine(e);
        }

        _sqLiteCommand.Connection = _connection;
        
        Console.WriteLine($"[Main Thread]Server started successfully!");
        
        // _sqliteQuery = "SELECT * FROM user_info";
        // _sqLiteCommand.CommandText = _sqliteQuery;
        // SQLiteDataReader reader = _sqLiteCommand.ExecuteReader();
        // while (reader.Read())
        // {
        //     string username = reader.GetString(0);
        //     string password = reader.GetString(1);
        //     Console.WriteLine("username: " + username + "\npassword:" + password + "\n");
        // }
        // reader.Close();
    }

    static void RunSqliteCommand(string sqliteQuery)
    {
        _sqLiteCommand.CommandText = sqliteQuery;
        _sqLiteCommand.ExecuteNonQuery();
    }
}