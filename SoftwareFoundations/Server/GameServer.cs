using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Documents;

namespace Server
{

    public class Message : EventArgs
    {

        private string _statusMessage;

        public string statusMessage
        {
            set { _statusMessage = value; }
            get { return this._statusMessage; }
        }
    }

    public class GameServer
    {
        //player list
        private GamePlayer Player_1;
        private GamePlayer Player_2;
        private GamePlayer Player_3;
        private GamePlayer Player_4;
        private GamePlayer Player_5;
        private GamePlayer Player_6;

        //need this event for GUI
        public event Handler outMessage;
        public EventArgs e = null;
        public delegate void Handler(GameServer gs, Message m);
        ///////////////////////////////////////////


        private TcpListener tcpListener;
        private Thread listenThread;

        ASCIIEncoding encoder = new ASCIIEncoding();
        Message serverMessage = new Message();

        public static List<NetworkStream> NetworkStreams = new List<NetworkStream>();

        public static Dictionary<int, NetworkStream> tcpClientList = new Dictionary<int, NetworkStream>();
        public static Dictionary<int, CommDataObject> playerList = new Dictionary<int, CommDataObject>();

        List<GamePlayer> gamePlayerList = new List<GamePlayer>();

        public static string SerializeJSon<T>(T t)
        {
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(T));
            ds.WriteObject(stream, t);
            string jsonString = Encoding.UTF8.GetString(stream.ToArray());
            stream.Close();
            return jsonString;
        }
        
        /// <summary>
        /// Constructor - initialize all players and room locations
        /// </summary>
        public GameServer()
        {
            Room.init();

            //initialize all players 
            Player_1 = new GamePlayer();
            Player_1.isAlive = true;
            Player_1.Coordinates = Room.Study;
            Player_1.PlayerID = 1;
            gamePlayerList.Add(Player_1);

            Player_2 = new GamePlayer();
            Player_2.isAlive = true;
            Player_2.Coordinates = Room.Kitchen;
            Player_2.PlayerID = 2;
            gamePlayerList.Add(Player_2);

            Player_3 = new GamePlayer();
            Player_3.isAlive = true;
            Player_3.Coordinates = Room.Hall;
            Player_3.PlayerID = 3;
            gamePlayerList.Add(Player_3);

            Player_4 = new GamePlayer();
            Player_4.isAlive = true;
            Player_4.Coordinates = Room.Conservatory;
            Player_4.PlayerID = 4;
            gamePlayerList.Add(Player_4);

            Player_5 = new GamePlayer();
            Player_5.isAlive = true;
            Player_5.Coordinates = Room.Conservatory;
            Player_5.PlayerID = 5;
            gamePlayerList.Add(Player_5);

            Player_6 = new GamePlayer();
            Player_6.isAlive = true;
            Player_6.Coordinates = Room.Lounge;
            Player_6.PlayerID = 6;
            gamePlayerList.Add(Player_6);
        }

        public void startServer()
        {
            this.tcpListener = new TcpListener(IPAddress.Any, 3000);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
            serverMessage = new Message();
            serverMessage.statusMessage = "Ready, waiting for connections...\n";
            outMessage(this, serverMessage);
        }

        public void stopServer()
        {
            tcpListener.Stop();
            listenThread.Abort();
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();

                //create a thread to handle communication 
                //with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }

        private void HandleClientComm(object client)
        {
            string jsonString = "";

            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            //make a dictionary tcpClientList wtih value: clientStream, key: ClientHandle
            //the following will run every time a new client connects
            if (!tcpClientList.ContainsKey((int) tcpClient.Client.Handle))
            {
                tcpClientList.Add((int) tcpClient.Client.Handle, clientStream);

                //now send the info from all players to the clients
                //this will initialize all players at predefined positions

                foreach (GamePlayer gamePlayer in gamePlayerList)
                {
                    CommDataObject playerClient = new CommDataObject();
                    playerClient.gamePlayer = gamePlayer;

                    MemoryStream stream = new MemoryStream();
                    DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(CommDataObject));
                    jsonSer.WriteObject(stream, playerClient);
                    stream.Position = 0;
                    StreamReader sr = new StreamReader(stream);
                    var json = sr.ReadToEnd();

                    byte[] buffer = encoder.GetBytes(json);
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();

                    Thread.Sleep(50);
                }

            }

            
            byte[] message = new byte[4096];
            int bytesRead;
            //so fire the event which will update the server GUI
            //could probably have done this with Dispatch
            serverMessage = new Message();
            serverMessage.statusMessage = "connected... client: " + tcpClient.Client.Handle + "\n";
            outMessage(this, serverMessage);
            /////////////////////////////////////////////////////

            while (true)
            {
                bytesRead = 0;
                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                //deserialize....
                //////////////////////////////////////////////////////////////
                var serializer = new JavaScriptSerializer();
                var jsonDeser = serializer.Deserialize<CommDataObject>(encoder.GetString(message, 0, bytesRead));
                CommDataObject receivedObject = (CommDataObject) jsonDeser;
                ///////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////
                // message out to server GUI
                //could we do Dispatch here, instead of custom event?
                serverMessage = new Message();
                serverMessage.statusMessage = "X:" + receivedObject.playerPositionX + " , Y:" +
                                                  receivedObject.playerPositionY + " ID: " + receivedObject.playerID;
                outMessage(this, serverMessage);
                ///////////////////////////////////////////////////////////////

//                //update positions for each client
//                playerList[receivedObject.playerID] = receivedObject;
//
//                if (message.Length > 0)
//                {
//                    //go througg all the clients
//                    foreach (KeyValuePair<int, NetworkStream> playerClient in tcpClientList)
//                    {
//                        //go through all the players
//                        foreach (KeyValuePair<int, CommDataObject> player in playerList)
//                        {
//                            
//                            //temporary fix --- BAD solution
//                            Thread.Sleep(5);
//                            
//                            MemoryStream stream = new MemoryStream();
//                            DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof (CommDataObject));
//                            jsonSer.WriteObject(stream, player.Value);
//                            stream.Position = 0;
//                            StreamReader sr = new StreamReader(stream);
//                            var json = sr.ReadToEnd();
//                            
//                            byte[] buffer = encoder.GetBytes(json);
//                            playerClient.Value.Write(buffer, 0, buffer.Length);
//                            playerClient.Value.Flush();
//                            
//                            Debug.Print("from server... " + playerClient.Key + " X: " + player.Value.playerPositionX +
//                                           " Y: " + player.Value.playerPositionY);
//                        }
//                    }
//                }
            }

            tcpClient.Close();
        }
    }
}
