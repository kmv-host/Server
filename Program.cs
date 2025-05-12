using System;
using System.Net; // добавили пространство имен
using System.Net.Sockets; //Добавили пространство имен
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading; // Добавили пространство имен

class Server
{
    static readonly List<TcpClient> clients = new List<TcpClient>();

    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 8888);
        listener.Start();
        Console.WriteLine("Сервер запущен...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            clients.Add(client);
            Console.WriteLine("Подключен новый клиент");

            Thread clientThread = new Thread(HandleClient);
            clientThread.Start(client);
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();

        try
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Получено: {message}");
                BroadcastMessage(message, client);
            }
        }
        catch
        {
            clients.Remove(client);
            client.Close();
            Console.WriteLine("Клиент отключен");
        }
    }

    static void BroadcastMessage(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        foreach (TcpClient client in clients)
        {
            if (client != sender && client.Connected)
            {
                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);
            }
        }
    }
}