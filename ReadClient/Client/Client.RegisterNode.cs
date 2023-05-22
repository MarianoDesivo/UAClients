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
        #region Properties
        List<NodeId> RegisteredNodes;
        #endregion

        ClientState RegisterNodes()
        {
            IList<NodeId> nodesToRegister = Settings.ReadVariableIds;
            try
            {
                //! [RegisterNodes]
                RegisteredNodes = Session.RegisterNodes(nodesToRegister, null);
                //! [RegisterNodes]
                Output("\nRegisterNodes succeeded");
            }
            catch (Exception e)
            {
                RegisteredNodes = null;
                Output($"\nRegisterNodes failed with message {e.Message}");
            }
            return ClientState.Connected;
        }

        ClientState RegisterNodesAsync()
        {
            IList<NodeId> nodesToRegister = Settings.ReadVariableIds;
            try
            {
                Session.BeginRegisterNodes(nodesToRegister, null, OnRegisterNodesComplete, null);
                Output("\nBeginRegisterNodes succeeded");
            }
            catch (Exception e)
            {
                Output($"\nBeginRegisterNodes failed with message {e.Message}");
            }
            return ClientState.Connected;
        }

        void OnRegisterNodesComplete(IAsyncResult result)
        {
            try
            {
                RegisteredNodes = Session.EndRegisterNodes(result);
                Output("\nEndRegisterNodes succeeded");
            }
            catch (Exception e)
            {
                RegisteredNodes = null;
                Output($"\nEndRegisterNodes failed with message {e.Message}");
            }
        }

        ClientState UnregisterNodes()
        {
            IList<NodeId> nodesToUnegister = RegisteredNodes;
            if (nodesToUnegister != null)
            {
                try
                {
                    Session.UnregisterNodes(nodesToUnegister, null);
                    RegisteredNodes = null;
                    Output("\nUnregisterNodes succeeded");
                }
                catch (Exception e)
                {
                    Output($"\nUnregisterNodes failed with message {e.Message}");
                }
            }
            else
            {
                Output("\nAborted by client. Nodes have not been registered.");
            }
            return ClientState.Connected;
        }
    }
}
