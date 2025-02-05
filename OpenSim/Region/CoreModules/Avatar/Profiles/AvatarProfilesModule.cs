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
using System.Globalization;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules.Avatar.Profiles
{
    public class AvatarProfilesModule : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;

        public AvatarProfilesModule()
        {
        }

        #region IRegionModule Members

        public void Initialise(Scene scene, IConfigSource config)
        {
            m_scene = scene;
            m_scene.EventManager.OnNewClient += NewClient;
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "AvatarProfilesModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #endregion

        public void NewClient(IClientAPI client)
        {
            client.OnRequestAvatarProperties += RequestAvatarProperty;
            client.OnUpdateAvatarProperties += UpdateAvatarProperties;
        }

        public void RemoveClient(IClientAPI client)
        {
            client.OnRequestAvatarProperties -= RequestAvatarProperty;
            client.OnUpdateAvatarProperties -= UpdateAvatarProperties;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="avatarID"></param>
        public void RequestAvatarProperty(IClientAPI remoteClient, UUID avatarID)
        {
            CachedUserInfo userinfo = m_scene.CommsManager.UserProfileCacheService.GetUserDetails(avatarID);
            UserProfileData profile = (userinfo == null) ? null : userinfo.UserProfile;
            if (null != profile)
            {
                Byte[] charterMember;
                if (profile.CustomType == "")
                {
                    charterMember = new Byte[1];
                    charterMember[0] = (Byte)((profile.UserFlags & 0xf00) >> 8);
                }
                else
                {
                    charterMember = Utils.StringToBytes(profile.CustomType);
                }

                remoteClient.SendAvatarProperties(profile.ID, profile.AboutText,
                                                  Util.UnixToLocalDateTime(profile.Created).ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                                                  charterMember, profile.FirstLifeAboutText, (uint)(profile.UserFlags & 0xff),
                                                  profile.FirstLifeImage, profile.Image, profile.ProfileURL, profile.Partner);
            }
            else
            {
                m_log.Debug("[AvatarProfilesModule]: Got null for profile for " + avatarID.ToString());
            }
        }
        public void UpdateAvatarProperties(IClientAPI remoteClient, UserProfileData newProfile)
        {
            UserProfileData Profile = m_scene.CommsManager.UserService.GetUserProfile(newProfile.ID);

            // if it's the profile of the user requesting the update, then we change only a few things.
            if (remoteClient.AgentId.CompareTo(Profile.ID) == 0)
            {
                Profile.Image = newProfile.Image;
                Profile.FirstLifeImage = newProfile.FirstLifeImage;
                Profile.AboutText = newProfile.AboutText;
                Profile.FirstLifeAboutText = newProfile.FirstLifeAboutText;
                Profile.ProfileURL = newProfile.ProfileURL;
            }
            else
            {
                return;
            }
            
            if (m_scene.CommsManager.UserService.UpdateUserProfile(Profile))
            {
                RequestAvatarProperty(remoteClient, newProfile.ID);
            }
        }

    }
}
