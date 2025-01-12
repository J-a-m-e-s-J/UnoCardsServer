﻿namespace UnoCardsServer;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data.SQLite;

public static class UnoCardsServer
{
    private static Socket _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private static byte[] _buffer = new byte[1024];
    private static string? _func;
    private static List<Socket> _userList = new List<Socket>();
    
    public static void Main(string[] args)
    {
        // _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _server.Bind(new IPEndPoint(IPAddress.Any, 25565));
        _server.Listen();
        StartAccept();
        while (true)
        {
            _func = Console.ReadLine();

            switch (_func)
            {
                case "exit":
                    Console.WriteLine("服务端已退出");
                    break;
                
                case "":
                    continue;
                
                default:
                    Console.WriteLine("Invalid function");
                    continue;
            }
        }
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
        client.Close();
        StartAccept();
    }

    static void StartRecieve(Socket client)
    {
        client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, RecieveCallback, client);
    }

    static void RecieveCallback(IAsyncResult iar)
    {
        Socket client = (iar.AsyncState as Socket)!;
        int len = client.EndReceive(iar);
        if (len == 0)
        {
            _userList.Remove(client);
            return;
        }
        string message = Encoding.UTF8.GetString(_buffer, 0, len);
        Console.WriteLine(message);
        client.Close();
        StartRecieve(client);
    }
}