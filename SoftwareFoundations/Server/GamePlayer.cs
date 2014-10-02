using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Server
{
    public class GamePlayer
    {
        private Canvas _playerCanvas;
        private int _gamePlayerID;
        private string _gamePlayerName;
        private bool _isAlive;
        private Coordinates _coordinates;

        public Canvas PlayerCanvas
        {
            set { _playerCanvas = value; }
            get { return _playerCanvas; }
        }

        public int PlayerID
        {
            set { _gamePlayerID = value; }
            get { return _gamePlayerID; }
        }

        public string PlayerName
        {
            set { _gamePlayerName = value; }
            get { return _gamePlayerName; }
        }

        public bool isAlive
        {
            set { _isAlive = value; }
            get { return _isAlive; }
        }

        public Coordinates Coordinates
        {
            set { _coordinates = value; }
            get { return _coordinates; }
        }


        public GamePlayer()
        {
            //nothing to initialize right now...
        }
    }

    public class Coordinates
    {
        private int _xPos;
        private int _yPos;

        public int xPos
        {
            set { _xPos = value; }
            get { return _xPos; }
        }

        public int yPos
        {
            set { _yPos = value; }
            get { return _yPos; }
        }

    }

}
