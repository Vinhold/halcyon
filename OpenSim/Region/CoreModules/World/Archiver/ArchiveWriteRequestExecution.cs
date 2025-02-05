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
using System.IO;
using System.Reflection;
using System.Xml;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Serialization;
using OpenSim.Framework.Serialization.External;
using OpenSim.Region.CoreModules.World.Terrain;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules.World.Archiver
{
    /// <summary>
    /// Method called when all the necessary assets for an archive request have been received.
    /// </summary>
    public delegate void AssetsRequestCallback(
        ICollection<UUID> assetsFoundUuids, ICollection<UUID> assetsNotFoundUuids);

    /// <summary>
    /// Execute the write of an archive once we have received all the necessary data
    /// </summary>
    public class ArchiveWriteRequestExecution
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected ITerrainModule m_terrainModule;
        protected IRegionSerialiserModule m_serialiser;
        protected List<SceneObjectGroup> m_sceneObjects;
        protected Scene m_scene;
        protected TarArchiveWriter m_archiveWriter;
        protected Guid m_requestId;

        public ArchiveWriteRequestExecution(
             List<SceneObjectGroup> sceneObjects,
             ITerrainModule terrainModule,
             IRegionSerialiserModule serialiser,
             Scene scene,
             TarArchiveWriter archiveWriter,
             Guid requestId)
        {
            m_sceneObjects = sceneObjects;
            m_terrainModule = terrainModule;
            m_serialiser = serialiser;
            m_scene = scene;
            m_archiveWriter = archiveWriter;
            m_requestId = requestId;
        }

        protected internal void ReceivedAllAssets(
            ICollection<UUID> assetsFoundUuids, ICollection<UUID> assetsNotFoundUuids)
        {
            foreach (UUID uuid in assetsNotFoundUuids)
            {
                m_log.DebugFormat("[ARCHIVER]: Could not find asset {0}", uuid);
            }

            m_log.InfoFormat(
                "[ARCHIVER]: Received {0} of {1} assets requested",
                assetsFoundUuids.Count, assetsFoundUuids.Count + assetsNotFoundUuids.Count);

            m_log.InfoFormat("[ARCHIVER]: Creating archive file.  This may take some time.");

            // Write out control file
            m_archiveWriter.WriteFile(ArchiveConstants.CONTROL_FILE_PATH, Create0p2ControlFile());

            m_log.InfoFormat("[ARCHIVER]: Added control file to archive.");

            // Write out region settings
            string settingsPath
                = String.Format("{0}{1}.xml", ArchiveConstants.SETTINGS_PATH, m_scene.RegionInfo.RegionName);
            m_archiveWriter.WriteFile(settingsPath, RegionSettingsSerializer.Serialize(m_scene.RegionInfo.RegionSettings));

            m_log.InfoFormat("[ARCHIVER]: Added region settings to archive.");

            // Write out terrain
            string terrainPath
                = String.Format("{0}{1}.r32", ArchiveConstants.TERRAINS_PATH, m_scene.RegionInfo.RegionName);

            MemoryStream ms = new MemoryStream();
            m_terrainModule.SaveToStream(terrainPath, ms);
            m_archiveWriter.WriteFile(terrainPath, ms.ToArray());
            ms.Close();

            m_log.InfoFormat("[ARCHIVER]: Added terrain information to archive.");

            // Write out scene object metadata
            foreach (SceneObjectGroup sceneObject in m_sceneObjects)
            {
                //m_log.DebugFormat("[ARCHIVER]: Saving {0} {1}, {2}", entity.Name, entity.UUID, entity.GetType());

                Vector3 position = sceneObject.AbsolutePosition;

                string serializedObject = m_serialiser.SaveGroupToXml2(sceneObject);
                string filename
                    = string.Format(
                        "{0}{1}_{2:000}-{3:000}-{4:000}__{5}.xml",
                        ArchiveConstants.OBJECTS_PATH, sceneObject.Name,
                        Math.Round(position.X), Math.Round(position.Y), Math.Round(position.Z),
                        sceneObject.UUID);

                m_archiveWriter.WriteFile(filename, serializedObject);
            }

            m_log.InfoFormat("[ARCHIVER]: Added scene objects to archive.");

            m_archiveWriter.Close();

            m_log.InfoFormat("[ARCHIVER]: Wrote out OpenSimulator archive for {0}", m_scene.RegionInfo.RegionName);

            m_scene.EventManager.TriggerOarFileSaved(m_requestId, String.Empty);
        }

        /// <summary>
        /// Create the control file for a 0.2 version archive
        /// </summary>
        /// <returns></returns>
        public static string Create0p2ControlFile()
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter xtw = new XmlTextWriter(sw);
            xtw.Formatting = Formatting.Indented;
            xtw.WriteStartDocument();
            xtw.WriteStartElement("archive");
            xtw.WriteAttributeString("major_version", "0");
            xtw.WriteAttributeString("minor_version", "2");
            xtw.WriteEndElement();

            xtw.Flush();
            xtw.Close();

            String s = sw.ToString();
            sw.Close();

            return s;
        }
    }
}
