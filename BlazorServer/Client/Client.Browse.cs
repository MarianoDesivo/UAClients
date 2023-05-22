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
        #region Browse
        static byte[] m_continuationPoint = null;

        ClientState Browse()
        {
            NodeId nodeToBrowse = Settings.BrowseVariableId;

            // browse the reference.
            //! [BrowseContext]
            BrowseContext context = new BrowseContext()
            {
                BrowseDirection = BrowseDirection.Both,
                ReferenceTypeId = ReferenceTypeIds.References,
                IncludeSubtypes = true,
                NodeClassMask = 0,
                ResultMask = (uint)BrowseResultMask.None,
                MaxReferencesToReturn = 100
            };
            //! [BrowseContext]

            List<ReferenceDescription> results = null;

            try
            {
                //! [Call Browse]
                results = m_session.Browse(
                    nodeToBrowse,
                    context,
                    null,
                    out m_continuationPoint);
                //! [Call Browse]
                Output("\nBrowse succeeded");
                PrintBrowseResults(results);
            }

            catch (Exception e)
            {
                Output($"\nBrowse failed with message {e.Message}");
            }
            if (m_continuationPoint != null)
            {
                return ClientState.BrowseDone;
            }
            return ClientState.Connected;
        }

        ClientState BrowseAsync()
        {
            NodeId nodeToBrowse = Settings.BrowseVariableId;

            // browse the reference.
            BrowseContext context = new BrowseContext()
            {
                BrowseDirection = BrowseDirection.Both,
                ReferenceTypeId = ReferenceTypeIds.References,
                IncludeSubtypes = true,
                NodeClassMask = 0,
                ResultMask = (uint)BrowseResultMask.None,
                MaxReferencesToReturn = 100
            };

            try
            {
                m_session.BeginBrowse(
                    nodeToBrowse,
                    context,
                    OnBrowseComplete,
                    null);
                Output("\nBeginBrowse succeeded");
            }
            catch (Exception e)
            {
                Output($"\nBeginBrowse failed with message {e.Message}");
            }
            return ClientState.Connected;
        }

        void OnBrowseComplete(IAsyncResult result)
        {
            try
            {
                List<ReferenceDescription> results = Session.EndBrowse(result, out m_continuationPoint);
                Output("\nEndBrowse succeeded\n");
                PrintBrowseResults(results);
            }
            catch (Exception e)
            {
                Output($"\nEndBrowse failed with message {e.Message}");
            }

            if (m_continuationPoint != null)
            {
                // There are more browse results available.
                // To keep this example small, only the synchronous call for BrowseNext is implemented.
                State = ClientState.BrowseDone;
                PrintBrowseDoneMenu();
            }
        }

        ClientState BrowseNext()
        {
            List<ReferenceDescription> results = null;
            try
            {
                //! [Call BrowseNext]
                results = Session.BrowseNext(ref m_continuationPoint);
                //! [Call BrowseNext]
                Output("\nBrowseNext succeeded");
                PrintBrowseResults(results);
            }

            catch (Exception e)
            {
                Output($"\nBrowseNext failed with message {e.Message}");
            }
            if (m_continuationPoint != null)
            {
                return ClientState.BrowseDone;
            }
            return ClientState.Connected;
        }

        ClientState ReleaseContinuationPoint()
        {
            try
            {
                //! [Call ReleaseBrowseContinuationPoint]
                Session.ReleaseBrowseContinuationPoint(m_continuationPoint);
                //! [Call ReleaseBrowseContinuationPoint]
                Output("\nReleaseBrowseContinuationPoint succeeded");
            }
            catch (Exception e)
            {
                Output($"\nReleaseBrowseContinuationPoint failed with message {e.Message}");
            }
            return ClientState.Connected;
        }

        void PrintBrowseResults(List<ReferenceDescription> values)
        {
            lock (ConsoleLock)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    Console.WriteLine($"{i}:  {values[i].NodeId}");
                }
            }
        }
        #endregion

        #region TranslateBrowsePath
        ClientState TranslateBrowsePathToNodeId()
        {
            //! [Setting TranslateBrowsePath]
            NodeId startingNodeId = Settings.TranslateStartingNodeId;

            List<BrowsePath> pathsToTranslate = new List<BrowsePath>();

            BrowsePath browsePath = new BrowsePath
            {
                StartingNode = startingNodeId
            };

            browsePath.RelativePath.Elements.Add(new RelativePathElement()
            {
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true,
                IsInverse = false,
                TargetName = Settings.TranslateElement
            });

            pathsToTranslate.Add(browsePath);
            //! [Setting TranslateBrowsePath]

            try
            {
                //! [Call TranslateBrowsePath]
                List<BrowsePathResult> results = Session.TranslateBrowsePath(pathsToTranslate);
                //! [Call TranslateBrowsePath]
                lock (ConsoleLock)
                {
                    Console.WriteLine("\nTranslateBrowsePath succeeded");
                    foreach (BrowsePathResult res in results)
                    {
                        if (res.StatusCode.IsBad())
                        {
                            Console.WriteLine($"\nTranslateBrowsePathToNodeId result is bad with status {res.StatusCode}");
                        }
                        else
                        {
                            foreach (BrowsePathTarget target in res.Targets)
                            {
                                Console.WriteLine($"\nTranslateBrowsePathtoNodeId result is good with target id: {target.TargetId}");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Output($"\n TranslateBrowsePath failed: {e.Message}");
            }
            return ClientState.Connected;
        }
        #endregion
    }
}
