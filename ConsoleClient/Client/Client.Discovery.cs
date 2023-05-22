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
        List<string> DiscoveryUrls { get; set; }
        List<EndpointDescription> Endpoints { get; set; }
        EndpointDescription SelectedEndpoint;
        string SelectedDiscoveryUrl = null;
        List<string> ReverseDiscoveryUrls
        {
            get
            {
                var ret = new List<string>();
                foreach (var group in Session.IncomingReverseConnections)
                {
                    if (group.Connections.Count > 0)
                    {
                        ret.Add(group.Connections[0].EndpointUrl);
                    }
                }
                return ret;
            }
        }
#if !REVERSE_CONNECT_UNSUPPORTED
        System.Threading.AutoResetEvent ResetEvent;
#endif

        ClientState StartDiscovery()
        {
            return ClientState.StartDiscovery;
        }

        ClientState GetEndpoints()
        {
            string discoveryUrl = string.IsNullOrEmpty(SelectedDiscoveryUrl) ? Settings.Connection.DiscoveryUrl : SelectedDiscoveryUrl;
            try
            {
                //! [GetEndpoints]
                using (Discovery discovery = new Discovery(Application))
                {
                    Endpoints = discovery.GetEndpoints(discoveryUrl);
                    Output("\nGetEndpoints succeeded");
                }
                //! [GetEndpoints]
            }
            catch (Exception e)
            {
                Endpoints = null;
                LogException(e, "\nGetEndpoints failed");
            }

            if (Endpoints != null && Endpoints.Count > 0)
                return ClientState.GetEndpointsDone;
            else
                return ClientState.Disconnected;
        }

        ClientState FindServers()
        {
            try
            {
                using (Discovery discovery = new Discovery(Application))
                {
                    List<ApplicationDescription> Servers = discovery.FindServers(Settings.Connection.DiscoveryUrl);
                    Output("\nFindServers succeeded");
                    CreateEmptyDiscoveryUrls();

                    foreach (ApplicationDescription server in Servers)
                    {
                        foreach (string discoveryUrl in server.DiscoveryUrls)
                        {
                            DiscoveryUrls.Add(discoveryUrl);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogException(e, "\nFindServers failed");
            }
            if (DiscoveryUrls == null || DiscoveryUrls.Count == 0)
            {
                return ClientState.Disconnected;
            }
            return ClientState.FindServersDone;
        }

        void CreateEmptyDiscoveryUrls()
        {
            if (DiscoveryUrls == null)
            {
                DiscoveryUrls = new List<string>();
            }
            else
            {
                DiscoveryUrls.Clear();
            }
        }

#if !REVERSE_CONNECT_UNSUPPORTED
        #region Reverse Discovery
        ClientState StartReverseDiscovery()
        {
            if (String.IsNullOrEmpty(Settings.Connection.ClientUrlForReverseConnect))
            {
                Output("ReverseConnect not possible because ClientUrlForReverseConnect is not set.");
                return ClientState.Disconnected;
            }

            return ClientState.StartReverseDiscovery;
        }

        ClientState WaitForConnectionGroups()
        {
            using (Discovery discovery = new Discovery(Application, Settings.Connection.ClientUrlForReverseConnect))
            {
                if (ResetEvent == null)
                {
                    ResetEvent = new System.Threading.AutoResetEvent(false);
                }
                else
                {
                    ResetEvent.Reset();
                }
                discovery.Application.ReverseConnectManager.IncomingConnectionOpened += ReverseConnect_IncomingConnectionOpened;
                ResetEvent.WaitOne(10000);
            }
            return ClientState.StartReverseDiscovery;
        }

        void ReverseConnect_IncomingConnectionOpened(object sender, IncomingReverseConnectionGroupUpdateEventArgs e)
        {
            if (e.ConnectionGroup.Connections.Count > 0)
            {
                ApplicationInstanceBase.Default.ReverseConnectManager.IncomingConnectionOpened -= ReverseConnect_IncomingConnectionOpened;
                ResetEvent.Set();
            }
        }

        ClientState ReverseFindServers()
        {
            try
            {
                using (Discovery discovery = new Discovery(Application, Settings.Connection.ClientUrlForReverseConnect))
                {
                    CreateEmptyDiscoveryUrls();
                    List<ApplicationDescription> Servers = discovery.ReverseFindServers(SelectedDiscoveryUrl);
                    Output("\nFindServers succeeded");

                    foreach (ApplicationDescription server in Servers)
                    {
                        foreach (string discoveryUrl in server.DiscoveryUrls)
                        {
                            DiscoveryUrls.Add(discoveryUrl);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogException(e, "\nRerverseFindServers failed");
            }
            if (DiscoveryUrls == null || DiscoveryUrls.Count == 0)
            {
                return ClientState.Disconnected;
            }
            return ClientState.ReverseFindServersDone;
        }

        ClientState ReverseGetEndpoints()
        {
            string discoveryUrl = string.IsNullOrEmpty(SelectedDiscoveryUrl) ? Settings.Connection.DiscoveryUrl : SelectedDiscoveryUrl;
            try
            {
                using (Discovery discovery = new Discovery(Application, Settings.Connection.ClientUrlForReverseConnect))
                {
                    Endpoints = discovery.ReverseGetEndpoints(discoveryUrl);
                    Output("\nReverseGetEndpoints succeeded");
                }
            }
            catch (Exception e)
            {
                Endpoints = null;
                LogException(e, "\nReverseGetEndpoints failed");
            }

            if (Endpoints != null && Endpoints.Count > 0)
                return ClientState.ReverseGetEndpointsDone;
            else
                return ClientState.Disconnected;
        }


        ClientState ReverseConnectToEndpoint()
        {
            try
            {
                //! [Connect Endpoint]
                Session.ReverseConnect(SelectedEndpoint, null);
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
                LogException(e, $"\nReverseConnect to {SelectedEndpoint.EndpointUrl} failed");
            }
            return ClientState.Disconnected;
        }
        #endregion
#endif
    }
}
