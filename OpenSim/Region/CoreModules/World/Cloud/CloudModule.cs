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

using System;
using System.Collections.Generic;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules
{
    public class CloudModule : ICloudModule
    {
//        private static readonly log4net.ILog m_log 
//            = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private uint m_frame = 0;
        private int m_frameUpdateRate = 1000;
        private Random m_rndnums = new Random(Environment.TickCount);
        private Scene m_scene = null;
        private bool m_ready = false;
        private bool m_enabled = false;
        private float m_cloudDensity = 1.0F;
        private float[] cloudCover = new float[16 * 16];

        public void Initialise(Scene scene, IConfigSource config)
        {
            IConfig cloudConfig = config.Configs["Cloud"];

            if (cloudConfig != null)
            {
                m_enabled = cloudConfig.GetBoolean("enabled", false);
                m_cloudDensity = cloudConfig.GetFloat("density", 0.5F);
                m_frameUpdateRate = cloudConfig.GetInt("cloud_update_rate", 1000);
            }

            if (m_enabled)
            {

                m_scene = scene;

                scene.EventManager.OnNewClient += CloudsToClient;
                scene.RegisterModuleInterface<ICloudModule>(this);
                scene.EventManager.OnFrame += CloudUpdate;

                GenerateCloudCover();

                m_ready = true;

            }

        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
            if (m_enabled)
            {
                m_ready = false;
                //  Remove our hooks
                m_scene.EventManager.OnNewClient -= CloudsToClient;
                m_scene.EventManager.OnFrame -= CloudUpdate;
            }
        }

        public string Name
        {
            get { return "CloudModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }


        public float CloudCover(int x, int y, int z)
        {
            float cover = 0f;
            x /= 16;
            y /= 16;
            if (x < 0) x = 0;
            if (x > 15) x = 15;
            if (y < 0) y = 0;
            if (y > 15) y = 15;

            if (cloudCover != null)
            {
                cover = cloudCover[y * 16 + x];
            }

            return cover;
        }

        private void UpdateCloudCover()
        {
            float[] newCover = new float[16 * 16];
            int rowAbove = new int();
            int rowBelow = new int();
            int columnLeft = new int();
            int columnRight = new int();
            for (int x = 0; x < 16; x++)
            {
                if (x == 0)
                {
                    columnRight = x + 1;
                    columnLeft = 15;
                }
                else if (x == 15)
                {
                    columnRight = 0;
                    columnLeft = x - 1;
                }
                else 
                {
                    columnRight = x + 1;
                    columnLeft = x - 1;
                }
                for (int y = 0; y< 16; y++)
                {
                    if (y == 0)
                    {
                        rowAbove = y + 1;
                        rowBelow = 15;
                    }
                    else if (y == 15)
                    {
                        rowAbove = 0;
                        rowBelow = y - 1;
                    }
                    else
                    {
                        rowAbove = y + 1;
                        rowBelow = y - 1;
                    }
                    float neighborAverage = (cloudCover[rowBelow * 16 + columnLeft] + 
                                             cloudCover[y * 16 + columnLeft] + 
                                             cloudCover[rowAbove * 16 + columnLeft] + 
                                             cloudCover[rowBelow * 16 + x] + 
                                             cloudCover[rowAbove * 16 + x] + 
                                             cloudCover[rowBelow * 16 + columnRight] + 
                                             cloudCover[y * 16 + columnRight] + 
                                             cloudCover[rowAbove * 16 + columnRight] + 
                                             cloudCover[y * 16 + x]) / 9;
                    newCover[y * 16 + x] = ((neighborAverage / m_cloudDensity) + 0.175f) % 1.0f;
                    newCover[y * 16 + x] *= m_cloudDensity;
                }
            }
            Array.Copy(newCover, cloudCover, 16 * 16);
        }
  
       private void CloudUpdate()
       {
           if (((m_frame++ % m_frameUpdateRate) != 0) || !m_ready || (m_cloudDensity == 0))
           {
               return;
           }
           UpdateCloudCover();
        }

        public void CloudsToClient(IClientAPI client)
        {
            if (m_ready)
            {
                client.SendCloudData(cloudCover);
            }
        }

       
        /// <summary>
        /// Calculate the cloud cover over the region.
        /// </summary>
        private void GenerateCloudCover()
        {
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    cloudCover[y * 16 + x] = (float)(m_rndnums.NextDouble()); // 0 to 1
                    cloudCover[y * 16 + x] *= m_cloudDensity;
                }
            }
        }
    }
}
