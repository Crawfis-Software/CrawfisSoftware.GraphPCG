using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrawfisSoftware.Collections.Maze;

namespace CrawfisSoftware.PCG
{
    /// <summary>
    /// A selector class that provides user ways to sample and select mazes using asynchronous
    /// programming
    /// </summary>
    /// <typeparam name="N">Node</typeparam>
    /// <typeparam name="E">Edge</typeparam>
    public class MazeSelector<N,E>
    {
        /// <summary>
        /// A list of maze builders for maze sampling
        /// </summary>
        public IList<IMazeBuilder<N,E>> MazeBuilders { get; set; }
        private readonly IList<Maze<N, E>> _selectedMazes;
        private readonly IList<Maze<N, E>> _displayingMazes;

        /// <summary>
        /// Construct a maze selector
        /// </summary>
        public MazeSelector()
        {
            _selectedMazes = new List<Maze<N, E>>();
            _displayingMazes = new List<Maze<N, E>>();
        }

        /// <summary>
        /// Asynchronously build mazes using the given maze builders
        /// </summary>
        public void CreateMazes()
        {
            _displayingMazes.Clear();
            var taskList = new List<Task>();
            foreach (var t in MazeBuilders)
            {
                taskList.Add( 
                    Task.Run(() =>
                        {
                            t.CreateMaze();
                            var maze = t.GetMaze();
                            lock (_displayingMazes)
                            {
                                _displayingMazes.Add(maze);
                            }
                        }
                    ));
            }
            Task.WhenAll(taskList.ToArray()).Wait();
        }

        /// <summary>
        /// Print all the generated mazes using 
        /// </summary>
        public void DisplayMazes()
        {
            for (int i = 0; i < _displayingMazes.Count; i++)
            {
                Console.WriteLine($"Maze #{i}");
                Console.WriteLine(_displayingMazes[i]);
            }
        }

        /// <summary>
        /// Print all the saved mazes
        /// </summary>
        public void ShowSavedMazes()
        {
            Console.WriteLine("Saved Mazes");
            for (int i = 0; i < _selectedMazes.Count; i++)
            {
                Console.WriteLine($"Maze #{i}");
                Console.WriteLine(_selectedMazes[i]);
            }
        }

        
        /// <summary>
        /// Save the desired maze in to a collection
        /// </summary>
        /// <param name="index">The index of the maze desired</param>
        public void SaveMaze(int index)
        {
            _selectedMazes.Add(_displayingMazes[index]);
        }

        public void WriteMazesToFile(string path)
        {
            
        }
    }
}