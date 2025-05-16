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
    private static readonly List<TcpClient> clients = new List<TcpClient>();
    private static readonly object lockObject = new object();

    static void Main()
    {
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();
            Console.WriteLine("Сервер запущен на порту 8888...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                lock (lockObject)
                {
                    clients.Add(client);
                }

                Console.WriteLine($"Подключен новый клиент. Всего клиентов: {clients.Count}");

                Thread clientThread = new Thread(HandleClient);
                clientThread.IsBackground = true;
                clientThread.Start(client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сервера: {ex.Message}");
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = null;

        try
        {
            stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Получено от {client.Client.RemoteEndPoint}: {message}");
                BroadcastMessage(message, client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при работе с клиентом: {ex.Message}");
        }
        finally
        {
            lock (lockObject)
            {
                clients.Remove(client);
            }

            stream?.Close();
            client.Close();
            Console.WriteLine($"Клиент отключен. Осталось клиентов: {clients.Count}");
        }
    }

    static void BroadcastMessage(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);

        lock (lockObject)
        {
            foreach (TcpClient client in clients.ToArray()) // ToArray для безопасной итерации
            {
                try
                {
                    if (client != sender && client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при отправке сообщения клиенту: {ex.Message}");
                }
            }
        }
    }
}