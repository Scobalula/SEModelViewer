// ------------------------------------------------------------------------
// SEModelViewer - Tool to view SEModel Files
// Copyright (C) 2018 Philip/Scobalula
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ------------------------------------------------------------------------
using System;
using SELib.Utilities;

namespace SEModelViewer.Util
{
    /// <summary>
    /// Rotation Utilities
    /// Contains methods for working with and converting rotation values
    /// </summary>
    class Rotation
    {
        /// <summary>
        /// Converts a Quaternion to Euler
        /// </summary>
        /// <param name="quaternion">Quaternion to convert</param>
        /// <returns>Resulting XYZ Vector</returns>
        public static Vector3 QuatToEuler(Quaternion quaternion)
        {
            Vector3 result = new Vector3();

            double t0 = 2.0 * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
            double t1 = 1.0 - 2.0 * (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);

            result.X = Math.Atan2(t0, t1);


            double t2 = 2.0 * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);

            t2 = t2 > 1.0 ? 1.0 : t2;
            t2 = t2 < -1.0 ? -1.0 : t2;
            result.Y = Math.Asin(t2);


            double t3 = +2.0 * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
            double t4 = +1.0 - 2.0 * (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);

            result.Z = Math.Atan2(t3, t4);

            return result;
        }

        /// <summary>
        /// Converts Eular value to degrees
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Value in degrees</returns>
        public static double ToDegrees(double value)
        {
            return (value / (2 * Math.PI)) * 360;
        }

        /// <summary>
        /// Holds a 3x3 Matrix
        /// </summary>
        public class Matrix
        {
            /// <summary>
            /// X Components
            /// </summary>
            public Vector3 X = new Vector3();

            /// <summary>
            /// Y Components
            /// </summary>
            public Vector3 Y = new Vector3();

            /// <summary>
            /// Z Components
            /// </summary>
            public Vector3 Z = new Vector3();

            /// <summary>
            /// Returns this matrix as a formatted string.
            /// </summary>
            public override string ToString()
            {
                return String.Format
                    (
                    "[ {0:0.000000}, {1:0.000000}, {2:0.000000} ]\n[ {3:0.000000}, {4:0.000000}, {5:0.000000} ]\n[ {6:0.000000}, {7:0.000000}, {8:0.000000} ]",
                    X.X, X.Y, X.Z, Y.X, Y.Y, Y.Z, Z.X, Z.Y, Z.Z);
            }
        }


        /// <summary>
        /// Converts a Quaternion to a 3x3 Matrix
        /// </summary>
        /// <returns>Resulting matrix</returns>
        public static Matrix QuatToMat(Quaternion quaternion)
        {
            Matrix matrix = new Matrix();

            double tempVar1;
            double tempVar2;

            double xSquared = quaternion.X * quaternion.X;
            double ySquared = quaternion.Y * quaternion.Y;
            double zSquared = quaternion.Z * quaternion.Z;
            double wSquared = quaternion.W * quaternion.W;

            double inverse = 1 / (xSquared + ySquared + zSquared + wSquared);

            matrix.X.X = (xSquared - ySquared - zSquared + wSquared) * inverse;
            matrix.Y.Y = (-xSquared + ySquared - zSquared + wSquared) * inverse;
            matrix.Z.Z = (-xSquared - ySquared + zSquared + wSquared) * inverse;

            tempVar1 = (quaternion.X * quaternion.Y);
            tempVar2 = (quaternion.Z * quaternion.W);

            matrix.Y.X = 2.0 * (tempVar1 + tempVar2) * inverse;
            matrix.X.Y = 2.0 * (tempVar1 - tempVar2) * inverse;

            tempVar1 = (quaternion.X * quaternion.Z);
            tempVar2 = (quaternion.Y * quaternion.W);

            matrix.Z.X = 2.0 * (tempVar1 - tempVar2) * inverse;
            matrix.X.Z = 2.0 * (tempVar1 + tempVar2) * inverse;

            tempVar1 = (quaternion.Y * quaternion.Z);
            tempVar2 = (quaternion.X * quaternion.W);

            matrix.Z.Y = 2.0 * (tempVar1 + tempVar2) * inverse;
            matrix.Y.Z = 2.0 * (tempVar1 - tempVar2) * inverse;

            return matrix;
        }
    }
}
