using System;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Diagnostics;


namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GameServer gameServer = new GameServer();

        public void Subscribe(GameServer gs)
        {
            gs.outMessage += new GameServer.Handler(HeardIt);
        }
        public void HeardIt(GameServer gs, Message m)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Dispatcher.Invoke((Action) (() => tbServerStatus.AppendText(m.statusMessage + "\n")));
                Dispatcher.Invoke((Action) (() => tbServerStatus.ScrollToEnd()));
            });
        }

        public MainWindow()
        {
            InitializeComponent();
            Subscribe(gameServer);     
 
            tbServerStatus.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
        }

        private void buttonStartServer_Click(object sender, RoutedEventArgs e)
        {
            gameServer.startServer();
        }

        private void buttonStopServer_Click(object sender, RoutedEventArgs e)
        {
            gameServer.stopServer();
        }

        private void ServerClosingEvent(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                gameServer.stopServer();
            }
            catch (Exception ex)
            {
                
            }
        }
    }
}
