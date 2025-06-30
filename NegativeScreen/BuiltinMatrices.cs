using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NegativeScreen
{
    /// <summary>
    /// Store various built in ColorMatrix
    /// </summary>
    public static class BuiltinMatrices
    {
        /// <summary>
        /// no color transformation
        /// </summary>
        public static float[,] Identity { get; private set; }
        /// <summary>
        /// simple colors transformations
        /// </summary>
        public static float[,] Negative { get; private set; }

        static BuiltinMatrices()
        {
            Identity = new float[,] {
                {  1.0f,  0.0f,  0.0f,  0.0f,  0.0f },
                {  0.0f,  1.0f,  0.0f,  0.0f,  0.0f },
                {  0.0f,  0.0f,  1.0f,  0.0f,  0.0f },
                {  0.0f,  0.0f,  0.0f,  1.0f,  0.0f },
                {  0.0f,  0.0f,  0.0f,  0.0f,  1.0f }
            };

            // Simple inversion matrix (pure color inversion)
            Negative = new float[,] {
                { -1.0f,  0.0f,  0.0f,  0.0f,  1.0f },
                {  0.0f, -1.0f,  0.0f,  0.0f,  1.0f },
                {  0.0f,  0.0f, -1.0f,  0.0f,  1.0f },
                {  0.0f,  0.0f,  0.0f,  1.0f,  0.0f },
                {  0.0f,  0.0f,  0.0f,  0.0f,  1.0f }
            };

            Console.WriteLine("BuiltinMatrices initialized");
            DumpMatrix("Identity", Identity);
            DumpMatrix("Negative", Negative);
        }

        private static void DumpMatrix(string name, float[,] matrix)
        {
            Console.WriteLine($"\nMatrix: {name}");
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"Row {i}: {matrix[i, 0]:F1} {matrix[i, 1]:F1} {matrix[i, 2]:F1} {matrix[i, 3]:F1} {matrix[i, 4]:F1}");
            }
        }

        private static List<float[,]> Interpolate(float[,] A, float[,] B)
        {
            const int STEPS = 10;
            const int SIZE = 5;

            if (A.GetLength(0) != SIZE ||
                A.GetLength(1) != SIZE ||
                B.GetLength(0) != SIZE ||
                B.GetLength(1) != SIZE)
            {
                throw new ArgumentException();
            }

            List<float[,]> result = new List<float[,]>(STEPS);

            for (int i = 0; i < STEPS; i++)
            {
                result.Add(new float[SIZE, SIZE]);

                for (int x = 0; x < SIZE; x++)
                {
                    for (int y = 0; y < SIZE; y++)
                    {
                        // f(x)=ya+(x-xa)*(yb-ya)/(xb-xa)
                        //calculate 10 steps, from 1 to 10 (we don't need 0, as we start from there)
                        result[i][x, y] = A[x, y] + (i + 1/*-0*/) * (B[x, y] - A[x, y]) / (STEPS/*-0*/);
                    }
                }
            }

            return result;
        }

        public static bool TryGetMatrix(string name, out float[,] matrix)
        {
            switch (name.ToLower())
            {
                case "negative":
                    matrix = Negative;
                    return true;
                case "identity":
                    matrix = Identity;
                    return true;
                default:
                    matrix = null;
                    return false;
            }
        }
    }
}
