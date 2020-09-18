using CrawfisSoftware.Collections.Maze;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// Static class used to carve a path
    /// </summary>
    public static class MazeWrapperFromBinary2DArray
    {
        /// <summary>
        /// Carve openings based on the list of compressed vertical and horizontal edge flags for each row
        /// </summary>
        /// <param name="mazeBuilder">An existing maze builder to use in the carving process</param>
        /// <param name="solidBlocks">2D array matching the maze builder's width and height.
        /// A value of true implies this cell is a solid block. Passages will be carved from non-solid
        /// blocks to adjacent non-solid blocks.</param>
        public static void CarveOpenings(MazeBuilderAbstract<int,int> mazeBuilder, bool[,] solidBlocks)
        {
            
            for(int row = 0; row < mazeBuilder.Height-1; row++)
            {
                for(int column = 0; column < mazeBuilder.Width-1; column++)
                {
                    if(!solidBlocks[row,column])
                    {
                        if (!solidBlocks[row + 1, column])
                        {
                            mazeBuilder.CarvePassage(column, row, column, row + 1);
                        }
                        if (!solidBlocks[row, column+1])
                        {
                            mazeBuilder.CarvePassage(column, row, column+1, row);
                        }
                    }
                }
            }
        }
    }
}
