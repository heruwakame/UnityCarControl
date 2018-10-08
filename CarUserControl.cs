using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using System.IO.Ports;
using System.Threading;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof (CarController))]
    public class CarUserControl : MonoBehaviour
    {
        // C#仕様 : 
        //          static 修飾子 : グローバル変数
        //          const  修飾子 : グローバル定数
        // CarUserControl は MonoBehaviour クラス ( 注 : 基底クラス ) を継承している
        // MonoBehaviour クラス は C# の Unityのスクリプトを動かす上でこの明示
        private CarController m_Car; // the car controller we want to use
        public delegate void SerialDataReceivedEventHandler(string message);
        public event SerialDataReceivedEventHandler OnDataReceived;

        // ip_address : 通信相手となるIP アドレス
        // port : 通信相手のポート指定
        private static string ip_address = "127.0.0.1";
        private static int port = 5555;
        // h : 自動車の左右を制御する
        public static float h = 0.0f;
        // v : 自動車の前進, 後退を制御する
        public static float v = 0.0f;

        // グローバル変数として TcpListenerの変数宣言 ( Close, Open が楽にできないので )
        private static TcpListener _listener;
        private static readonly List<TcpClient> _clients = new List<TcpClient>();

        // OnDestroy : Unity側が、ゲームの終了時に一回だけ呼び出す関数, デストラクタのようなもの
        // 
        private static void OnDestroy()
        {
            
        }

        // TCP/IP通信用のソケットを開く関数
        private static void Open(string host, int port)
        {
            Debug.Log("IP Address:" + host + " Port:" + port);
            var IP = IPAddress.Parse(host);
            _listener = new TcpListener(IP, port);
            _listener.Start();
            _listener.BeginAcceptSocket(DoAcceptTcpClientCallback, _listener);
        }

        // クライアントからの接続処理
        private static void DoAcceptTcpClientCallback(IAsyncResult ar)
        {
            var listener = (TcpListener)ar.AsyncState;
            var client = listener.EndAcceptTcpClient(ar);
            _clients.Add(client);
            Debug.Log("Connect: " + client.Client.RemoteEndPoint);

            // Listener の接続が確立したら次の人を受け付ける
            listener.BeginAcceptSocket(DoAcceptTcpClientCallback, listener);

            // 今接続した人とのネットワークストリームを取得
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);

            // 接続が切れるまで送受信を繰り返す
            while (client.Connected)
            {
                while (!reader.EndOfStream)
                {
                    // 1行分の文字列を受け取る
                    var str = reader.ReadLine();
                    // 受け取った文字列をデバッグ表示する
                    Debug.Log("Recieved string : " + str);
                    string[] arr = str.Split(',');
                    v = float.Parse(arr[0]);
                    h = float.Parse(arr[1]);

                    Debug.Log("v : " + v + " h: " + h);
                }

                // クライアントからの接続が切れたら
                if (client.Client.Poll(1000, SelectMode.SelectRead) && (client.Client.Available == 0))
                {
                    Debug.Log("Disconnect: " + client.Client.RemoteEndPoint);
                    client.Close();
                    _clients.Remove(client);
                    break;
                }
            }
        }

        protected virtual void OnMessage(string msg)
        {
            Debug.Log(msg);
        }

        protected void SendMessageToClient(string msg)
        {
            if (_clients.Count == 0)
            {
                return;
            }

            var body = Encoding.UTF8.GetBytes(msg);

            foreach (var client in _clients)
            {
                try
                {
                    var stream = client.GetStream();
                    stream.Write(body, 0, body.Length);
                }
                catch
                {
                    _clients.Remove(client);
                }
            }
        }

        // 終了処理
        protected virtual void OnApplicationQuit()
        {
            if (_listener == null)
            {
                return;
            }

            if (_clients.Count != 0)
            {
                foreach (var client in _clients)
                {
                    client.Close();
                }
            }
            _listener.Stop();
        }

        // Awake : Unity側が、ゲームの起動時に一回だけ呼び出す関数, コンストラクタのようなもの
        private void Awake()
        {
            // get the car controller
            Open(ip_address, port);
            m_Car = GetComponent<CarController>();
        }

        // MonoBehaciour基底クラスを継承しているのでUnity側が呼び出す関数
        // FixedUpdate : Unity側が固定フレームレートでこの関数を呼び出す
        // やるべきこと :
        //                  ソケット通信で受信した値をこの関数内で取得
        //                  ゲームで得られた衝突危険性の判定をこの関数内でソケット通信を用いて送信
        //                      得られた値をそれぞれh, vに代入できれば, 操作が反映される
        //                      値に対応する操作はそれぞれ説明を記述済み
        private void FixedUpdate()
        {
            // pass the input to the car!
            // h : 前輪の角度を決定する
            //     値の範囲 [-1.0, 1.0], 連続値, この値域を超えても-1.0または1.0に落とし込まれる
            // mbedと通信する仕組みが必要だが実地試験以外にやりようがない
            // CrossPlatformInputManager : PCの場合は矢印キーで操作する
            //                             Horizontalなら→ ←
            //                             Vertical  なら↑ ↓
            // float h = CrossPlatformInputManager.GetAxis("Horizontal");
            // v : アクセル制御
            //     値の範囲 [-1.0, 1.0], 連続値, この値域を超えても-1.0または1.0に落とし込まれる
            // float v = CrossPlatformInputManager.GetAxis("Vertical");

#if !MOBILE_INPUT
            // ハンドブレーキ, 0か1以外とらないがfloat型
            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
            // 上記で定義された値を用いて自動車に反映
            m_Car.Move(h, v, v, handbrake);
#else
            m_Car.Move(h, v, v, 0f);
#endif
        }
    }
}
