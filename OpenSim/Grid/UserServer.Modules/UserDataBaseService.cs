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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Grid.Framework;

namespace OpenSim.Grid.UserServer.Modules
{
    public class UserDataBaseService : UserManagerBase
    {
        protected IGridServiceCore m_core;

        public UserDataBaseService(CommunicationsManager commsManager)
            : base(commsManager)
        {
        }

        public void Initialise(IGridServiceCore core)
        {
            m_core = core;

            UserConfig cfg;
            if (m_core.TryGet<UserConfig>(out cfg))
            {
                AddPlugin(cfg.DatabaseProvider, cfg.DatabaseConnect);
            }

            m_core.RegisterInterface<UserDataBaseService>(this);
        }

        public void PostInitialise()
        {
        }

        public void RegisterHandlers(BaseHttpServer httpServer)
        {
        }

        public UserAgentData GetUserAgentData(UUID AgentID)
        {
            UserProfileData userProfile = GetUserProfile(AgentID);

            if (userProfile != null)
            {
                return userProfile.CurrentAgent;
            }

            return null;
        }

        public override UserProfileData SetupMasterUser(string firstName, string lastName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override UserProfileData SetupMasterUser(string firstName, string lastName, string password)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override UserProfileData SetupMasterUser(UUID uuid)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
