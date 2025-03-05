
using CrawfisSoftware.Maze;
using CrawfisSoftware.Path;
using CrawfisSoftware.PCG;


namespace CrawfisSoftware.MazeBuilder
{
    /// <summary>
    /// Extensions for IMazeBuilder for loop and path sampling algorithm.
    /// </summary>
    // todo: .Net Standard 3.0 and .Net 8.0 support partial static classes.
    // When Unity supports 3.0 we can make these partial and have the name w/o numbers.
    public static /*partial*/ class Path
    {
        private static PathSamplerBottomToTop _pathSampler;
        
        /// <summary>
        /// Create a path maze using the sweeping algorithm and may take a while.
        /// </summary>
        /// <param name="mazeBuilder">A maze builder</param>
        /// <param name="preserveExistingCells">Boolean indicating whether to only replace maze cells that are undefined.
        /// Default is false.</param>
        /// <typeparam name="N">The type used for node labels</typeparam>
        /// <typeparam name="E">The type used for edge weights</typeparam>
        public static void CreatePath<N,E> (this IMazeBuilder<N,E> mazeBuilder,bool preserveExistingCells = false)
        {
            if (_pathSampler == null || _pathSampler.GetWidth() != mazeBuilder.Width)
            {
                _pathSampler =
                    new PathSamplerBottomToTop(mazeBuilder.Width, mazeBuilder.Height, mazeBuilder.RandomGenerator);
            }

            var samplerGrid = _pathSampler.Sample(mazeBuilder.StartCell, mazeBuilder.EndCell);
            MazeWrapperFromGridBitArrays<N,E>.CarvePath(mazeBuilder, samplerGrid.vertical, samplerGrid.horizontal);
        }
    }
}