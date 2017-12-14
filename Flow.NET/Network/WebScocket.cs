using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlowNET.Network
{
    public class WebScocket
    {
        private Socket mSocket;
        private List<Socket> mClients;

        public WebScocket()
        {
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.mClients = new List<Socket>();
        }

        public void Listen(int port)
        {
            this.mSocket.Bind(new IPEndPoint(IPAddress.Any, 1200));
            this.mSocket.Listen(port);
            this.Accept();
        }


        public void OnNext(string messages)
        {
            if (this.mClients == null)
            {
                return;
            }
            Parallel.ForEach(this.mClients, client =>
            {
                try
                {
                    byte[] content = null;
                    byte[] temp = Encoding.Default.GetBytes(messages);
                    if (temp.Length < 126)
                    {
                        content = new byte[temp.Length + 2];
                        content[0] = 0x81;
                        content[1] = (byte)temp.Length;
                        Array.Copy(temp, 0, content, 2, temp.Length);
                    }
                    else if (temp.Length < 0xFFFF)
                    {
                        content = new byte[temp.Length + 4];
                        content[0] = 0x81;
                        content[1] = 126;
                        content[2] = (byte)(temp.Length & 0xFF);
                        content[3] = (byte)(temp.Length >> 8 & 0xFF);
                        Array.Copy(temp, 0, content, 4, temp.Length);
                    }
                    else
                    {
                        // 暂不处理超长内容  
                    }
                    client.Send(content);
                }
                catch(Exception e)
                {
                    
                }
            });
        }

        private void Accept()
        {
            bool isAccept = false;
            Task.Run(() =>
            {
                while (true)
                {
                    if (!isAccept)
                    {
                        isAccept = true;
                        this.mSocket.BeginAccept(new AsyncCallback((ar) =>
                        {
                            isAccept = false;
                            //这就是客户端的Socket实例，我们后续可以将其保存起来
                            Socket client = this.mSocket.EndAccept(ar);
                            this.mClients.Add(client);
                            //给客户端发送一个欢迎消息
                            //client.Send(Encoding.Unicode.GetBytes("connection finsh."));
                            this.Receive(client);
                        }), this.mSocket);
                    }
                }
            });
        }
        public void Receive(Socket client)
        {
            bool isReceive = false;
            byte[] buffer = new byte[1024];
            Task.Run(() =>
            {
                //while (true)
                //{
                if (!isReceive)
                {
                    isReceive = true;
                    client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback((ar) =>
                    {
                        isReceive = false;
                        string msg = Encoding.Default.GetString(buffer, 0, buffer.Length);
                        this.Handshake(client, msg);
                    }), this.mSocket);
                }
                //}
            });
        }
        private void Handshake(Socket client, string clientMsg)
        {
            if (client.Connected)
            {
                try
                {
                    string key = string.Empty;
                    Regex reg = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
                    Match m = reg.Match(clientMsg);
                    if (m.Value != "")
                    {
                        key = Regex.Replace(m.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();
                    }
                    byte[] secKeyBytes = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
                    string secKey = Convert.ToBase64String(secKeyBytes);
                    var responseBuilder = new StringBuilder();
                    responseBuilder.Append("HTTP/1.1 101 Switching Protocols" + "\r\n");
                    responseBuilder.Append("Upgrade:websocket" + "\r\n");
                    responseBuilder.Append("Connection:Upgrade" + "\r\n");
                    responseBuilder.Append("Sec-WebSocket-Accept:" + secKey + "\r\n\r\n");
                    client.Send(Encoding.Default.GetBytes(responseBuilder.ToString()));
                }
                catch (Exception e)
                {
                }
            }
        }
    }
}
