using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Splawft
{
    /// <summary>
    /// A class that contains some shortcuts and useful helpers for certain tasks.
    /// </summary>
    public static class DebugUtil
    {
        public static Dictionary<int, string> GetLayerNames()
        {
            return Enumerable.Range(0, 32)
                .Select(i => (i, Name: LayerMask.LayerToName(i)))
                .Where(k => !string.IsNullOrEmpty(k.Name))
                .ToDictionary(p => p.i, p => p.Name);
        }

        /// <summary>
        /// Returns the collision matrix, as found in DynamicsManager.asset::m_LayerCollisionMatrix.
        /// (i.e. you can copy this to an Unity project to mimick the layer mask)
        /// </summary>
        /// <returns></returns>
        public static string GetLayerCollisionMatrix()
        {
            int[] layers = new int[32];
            for (int i = 0; i < 32; i++)
            {
                int mask = 0;
                for (int j = 0; j < 32; j++)
                    if (!Physics.GetIgnoreLayerCollision(i, j))
                        mask |= 1 << j;

                layers[i] = mask;
            }

            var str = string.Concat(layers.SelectMany(i => BitConverter.GetBytes(i).Select(s => Convert.ToString(s, 16).PadLeft(2, '0')))).PadRight(256, '0');
            return str;
        }
    }
}
