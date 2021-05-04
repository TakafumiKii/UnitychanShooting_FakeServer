using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace FakeServer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        //        Server
        public int PortNo { get; set; } = 40080;

        Network.MessageServer Server;
        DataBase.DataBaseManager DBManager = new DataBase.DataBaseManager();

        public ReadOnlyObservableCollection<Network.ClientData> ClientDataCollection { get { return Server.ClientDataCollection.ReadOnlyCollection; } }
        public ReadOnlyObservableCollection<Network.UserScoreRankData> UserScoreRankDataCollection { get { return Server.UserScoreRankDataCollection.ReadOnlyCollection; } }
        
        public MainWindow()
        {
            Server = new Network.MessageServer(DBManager);

            DataContext = this;
            InitializeComponent();

            Utility.ConsoleLog.SetTextBox(LogText);
            Startup();
        }

        public void Startup()
        {
            // データベースの起動
            DataBase.DataBaseManager.Description desc = new DataBase.DataBaseManager.Description();
            desc.DBName = "unitychan_shooting";
            DBManager.Initialize(desc);

            // 通信サーバーの起動
            Server.Start(PortNo);
        }

        //protected override void OnClosed(EventArgs e)
        //{
        //    Server.Stop();
        //}
        public void RestartServer()
        {
            Console.WriteLine("再起動処理開始");
            Server.Stop();
            Server.Start(PortNo);
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            RestartServer();
        }
    }
}
