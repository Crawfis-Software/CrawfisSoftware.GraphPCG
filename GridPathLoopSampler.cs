// using CrawfisSoftware.Collections.Path;
// using CrawfisSoftware.Collections.Maze;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using CrawfisSoftware.Collections.Graph;
// using CrawfisSoftware.PCG.MazeBuilder;
//
// namespace CrawfisSoftware.PCG
// {
//     public class GridPathLoopSampler<TNodeValue, TEdgeValue>
//     {
//         private MazeBuilderLoopSampler<TNodeValue, TEdgeValue> _mazeBuilderLoopSampler;
//         private int _width;
//         private int _height;
//         private GetEdgeLabel<TEdgeValue> _edgeAccessor;
//         private GetGridLabel<TNodeValue> _nodeAccessor;
//         
//         public GridPathLoopSampler(int width, int height, 
//             GetGridLabel<TNodeValue> nodeAccessor = null, 
//             GetEdgeLabel<TEdgeValue> edgeAccessor = null)
//         {
//             _width = width;
//             _height = height;
//             _nodeAccessor = nodeAccessor;
//             _edgeAccessor = edgeAccessor;
//             _mazeBuilderLoopSampler = new MazeBuilderLoopSampler<TNodeValue, TEdgeValue>(_width, _height, 
//                 nodeAccessor, edgeAccessor);
//         }
//
//         public GridPath<TNodeValue, TEdgeValue> Sample(int start = 0, int end = 0, bool toPrint = false)
//         {
//             _mazeBuilderLoopSampler.StartCell = start;
//             _mazeBuilderLoopSampler.EndCell = end;
//             _mazeBuilderLoopSampler.CreateMaze();
//             var maze = _mazeBuilderLoopSampler.GetMaze();
//
//             if (toPrint)
//             {
//                 Console.WriteLine(maze);
//             }
//             var grid = new Grid<TNodeValue, TEdgeValue>(_width, _height, _nodeAccessor, _edgeAccessor);
//             int currentNode = (_width * (_height - 1) + end);
//             int endNode = (_width * (_height - 1) + end);
//             int previous = int.MaxValue;
//             List<int> path = new List<int>();
//             path.Add(currentNode);
//             do
//             {
//                 foreach (var neighbor in maze.Neighbors(currentNode))
//                 {
//                     if (neighbor != previous)
//                     {
//                         previous = currentNode;
//                         currentNode = neighbor;
//                         path.Add(currentNode);
//                         break;
//                     }
//                 }
//             } while (currentNode != endNode);
//             return new GridPath<TNodeValue, TEdgeValue>(grid, path);
//         }
//
//         public Maze<TNodeValue, TEdgeValue> GetMazeFromCurrentPath()
//         {
//             return _mazeBuilderLoopSampler.GetMaze();
//         }
//     }
// }