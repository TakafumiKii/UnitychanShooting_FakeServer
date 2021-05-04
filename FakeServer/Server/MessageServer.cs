#if UNITY_EDITOR || UNITY_STANDALONE    //  TODOあとで正式な対応をする
#else
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;


using Newtonsoft.Json;

using FakeServer.Network.Information;
using FakeServer.Utility;

using System.Windows.Data;
using System.ComponentModel;
using System.Collections.ObjectModel;


namespace FakeServer.Network
{
    class MessageServer
    {
        class MessageReciever : MessageManager.IRecieveMessage
        {
            MessageServer Server;
            DataBase.DataBaseManager DBManager;
            public MessageReciever(MessageServer server)
            {
                Server = server;
                DBManager = server.DBManager;
            }

            public bool RecieveMessage(MessageManager manager, MessageHeader header, byte[] data)
            {
                Debug.Assert(data != null);
                string text = Encoding.UTF8.GetString(data);
                Console.WriteLine("Recieve " + header.Name);

                MessageCommand param;
                try
                {
                    param = (MessageCommand)Enum.Parse(typeof(MessageCommand), header.Name);
                }
                catch(Exception e)
                {
                    Console.WriteLine(header.Name + " is unmanaged " + e);
                    return false;
                }
                switch (param)
                {
                case MessageCommand.Login:
                    UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(text);

                    int userId = DBManager.UpdateUserData(userInfo);
                    if(userId > 0)
                    {
                        UserInfoResponse userInfoRespon = new UserInfoResponse(userInfo, userId);
                            
                        // アップデートした情報を返す
                        manager.SendSystemMessage(MessageCommand.ResUserInfo, userInfoRespon);

                        lock (Server.ClientDataCollection)
                        {
                            foreach (var client in Server.ClientDataCollection)
                            {
                                if (client.Manager == manager)
                                {
                                    client.Serieal = userInfo.Sereal;
                                    client.Name = userInfo.Name;
                                    client.UserId = userId;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Login Failed");
                        return false;
                    }
                    break;
//                case "UploadScore":
                case MessageCommand.UploadUserScore:
                    UserScoreParam scoreParam = JsonConvert.DeserializeObject<UserScoreParam>(text);

                    UserRankInfo rankInfo = new UserRankInfo();

                    if (DBManager.StoreUserScore(scoreParam, ref rankInfo))
                    {
                        // アップデートした情報を返す
                        manager.SendSystemMessage(MessageCommand.ResUserRank, rankInfo);
                    }
                    else
                    {
                        Console.WriteLine("Login Failed");
                        return false;
                    }

                    break;
//                case "ReqScoreRanking":
                case MessageCommand.GetScoreRanking:
                    RankingRequest rankingReq = JsonConvert.DeserializeObject<RankingRequest>(text);
                    UserRankInfo[] rankInfos = DBManager.GetRanking(rankingReq);

                    if (rankInfos != null)
                    {
                        // アップデートした情報を返す
                        manager.SendSystemMessage(MessageCommand.ResScoreRanking, rankInfos);

                        Server.UserScoreRankDataCollection.Clear();

                        foreach(var info in rankInfos)
                        {
                            Server.UserScoreRankDataCollection.Add(new UserScoreRankData(info));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Login Failed");
                        return false;
                    }
                    break;
                default:
                    Console.WriteLine(header.Name + " is unmanaged");
                //                        break;
                    return false;
                }
                return true;
            }
        }


        //        Socket
        Task RecvTask = null;
        TcpListener Listener;
        CancellationTokenSource CancellTokenSource = null;
        DataBase.DataBaseManager DBManager;

        //        List<MessageManager> MessageManagerList = new List<MessageManager>();
        public UIDataCollection<ClientData> ClientDataCollection { get; private set; } = new UIDataCollection<ClientData>();
        public UIDataCollection<UserScoreRankData> UserScoreRankDataCollection { get; private set; } = new UIDataCollection<UserScoreRankData>();

        public MessageServer(DataBase.DataBaseManager manager)
        {
            DBManager = manager;
        }

        ~MessageServer()
        {
            Stop();
        }

        public bool IsRunning { get { return (RecvTask != null); } }

        
        string GetIPAddress()
        {
            string ipaddress = "";
            IPHostEntry ipentry = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in ipentry.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipaddress = ip.ToString();
                    break;
                }
            }
            return ipaddress;
        }
        public bool Start(int portNo)
        {
            if(RecvTask != null)
            {
                return false;
            }
            CancellTokenSource = new CancellationTokenSource();

            RecvTask = Task.Run(() =>
            {
                try
                {
                    int clientNo = 0;
                    Listener = TcpListener.Create(portNo);
                    Listener.Start();

                    Console.WriteLine($"IPアドレス[{GetIPAddress()}] ポート番号[{portNo}]でサーバー起動開始");
                    while (true)
                    {
                        int targetNo = clientNo;
                        Console.WriteLine($"クライアント[{targetNo}]接続待ち");
                        TcpClient client = Listener.AcceptTcpClient();
                        string ip =((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                        // そこから接続している相手の IPAddress が取れる。
                        Console.WriteLine($"クライアント[{targetNo}]:{ip}接続開始");

                        MessageManager man = new MessageManager(client, new MessageReciever(this));

                        ClientData clientData = new ClientData();
                        clientData.Manager = man;
                        clientData.Id = targetNo;
                        clientData.IPAddress = ip;
                        ClientDataCollection.Add(clientData);

                        man.RunRecvTask().ContinueWith((t) => {
                            Console.WriteLine($"クライアント[{targetNo}]:終了処理開始");

                            try
                            {
                                ClientDataCollection.Remove(clientData);
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine("ClientDatas.Remove Error:" + e);
                            }

                            client.Close();
                            client.Dispose();
                            Console.WriteLine($"クライアント[{targetNo}]:接続終了");
                        });
                        clientNo++;
                    }
                }
                catch(SocketException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch(Exception e)
                {// 予期せぬ例外
                    Console.WriteLine(e.GetType().FullName + e.Message);
                }
                //finally
                //{

                //}
            }, CancellTokenSource.Token).ContinueWith(t =>
            {
                RecvTask = null;
                Stop();
                //// TODO:あとしまつ
                //if (t.IsCanceled)
                //{
                //}
            });
            return true;
        }
        public void Stop()
        {
            StopRecvTask();
        }
        public void StopRecvTask()
        {
            if (Listener != null)
            {
                Console.WriteLine("リスナー停止");
                Listener.Stop();
                Listener = null;
            }
            if (RecvTask != null)
            {
                Console.WriteLine("受信タスクキャンセル");
                if (CancellTokenSource != null)
                {
                    using (CancellTokenSource)
                    {
                        if (!CancellTokenSource.IsCancellationRequested)
                        {
                            CancellTokenSource.Cancel();
                        }
                    }
                }
                RecvTask.Wait();
            }
            if (CancellTokenSource != null)
            {
                using (CancellTokenSource)
                {
                    CancellTokenSource.Dispose();
                    CancellTokenSource = null;
                }
            }
        }


    }
}
#endif