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

        private List<Room> roomList = new List<Room>();
        private int minRoomDistance = 2;
        private int maxNumberOfTrys = 100000;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">The width of the desired maze</param>
        /// <param name="height">The height of the desired maze</param>
        /// <param name="nodeAccessor">A function to retrieve any node labels</param>
        /// <param name="edgeAccessor">A function to retrieve any edge weights</param>
        public DungeonMazeBuilder(int width, int height, GetGridLabel<N> nodeAccessor, GetEdgeLabel<E> edgeAccessor)
            : base(width, height, nodeAccessor, edgeAccessor)
        {
            this.Width = width;
            this.Height = height;
        }

        /// <inheritdoc/>
        public override void CreateMaze(bool preserveExistingCells)
        {
            if (!preserveExistingCells)
            {
                Clear();
            }
            MakeRooms();
            MakePassages();
        }

        private void MakePassages()
        {
            //throw new NotImplementedException();
        }

        private void MakeRooms()
        {
            CreateRandomRooms();
            foreach (Room room in roomList)
            {
                int lowerLeftIndex = room.minX + Width * room.minY;
                int upperRightIndex = lowerLeftIndex + (room.height - 1) * Width + (room.width - 1);
                Console.WriteLine("Creating Room {0}, {1}  to {2}, {3}", lowerLeftIndex % Width, lowerLeftIndex / Width, upperRightIndex % Width, upperRightIndex / Width);

                MakeRoom(lowerLeftIndex, upperRightIndex);
            }
            MoveRoomsToCircle();
        }

        private void MakeRoom(int lowerLeftIndex, int upperRightIndex)
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
            int minimumXCoord = Width - MaxRoomSize;
            int minumumYCoord = Height - MaxRoomSize;
            // Random create rooms
            int roomTrys = 0;
            while (roomList.Count < this.NumberOfRooms && roomTrys < maxNumberOfTrys)
            {
                int roomWidth = MinRoomSize + RandomGenerator.Next(deltaWidth);
                int roomHeight = MinRoomSize + RandomGenerator.Next(deltaWidth);
                int minX = RandomGenerator.Next(minimumXCoord);
                int minY = RandomGenerator.Next(minumumYCoord);
                Room room = new Room(minX, minY, roomWidth, roomHeight);
                roomTrys++;
                // Ensure they are minRoomDistance apart.
                int minDistance = Width + Height;
                foreach (Room placedRoom in roomList)
                {
                    int distance = RoomDistance(placedRoom, room);
                    if (distance < minRoomDistance)
                    {
                        // Move Room
                        minDistance = distance;
                    }
                }
                //if (minDistance > minRoomDistance)
                {
                    roomList.Add(room);
                }
            }
        }

        private void MoveRoomsForceImpule()
        {

        }
        private void MoveRoomsToCircle()
        {
            // Ignore Room position and only use the width and height
            int count = roomList.Count;
            float deltaRadians = 2.0f * (float)Math.PI / (float)count;
            int minRadius = 3 * MaxRoomSize;

            List<Room> newList = new List<Room>(roomList.Count);
            float angle = (float) RandomGenerator.NextDouble();
            foreach(Room room in roomList)
            {
                float deltaX = (float)Math.Cos(angle);
                float deltaY = (float)Math.Sin(angle);
                int xEnd = Width / 2 - room.width - 1;
                int xStart = Math.Min(minRadius, xEnd);
                int newX = (int)(deltaX * RandomGenerator.Next(xStart, xEnd));
                int yEnd = Height / 2 - room.height - 1;
                int yStart = Math.Min(minRadius, xEnd);
                int newY = (int)(deltaY * RandomGenerator.Next(xStart, xEnd));
                angle += deltaRadians;
                Room newRoom = new Room(newX, newY, room.width, room.height);
                newList.Add(newRoom);
            }
            roomList = newList;
        }
        //public static Room NodeValues(int i, int j)
        //{
        //    return null;
        //}
        //public static int EdgeValues(int i, int j, Direction dir)
        //{
        //    return 1;
        //}

        private static int RoomDistance(Room room1, Room room2)
        {
            int xDistance = 0;
            int yDistance = 0;
            int x1 = room1.minX;
            int x2 = x1 + room1.width;
            int y1 = room2.minY;
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
            return xDistance + yDistance;
        }
    }
}
