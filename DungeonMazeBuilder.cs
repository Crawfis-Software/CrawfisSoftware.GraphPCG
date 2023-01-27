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
        /// Defined algorithms for connecting two rooms.
        /// </summary>
        public enum PassageRasterizerType { 
            /// <summary>
            /// No algorithm selected.
            /// </summary>
            Unspecified,
            /// <summary>
            /// A simple path with one turn.
            /// </summary>
            Elbow,
            /// <summary>
            /// Determine the shortest path between the rooms using edge weights.
            /// </summary>
            ShortestPathBetweenRooms,
            /// <summary>
            /// Shortest path taking advantage of existing paths.
            /// </summary>
            ShortestPathUsingExisting,
            /// <summary>
            /// Use a random walk
            /// </summary>
            RandomWalk };

        private int NumberOfRasterizers = 5;

        private List<Room> roomList = new List<Room>();
        SourceShortestPaths<N, E> pathGenerator;
        List<Tuple<int, int, float>> pathLengthsFromSource;
        List<Tuple<int, int, PassageRasterizerType>> roomConnections = new List<Tuple<int, int, PassageRasterizerType>>();

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
        /// Get or set the algorithm to use when carving a passage with PassageRasterizerType.Unspecified
        /// </summary>
        public PassageRasterizerType DefaultPassageRasterizer { get; set; } = PassageRasterizerType.Elbow;

        /// <summary>
        /// Get or set the maximum number of attempts to place all rooms
        /// </summary>
        public int MaxNumberOfTrys { get; set; } = 1000000;
        
        /// <summary>
        /// Get or set a cost associated with carving a wall
        /// </summary>
        public float wallCarveCost { get; set; } = 20.2f;

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

        /// <summary>
        /// Copy Constructor for MazeBuilderAbstract classes.
        /// </summary>
        /// <param name="mazeBuilder"></param>
        public DungeonMazeBuilder(MazeBuilderAbstract<N,E> mazeBuilder)
            : base(mazeBuilder)
        {
        }

        /// <inheritdoc/>
        public override void CreateMaze(bool preserveExistingCells)
        {
            //MakeRooms();
            //if (CreatePassages)
            //{
            //    MakePassages3();
            //}
        }

        /// <summary>
        /// Create a room explicitly at the specified location with the specified size.
        /// </summary>
        /// <param name="minX">Lowerleft x coordinate.</param>
        /// <param name="minY">Lower-left y coordinate.</param>
        /// <param name="roomWidth">Width of the room.</param>
        /// <param name="roomHeight">Height of the room.</param>
        /// <returns>An id for the room.</returns>
        public int AddRoom(int minX, int minY, int roomWidth, int roomHeight)
        {
            Room room = new Room(minX, minY, roomWidth, roomHeight);
            roomList.Add(room);
            return roomList.Count - 1;
        }

        /// <summary>
        /// Associate a passage between tow rooms
        /// </summary>
        /// <param name="room1">The id of the first room.</param>
        /// <param name="room2">The id of the second room.</param>
        /// <param name="passageType">A suggestion on how the path should be constructed. The default is Unspecified.</param>
        public void AddConnection(int room1, int room2, PassageRasterizerType passageType = PassageRasterizerType.Unspecified)
        {
            roomConnections.Add(new Tuple<int, int, PassageRasterizerType>(room1, room2, passageType));
        }

        /// <summary>
        /// Add passageways between rooms in the order they were generated.
        /// </summary>
        /// <param name="passageType">A suggestion on how the path should be constructed. The default is Unspecified.</param>
        public void MakeSequentialRoomConnections(PassageRasterizerType passageType = PassageRasterizerType.Unspecified)
        {
            for (int i = 1; i < roomList.Count; i++)
            {
                AddConnection(i-1, i, passageType);
            }
        }

        /// <summary>
        /// Finds the rooms with the closest center points and adds passageways between them.
        /// </summary>
        /// <param name="maxNumberOfConnections">The maximum number of room connections to make.</param>
        /// <param name="passageType">A suggestion on how the path should be constructed. The default is ShortestPathUsingExisting.</param>
        public void MakeClosestPathConnections(int maxNumberOfConnections = int.MaxValue, PassageRasterizerType passageType = PassageRasterizerType.ShortestPathUsingExisting)
        {
            // Would need to build a graph for the below. Which would require path costs.
            //var shortestPaths = new AllPairsShortestPath<N, E>(grid, edgeCost);
            int numberCreated = 0;
            var allPathLengths = new List<Tuple<int, int, float>>(roomList.Count*roomList.Count/2);
            for (int roomIndex = 0; roomIndex < roomList.Count; roomIndex++)
            {
                int sourceCenterX = roomList[roomIndex].minX + roomList[roomIndex].width / 2;
                int sourceCenterY = roomList[roomIndex].minY + roomList[roomIndex].height / 2;
                int centerIndex = sourceCenterX + sourceCenterY * Width;
                pathGenerator = new SourceShortestPaths<N, E>(grid, centerIndex, edgeCost);
                for (int i = roomIndex + 1; i < roomList.Count; i++)
                {
                    int room1CenterX = roomList[i].minX + roomList[i].width / 2;
                    int room1CenterY = roomList[i].minY + roomList[i].height / 2;
                    int targetIndex = room1CenterX + room1CenterY * Width;
                    float pathLength = pathGenerator.GetCost(targetIndex);
                    allPathLengths.Add(new Tuple<int, int, float>(roomIndex, i, pathLength));
                }
            }
            allPathLengths.Sort(TupleComparer);
            foreach (var path in allPathLengths)
            {
                numberCreated++;
                if (numberCreated > maxNumberOfConnections)
                    break;
                AddConnection(path.Item1, path.Item2, passageType);
            }
        }

        /// <summary>
        /// Precompute all of the path lengths from a certain room.Required for CarveFurthestPassages
        /// </summary>
        /// <param name="roomIndex">The room id that the paths to all other rooms should be computed from.</param>
        /// <remarks>Useful if you have a center room and want to compute paths to all other rooms to determine connections.</remarks>
        public void ComputePathLengthsFromSource(int roomIndex)
        {
            int sourceCenterX = roomList[roomIndex].minX + roomList[roomIndex].width / 2;
            int sourceCenterY = roomList[roomIndex].minY + roomList[roomIndex].height / 2;
            int centerIndex = sourceCenterX + sourceCenterY * Width;
            pathGenerator = new SourceShortestPaths<N, E>(grid, centerIndex, edgeCost);
            pathLengthsFromSource = new List<Tuple<int, int, float>>(roomList.Count);
            for (int i = 0; i < roomList.Count; i++)
            {
                if (i == roomIndex) continue;
                int room1CenterX = roomList[i].minX + roomList[i].width / 2;
                int room1CenterY = roomList[i].minY + roomList[i].height / 2;
                int targetIndex = room1CenterX + room1CenterY * Width;
                float pathLength = 0;
                foreach (var cell in pathGenerator.GetPath(targetIndex))
                {
                    //CarvePassage(cell.From, cell.To);
                    pathLength += edgeCost(cell);
                }
                pathLengthsFromSource.Add(new Tuple<int, int, float>(roomIndex, i, pathLength));
            }
        }

        /// <summary>
        /// Actually creates the passages in the underlying grid maze.
        /// </summary>
        public void CarveAllPassages()
        {
            foreach(var edge in roomConnections)
            {
                PassageRasterizerType rasterizer = edge.Item3;
                if(edge.Item3 == PassageRasterizerType.Unspecified)
                {
                    int randomRasterizer = RandomGenerator.Next(NumberOfRasterizers) + 1;
                    rasterizer = (PassageRasterizerType)randomRasterizer;
                }
                switch(rasterizer)
                {
                    case PassageRasterizerType.Elbow:
                        CarveElbowPassage(edge.Item1, edge.Item2);
                        break;
                    case PassageRasterizerType.ShortestPathBetweenRooms:
                        CarvePathFromSource(edge.Item2);
                        break;
                    case PassageRasterizerType.ShortestPathUsingExisting:
                        CarveCurrentShortestPath(edge.Item1, edge.Item2);
                        break;
                }
            }
        }

        private void CarvePathFromSource(int targetRoom)
        {
            if (pathGenerator == null || targetRoom < 0 || targetRoom >= roomList.Count)
                throw new InvalidOperationException("Target room index is out of bounds or ComputePathLengthsFromSource has not been called");
            int room1CenterX = roomList[targetRoom].minX + roomList[targetRoom].width / 2;
            int room1CenterY = roomList[targetRoom].minY + roomList[targetRoom].height / 2;
            int targetIndex = room1CenterX + room1CenterY * Width;
            foreach (var cell in pathGenerator.GetPath(targetIndex))
            {
                CarvePassage(cell.From, cell.To);
            }
        }

        private void CarveCurrentShortestPath(int room1, int room2)
        {
            int room1CenterX = roomList[room1].minX + roomList[room1].width / 2;
            int room1CenterY = roomList[room1].minY + roomList[room1].height / 2;
            int sourceIndex = room1CenterX + room1CenterY * Width;
            int room2CenterX = roomList[room2].minX + roomList[room2].width / 2;
            int room2CenterY = roomList[room2].minY + roomList[room2].height / 2;
            int targetIndex = room2CenterX + room2CenterY * Width;
            foreach (var cell in PathQuery<N, E>.FindPath(grid, sourceIndex, targetIndex, edgeCost))
            {
                CarvePassage(cell.From, cell.To);
            }
        }

        private void CarveElbowPassage(int roomIndex1, int roomIndex2)
        {
            int room1CenterX = roomList[roomIndex1].minX + roomList[roomIndex1].width / 2;
            int room1CenterY = roomList[roomIndex1].minY + roomList[roomIndex1].height / 2;
            int room2CenterX = roomList[roomIndex2].minX + roomList[roomIndex2].width / 2;
            int room2CenterY = roomList[roomIndex2].minY + roomList[roomIndex2].height / 2;
            CarveHorizontalSpan(room1CenterY, room1CenterX, room2CenterX, false);
            CarveVerticalSpan(room2CenterX, room1CenterY, room2CenterY, false);
        }

        /// <summary>
        /// Using existing path distances, find the furthestpaths and carve them.
        /// </summary>
        /// <param name="numberOfPathsToCarve">THe number of passageways to create.</param>
        public void CarveExtraPassagesFurthestAway(int numberOfPathsToCarve = 1)
        {
            int pathsToCarve = numberOfPathsToCarve;
            pathsToCarve = (pathsToCarve >= pathLengthsFromSource.Count) ? pathLengthsFromSource.Count - 1 : pathsToCarve;
            for(int i=1; i <= pathsToCarve; i+=2)
            {
                pathLengthsFromSource.Sort(TupleComparer);
                int room1 = pathLengthsFromSource[pathLengthsFromSource.Count - i].Item1;
                int centerX = roomList[room1].minX + roomList[room1].width / 2;
                int centerY = roomList[room1].minY + roomList[room1].height / 2;
                int room2 = pathLengthsFromSource[pathLengthsFromSource.Count - i-1].Item1;
                int center2X = roomList[room2].minX + roomList[room2].width / 2;
                int center2Y = roomList[room2].minY + roomList[room2].height / 2;
                CarveHorizontalSpan(centerY, centerX, center2X, false);
                CarveVerticalSpan(center2X, centerY, center2Y, false);
            }
        }

        private float edgeCost(IIndexedEdge<E> edge)
        {
            const float largeCost = 10000000f;
            int row1 = edge.From / Width;
            int col1 = edge.From % Width;
            int row2 = edge.To / Width;
            int col2 = edge.To % Width;
            Direction cellDirs = directions[col1, row1];
            Direction edgeDir = DirectionExtensions.GetEdgeDirection(edge.From, edge.To, Width);
            if ((cellDirs & edgeDir) == edgeDir)
                return 1;
            else if ((cellDirs & Direction.Undefined) != Direction.Undefined)
                return largeCost;
            else
                return wallCarveCost * (float)RandomGenerator.NextDouble();
        }

        /// <summary>
        /// Actually create the rooms in the underlying grid maze.
        /// </summary>
        public void CarveAllRooms()
        {
            //AddRandomRooms(this.NumberOfRooms-roomList.Count);
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

        /// <summary>
        /// Create randome rooms that do not overlap.
        /// </summary>
        /// <param name="numberOfRoomsToAdd"></param>
        public void AddRandomRooms(int numberOfRoomsToAdd)
        {
            int deltaWidth = MaxRoomSize - MinRoomSize + 1;
            // Random create rooms
            int roomTrys = 0;
            int roomsAdded = 0;
            while (roomsAdded < numberOfRoomsToAdd && roomTrys < MaxNumberOfTrys)
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
                    roomsAdded++;
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
                if (distance - RoomMoatSize < 0)
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
            int distance = xDistance + yDistance;
            if (distance == 0)
            {
                // Not entirely accurate if one is completely within the other
                distance = Math.Min(Math.Max(x1, u1) - Math.Max(x2, u2), Math.Max(y1, v1) - Math.Max(y2, v2));
            }
            return distance;
        }

        private int TupleComparer(Tuple<int, int, float> x, Tuple<int, int, float> y)
        {
            return x.Item3.CompareTo(y.Item3);
        }
    }
}
