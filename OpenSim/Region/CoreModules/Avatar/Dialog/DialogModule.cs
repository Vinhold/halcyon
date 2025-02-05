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

using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules.Avatar.Dialog
{
    public class DialogModule : IRegionModule, IDialogModule
    { 
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected Scene m_scene;
        
        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scene = scene;
            m_scene.RegisterModuleInterface<IDialogModule>(this);
            
            m_scene.AddCommand(
                this, "alert", "alert <first> <last> <message>", "Send an alert to a user", HandleAlertConsoleCommand);

            m_scene.AddCommand(
                this, "alert general", "alert general <message>", "Send an alert to everyone", HandleAlertConsoleCommand);            
        }
        
        public void PostInitialise() {}
        public void Close() {}
        public string Name { get { return "Dialog Module"; } }
        public bool IsSharedModule { get { return false; } }
        
        public void SendAlertToUser(IClientAPI client, string message)
        {
            SendAlertToUser(client, message, false);
        }          
        
        public void SendAlertToUser(IClientAPI client, string message, bool modal)
        {
            client.SendAgentAlertMessage(message, modal);
        } 
        
        public void SendAlertToUser(UUID agentID, string message)
        {
            SendAlertToUser(agentID, message, false);
        }          
        
        public void SendAlertToUser(UUID agentID, string message, bool modal)
        {
            ScenePresence sp = m_scene.GetScenePresence(agentID);
            
            if (sp != null)
                sp.ControllingClient.SendAgentAlertMessage(message, modal);
        }           
        
        public void SendAlertToUser(string firstName, string lastName, string message, bool modal)
        {
            List<ScenePresence> presenceList = m_scene.GetScenePresences();

            foreach (ScenePresence presence in presenceList)
            {
                if (presence.Firstname == firstName && presence.Lastname == lastName)
                {
                    presence.ControllingClient.SendAgentAlertMessage(message, modal);
                    break;
                }
            }
        }     
        
        public void SendGeneralAlert(string message)
        {
            List<ScenePresence> presenceList = m_scene.GetScenePresences();

            foreach (ScenePresence presence in presenceList)
            {
                if (!(presence.IsChildAgent||presence.IsDeleted||presence.IsInTransit))
                    presence.ControllingClient.SendAlertMessage(message);
            }
        }    
        
        public void SendDialogToUser(
            UUID avatarID, string objectName, UUID objectID, UUID ownerID, 
            string message, UUID textureID, int ch, string[] buttonlabels)
        {
            ScenePresence sp = m_scene.GetScenePresence(avatarID);

            if (sp != null && !sp.IsChildAgent)
            {
                string firstName = "(unknown";
                string lastName = "user)";
                UserProfileData ownerProfile = m_scene.CommsManager.UserService.GetUserProfile(ownerID);

                if (ownerProfile != null)
                {
                    firstName = ownerProfile.FirstName;
                    lastName = ownerProfile.SurName;
                }

                sp.ControllingClient.SendDialog(
                    objectName, objectID, ownerID, firstName, lastName, 
                    message, textureID, ch, buttonlabels);
            }
        }

        public void SendTextBoxToUser(
            UUID avatarID, string message, int chatChannel, string name,
            UUID objectID, UUID ownerID)
        {
            ScenePresence sp = m_scene.GetScenePresence(avatarID);

            // XXX MCD Should test for child agent for other calls as well
            if (sp != null && !sp.IsChildAgent)
            {
                string firstName = "(unknown";
                string lastName = "user)";
                UserProfileData ownerProfile = m_scene.CommsManager.UserService.GetUserProfile(ownerID);

                if (ownerProfile != null)
                {
                    firstName = ownerProfile.FirstName;
                    lastName = ownerProfile.SurName;
                }

                sp.ControllingClient.SendTextBoxRequest(
                    message, chatChannel, name, ownerID, firstName, lastName, objectID);
            }
        }

        public void SendUrlToUser(
            UUID avatarID, string objectName, UUID objectID, UUID ownerID, bool groupOwned, string message, string url)
        {
            ScenePresence sp = m_scene.GetScenePresence(avatarID);
            
            if (sp != null)            
                sp.ControllingClient.SendLoadURL(objectName, objectID, ownerID, groupOwned, message, url);
        }        
        
        public void SendNotificationToUsersInEstate(
            UUID fromAvatarID, string fromAvatarName, string message)
        {
            // TODO: This does not yet do what it says on the tin - it only sends the message to users in the same
            // region as the sending avatar.
            SendNotificationToUsersInRegion(fromAvatarID, fromAvatarName, message);
        }        
        
        public void SendNotificationToUsersInRegion(
            UUID fromAvatarID, string fromAvatarName, string message)
        {        
            List<ScenePresence> presenceList = m_scene.GetScenePresences();

            foreach (ScenePresence presence in presenceList)
            {
                if (!(presence.IsChildAgent || presence.IsDeleted || presence.IsInTransit))
                    presence.ControllingClient.SendBlueBoxMessage(fromAvatarID, fromAvatarName, message);
            }
        }
        
        /// <summary>
        /// Handle an alert command from the console.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams"></param>
        public void HandleAlertConsoleCommand(string module, string[] cmdparams)
        {
            if (m_scene.ConsoleScene() != null && m_scene.ConsoleScene() != m_scene)
                return;
            int count = cmdparams.GetLength(0);
            if (count < 2) 
                return;
            if (cmdparams[1] == "general")
            {
                if (count < 3)
                    return;
                string message = CombineParams(cmdparams, 2);
                
                m_log.InfoFormat(
                    "[DIALOG]: Sending general alert in region {0} with message {1}", m_scene.RegionInfo.RegionName, message);
                SendGeneralAlert(message);
            }
            else
            {
                if (count < 4)
                    return;
                string firstName = cmdparams[1];
                string lastName = cmdparams[2];
                string message = CombineParams(cmdparams, 3);
                
                m_log.InfoFormat(
                    "[DIALOG]: Sending alert in region {0} to {1} {2} with message {3}", 
                    m_scene.RegionInfo.RegionName, firstName, lastName, message);  
                SendAlertToUser(firstName, lastName, message, false);
            }
        }

        private string CombineParams(string[] commandParams, int pos)
        {
            string result = string.Empty;
            for (int i = pos; i < commandParams.Length; i++)
            {
                result += commandParams[i] + " ";
            }
            
            return result;
        }        
    }
}
