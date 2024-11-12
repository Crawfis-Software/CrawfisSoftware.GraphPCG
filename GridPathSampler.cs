using CrawfisSoftware.Collections.Path;
using CrawfisSoftware.Collections.Maze;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrawfisSoftware.Collections.Graph;
using CrawfisSoftware.PCG.MazeBuilder;

namespace CrawfisSoftware.PCG
{
    public class GridPathSampler<TNodeValue, TEdgeValue>
    {
        private MazeBuilderPathSampler<TNodeValue, TEdgeValue> _mazeBuilderPathSampler;
        private int _width;
        private int _height;
        private GetEdgeLabel<TEdgeValue> _edgeAccessor;
        private GetGridLabel<TNodeValue> _nodeAccessor;
        
        public GridPathSampler(int width, int height, 
            GetGridLabel<TNodeValue> nodeAccessor = null, 
            GetEdgeLabel<TEdgeValue> edgeAccessor = null)
        {
            _width = width;
            _height = height;
            _nodeAccessor = nodeAccessor;
            _edgeAccessor = edgeAccessor;
            _mazeBuilderPathSampler = new MazeBuilderPathSampler<TNodeValue, TEdgeValue>(_width, _height, 
                nodeAccessor, edgeAccessor);
        }

        public GridPath<TNodeValue, TEdgeValue> Sample(int start = 0, int end = 0, bool toPrint = false)
        {
            _mazeBuilderPathSampler.StartCell = start;
            _mazeBuilderPathSampler.EndCell = end;
            _mazeBuilderPathSampler.CreateMaze();
            var maze = _mazeBuilderPathSampler.GetMaze();

            if (toPrint)
            {
                Console.WriteLine(maze);
            }
            var grid = new Grid<TNodeValue, TEdgeValue>(_width, _height, _nodeAccessor, _edgeAccessor);
            int currentNode = start;
            int previous = int.MaxValue;
            List<int> path = new List<int>();
            path.Add(currentNode);
            int endNode = (_width * (_height - 1) + end);
            while (currentNode != endNode )
            {
                foreach (var neighbor in maze.Neighbors(currentNode))
                {
                    if (neighbor != previous)
                    {
                        previous = currentNode;
                        currentNode = neighbor;
                        path.Add(currentNode);
                        break;
                    }
                }
            }
            return new GridPath<TNodeValue, TEdgeValue>(grid, path);
        }
    }
}