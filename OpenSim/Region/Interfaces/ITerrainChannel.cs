/*
 * Copyright (c) InWorldz Halcyon Developers
 * Copyright (c) Contributors, http://opensimulator.org/
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using OpenMetaverse;

namespace OpenSim.Region.Interfaces
{
    public interface ITerrainChannel
    {
        int Height { get; }
        double this[int x, int y] { get; set; }
        int Width { get; }

        int RevisionNumber { get; }

        float CalculateHeightAt(float x, float y);
        float Calculate4PointHeightAt(float x, float y);

        float GetRawHeightAt(int x, int y);
        Vector3 NormalToSlope(Vector3 normal);  // convert a normal to a slope

        Vector3 CalculateNormalAt(float x, float y); // 3-point triangle surface normal
        Vector3 Calculate4PointNormalAt(float x, float y);
        Vector3 CalculateSlopeAt(float x, float y);  // 3-point triangle slope
        Vector3 Calculate4PointSlopeAt(float x, float y);

        /// <summary>
        /// Squash the entire heightmap into a single dimensioned array
        /// </summary>
        /// <returns></returns>
        float[] GetFloatsSerialised();

        double[,] GetDoubles();
        bool Tainted(int x, int y);
        ITerrainChannel MakeCopy();
        string SaveToXmlString();
        void LoadFromXmlString(string data);

        int IncrementRevisionNumber();
    }
}
