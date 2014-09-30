
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using Server;


namespace SoftwareFoundations
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        TcpClient client = new TcpClient();
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3000);

        public static Dictionary<int, CommDataObject> playerListClient = new Dictionary<int, CommDataObject>();
        public int currentPlayerID = 0;
        public int commCount = 0;

        private TcpListener tcpListener;
        private CommDataObject receivedFromServer;

        public CommDataObject currentPlayer;
        public PositionCoordinates currentPlayerPosition;

        public static Dictionary<int , Canvas> playerList = new Dictionary<int, Canvas>();



        public MainWindow()
        {

            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);

            connectToServerButton.IsEnabled = false;

            //text box
            InfoTextBox.AcceptsReturn = true;
            InfoTextBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

            InfoTextBox.AppendText("Start...\n");

            tcpListener = new TcpListener(serverEndPoint);

        }

        private void connectToServerButton_Click(object sender, RoutedEventArgs e)
        {

            connectToServerButton.IsEnabled = false;
            bool server = true;

            try
            {
                client.Connect(serverEndPoint);

                // Create a thread to read data sent from the server.
                ThreadPool.QueueUserWorkItem(
                   delegate
                   {
                       Read();
                   });
            }
            catch (Exception ex)
            {
                server = false;
                MessageBox.Show("Unable to connect - no server \n" + ex.StackTrace, "Can't Connect", MessageBoxButton.OK);
            }

            if (client.Connected && server)
            {
                connectToServerButton.IsEnabled = false;
                InfoTextBox.AppendText("Connected: " + serverEndPoint.Address.ToString());
            }
            else if (server)
            {
                MessageBox.Show("Unable to connect", "Can't Connect", MessageBoxButton.OK);
            }
            else
            {
                //do nothing
            }

            connectToServerButton.IsEnabled = true;

        }

        private void Read()
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            byte[] buffer = new byte[4096];
            int bytesRead;
            while (true)
            {
                Monitor.Enter(this);
                var getClientStream = client.GetStream();
                bytesRead = getClientStream.Read(buffer, 0, 4096); ;
                Monitor.Exit(this);
                try
                {
                    string receivedStr = encoder.GetString(buffer, 0, bytesRead);

                    DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof (CommDataObject));
                    MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(receivedStr));
                    receivedFromServer = (CommDataObject) jsonSer.ReadObject(stream);

                    ///////////////////////message out to player GUI/////////////////////////////////
                    string temp = "X: " + receivedFromServer.playerPositionX + " Y: " +
                                  receivedFromServer.playerPositionY +
                                  " ID: " + receivedFromServer.playerID + "\n";
                    Dispatcher.Invoke((Action) (() => InfoTextBox.AppendText(temp)));
                    Dispatcher.Invoke((Action) (() => InfoTextBox.ScrollToEnd()));
                    ////////////////////////////////////////////////////////////////////////////////

                    ///////////////////////Current client/////////////////////////////////////
                    //get current client ID
                    /////////////////////////////////////////////////////////////////////////
                    if (receivedFromServer.initialize && currentPlayerID == 0)
                    {
                        currentPlayerID = receivedFromServer.playerID;
                        currentPlayer = receivedFromServer;
                    }
                    //////////////////////////////////////////////////////////////////////////

                    //////////////////////////////////////////////////////////////////////////
                    //make a local list of all players
                    /////////////////////////////////////////////////////////////////////////
                    if (!playerListClient.ContainsKey(receivedFromServer.playerID))
                    {
                        playerListClient.Add(receivedFromServer.playerID, receivedFromServer);
                    }
                    /////////////////////////////////////////////////////////////////////////
                    

                    playerListClient[receivedFromServer.playerID] = receivedFromServer;

                    Dispatcher.Invoke((Action) (() => updatePlayersList(receivedFromServer.playerID)));

                    //this initializes player position
                    Dispatcher.Invoke((Action) (() => playerPosition(receivedFromServer)));
                    commCount++;

                    //need to initialize other players here...
                    if (receivedFromServer.numOfPlayers > 1)
                    {
                        Dispatcher.Invoke((Action) (() => UpdateAllPlayersPositions()));
                        //MainCanvas.UpdateLayout();
                        commCount = 0;
                    }

                }
                catch (Exception exception)
                {
                    Debug.Print((exception.StackTrace));
                }

            }
        }

        /// <summary>
        /// this will associate each player id with canvas on the board
        /// </summary>
        public void updatePlayersList(int playerID)
        {
            if (!playerList.ContainsKey(playerID) && !playerList.ContainsValue(player1))
            {
                playerList.Add(playerID, player1);
            }
            else if (!playerList.ContainsKey(playerID) && !playerList.ContainsValue(player2))
            {
                playerList.Add(playerID, player2);
            }
            else if (!playerList.ContainsKey(playerID) && !playerList.ContainsValue(player3))
            {
                playerList.Add(playerID, player3);
            }
            else if (!playerList.ContainsKey(playerID) && !playerList.ContainsValue(player4))
            {
                playerList.Add(playerID, player4);
            }
            else if (!playerList.ContainsKey(playerID) && !playerList.ContainsValue(player5))
            {
                playerList.Add(playerID, player5);
            }
            else if (!playerList.ContainsKey(playerID) && !playerList.ContainsValue(player6))
            {
                playerList.Add(playerID, player6);
            }
            else
            {
                //do nothing...
            }
        }

        public void UpdateAllPlayersPositions()
        {

            foreach (KeyValuePair<int, CommDataObject> _player in playerListClient)
            {

                if (_player.Key == currentPlayerID)
                {
                    movePlayer(currentPlayerPosition);
                }
                else
                {
                    Canvas currentPlayerCanvas = playerList[_player.Key];
                    PositionCoordinates newPositionCoordinates = playerPosition(_player.Value, true);
                    movePlayer(newPositionCoordinates, currentPlayerCanvas);
                }

                MainCanvas.InvalidateVisual();
            }
        }

        #region Coordinates calculations

        private PositionCoordinates get_0_0_Coordinates()
        {
            Point topLeft = roomTopLeft.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)topLeft.X + (float)(roomBottomLeft.ActualWidth / 2);
            Coordinates.yPosition = (float)topLeft.Y + (float)(roomBottomLeft.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_1_0_Coordinates()
        {
            Point point = pathTopLeft.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathTopLeft.ActualHeight / 2);
            Coordinates.yPosition = (float)point.Y - (float)(pathTopLeft.ActualWidth / 2);

            return Coordinates;
        }

        private PositionCoordinates get_2_0_Coordinates()
        {
            Point point = roomLeft.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(roomLeft.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(roomLeft.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_3_0_Coordinates()
        {
            Point point = pathBottomLeft.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathBottomLeft.ActualHeight / 2);
            Coordinates.yPosition = (float)point.Y - (float)(pathBottomLeft.ActualWidth / 2);

            return Coordinates;
        }

        private PositionCoordinates get_4_0_Coordinates()
        {
            Point point = roomBottomLeft.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(roomBottomLeft.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(roomBottomLeft.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_0_1_Coordinates()
        {
            Point point = pathTopCenterLeft.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathTopCenterLeft.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(pathTopCenterLeft.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_2_1_Coordinates()
        {
            Point point = pathLeft.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathLeft.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(pathLeft.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_4_1_Coordinates()
        {
            Point point = pathBottomCenterLeft.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathBottomCenterLeft.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(pathBottomCenterLeft.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_0_2_Coordinates()
        {
            Point point = roomTopCenter.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(roomTopCenter.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(roomTopCenter.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_1_2_Coordinates()
        {
            Point point = pathTopCenter.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathTopCenter.ActualHeight / 2);
            Coordinates.yPosition = (float)point.Y - (float)(pathTopCenter.ActualWidth / 2);

            return Coordinates;
        }

        private PositionCoordinates get_2_2_Coordinates()
        {
            Point point = roomCenter.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(roomCenter.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(roomCenter.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_3_2_Coordinates()
        {
            Point point = pathBottomCenter.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathBottomCenter.ActualHeight / 2);
            Coordinates.yPosition = (float)point.Y - (float)(pathBottomCenter.ActualWidth / 2);

            return Coordinates;
        }

        private PositionCoordinates get_4_2_Coordinates()
        {
            Point point = roomBottomCenter.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(roomBottomCenter.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(roomBottomCenter.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_0_3_Coordinates()
        {
            Point point = pathTopCenterRight.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathTopCenterRight.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(pathTopCenterRight.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_2_3_Coordinates()
        {
            Point point = pathRight.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathRight.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(pathRight.ActualHeight / 2);

            return Coordinates;
        }


        private PositionCoordinates get_4_3_Coordinates()
        {
            Point point = pathBottomCenterRight.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathBottomCenterRight.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(pathBottomCenterRight.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_0_4_Coordinates()
        {
            Point point = roomTopRight.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(roomTopRight.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(roomTopRight.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_1_4_Coordinates()
        {
            Point point = pathTopRight.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathTopRight.ActualHeight / 2);
            Coordinates.yPosition = (float)point.Y - (float)(pathTopRight.ActualWidth / 2);

            return Coordinates;
        }

        private PositionCoordinates get_2_4_Coordinates()
        {
            Point point = roomRight.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(roomRight.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(roomRight.ActualHeight / 2);

            return Coordinates;
        }

        private PositionCoordinates get_3_4_Coordinates()
        {
            Point point =pathBottomRight.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(pathBottomRight.ActualHeight / 2);
            Coordinates.yPosition = (float)point.Y - (float)(pathBottomRight.ActualWidth / 2);

            return Coordinates;
        }

        private PositionCoordinates get_4_4_Coordinates()
        {
            Point point = roomBottomRight.TranslatePoint(new Point(0, 0), MainCanvas);

            PositionCoordinates Coordinates = new PositionCoordinates();
            Coordinates.xPosition = (float)point.X + (float)(roomBottomRight.ActualWidth / 2);
            Coordinates.yPosition = (float)point.Y + (float)(roomBottomRight.ActualHeight / 2);

            return Coordinates;
        }
        #endregion

        #region player position/coordinates translation

        private void playerPosition(CommDataObject receivedDataObject)
        {
            if (receivedDataObject.playerPositionX == 0 && receivedDataObject.playerPositionY == 0)
            {
                PositionCoordinates newCorCoordinates = get_0_0_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 0,0 \n");
            }
            else if (receivedDataObject.playerPositionX == 1 && receivedDataObject.playerPositionY == 0)
            {
                PositionCoordinates newCorCoordinates = get_1_0_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 1,0 \n");
            }
            else if (receivedDataObject.playerPositionX == 2 && receivedDataObject.playerPositionY == 0)
            {
                PositionCoordinates newCorCoordinates = get_2_0_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 2,0 \n");
            }
            else if (receivedDataObject.playerPositionX == 3 && receivedDataObject.playerPositionY == 0)
            {
                PositionCoordinates newCorCoordinates = get_3_0_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 3,0 \n");
            }
            else if (receivedDataObject.playerPositionX == 4 && receivedDataObject.playerPositionY == 0)
            {
                PositionCoordinates newCorCoordinates = get_4_0_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 4,0 \n");
            }
            else if (receivedDataObject.playerPositionX == 0 && receivedDataObject.playerPositionY == 1)
            {
                PositionCoordinates newCorCoordinates = get_0_1_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 0,1 \n");
            }
            else if (receivedDataObject.playerPositionX == 2 && receivedDataObject.playerPositionY == 1)
            {
                PositionCoordinates newCorCoordinates = get_2_1_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 2,1 \n");
            }
            else if (receivedDataObject.playerPositionX == 4 && receivedDataObject.playerPositionY == 1)
            {
                PositionCoordinates newCorCoordinates = get_4_1_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 2,1 \n");
            }
            else if (receivedDataObject.playerPositionX == 0 && receivedDataObject.playerPositionY == 2)
            {
                PositionCoordinates newCorCoordinates = get_0_2_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 2,0 \n");
            }
            else if (receivedDataObject.playerPositionX == 1 && receivedDataObject.playerPositionY == 2)
            {
                PositionCoordinates newCorCoordinates = get_1_2_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 2,0 \n");
            }
            else if (receivedDataObject.playerPositionX == 2 && receivedDataObject.playerPositionY == 2)
            {
                PositionCoordinates newCorCoordinates = get_2_2_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 2,2 \n");
            }
            else if (receivedDataObject.playerPositionX == 3 && receivedDataObject.playerPositionY == 2)
            {
                PositionCoordinates newCorCoordinates = get_3_2_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 3,2 \n");
            }
            else if (receivedDataObject.playerPositionX == 4 && receivedDataObject.playerPositionY == 2)
            {
                PositionCoordinates newCorCoordinates = get_4_2_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 4,2 \n");
            }
            else if (receivedDataObject.playerPositionX == 0 && receivedDataObject.playerPositionY == 3)
            {
                PositionCoordinates newCorCoordinates = get_0_3_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 0,3 \n");
            }
            else if (receivedDataObject.playerPositionX == 2 && receivedDataObject.playerPositionY == 3)
            {
                PositionCoordinates newCorCoordinates = get_2_3_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 2,3 \n");
            }
            else if (receivedDataObject.playerPositionX == 4 && receivedDataObject.playerPositionY == 3)
            {
                PositionCoordinates newCorCoordinates = get_4_3_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 4,3 \n");
            }
            else if (receivedDataObject.playerPositionX == 0 && receivedDataObject.playerPositionY == 4)
            {
                PositionCoordinates newCorCoordinates = get_0_4_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 0,4 \n");
            }
            else if (receivedDataObject.playerPositionX == 1 && receivedDataObject.playerPositionY == 4)
            {
                PositionCoordinates newCorCoordinates = get_1_4_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 1,4 \n");
            }
            else if (receivedDataObject.playerPositionX == 2 && receivedDataObject.playerPositionY == 4)
            {
                PositionCoordinates newCorCoordinates = get_2_4_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 2,4 \n");
            }
            else if (receivedDataObject.playerPositionX == 3 && receivedDataObject.playerPositionY == 4)
            {
                PositionCoordinates newCorCoordinates = get_3_4_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 3,4 \n");
            }
            else if (receivedDataObject.playerPositionX == 4 && receivedDataObject.playerPositionY == 4)
            {
                PositionCoordinates newCorCoordinates = get_4_4_Coordinates();
                movePlayer(newCorCoordinates);
                InfoTextBox.AppendText("initialize player position... 4,4 \n");
            }
            else
            {
                //do nothing...
            }
        }

        private PositionCoordinates playerPosition(CommDataObject receivedDataObject, bool withReturn)
        {
            if (receivedDataObject.playerPositionX == 0 && receivedDataObject.playerPositionY == 0)
            {
                PositionCoordinates newCorCoordinates = get_0_0_Coordinates();
                InfoTextBox.AppendText("initialize player position... 0,0 \n");
                return newCorCoordinates;

            }
            else if (receivedDataObject.playerPositionX == 1 && receivedDataObject.playerPositionY == 0)
            {
                PositionCoordinates newCorCoordinates = get_1_0_Coordinates();
                InfoTextBox.AppendText("initialize player position... 1,0 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 2 && receivedDataObject.playerPositionY == 0)
            {
                PositionCoordinates newCorCoordinates = get_2_0_Coordinates();
                InfoTextBox.AppendText("initialize player position... 2,0 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 3 && receivedDataObject.playerPositionY == 0)
            {
                PositionCoordinates newCorCoordinates = get_3_0_Coordinates();
                InfoTextBox.AppendText("initialize player position... 3,0 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 4 && receivedDataObject.playerPositionY == 0)
            {
                PositionCoordinates newCorCoordinates = get_4_0_Coordinates();
                InfoTextBox.AppendText("initialize player position... 4,0 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 0 && receivedDataObject.playerPositionY == 1)
            {
                PositionCoordinates newCorCoordinates = get_0_1_Coordinates();
                InfoTextBox.AppendText("initialize player position... 0,1 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 2 && receivedDataObject.playerPositionY == 1)
            {
                PositionCoordinates newCorCoordinates = get_2_1_Coordinates();
                InfoTextBox.AppendText("initialize player position... 2,1 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 4 && receivedDataObject.playerPositionY == 1)
            {
                PositionCoordinates newCorCoordinates = get_4_1_Coordinates();
                InfoTextBox.AppendText("initialize player position... 2,1 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 0 && receivedDataObject.playerPositionY == 2)
            {
                PositionCoordinates newCorCoordinates = get_0_2_Coordinates();
                InfoTextBox.AppendText("initialize player position... 2,0 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 1 && receivedDataObject.playerPositionY == 2)
            {
                PositionCoordinates newCorCoordinates = get_1_2_Coordinates();
                InfoTextBox.AppendText("initialize player position... 2,0 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 2 && receivedDataObject.playerPositionY == 2)
            {
                PositionCoordinates newCorCoordinates = get_2_2_Coordinates();
                InfoTextBox.AppendText("initialize player position... 2,2 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 3 && receivedDataObject.playerPositionY == 2)
            {
                PositionCoordinates newCorCoordinates = get_3_2_Coordinates();
                InfoTextBox.AppendText("initialize player position... 3,2 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 4 && receivedDataObject.playerPositionY == 2)
            {
                PositionCoordinates newCorCoordinates = get_4_2_Coordinates();
                InfoTextBox.AppendText("initialize player position... 4,2 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 0 && receivedDataObject.playerPositionY == 3)
            {
                PositionCoordinates newCorCoordinates = get_0_3_Coordinates();
                InfoTextBox.AppendText("initialize player position... 0,3 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 2 && receivedDataObject.playerPositionY == 3)
            {
                PositionCoordinates newCorCoordinates = get_2_3_Coordinates();
                InfoTextBox.AppendText("initialize player position... 2,3 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 4 && receivedDataObject.playerPositionY == 3)
            {
                PositionCoordinates newCorCoordinates = get_4_3_Coordinates();
                InfoTextBox.AppendText("initialize player position... 4,3 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 0 && receivedDataObject.playerPositionY == 4)
            {
                PositionCoordinates newCorCoordinates = get_0_4_Coordinates();
                InfoTextBox.AppendText("initialize player position... 0,4 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 1 && receivedDataObject.playerPositionY == 4)
            {
                PositionCoordinates newCorCoordinates = get_1_4_Coordinates();
                InfoTextBox.AppendText("initialize player position... 1,4 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 2 && receivedDataObject.playerPositionY == 4)
            {
                PositionCoordinates newCorCoordinates = get_2_4_Coordinates();
                InfoTextBox.AppendText("initialize player position... 2,4 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 3 && receivedDataObject.playerPositionY == 4)
            {
                PositionCoordinates newCorCoordinates = get_3_4_Coordinates();
                InfoTextBox.AppendText("initialize player position... 3,4 \n");
                return newCorCoordinates;
            }
            else if (receivedDataObject.playerPositionX == 4 && receivedDataObject.playerPositionY == 4)
            {
                PositionCoordinates newCorCoordinates = get_4_4_Coordinates();
                InfoTextBox.AppendText("initialize player position... 4,4 \n");
                return newCorCoordinates;
            }
            else
            {
                //do nothing...
                return null;
            }
        }

        #endregion

        private void movePlayer(PositionCoordinates newPosition)
        {
            if (newPosition == null)
            {
                return;
            }

            //can only move if the position is free

            //how do we check that?????

            //what is the current psotion

            //what was the past position???
            PositionCoordinates temp = null;
            if (temp == null)
            {
                Canvas.SetTop(player1, newPosition.yPosition - (float)(player1.ActualHeight / 2));
                Canvas.SetLeft(player1, newPosition.xPosition - (float)(player1.ActualWidth / 2));
                newPosition.occupied = true;
            }
        }

        private void movePlayer(PositionCoordinates newPosition, Canvas playerCanvas)
        {

            //can only move if the position is free

            //how do we check that?????

            //what is the current psotion

            //what was the past position???
            PositionCoordinates temp = null;
            if (temp == null)
            {
                Canvas.SetTop(playerCanvas, newPosition.yPosition - (float)(playerCanvas.ActualHeight / 2));
                Canvas.SetLeft(playerCanvas, newPosition.xPosition - (float)(playerCanvas.ActualWidth / 2));
                newPosition.occupied = true;
            }
        }
        ////////////////////////////////////////////////////////////////////


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //initializePlayerPosition();
            connectToServerButton.IsEnabled = true;
        }


        #region Mouse Down Events
        private void MouseDown_0_0(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_0_0_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            //CommDataObject newDataObject = new CommDataObject();
            currentPlayer.playerPositionX = 0;
            currentPlayer.playerPositionY = 0;

            statusToServer(currentPlayer);

        }

        private void MouseDown_1_0(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_1_0_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 1;
            currentPlayer.playerPositionY = 0;

            statusToServer(currentPlayer);
        }

        private void MouseDown_2_0(object sender, MouseButtonEventArgs e)
        {
            
            PositionCoordinates newCorCoordinates = get_2_0_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 2;
            currentPlayer.playerPositionY = 0;

            statusToServer(currentPlayer);

        }

        private void MouseDown_3_0(object sender, MouseButtonEventArgs e)
        {

            PositionCoordinates newCorCoordinates = get_3_0_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 3;
            currentPlayer.playerPositionY = 0;

            statusToServer(currentPlayer);
        }

        private void MouseDown_4_0(object sender, MouseButtonEventArgs e)
        {
            //need to check if this location is available, get all "occupied" locations from sever
            bool locationAvailable = true;

            if (locationAvailable)
            {
                PositionCoordinates newCorCoordinates = get_4_0_Coordinates();
                movePlayer(newCorCoordinates);
                currentPlayerPosition = newCorCoordinates;

                currentPlayer.playerPositionX = 4;
                currentPlayer.playerPositionY = 0;

                statusToServer(currentPlayer);
            }
        }

        private void Mousedown_2_1(object sender, MouseButtonEventArgs e)
        {

            PositionCoordinates newCorCoordinates = get_2_1_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 2;
            currentPlayer.playerPositionY = 1;

            statusToServer(currentPlayer);
        }

        private void MouseDown_0_1(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_0_1_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 0;
            currentPlayer.playerPositionY = 1;

            statusToServer(currentPlayer);
        }

        private void MouseDown_4_1(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_4_1_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 4;
            currentPlayer.playerPositionY = 1;

            statusToServer(currentPlayer);
        }

        private void MouseDown_0_2(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_0_2_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 0;
            currentPlayer.playerPositionY = 2;

            statusToServer(currentPlayer);
        }

        private void MouseDown_1_2(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_1_2_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 1;
            currentPlayer.playerPositionY = 2;

            statusToServer(currentPlayer);

        }

        private void MouseDown_2_2(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_2_2_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 2;
            currentPlayer.playerPositionY = 2;

            statusToServer(currentPlayer);
        }

        private void MouseDown_3_2(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_3_2_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 3;
            currentPlayer.playerPositionY = 2;

            statusToServer(currentPlayer);
        }

        private void MouseDown_4_2(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_4_2_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 4;
            currentPlayer.playerPositionY = 2;

            statusToServer(currentPlayer);
        }

        private void MouseDown_0_3(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_0_3_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 0;
            currentPlayer.playerPositionY = 3;

            statusToServer(currentPlayer);
        }

        private void MouseDown_2_3(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_2_3_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 2;
            currentPlayer.playerPositionY = 3;

            statusToServer(currentPlayer);
        }

        private void MouseDown_4_3(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_4_3_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 4;
            currentPlayer.playerPositionY = 3;

            statusToServer(currentPlayer);
        }

        private void MouseDown_0_4(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_0_4_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 0;
            currentPlayer.playerPositionY = 4;

            statusToServer(currentPlayer);
        }

        private void MouseDown_1_4(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_1_4_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 1;
            currentPlayer.playerPositionY = 4;

            statusToServer(currentPlayer);
        }

        private void MouseDown_2_4(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_2_4_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 2;
            currentPlayer.playerPositionY = 4;

            statusToServer(currentPlayer);
        }

        private void MouseDown_3_4(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_3_4_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 3;
            currentPlayer.playerPositionY = 4;

            statusToServer(currentPlayer);
        }

        private void MouseDown_4_4(object sender, MouseButtonEventArgs e)
        {
            PositionCoordinates newCorCoordinates = get_4_4_Coordinates();
            movePlayer(newCorCoordinates);
            currentPlayerPosition = newCorCoordinates;

            currentPlayer.playerPositionX = 4;
            currentPlayer.playerPositionY = 4;

            statusToServer(currentPlayer);
        }

        #endregion

        private void statusToServer(CommDataObject commDataObject)
        {
            if (client.Connected)
            {
                var json = new JavaScriptSerializer().Serialize(commDataObject);

                NetworkStream clientStream = client.GetStream();

                ASCIIEncoding encoder = new ASCIIEncoding();
                byte[] buffer = encoder.GetBytes(json);

                commDataObject.initialize = false;

                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();

            }
            else
            {
                MessageBox.Show(this, "Not Connected to server....", "Test", MessageBoxButton.OK);
            }
        }

        private void buttonDisconect_Click(object sender, RoutedEventArgs e)
        {
            InfoTextBox.AppendText("Disconecting...\n");
            InfoTextBox.ScrollToEnd();
        }

    }
}
