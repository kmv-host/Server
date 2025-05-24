using System;
using System.Net; // добавили пространство имен
using System.Net.Sockets; //Добавили пространство имен
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading; // Добавили пространство имен
using System.Collections.Concurrent; // Добавили пространство имен

class Server
{
    private static readonly List<TcpClient> clients = new List<TcpClient>();
    private static readonly object lockObject = new object();
    private static readonly ConcurrentQueue<string> messageHistory = new ConcurrentQueue<string>();
    private const int MaxHistorySize = 50; // Количество сохраненных сообщений

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

                // Send message history to new client
                SendHistoryToClient(client);

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

    static void SendHistoryToClient(TcpClient client)
    {
        try
        {
            if (!messageHistory.Any()) return;

            NetworkStream stream = client.GetStream();
            string historyHeader = "=== История сообщений ===\n";
            byte[] headerData = Encoding.UTF8.GetBytes(historyHeader);
            stream.Write(headerData, 0, headerData.Length);

            foreach (string message in messageHistory)
            {
                byte[] messageData = Encoding.UTF8.GetBytes(message + "\n");
                stream.Write(messageData, 0, messageData.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке истории: {ex.Message}");
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = null;
        byte[] buffer = new byte[8192]; // Увеличиваем буфер

        try
        {
            stream = client.GetStream();

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Проверяем на команды (например, NAME:)
                if (message.StartsWith("NAME:"))
                {
                    // Обработка имени пользователя
                    continue;
                }

                Console.WriteLine($"Получено: {message}");
                AddMessageToHistory(message);
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

    static void AddMessageToHistory(string message)
    {
        messageHistory.Enqueue(message);

        // Maintain history size
        while (messageHistory.Count > MaxHistorySize)
        {
            messageHistory.TryDequeue(out _);
        }
    }

    static void BroadcastMessage(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);

        lock (lockObject)
        {
            foreach (TcpClient client in clients.ToArray())
            {
                try
                {
                    if (client.Connected)
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