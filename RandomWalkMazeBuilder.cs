using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.Maze;

using System.Collections.Generic;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Graph builder using the Drunken Walk or Random Walk algorithm with multiple walkers.
    /// </summary>
    /// <typeparam name="N">The type used for node labels</typeparam>
    /// <typeparam name="E">The type used for edge weights</typeparam>
    public class RandomWalkMazeBuilder<N, E>
    {
        // Having Walker in a separate class allows for multiple walkers. Need to make this thread safe though.
        private class Walker
        {
            private IMazeBuilder<N, E> mazeBuilder;
            public int currentCell;
            private System.Random random;
            private bool preserveExistingCells;
            private bool favorForwardCarving;
            private int[] nextCellIncrement;
            private int lastMove;
            public void StartWalker(IMazeBuilder<N, E> mazeBuilder, int cell, bool preserveExistingCells, bool favorForwardCarving, System.Random random)
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
            public void Update(RandomWalkMazeBuilder<N, E> walkerController)
            {
                int nextCell = currentCell + Move();
                if (mazeBuilder.Grid.ContainsEdge(currentCell, nextCell))
                {
                    if (mazeBuilder.CarvePassage(currentCell, nextCell, preserveExistingCells))
                    {
                        walkerController.numberOfCarvedPassages++;
                    }
                    currentCell = nextCell;
                    walkerController.numberOfSteps++;
                }
            }

            private int Move()
            {
                if (!favorForwardCarving || (random.NextDouble() > 0.5f))
                    lastMove = nextCellIncrement[random.Next(4)];
                return lastMove;
            }
        }

        private IMazeBuilder<N, E> _mazeBuilder;
        protected int numberOfCarvedPassages = 0;
        protected int numberOfSteps = 0;
        private float ChanceNewWalker { get; set; } = 0.8f;
        private List<Walker> walkers;
        private bool preserveExistingCells = false;
        private int numberOfNewPassages;

        /// <summary>
        /// The main control parameter for the algorithm. Specifies new passages to open (carve).
        /// A value of zero provides no carving.
        /// A value of 1.0 will carve the entire grid.
        /// Note: If carving a partial maze already, this parameter is for any new carvings.
        /// </summary>
        public float PercentToCarve { get; set; } = 0.6f;

        /// <summary>
        /// A safety parameter or a useful control parameter. The algorithm stops after
        /// MazWalkingDistance steps.
        /// </summary>
        public int MaxWalkingDistance { get; set; } = 1000000;

        /// <summary>
        /// The number of walkers to spawn (eventually). New walkers can be spawned
        /// at random locations during initialization or at an existing walker's
        /// location as the algorithm progresses. The later will carve out more open areas.
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

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mazeBuilder">A maze builder</param>
        public RandomWalkMazeBuilder(IMazeBuilder<N, E> mazeBuilder)
        {
            _mazeBuilder = mazeBuilder;
        }

        /// <summary>
        /// Main method where the algorithm is performed.
        /// Note: This could be called many times with all but the 
        /// first passing in a value of true. New walkers would be
        /// spawned on each invocation.
        /// </summary>
        /// <param name="startCell">The starting cell for the walkers.</param>
        /// <param name="preserveExistingCells">If true, _occupied with existing
        /// values already set will not be affected.</param>
        public void CarveWalk(int startCell, bool preserveExistingCells = false)
        {
            numberOfCarvedPassages = 0;
            numberOfSteps = 0;
            numberOfNewPassages = (int)(PercentToCarve * _mazeBuilder.Grid.NumberOfNodes);
            //numberOfNewPassages = (int)(PercentToCarve * 2 * (Width - 1) * (Height - 1) + 2 * Width + 2 * Height);
            this.preserveExistingCells = preserveExistingCells;
            InitializeWalkers(startCell);
        }

        private void InitializeWalkers(int startCell)
        {
            walkers = new List<Walker>(NumberOfWalkers);
            for (int i = 0; i < InitialNumberOfWalkers; i++)
            {
                Walker initialWalker = new Walker();
                // Initial start is placed randomly avoiding the borders. Assumes height > 2.
                //int startCell = RandomGenerator.Next(Width - 2) + 1;
                //int heightCheck = (Height > 2) ? RandomGenerator.Next(Height - 2) + 1 : Height - 1;
                //startCell += Width * heightCheck;

                initialWalker.StartWalker(_mazeBuilder, startCell, preserveExistingCells, favorForwardCarving, new System.Random(_mazeBuilder.RandomGenerator.Next()));
                walkers.Add(initialWalker);
            }

            PerformWalk();
        }

        private void PerformWalk()
        {
            var random = new System.Random(_mazeBuilder.RandomGenerator.Next());
            while (true)
            {
                foreach (var walker in walkers)
                {
                    walker.Update(this);
                    if (numberOfCarvedPassages < numberOfNewPassages && numberOfSteps < MaxWalkingDistance)
                        continue;
                    return;
                }
                if ((walkers.Count < NumberOfWalkers) && (random.NextDouble() < ChanceNewWalker))
                {
                    Walker newWalker = new Walker();
                    int startCell = walkers[random.Next(walkers.Count)].currentCell;
                    newWalker.StartWalker(_mazeBuilder, startCell, preserveExistingCells, favorForwardCarving, new System.Random(random.Next()));
                    walkers.Add(newWalker);
                }
            }
        }
    }
}