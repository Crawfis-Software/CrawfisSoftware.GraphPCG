using System;
using System.Collections.Generic;
using System.Linq;

namespace CrawfisSoftware.Utility
{
    /// <summary>
    /// Static helper class for enumerables
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Performs a n-Cartesian product (or cross product) from the 
        /// set of sets passed in. 
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sets.</typeparam>
        /// <param name="inputs">A set, N of sets.</param>
        /// <returns>A set of N-dimensional sets.</returns>
        /// <example>{a,b} x {c,d} => {(a,c), (a,d), (b,c), (b,d)}</example>
        /// <seealso cref="CartesianProduct{T}(IEnumerable{T}[])"/>
        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(
            IEnumerable<IEnumerable<T>> inputs)
        {
            return inputs.Aggregate(
                EnumerableFrom(Enumerable.Empty<T>()),
                (oldCoordinate, newCoordinate) =>
                    from cartesianProductInSoFar in oldCoordinate
                    from item in newCoordinate
                    select cartesianProductInSoFar.Append(item));
        }
        /// <summary>
        /// Performs a n-Cartesian product (or cross product) from the 
        /// set of sets passed in. 
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sets.</typeparam>
        /// <param name="inputs">A list of sets.</param>
        /// <returns>A set of N-dimensional sets.</returns>
        /// <example>{a,b} x {c,d} => {(a,c), (a,d), (b,c), (b,d)}</example>
        /// <seealso cref="CartesianProduct{T}(IEnumerable{IEnumerable{T}})"/>
        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(
            params IEnumerable<T>[] inputs)
        {
            return inputs.Aggregate(
                EnumerableFrom(Enumerable.Empty<T>()),
                (oldCoordinate, newCoordinate) =>
                    from cartesianProductInSoFar in oldCoordinate
                    from item in newCoordinate
                    select cartesianProductInSoFar.Append(item));
        }
        /// <summary>
        /// Creates a new IEnumerable by adding the passed in item to the old IEnumerable.
        /// </summary>
        /// <typeparam name="T">A generic type for the IEnumerable and item.</typeparam>
        /// <param name="currentList">The existing list of size N.</param>
        /// <param name="newItem">The new item to add to the list.</param>
        /// <returns>A new list of size N+1 containing the newItem.</returns>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> currentList, T newItem)
        {
            IEnumerable<T> itemAsSequence = new T[] { newItem };
            return currentList.Concat(itemAsSequence);
        }
        /// <summary>
        /// Converts a list of Enum flags to a combined Flag Enum (a Union of the Flags).
        /// </summary>
        /// <typeparam name="T">Must be a Flag Enum</typeparam>
        /// <param name="values">The list of Enum values to combine.</param>
        /// <returns></returns>
        /// <example>Direction[] enums = new[] { Direction.N, Direction.S };
        ///          Direction flags = enums.EnumFlagUnion(); // Direction.N | Direction.S
        /// </example>
        public static T EnumFlagUnion<T>(this IEnumerable<T> values) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("E must be of type Enum and must be have an attribute of Flag.");

            int builtValue = 0;
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                if (values.Contains(value))
                {
                    builtValue |= Convert.ToInt32(value);
                }
            }
            return (T)Enum.Parse(typeof(T), builtValue.ToString());
        }


        /// <summary>
        /// Returns a list of individual Enum values from a Enum Flag.
        /// </summary>
        /// <typeparam name="T">Needs to be an Enum</typeparam>
        /// <param name="flags">The bits or Flags of the Enum to enumerate.</param>
        /// <returns>Each flag that is "set" or turned on in the Flag Enum.</returns>
        /// <example>Direction directionFlags = Direction.N | Direction.E | Direction.S;
        ///          Direction[] directionList = flags.EnumFlagSubsets().ToArray();
        ///          { Direction.N, Direction.E, Direction.S }
        /// </example>
        public static IEnumerable<T> EnumFlagSubsets<T>(this T flags) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("E must be of type Enum and must be have an attribute of Flag.");

            int inputInt = (int)(object)(T)flags;
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                int valueInt = (int)(object)(T)value;
                if (0 != (valueInt & inputInt))
                {
                    yield return value;
                }
            }
        }

        private static IEnumerable<T> EnumerableFrom<T>(this T item)
        {
            yield return item;
        }
    }
}
