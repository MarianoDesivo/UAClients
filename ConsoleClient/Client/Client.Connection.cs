/******************************************************************************
** Copyright (c) 2006-2023 Unified Automation GmbH All rights reserved.
**
** Software License Agreement ("SLA") Version 2.8
**
** Unless explicitly acquired and licensed from Licensor under another
** license, the contents of this file are subject to the Software License
** Agreement ("SLA") Version 2.8, or subsequent versions
** as allowed by the SLA, and You may not copy or use this file in either
** source code or executable form, except in compliance with the terms and
** conditions of the SLA.
**
** All software distributed under the SLA is provided strictly on an
** "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED,
** AND LICENSOR HEREBY DISCLAIMS ALL SUCH WARRANTIES, INCLUDING WITHOUT
** LIMITATION, ANY WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
** PURPOSE, QUIET ENJOYMENT, OR NON-INFRINGEMENT. See the SLA for specific
** language governing rights and limitations under the SLA.
**
** Project: .NET based OPC UA Client Server SDK
**
** Description: OPC Unified Architecture Software Development Kit.
**
** The complete license agreement can be found here:
** http://unifiedautomation.com/License/SLA/2.8/
******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;

namespace ConsoleClient
{
    partial class Client
    {
        #region Simple Connect
        ClientState SimpleConnect()
        {
            return SimpleConnect(false);
        }

        ClientState SimpleConnectWithSecurity()
        {
            return SimpleConnect(true);
        }

        ClientState SimpleConnect(bool security)
        {
            try
            {
                //! [Simple Connect]
                Session.Connect(Settings.Connection.DiscoveryUrl, security ? SecuritySelection.BestAvailable : SecuritySelection.None);
                //! [Simple Connect]
                Output($"\nSuccessfully connected to {Settings.Connection.DiscoveryUrl}");
                Output($"    RevicedSessionTimeout: {Session.RevicedSessionTimeout}");
                try
                {
                    //! [Access Namespaces]
                    Settings.CurrentNamespaceTable = Session.NamespaceUris;
                    //! [Access Namespaces]
                }
                catch (Exception e)
                {
                    Output($"Mapping of nodes failed. Check the configuration. {e.Message}");
                }
                return ClientState.Connected;
            }
            catch (Exception e)
            {
                LogException(e, $"\nConnecting to {Settings.Connection.DiscoveryUrl} failed");
            }
            return ClientState.Disconnected;
        }
        #endregion

        #region Advanced Connect
        ClientState ConnectToEndpoint()
        {
            try
            {
                //! [Connect Endpoint]
                Session.Connect(SelectedEndpoint, null);
                //! [Connect Endpoint]
                Output($"\nSuccessfully connected to {SelectedEndpoint.EndpointUrl}");
                Output($"    RevicedSessionTimeout: {Session.RevicedSessionTimeout}");
                try
                {
                    Settings.CurrentNamespaceTable = Session.NamespaceUris;
                }
                catch (Exception e)
                {
                    Output($"Mapping of nodes failed. Check the configuration. {e.Message}");
                }
                return ClientState.Connected;
            }
            catch (Exception e)
            {
                LogException(e, $"\nConnecting to {Settings.Connection.DiscoveryUrl} failed");
            }
            return ClientState.Disconnected;
        }
        #endregion

        #region Disconnect
        ClientState Disconnect()
        {
            try
            {
                Session.Disconnect();
                Output("\nDisconnect.");
                return ClientState.Disconnected;
            }
            catch (Exception e)
            {
                Output($"\nDisconnect failed with message {e.Message}");
                return State;
            }
        }
        #endregion

        #region User Identity
        ClientState SetUsername()
        {
            //! [Set UserName]
            if (Session.UserIdentity == null)
            {
                Session.UserIdentity = new UserIdentity();
            }
            Session.UserIdentity.IdentityType = UserIdentityType.UserName;
            Session.UserIdentity.UserName = Settings.Connection.UserName;
            Session.UserIdentity.Password = Settings.Connection.Password;
            //! [Set UserName]

            return ClientState.Disconnected;
        }

        ClientState SetUserToAnonymous()
        {
            if (Session.UserIdentity == null)
            {
                Session.UserIdentity = new UserIdentity();
            }

            Session.UserIdentity.IdentityType = UserIdentityType.Anonymous;
            return ClientState.Disconnected;
        }
        #endregion

        #region Change User
        ClientState ChangeUser()
        {
            try
            {
                if (Session.UserIdentity == null)
                {
                    Session.UserIdentity = new UserIdentity();
                }

                if (Session.UserIdentity.IdentityType == UserIdentityType.Anonymous)
                {
                    //! [change User]
                    Session.UserIdentity.IdentityType = UserIdentityType.UserName;
                    Session.UserIdentity.UserName = Settings.Connection.UserName;
                    Session.UserIdentity.Password = Settings.Connection.Password;
                    //! [change User]
                }
                else
                {
                    Session.UserIdentity.IdentityType = UserIdentityType.Anonymous;
                 }
                //! [apply changed User]
                Session.ChangeUser();
                //! [apply changed User]
                Output("\nChangeUser succeeded.");
            }
            catch(Exception e)
            {
                Output($"\nChangeUser failed with message: {e.Message}");
            }
            return ClientState.Connected;
        }
        #endregion

        #region EventHandler
        void OnConnectionStatusUpdate(Session sender, ServerConnectionStatusUpdateEventArgs e)
        {
            Output($"\n{DateTime.Now}: ConnectionStatusUpdate: {e.PreviousStatus} -> {e.Status}");
            if (e.Status == ServerConnectionStatus.SessionAutomaticallyRecreated)
            {
                // Node are not re-registered when a session is recreated.
                RegisteredNodes = null;
            }
        }
        #endregion

#if !REVERSE_CONNECT_UNSUPPORTED
        #region Simple Reverse Connect
        ClientState SimpleReverseConnect()
        {
            try
            {
                if (String.IsNullOrEmpty(Settings.Connection.ClientUrlForReverseConnect))
                {
                    Output("ReverseConnect not possible because ClientUrlForReverseConnect is not set.");
                    return ClientState.Disconnected;
                }

                //! [Simple Reverse Connect]
                Session.ReverseConnect(Settings.Connection.DiscoveryUrl, SecuritySelection.None);
                //! [Simple Reverse Connect]
                Output($"\nSuccessfully connected to {Settings.Connection.DiscoveryUrl}");
                Output($"    RevicedSessionTimeout: {Session.RevicedSessionTimeout}");
                try
                {
                    Settings.CurrentNamespaceTable = Session.NamespaceUris;
                }
                catch (Exception e)
                {
                    Output($"Mapping of nodes failed. Check the configuration. {e.Message}");
                }
                return ClientState.Connected;
            }
            catch (Exception e)
            {
                LogException(e, $"\nConnecting to {Settings.Connection.DiscoveryUrl} failed");
            }
            return ClientState.Disconnected;
        }
        #endregion
#endif
    }
}
