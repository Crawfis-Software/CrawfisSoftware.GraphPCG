using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Collections.Maze;
using System.Collections.Generic;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Graph builder using the Drunken Walk or Random Walk algorithm with multiple walkers.
    /// </summary>
    /// <typeparam name="N"></typeparam>
    /// <typeparam name="E"></typeparam>
    public class RandomWalkMazeBuilder<N, E> : MazeBuilderAbstract<N, E>
    {
        // Having Walker in a seperate class allows for multiple walkers. Need to make this thread safe though.
        private class Walker
        {
            private RandomWalkMazeBuilder<N, E> mazeBuilder;
            public int currentCell;
            private System.Random random;
            private bool preserveExistingCells;
            private bool favorForwardCarving;
            private int[] nextCellIncrement;
            private int lastMove;
            public void StartWalker(RandomWalkMazeBuilder<N, E> mazeBuilder, int cell, bool preserveExistingCells, bool favorForwardCarving, System.Random random)
            {
                this.mazeBuilder = mazeBuilder;
                currentCell = cell;
                this.preserveExistingCells = preserveExistingCells;
                this.favorForwardCarving = favorForwardCarving;
                this.random = random;
                nextCellIncrement = new int[] { -1, 1, mazeBuilder.Width, -mazeBuilder.Width };
                lastMove = nextCellIncrement[random.Next(4)];
            }
            // Could move carving logic to the RandomWalk class.
            public void Update()
            {
                int nextCell = currentCell + Move();
                if (mazeBuilder.grid.ContainsEdge(currentCell, nextCell))
                {
                    if (mazeBuilder.CarvePassage(currentCell, nextCell, preserveExistingCells))
                    {
                        mazeBuilder.numberOfCarvedPassages++;
                    }
                    currentCell = nextCell;
                    mazeBuilder.numberOfSteps++;
                }
            }

            private int Move()
            {
                if (!favorForwardCarving || (random.NextDouble() > 0.5f))
                    lastMove = nextCellIncrement[random.Next(4)];
                return lastMove;
            }
        }
        /// <summary>
        /// The main control parameter for the algorithm. Specifies new passages to open (carve).
        /// A value of zero provides no carving.
        /// A value of 1.0 will carve the entire grid.
        /// Note: If carving a partial maze already, this parameter is for any new carvings.
        /// </summary>
        public float PercentToCarve { get; set; } = 0.6f;
        private int numberOfNewPassages;
        /// <summary>
        /// A safety parameter or a useful control parameter. The algorithm stops after
        /// MazWalkingDistance steps.
        /// </summary>
        public int MaxWalkingDistance { get; set; } = 1000000;
        /// <summary>
        /// The number of walkers to spawn (eventually). New walkers can be spawned
        /// at random locations during initialization or at an existing walker's
        /// location as the algorithm progesses. The later will carve out more open areas.
        /// </summary>
        public int NumberOfWalkers { get; set; } = 4;
        /// <summary>
        /// The number of initial walkers to spawn. Each walker will start at a random location.
        /// </summary>
        public int InitialNumberOfWalkers { get; set; } = 1;
        /// <summary>
        /// If favorForwardCarving is true. A walker is more likely to walk in a straight line.
        /// This moves the walker further around the room. Setting to false provides less
        /// exploration of the grid and carves out a more open area.
        /// </summary>
        public bool favorForwardCarving { get; set; }
        private int numberOfCarvedPassages = 0;
        private int numberOfSteps = 0;
        private float ChanceNewWalker { get; set; } = 0.8f;
        private List<Walker> walkers;
        private bool preserveExistingCells = false;
        /// <summary>
        /// Constructor. All of the parameters are the same as the grid data type.
        /// </summary>
        /// <param name="width">Number of nodes in the horizontal direction.</param>
        /// <param name="height">Number of nodes in the vertical direction.</param>
        /// <param name="nodeAccessor">A GetGridLabel delegate instance used to determine
        /// a node's label when queried.</param>
        /// <param name="edgeAccessor">A GetEdgeLabel delegate instance used to determine
        /// a edge's label when queried.</param>
        public RandomWalkMazeBuilder(int width, int height, GetGridLabel<N> nodeAccessor, GetEdgeLabel<E> edgeAccessor)
            : base(width, height, nodeAccessor, edgeAccessor)
        {
        }
        /// <summary>
        /// Main method where the algorithm is performed.
        /// Note: This could be called many times with all but the 
        /// first passing in a value of true. New walkers would be
        /// spawned on each invocation.
        /// </summary>
        /// <param name="preserveExistingCells">If true, cells with existing
        /// values already set will not be affected.</param>
        public override void CreateMaze(bool preserveExistingCells = false)
        {
            numberOfCarvedPassages = 0;
            numberOfSteps = 0;
            if (!preserveExistingCells)
            {
                Clear();
            }
            numberOfNewPassages = (int)(PercentToCarve * grid.NumberOfEdges);
            this.preserveExistingCells = preserveExistingCells;
            InitializeWalkers();
        }

        private void InitializeWalkers()
        {
            walkers = new List<Walker>(NumberOfWalkers);
            for (int i = 0; i < InitialNumberOfWalkers; i++)
            {
                Walker initialWalker = new Walker();
                // Initial start is placed randomly avoiding the borders. Assumes height > 2.
                int startCell = RandomGenerator.Next(Width - 2) + 1;
                int heightCheck = (Height > 2) ? RandomGenerator.Next(Height - 2) + 1 : Height - 1;
                startCell += Width * heightCheck;

                initialWalker.StartWalker(this, startCell, preserveExistingCells, favorForwardCarving, RandomGenerator);
                walkers.Add(initialWalker);
            }

            PerformWalk();
        }

        private void PerformWalk()
        {
            while (true)
            {
                foreach (var walker in walkers)
                {
                    walker.Update();
                    if (numberOfCarvedPassages < numberOfNewPassages && numberOfSteps < MaxWalkingDistance)
                        continue;
                    return;
                }
                if ((walkers.Count < NumberOfWalkers) && (this.RandomGenerator.NextDouble() < ChanceNewWalker))
                {
                    Walker newWalker = new Walker();
                    int startCell = walkers[RandomGenerator.Next(walkers.Count)].currentCell;
                    newWalker.StartWalker(this, startCell, preserveExistingCells, favorForwardCarving, this.RandomGenerator);
                    walkers.Add(newWalker);
                }
            }
        }
    }
}