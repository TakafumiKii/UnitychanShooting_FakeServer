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

    public class User
    {
        [Table(Name = "users")]
        class RawData
        {
            [Column(Name = "id", IsPrimaryKey = true)]
            public int? Id { get; private set; } = null;    //  AutoIncrementの為にnullにしておく
            [Column(Name = "sereal", CanBeNull = false)]
            public int Sereal { get; internal set; }

            [Column(Name = "name", CanBeNull = false)]
            public string Name { get; set; } = "";

            //  簡略化のため実装見送り
            //[Column(Name = "password", CanBeNull = false)]    
            //public string Password { get; set; } = "";

            //[Column(Name = "login_num", CanBeNull = false)]
            //public int LoginNum { get; set; }
            //[Column(Name = "updated_at", CanBeNull = false)]
            //public DateTime UpdatedAt { get; set; }
            internal void Copy(RawData raw)
            {
                Name = raw.Name;
            }
        }

        RawData Raw = new RawData();

        public int? Id { get { return Raw.Id; } }
        public string Name { get { return Raw.Name; } set { Raw.Name = value; } }
        public int Sereal { get { return Raw.Sereal; } }

        internal static void CreateTable(SQLiteConnection connection)
        {
            Debug.Assert(connection != null);
            Debug.Assert(connection.State == System.Data.ConnectionState.Open);
            // コマンドの実行
            using (var command = connection.CreateCommand())
            {
                // ※各テーブルデータについて
                //  本来はmigrationでテーブル管理しますが、今回はテーブルやカラムの存在チェックで簡易的に設定しています。

                command.CommandText = "CREATE TABLE IF NOT EXISTS users (" +
                    "  id INTEGER PRIMARY KEY" +
                    "  , sereal INTEGER NOT NULL UNIQUE" +
                    "  , name TEXT NOT NULL" +
//                    "  , login_num INTEGER NOT NULL" +
//                    "  , updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP" +
                    ")";
                command.ExecuteNonQuery();


                command.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS user_sereal_unique ON users(sereal)";
                command.ExecuteNonQuery();
            }
        }

        public User()
        {
        }
        User(RawData raw)
        {
            Raw = raw;
        }

        public static int Count(DataContext context)
        {
            Debug.Assert(context != null);
            Table<RawData> users = context.GetTable<RawData>();
            return users.Count();
        }

        public static User Find(DataContext context, int sereal)
        {
            Debug.Assert(context != null);
            Table<RawData> users = context.GetTable<RawData>();
            return Find(users, sereal);
        }
        static User Find(Table<RawData> table, int sereal)
        {
            var data = from x in table
                       where x.Sereal == sereal
                       select x;
            //                    var test = users.Where(x => x.Sereal == Sereal).Select(x => x);

            //  Todo:レコードの取得数を制限し、該当クラスにマッピングしたデータを取得する方法の模索
            //      SQLiteはTake()やFirst()等が使えない(SELECT TOPコマンドに対応していない)ため、
            //      Linqを使う場合は一度リスト化(全取得)してから限定するしかない様子
            var list = data.ToList();
            if (list.Count == 0)
            {// 見つからなかった
                return null;
            }
            else
            {
                return new User(list.First());
            }
        }
        public static User FindByUserId(DataContext context, int userId)
        {
            Debug.Assert(context != null);
            Table<RawData> users = context.GetTable<RawData>();
            return FindByUserId(users, userId);
        }
        static User FindByUserId(Table<RawData> table, int userId)
        {
            var data = from x in table
                       where x.Id == userId
                       select x;
            //                    var test = users.Where(x => x.Sereal == Sereal).Select(x => x);

            //  Todo:レコードの取得数を制限し、該当クラスにマッピングしたデータを取得する方法の模索
            //      SQLiteはTake()やFirst()等が使えない(SELECT TOPコマンドに対応していない)ため、
            //      Linqを使う場合は一度リスト化(全取得)してから限定するしかない様子
            var list = data.ToList();
            if (list.Count == 0)
            {// 見つからなかった
                return null;
            }
            else
            {
                return new User(list.First());
            }
        }

        static RawData Insert(DataContext context, RawData raw)
        {
            Debug.Assert(context != null);

            Table<RawData> table = context.GetTable<RawData>();

            Random rand = new Random();
            const int LOOP_MAX = 10;
            int loop = 0;
            while (true)
            {
                // ユニークなのでシリアルが被るのは困る
                int sereal = rand.Next();
                User target = Find(table, sereal);

                if (target == null)
                {// 該当なし
                    raw.Sereal = sereal;
                    break;
                }
                if (++loop >= LOOP_MAX)
                {// 再試行回数制限
                    return null;
                }
            }

            try
            {// レコードの挿入
                table.InsertOnSubmit(raw);
                context.SubmitChanges();
                var list = table.Where(item => item.Sereal == raw.Sereal).ToList();
                if(list.Count == 0)
                {
                    return null;
                }
                return list.First();
            }
            catch (Exception e)
            {// 挿入に失敗した
                Console.WriteLine(e);
                //                    throw;
                return null;
            }
        }

        //       public bool Commit(SQLiteConnection connection)

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
            {// データの更新
                using (var context = new DataContext(connection))
                {
                    //  TODO:無駄がある気がするので、Update文を直接書いて高速化を検討する
                    var user = Find(context, Raw.Sereal);
                    user.Raw.Copy(Raw);

                    context.SubmitChanges();
                }
                return true;
            }
            return false;
        }
    }
}
