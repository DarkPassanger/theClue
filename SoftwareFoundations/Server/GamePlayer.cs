using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Server
{
    class GamePlayer
    {
        private Canvas _playerCanvas;

        public Canvas PlayerCanvas
        {
            set { _playerCanvas = value; }
            get { return _playerCanvas; }
        }

        public GamePlayer()
        {
            //nothing to initialize right now...
        }

    }
}
