using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Client
{
    public static void Main()
    {
        // string sendMsg = "1qaz2wsx"; // OKの場合
        string sendMsg = "aaaa";  // NGの場合
        string host = "127.0.0.1";
        int port = 12345;

        // サーバーにTCP接続
        TcpClient tcp = new TcpClient(host, port);
        NetworkStream stream = tcp.GetStream();

        // サーバーにデータを送信する
        /* byte[] sendBytes = Encoding.UTF8.GetBytes(sendMsg);
        stream.Write(sendBytes, 0, sendBytes.Length);
        Console.WriteLine("send message: " + sendMsg);*/

        // サーバーから送られたデータを受信する
        MemoryStream ms = new MemoryStream();
        byte[] resBytes = new byte[256];
        int resSize = 5;
        do
        {
            resSize = stream.Read(resBytes, 0, resBytes.Length);
            if (resSize == 0)
            {
                break;
            }
            ms.Write(resBytes, 0, resSize);
        } while (stream.DataAvailable || resBytes[resSize - 1] != '\n');

        string resMsg = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
        Console.WriteLine("response message: " + resMsg);

        ms.Close();
        stream.Close();
        tcp.Close();
    }
}