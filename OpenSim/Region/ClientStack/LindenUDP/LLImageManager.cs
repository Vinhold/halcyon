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
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using log4net;

namespace OpenSim.Region.ClientStack.LindenUDP
{
    public class LLImageManager
    {
        private sealed class J2KImageComparer : IComparer<J2KImage>
        {
            public int Compare(J2KImage x, J2KImage y)
            {
                return x.Priority.CompareTo(y.Priority);
            }
        }

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private bool m_shuttingdown;
        private AssetBase m_missingImage;
        private LLClientView m_client; //Client we're assigned to
        private IAssetCache m_assetCache; //Asset Cache
        private IJ2KDecoder m_j2kDecodeModule; //Our J2K module
        private C5.IntervalHeap<J2KImage> m_priorityQueue = new C5.IntervalHeap<J2KImage>(10, new J2KImageComparer());
        private object m_syncRoot = new object();

        public LLClientView Client { get { return m_client; } }
        public AssetBase MissingImage { get { return m_missingImage; } }

        public LLImageManager(LLClientView client, IAssetCache pAssetCache, 
            IJ2KDecoder pJ2kDecodeModule)
        {
            m_client = client;
            m_assetCache = pAssetCache;

            /*if (pAssetCache != null)
                m_missingImage = pAssetCache.GetAsset(UUID.Parse("5748decc-f629-461c-9a36-a35a221fe21f"), AssetRequestInfo.InternalRequest());
            
            if (m_missingImage == null)
                m_log.Error("[ClientView] - Couldn't set missing image asset, falling back to missing image packet. This is known to crash the client");
            */
            m_j2kDecodeModule = pJ2kDecodeModule;
        }

        /// <summary>
        /// Handles an incoming texture request or update to an existing texture request
        /// </summary>
        /// <param name="newRequest"></param>
        public void EnqueueReq(TextureRequestArgs newRequest)
        {
            //Make sure we're not shutting down..
            if (!m_shuttingdown)
            {
                J2KImage imgrequest;

                // Do a linear search for this texture download
                lock (m_syncRoot)
                    m_priorityQueue.Find(delegate(J2KImage img) { return img.TextureID == newRequest.RequestedAssetID; }, out imgrequest);

                if (imgrequest != null)
                {
                    if (newRequest.DiscardLevel == -1 && newRequest.Priority == 0f)
                    {
                        //m_log.Debug("[TEX]: (CAN) ID=" + newRequest.RequestedAssetID);

                        try 
                        {
                            lock (m_syncRoot)
                                m_priorityQueue.Delete(imgrequest.PriorityQueueHandle); 
                        }
                        catch (Exception) { }
                    }
                    else
                    {
                        //m_log.DebugFormat("[TEX]: (UPD) ID={0}: D={1}, S={2}, P={3}",
                        //    newRequest.RequestedAssetID, newRequest.DiscardLevel, newRequest.PacketNumber, newRequest.Priority);

                        //Check the packet sequence to make sure this isn't older than 
                        //one we've already received
                        if (newRequest.requestSequence > imgrequest.LastSequence)
                        {
                            //Update the sequence number of the last RequestImage packet
                            imgrequest.LastSequence = newRequest.requestSequence;

                            //Update the requested discard level
                            imgrequest.DiscardLevel = newRequest.DiscardLevel;

                            //Update the requested packet number
                            imgrequest.StartPacket = Math.Max(1, newRequest.PacketNumber);

                            //Update the requested priority
                            imgrequest.Priority = newRequest.Priority;
                            UpdateImageInQueue(imgrequest);

                            //Run an update
                            //imgrequest.RunUpdate();
                        }
                    }
                }
                else
                {
                    if (newRequest.DiscardLevel == -1 && newRequest.Priority == 0f)
                    {
                        //m_log.DebugFormat("[TEX]: (IGN) ID={0}: D={1}, S={2}, P={3}",
                        //    newRequest.RequestedAssetID, newRequest.DiscardLevel, newRequest.PacketNumber, newRequest.Priority);
                    }
                    else
                    {
                        //m_log.DebugFormat("[TEX]: (NEW) ID={0}: D={1}, S={2}, P={3}",
                        //    newRequest.RequestedAssetID, newRequest.DiscardLevel, newRequest.PacketNumber, newRequest.Priority);

                        imgrequest = new J2KImage(this);
                        imgrequest.J2KDecoder = m_j2kDecodeModule;
                        imgrequest.AssetService = m_assetCache;
                        imgrequest.AgentID = m_client.AgentId;
                        imgrequest.DiscardLevel = newRequest.DiscardLevel;
                        imgrequest.StartPacket = Math.Max(1, newRequest.PacketNumber);
                        imgrequest.Priority = newRequest.Priority;
                        imgrequest.TextureID = newRequest.RequestedAssetID;
                        imgrequest.Priority = newRequest.Priority;

                        //Add this download to the priority queue
                        AddImageToQueue(imgrequest);

                        //Run an update
                        //imgrequest.RunUpdate();
                    }
                }
            }
        }

        public bool ProcessImageQueue(int packetsToSend)
        {
            int packetsSent = 0;

            while (packetsSent < packetsToSend)
            {
                J2KImage image = GetHighestPriorityImage();

                // If null was returned, the texture priority queue is currently empty
                if (image == null)
                    return false;

                image.CheckDoFirstUpdate();

                if (image.IsDecoded)
                {
                    int sent;
                    bool imageDone = image.SendPackets(m_client, packetsToSend - packetsSent, out sent);
                    packetsSent += sent;

                    // If the send is complete, destroy any knowledge of this transfer
                    if (imageDone)
                        RemoveImageFromQueue(image);
                }
                else
                {
                    // TODO: This is a limitation of how LLImageManager is currently
                    // written. Undecoded textures should not be going into the priority
                    // queue, because a high priority undecoded texture will clog up the
                    // pipeline for a client
                    return true;
                }
            }

            return m_priorityQueue.Count > 0;
        }

        /// <summary>
        /// Faux destructor
        /// </summary>
        public void Close()
        {
            m_shuttingdown = true;
        }

        #region Priority Queue Helpers

        J2KImage GetHighestPriorityImage()
        {
            J2KImage image = null;

            lock (m_syncRoot)
            {
                if (m_priorityQueue.Count > 0)
                {
                    try { image = m_priorityQueue.FindMax(); }
                    catch (Exception) { }
                }
            }
            return image;
        }

        void AddImageToQueue(J2KImage image)
        {
            image.PriorityQueueHandle = null;

            lock (m_syncRoot)
                try { m_priorityQueue.Add(ref image.PriorityQueueHandle, image); }
                catch (Exception) { }
        }

        void RemoveImageFromQueue(J2KImage image)
        {
            lock (m_syncRoot)
                try { m_priorityQueue.Delete(image.PriorityQueueHandle); }
                catch (Exception) { }
        }

        void UpdateImageInQueue(J2KImage image)
        {
            lock (m_syncRoot)
            {
                try { m_priorityQueue.Replace(image.PriorityQueueHandle, image); }
                catch (Exception)
                {
                    image.PriorityQueueHandle = null;
                    m_priorityQueue.Add(ref image.PriorityQueueHandle, image);
                }
            }
        }

        #endregion Priority Queue Helpers
    }
}
