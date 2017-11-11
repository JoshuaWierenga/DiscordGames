// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Helpers.cs" company="n\a">
//   n\a
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MafiaDiscord
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Class containing helpers for various types 
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// Instance of random
        /// </summary>
        private static readonly Random Random = new Random();

        /// <summary>
        /// Extension method to shuffle values in Generic Lists using a Fisher-Yates shuffle
        /// </summary>
        /// <param name="list">
        /// The list to shuffle
        /// </param>
        /// <typeparam name="T">
        /// The type of the list
        /// </typeparam>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Extension method to get the prefix for a role
        /// </summary>
        /// <param name="role">
        /// The role to get the prefix for
        /// </param>
        /// <param name="mafiaCount">
        /// The number of mafia in the game
        /// </param>
        /// <param name="policeCount">
        /// The number of police in the game
        /// </param>
        /// <returns>
        /// <see cref="string"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Causes Exception if role is not known
        /// </exception>
        public static string GetRoleWithPrefix(this Role role, int mafiaCount, int policeCount)
        {
            switch (role)
            {
                case Role.Mafia:
                    return mafiaCount > 1 ? "a member of the mafia" : "the only member of the mafia";
                case Role.Police:
                    return policeCount > 1 ? "a member of the police" : "the only member of the police";
                case Role.Doctor:
                        return "the doctor";
                case Role.Civilian:
                    return "a civilian";
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }
    }
}