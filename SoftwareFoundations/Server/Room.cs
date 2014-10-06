using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server
{
    public static class Room
    {
        public static Coordinates Study;
        public static Coordinates Kitchen;
        public static Coordinates Hall;
        public static Coordinates Conservatory;
        public static Coordinates Lounge;
        public static Coordinates BallRoom;
        public static Coordinates DinningRoom;
        public static Coordinates Library;
        public static Coordinates BilliardRoom;

        public static void init()
        {
            Study = new Coordinates();
            Study.xPos = 0;
            Study.yPos = 0;

            Kitchen = new Coordinates();
            Kitchen.xPos = 2;
            Kitchen.yPos = 0;

            Hall = new Coordinates();
            Hall.xPos = 4;
            Hall.yPos = 0;

            Conservatory = new Coordinates();
            Conservatory.xPos = 0;
            Conservatory.yPos = 2;

            Lounge = new Coordinates();
            Lounge.xPos = 2;
            Lounge.yPos = 2;

            BallRoom = new Coordinates();
            BallRoom.xPos = 4;
            BallRoom.yPos = 2;

            DinningRoom = new Coordinates();
            DinningRoom.xPos = 0;
            DinningRoom.yPos = 4;

            Library = new Coordinates();
            Library.xPos = 2;
            Library.yPos = 4;

            BilliardRoom = new Coordinates();
            BilliardRoom.xPos = 4;
            BilliardRoom.yPos = 4;
        }
    }
}
