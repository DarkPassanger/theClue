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

        public static int clientCount = 0;

        private TcpListener tcpListener;
        private Thread listenThread;

        public event Handler outMessage;
        public EventArgs e = null;
        public delegate void Handler(GameServer gs, Message m);

        ASCIIEncoding encoder = new ASCIIEncoding();
        Message serverMessage = new Message();

        public static List<NetworkStream> NetworkStreams = new List<NetworkStream>();

        public static Dictionary<int, NetworkStream> tcpClientList = new Dictionary<int, NetworkStream>();
        public static Dictionary<int, CommDataObject> playerList = new Dictionary<int, CommDataObject>();
        
        public GameServer()
        {
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

                TcpClient tcpClient = (TcpClient) client;
                NetworkStream clientStream = tcpClient.GetStream();

                if (!tcpClientList.ContainsKey((int) tcpClient.Client.Handle))
                {
                    tcpClientList.Add((int) tcpClient.Client.Handle, clientStream);

                    //only if player is been newly added set position
                    CommDataObject playerClient = new CommDataObject();
                    playerClient.playerID = (int) tcpClient.Client.Handle;
                    playerClient.playerPositionX = 0;
                    playerClient.playerPositionY = 0;
                    playerClient.initialize = true;
                    playerClient.numOfPlayers = tcpClientList.Count;

                    //also add each player to a dictionary
                    if (!playerList.ContainsKey(playerClient.playerID))
                    {
                        playerList.Add(playerClient.playerID, playerClient);
                    }

                    MemoryStream stream = new MemoryStream();
                    DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof (CommDataObject));
                    jsonSer.WriteObject(stream, playerClient);
                    stream.Position = 0;
                    StreamReader sr = new StreamReader(stream);
                    var JSON = sr.ReadToEnd();

                    byte[] buffer2 = encoder.GetBytes(JSON);
                    clientStream.Write(buffer2, 0, buffer2.Length);
                    clientStream.Flush();

                }

                byte[] message = new byte[4096];
                int bytesRead;

                //message has successfully been received
                serverMessage = new Message();
                serverMessage.statusMessage = "connected... client: " + tcpClient.Client.Handle + "\n";
                clientCount++;
                Debug.Print(clientCount.ToString());
                outMessage(this, serverMessage);

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

                    //deserialize
                    var serializer = new JavaScriptSerializer();
                    var jsonDeser = serializer.Deserialize<CommDataObject>(encoder.GetString(message, 0, bytesRead));
                    CommDataObject receivedObject = (CommDataObject) jsonDeser;
                    serverMessage = new Message();
                    serverMessage.statusMessage = "X:" + receivedObject.playerPositionX + " , Y:" +
                                                  receivedObject.playerPositionY + " ID: " + receivedObject.playerID;
                    outMessage(this, serverMessage);

                    //update positions for each client
                    playerList[receivedObject.playerID] = receivedObject;


                    if (message.Length > 0)
                    {
                        //go througg all the clients
                        foreach (KeyValuePair<int, NetworkStream> playerClient in tcpClientList)
                        {
                            Debug.Print("--------------------------------------");

                            foreach (KeyValuePair<int, CommDataObject> player in playerList)
                            {

                                MemoryStream stream = new MemoryStream();
                                DataContractJsonSerializer jsonSer =
                                    new DataContractJsonSerializer(typeof (CommDataObject));
                                jsonSer.WriteObject(stream, player.Value);
                                stream.Position = 0;
                                StreamReader sr = new StreamReader(stream);
                                var json = sr.ReadToEnd();

                                ///////////////////////////////////////////////////
                                Debug.Print("JSON: " + json.ToString());
                                serverMessage = new Message();
                                serverMessage.statusMessage = "\nJSON:" + json.ToString() + "\n";
                                outMessage(this, serverMessage);
                                ///////////////////////////////////////////////////


                                byte[] buffer = encoder.GetBytes(json);

                                playerClient.Value.Write(buffer, 0, buffer.Length);
                                playerClient.Value.Flush();

                                Debug.Print("from server... " + playerClient.Key + " X: " + player.Value.playerPositionX +
                                            " Y: " + player.Value.playerPositionY);

                            }

                            Debug.Print("--------------------------------------");
                        }
                    }
                }

                tcpClient.Close();

        }
    }
}
