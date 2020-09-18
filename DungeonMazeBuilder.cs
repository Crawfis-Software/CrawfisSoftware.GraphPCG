using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Collections.Maze;
using System;
using System.Collections.Generic;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Maze builder that allows for rooms and passages
    /// </summary>
    /// <typeparam name="N">The type used for node labels</typeparam>
    /// <typeparam name="E">The type used for edge weights</typeparam>
    public class DungeonMazeBuilder<N, E> : MazeBuilderAbstract<N, E>
    {
        internal struct Room
        {
            public int minX;
            public int minY;
            public int width;
            public int height;
            public Room(int minX, int minY, int width, int height)
            {
                this.minX = minX;
                this.minY = minY;
                this.width = width;
                this.height = height;
            }
        }

        private List<Room> roomList = new List<Room>();

        /// <summary>
        /// Get or set the number of rooms to create
        /// </summary>
        public int NumberOfRooms { get; set; } = 2;

        /// <summary>
        /// Get or set the minimum room size to create
        /// </summary>
        public int MinRoomSize { get; set; } = 4;

        /// <summary>
        /// Get or set the maximum room size to create
        /// </summary>
        public int MaxRoomSize { get; set; } = 8;

        /// <summary>
        /// Get or set the outside wall buffer size
        /// </summary>
        public int RoomMoatSize { get; set; } = 1;

        /// <summary>
        /// Get or set whether the algorithm should add passages between various rooms
        /// </summary>
        public bool CreatePassages { get; set; } = true;

        /// <summary>
        /// Get or set the maximum number of attempts to place all rooms
        /// </summary>
        public int MaxNumberOfTrys { get; set; } = 1000000;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">The width of the desired maze</param>
        /// <param name="height">The height of the desired maze</param>
        /// <param name="nodeAccessor">A function to retrieve any node labels</param>
        /// <param name="edgeAccessor">A function to retrieve any edge weights</param>
        public DungeonMazeBuilder(int width, int height, GetGridLabel<N> nodeAccessor = null, GetEdgeLabel<E> edgeAccessor = null)
            : base(width, height, nodeAccessor, edgeAccessor)
        {
        }

        /// <inheritdoc/>
        public override void CreateMaze(bool preserveExistingCells)
        {
            MakeRooms();
            if (CreatePassages)
            {
                MakePassages();
            }
        }

        private void MakePassages()
        {
            for(int i=1; i < roomList.Count; i ++)
            {
                int room1CenterX = roomList[i - 1].minX + roomList[i - 1].width / 2;
                int room1CenterY = roomList[i - 1].minY + roomList[i - 1].height / 2;
                int room2CenterX = roomList[i].minX + roomList[i].width / 2;
                int room2CenterY = roomList[i].minY + roomList[i].height / 2;
                CarveHorizontalSpan(room1CenterY, room1CenterX, room2CenterX, false);
                CarveVerticalSpan(room2CenterX, room1CenterY, room2CenterY, false);
            }
        }

        private void MakeRooms()
        {
            CreateRandomRooms();
            foreach (Room room in roomList)
            {
                int lowerLeftIndex = room.minX + Width * room.minY;
                int upperRightIndex = lowerLeftIndex + (room.height - 1) * Width + (room.width - 1);
                CarveRoom(lowerLeftIndex, upperRightIndex);
            }
        }

        private void CarveRoom(int lowerLeftIndex, int upperRightIndex)
        {
            int left = lowerLeftIndex % Width;
            int right = upperRightIndex % Width;
            int bottom = lowerLeftIndex / Width;
            int top = upperRightIndex / Width;
            // Set corners
            directions[left, bottom] |= Direction.N | Direction.E;
            directions[left, top] |= Direction.S | Direction.E;
            directions[right, bottom] |= Direction.N | Direction.W;
            directions[right, top] |= Direction.S | Direction.W;
            //int i, j;
            for (int i = left + 1; i < right; i++)
            {
                directions[i, bottom] |= Direction.W | Direction.N | Direction.E;
                directions[i, top] |= Direction.W | Direction.E | Direction.S;
            }
            for (int j = bottom + 1; j < top; j++)
            {
                directions[left, j] |= Direction.N | Direction.E | Direction.S;
                directions[right, j] |= Direction.W | Direction.N | Direction.S;
            }
            for (int i = left + 1; i < right; i++)
            {
                for (int j = bottom + 1; j < top; j++)
                {
                    directions[i, j] |= Direction.W | Direction.N | Direction.E | Direction.S;
                }
            }
        }

        private void CreateRandomRooms()
        {
            int deltaWidth = MaxRoomSize - MinRoomSize + 1;
            // Random create rooms
            int roomTrys = 0;
            while (roomList.Count < this.NumberOfRooms && roomTrys < MaxNumberOfTrys)
            {
                int roomWidth = MinRoomSize + RandomGenerator.Next(deltaWidth);
                int roomHeight = MinRoomSize + RandomGenerator.Next(deltaWidth);
                int minimumXCoord = Width - roomWidth;
                int minumumYCoord = Height - roomHeight;
                int minX = RandomGenerator.Next(minimumXCoord);
                int minY = RandomGenerator.Next(minumumYCoord);
                Room room = new Room(minX, minY, roomWidth, roomHeight);
                roomTrys++;
                bool canPlace = CheckForOverlap(room);
                if (canPlace)
                {
                    roomList.Add(room);
                }
            }
        }

        private bool CheckForOverlap(Room room)
        {
            bool canPlace = true;
            foreach (Room placedRoom in roomList)
            {
                int distance = RoomDistance(placedRoom, room);
                // Ensure they are RoomMoatSize apart.
                if (distance-RoomMoatSize < 0)
                {
                    canPlace = false;
                    break;
                }
            }

            return canPlace;
        }

        private static int RoomDistance(Room room1, Room room2)
        {
            int xDistance = 0;
            int yDistance = 0;
            int x1 = room1.minX;
            int x2 = x1 + room1.width;
            int y1 = room1.minY;
            int y2 = y1 + room1.height;
            int u1 = room2.minX;
            int u2 = u1 + room2.width;
            int v1 = room2.minY;
            int v2 = v1 + room2.height;
            if (x2 < u1)
            {
                xDistance = u1 - x2;
            }
            else if (u2 < x1)
            {
                xDistance = x1 - u2;
            }
            if (y2 < v1)
            {
                yDistance = v1 - y2;
            }
            else if (v2 < y1)
            {
                yDistance = y1 - v2;
            }
            int distance =  xDistance + yDistance;
            if(distance == 0)
            {
                // Not entirely accurate if one is completely within the other
                distance = Math.Min(Math.Max(x1, u1) - Math.Max(x2, u2), Math.Max(y1, v1) - Math.Max(y2, v2));
            }
            return distance;
        }
    }
}
