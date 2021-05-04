using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using System.Data.Linq;
using FakeServer.Network.Information;

namespace FakeServer.DataBase
{
    //  データベースの処理管理クラス
    public class DataBaseManager
    {
        public class Description
        {
            public string DBName = "Test";
        }

        string ConnectionString;
        public DataBaseManager()
        {
        }

        public void Initialize(Description desc)
        {
            CreateDataBase(desc.DBName);
        }

        bool CreateDataBase(string dbName)
        {
            ConnectionString = $"DataSource={dbName}.sqlite";
            using (var connection = new SQLiteConnection(ConnectionString))// 「DataSource=:memory:」にするとオンメモリのDBとして動作
            {
                // データベースに接続
                connection.Open();
                // ユーザーテーブルの作成
                User.CreateTable(connection);
                // ランキング用テーブルの作成
                UserScore.CreateTable(connection);

                //  ダミーデータの作成
                int count = 0;
                using (var context = new DataContext(connection))
                {
                    count = User.Count(context);
                }
                const int DATA_MIN = 10;

                Random rand = new Random();
                for (int i = count; i < DATA_MIN;i++)
                {
                    User user = new User();
                    user.Name = "CPU" + i;
                    user.Commit(connection);

                    UserScore score = new UserScore(user);
                    score.Point = rand.Next(10000);
                    score.Commit(connection);
                }

                // 切断
                connection.Close();
            }
            return true;
        }

        public int UpdateUserData(UserInfo info)
        {
            using (var connection = new SQLiteConnection(ConnectionString))// 「DataSource=:memory:」にするとオンメモリのDBとして動作
            {
                // データベースに接続
                connection.Open();
                if (info.Sereal == 0)
                {// 新規
                    User user = new User();
                    if(info.Name.Length > 0)
                    {
                        user.Name = info.Name;
                    }
                    else
                    {
                        user.Name = info.Name = "Nameless";
                    }

                    bool ret = user.Commit(connection);
                    if (ret)
                    {
                        info.Sereal = user.Sereal;
                        connection.Close();
                        return user.Id.Value;
                    }
                }
                else
                {//二回目以降
                    using (var context = new DataContext(connection))
                    {
                        User user = User.Find(context, info.Sereal);
                        if(user != null)
                        {
                            if(user.Name != info.Name)
                            {
                                user.Name = info.Name;
                            }
                            context.SubmitChanges();
                            connection.Close();

                            return user.Id.Value;
                        }
                    }
                }
                connection.Close();
            }
            return 0;
        }

        public bool StoreUserScore(UserScoreParam param,ref UserRankInfo rankInfo)
        {
            using (var connection = new SQLiteConnection(ConnectionString))// 「DataSource=:memory:」にするとオンメモリのDBとして動作
            {
                // データベースに接続
                connection.Open();

                using (var context = new DataContext(connection))
                {
                    User user = User.Find(context, param.Sereal);

                    if (user == null)
                    {
                        connection.Close();
                        return false;
                    }
                    UserScore score = new UserScore(user);
                    score.Point = param.Point;
                    bool ret = score.Commit(connection);

                    if(rankInfo != null)
                    {
                        rankInfo.UserId = score.UserId;
                        rankInfo.Name = user.Name;
                        rankInfo.Point = score.Point;
                        rankInfo.Rank = score.Rank(context);
                    }

                    connection.Close();
                    return ret;
                }
            }
        }

        public UserRankInfo[] GetRanking(RankingRequest request)
        {
            using (var connection = new SQLiteConnection(ConnectionString))// 「DataSource=:memory:」にするとオンメモリのDBとして動作
            {
                // データベースに接続
                connection.Open();

                using (var context = new DataContext(connection))
                {
                    User target = (request.Sereal != 0)? User.Find(context, request.Sereal): null;

                    var data = UserScore.Rank(context, request.Skip, request.Take, target);


                    UserRankInfo[] ret = new UserRankInfo[data.Length];

//                    foreach (var score in data)
                    for(int i = 0;i < data.Length;i++)
                    {
                        var score = data[i];

                        UserRankInfo rankInfo = new UserRankInfo();
                        rankInfo.Point = score.Point;
                        rankInfo.Rank = request.Skip + i;

                        User user = User.FindByUserId(context, score.UserId);
                        rankInfo.UserId = score.UserId;
                        rankInfo.Name = user.Name;

                        ret[i] = rankInfo;
                    }
                    connection.Close();
                    return ret;
                }
            }
        }

    }
}
