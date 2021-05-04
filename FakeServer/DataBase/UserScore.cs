using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data.SQLite;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace FakeServer.DataBase
{
    public class UserScore
    {
        [Table(Name = "user_scores")]
        class RawData
        {
            [Column(Name = "id", IsPrimaryKey = true)]
            public int? Id { get; private set; } = null;
            [Column(Name = "user_id", CanBeNull = false)]
            public int UserId { get; internal set; }
            [Column(Name = "point", CanBeNull = false)]
            public int Point { get; set; }

            //[Column(Name = "created_at", CanBeNull = false)]
            //public DateTime CreatedAt { get; set; }
            internal void Copy(RawData raw)
            {
                Point = raw.Point;
            }
        }
        RawData Raw = new RawData();

        public int UserId { get { return Raw.UserId; } }
        public int Point { get { return Raw.Point; } set { Raw.Point = value; } }

        internal static void CreateTable(SQLiteConnection connection)
        {
            Debug.Assert(connection != null);
            Debug.Assert(connection.State == System.Data.ConnectionState.Open);
            // コマンドの実行
            using (var command = connection.CreateCommand())
            {
                // ※各テーブルデータについて
                //  本来はmigrationでテーブル管理しますが、今回はテーブルやカラムの存在チェックで簡易的に設定しています。

                command.CommandText = "CREATE TABLE IF NOT EXISTS user_scores (" +
                    "  id INTEGER PRIMARY KEY" +
                    "  , user_id INTEGER NOT NULL" +
                    "  , point INTEGER NOT NULL" +
//                    "  , created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP" +
                    "  , FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE" +
                    ")";
                command.ExecuteNonQuery();

                command.CommandText = "CREATE INDEX IF NOT EXISTS ranking_point_index ON user_scores(point);";
                command.CommandText += "CREATE INDEX IF NOT EXISTS ranking_user_id_point_index ON user_scores(user_id, point);";
                command.ExecuteNonQuery();
            }
        }

        public static UserScore[] Rank(DataContext context,int skip, int take, User user = null)
        {
            Table<RawData> user_scores = context.GetTable<RawData>();
            var query = (user != null) ? user_scores.Where(item => (item.Id == user.Id)) : user_scores;

            var data = query.OrderByDescending(item => item.Point);
            
            //  Todo:レコードの取得数を制限し、該当クラスにマッピングしたデータを取得する方法の模索
            //      SQLiteはTake()やFirst()等が使えない(SELECT TOPコマンドに対応していない)ため、
            //      Linqを使う場合は一度リスト化(全取得)してから限定するしかない様子
            var list = data.ToList();

            if(list.Count == 0)
            {
                return null;
            }
            else
            {
                int left = list.Count - skip;
                int count = (left <= take) ? left : take;
                int max = skip + count;
                Debug.Assert(max <= list.Count);

                UserScore[] scores = new UserScore[count];
                for(int i = 0; i < count; i++)
                {
                    int index = skip + i;
                    scores[i] = new UserScore(list[index]);
                }
                return scores;
            }
        }

        //static UserScore Find(DataContext context, int id)
        //{
        //    var table = context.GetTable<RowData>();
        //    var data = from x in table
        //                 where x.Id == id
        //                 select x;
        //    var list = data.ToList();
        //    if (list.Count == 0)
        //    {// 見つからなかった
        //        return null;
        //    }
        //    else
        //    {
        //        return new UserScore(list.First());
        //    }
        //}

        static RawData Insert(DataContext context, RawData score)
        {
            Table<RawData> table = context.GetTable<RawData>();
            try
            {// レコードの挿入
                table.InsertOnSubmit(score);
                context.SubmitChanges();

                var list = table.Where(item => item.UserId == score.UserId).OrderByDescending(item => item.Id).ToList();
                if (list.Count == 0)
                {
                    return null;
                }
                return list.First();
            }
            catch (Exception e)
            {// 挿入に失敗した
                Console.WriteLine(e);
                //                    throw;
            }
            return null;
        }
        UserScore()
        {

        }
        UserScore(RawData raw)
        {
            Raw = raw;
        }

        public UserScore(User user)
        {
            Debug.Assert(user != null && user.Id != null);

            Raw.UserId = user.Id.Value;
        }

        public int Rank(DataContext context)
        {
            Table<RawData> user_scores = context.GetTable<RawData>();
            var data = user_scores.OrderByDescending(item => item.Point);
            var list = data.ToList();
            int order = 0;
            // TODO: 全検索以外の方法を考える
            foreach(var record in list)
            {
                if(record.UserId == Raw.UserId && record.Point == Raw.Point)
                {
                    return order;
                }
                order++;
            }
            return -1;
        }

        public bool Commit(SQLiteConnection connection)
        {
            Debug.Assert(connection != null);
            Debug.Assert(connection.State == System.Data.ConnectionState.Open);
            if (Raw.Id == null)
            {
                using (var context = new DataContext(connection))
                {
                    var raw = Insert(context, Raw);
                    if (raw != null)
                    {
                        Raw = raw;
                        return true;
                    }
                }
            }
            else
            {// TODO:現状Updateが実行される予定はないが、一応作っておく
                using (var context = new DataContext(connection))
                {
                    var table = context.GetTable<RawData>();
                    var data = from x in table
                               where x.Id == Raw.Id
                               select x;
                    var list = data.ToList();
                    if(list.Count > 0)
                    {
                        var score = list.First();
                        score.Copy(Raw);
                        context.SubmitChanges();
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
