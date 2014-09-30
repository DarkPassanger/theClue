using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Controls;

namespace Server
{
    [DataContract]
    public class CommDataObject
    {
        private int _playerPositionX;
        [DataMember]
        public int playerPositionX
        {
            set { _playerPositionX = value; }
            get { return _playerPositionX; }
        }

        private int _playerPositionY;
        [DataMember]
        public int playerPositionY
        {
            set { _playerPositionY = value; }
            get { return _playerPositionY; }
        }

        private int _clientHandle;
        [DataMember]
        public int clientHandle
        {
            set { _clientHandle = value; }
            get { return _clientHandle; }
        }

        private bool _initialPosition;
        [DataMember]
        public bool initialPosition
        {
            set { _initialPosition = value; }
            get { return _initialPosition; }
        }

        //////////////////////////////
        private bool _initialize;
        [DataMember]
        public bool initialize
        {
            set { _initialize = value; }
            get { return _initialize; }
        }

        private int _numOfPlayers;
        [DataMember]
        public int numOfPlayers
        {
            set { _numOfPlayers = value; }
            get { return _numOfPlayers; }
        }
        //////////////////////////////

        private int _playerID;
        [DataMember]
        public int playerID
        {
            set { _playerID = value; }
            get { return _playerID; }
        }

    }
}
