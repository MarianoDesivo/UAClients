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
        HistoryReadValueIdCollection m_historyReadValueIdsWithContinuationPoint;
        IList<HistoryReadValueId> m_historyReadEventValueIdsWithContinuationPoint;
        #endregion

        #region History Read Raw
        ClientState HistoryReadData()
        {
            m_historyReadValueIdsWithContinuationPoint = new HistoryReadValueIdCollection();
            //! [Configure HistoryRead]
            HistoryReadValueIdCollection nodesToRead = new HistoryReadValueIdCollection();

            foreach (NodeId node in Settings.HistoryVariableIds)
            {
                nodesToRead.Add(new HistoryReadValueId() { NodeId = node });
            }

            ReadRawModifiedDetails details = new ReadRawModifiedDetails()
            {
                StartTime = new DateTime(2017, 1, 1, 0, 0, 0, 0, DateTimeKind.Local),
                EndTime = DateTime.UtcNow,
                NumValuesPerNode = 10,
                IsReadModified = false,
                ReturnBounds = false,
            };
            //! [Configure HistoryRead]

            try
            {
                //! [Call HistoryRead]
                // fetch the history (with a 10s timeout).
                List<HistoryDataReadResult> results = Session.HistoryReadRaw(
                    nodesToRead,
                    details,
                    TimestampsToReturn.Source,
                    new RequestSettings() { OperationTimeout = 10000 });
                //! [Call HistoryRead]

                PrintHistoryReadResultsAndCheckContinuationPoint(nodesToRead, results);
            }
            catch (Exception e)
            {
                Output($"\nHistory read failed: {e.Message}");
            }

            if (m_historyReadValueIdsWithContinuationPoint.Count == 0)
            {
                return ClientState.Connected;
            }
            else
            {
                return ClientState.HistoryReadDone;
            }
        }

        void PrintHistoryReadResultsAndCheckContinuationPoint(HistoryReadValueIdCollection nodesToRead, List<HistoryDataReadResult> results)
        {
            lock (ConsoleLock)
            {
                // check for operation error.
                for (int ii = 0; ii < nodesToRead.Count; ii++)
                {
                    Console.WriteLine($"\nNode {nodesToRead[ii].NodeId}");

                    HistoryDataReadResult res = results[ii];

                    if (StatusCode.IsBad(res.StatusCode))
                    {
                        Console.WriteLine($"History read failed with {res.StatusCode}");
                    }
                    else if (res.DataValues != null)
                    {
                        foreach (DataValue value in res.DataValues)
                        {
                            Console.WriteLine($"  {value.SourceTimestamp}: {value}");
                        }
                        if (res.ContinuationPoint != null)
                        {
                            m_historyReadValueIdsWithContinuationPoint.Add(
                                new HistoryReadValueId()
                                {
                                    NodeId = nodesToRead[ii].NodeId,
                                    ContinuationPoint = res.ContinuationPoint
                                });
                        }
                    }
                }
            }
        }

        ClientState HistoryReadRawNext()
        {
            var nodesToRead = m_historyReadValueIdsWithContinuationPoint;
            m_historyReadValueIdsWithContinuationPoint = new HistoryReadValueIdCollection();
            try
            {
                var results = Session.HistoryReadRaw(
                    nodesToRead,
                    new ReadRawModifiedDetails());
                PrintHistoryReadResultsAndCheckContinuationPoint(nodesToRead, results);
            }
            catch (Exception e)
            {
                Output($"\nHistory read failed: {e.Message}");
            }

            if (m_historyReadValueIdsWithContinuationPoint.Count == 0)
            {
                return ClientState.Connected;
            }
            else
            {
                return ClientState.HistoryReadDone;
            }
        }

        ClientState ReleaseHistoryContinuationPoint()
        {
            var nodesToRead = m_historyReadValueIdsWithContinuationPoint;
            m_historyReadValueIdsWithContinuationPoint = null;
            try
            {
                Session.ReleaseHistoryContinuationPoints(nodesToRead);
            }
            catch (Exception e)
            {
                Output($"\nReleaseContinuationPoints failed: {e.Message}");
            }
            return ClientState.Connected;
        }
        #endregion

        #region History Update Data
        ClientState HistoryUpdateData()
        {
            //! [Configure HistoryUpdate]
            List<UpdateDataDetails> nodesToUpdate = new List<UpdateDataDetails>();
            IList<NodeId> historyIds = Settings.HistoryVariableIds;

            foreach(NodeId nodeId in historyIds)
            {
                nodesToUpdate.Add(new UpdateDataDetails()
                {
                    NodeId = nodeId,
                    PerformInsertReplace = PerformUpdateType.Update,
                    UpdateValues = new DataValueCollection()
                    {
                        new DataValue()
                        {
                            SourceTimestamp = DateTime.UtcNow,
                            ServerTimestamp = DateTime.UtcNow,
                            WrappedValue = new Variant(0.0)
                        }
                    }
                });
            }
            //! [Configure HistoryUpdate]

            try
            {
                //! [Call HistoryUpdate]
                var results = Session.HistoryUpdateData(nodesToUpdate);
                //! [Call HistoryUpdate]

                lock (ConsoleLock)
                {
                    Console.WriteLine("\nHistoryUpdate succeeded");
                    for (int ii = 0; ii < results.Count; ii++)
                    {
                        Console.WriteLine($"  {historyIds[ii]}: {results[ii].StatusCode}");
                    }
                }
            }
            catch (Exception e)
            {
                Output($"\nHistoryUpdate failed: {e.Message}");
            }
            return ClientState.Connected;
        }
        #endregion

        #region History Read Events
        ClientState HistoryReadEvents()
        {
            try
            {
                //! [Set Timespan and EventFilter]
                var readEventDetails = new ReadEventDetails()
                {
                    StartTime = new DateTime(2017, 1, 1, 0, 0, 0, 0, DateTimeKind.Local),
                    EndTime = DateTime.UtcNow,
                    NumValuesPerNode = 10,
                    Filter = new EventFilter()
                    {
                        SelectClauses = new SimpleAttributeOperandCollection()
                    }
                };
                foreach (QualifiedName name in EventFieldsForHistoryRead())
                {
                    readEventDetails.Filter.SelectClauses.Add(
                        new SimpleAttributeOperand()
                        {
                            AttributeId = Attributes.Value,
                            BrowsePath = new QualifiedNameCollection()
                            {
                                name
                            }
                        });
                }
                //! [Set Timespan and EventFilter]

                //! [Load HistoryReadEventConfig]
                IList<HistoryReadValueId> nodesToRead = HistoryReadValueIdsFromConfiguration();
                //! [Load HistoryReadEventConfig]
                //! [Call HistoryReadEvent]
                var results = Session.HistoryReadEvent(nodesToRead, readEventDetails);
                //! [Call HistoryReadEvent]
                Output("\nHistory Read Events succeeded.");

                PrintHistoryReadEventsResultsAndCheckContinuationPoints(nodesToRead, results);

                if (m_historyReadEventValueIdsWithContinuationPoint.Count == 0)
                {
                    return ClientState.Connected;
                }
                else
                {
                    return ClientState.HistoryReadEventsDone;
                }
            }
            catch (Exception e)
            {
                Output($"\nHistory Read Events failed: {e.Message}");
            }
            return ClientState.Connected;
        }


        ClientState HistoryReadEventsNext()
        {
            var nodesToRead = m_historyReadEventValueIdsWithContinuationPoint;
            m_historyReadEventValueIdsWithContinuationPoint = new List<HistoryReadValueId>();
            try
            {
                var results = Session.HistoryReadEvent(
                    nodesToRead,
                    new ReadEventDetails());
                Output("\nHistory Read Events succeeded.");
                PrintHistoryReadEventsResultsAndCheckContinuationPoints(nodesToRead, results);
            }
            catch (Exception e)
            {
                Output($"\nHistory Read Event failed: {e.Message}");
            }

            if (m_historyReadEventValueIdsWithContinuationPoint.Count == 0)
            {
                return ClientState.Connected;
            }
            else
            {
                return ClientState.HistoryReadEventsDone;
            }
        }

        ClientState ReleaseHistoryEventsContinuationPoint()
        {
            var nodesToRead = m_historyReadEventValueIdsWithContinuationPoint;
            m_historyReadEventValueIdsWithContinuationPoint = null;
            try
            {
                Session.ReleaseHistoryContinuationPoints(nodesToRead);
                Output("\nReleaseContinuationPoint succeeded.");
            }
            catch (Exception e)
            {
                Output($"\nReleaseContinuationPoints failed: {e.Message}");
            }
            return ClientState.Connected;
        }

        void PrintHistoryReadEventsResultsAndCheckContinuationPoints(IList<HistoryReadValueId> nodesToRead, List<HistoryEventReadResult> results)
        {
            lock (ConsoleLock)
            {
                m_historyReadEventValueIdsWithContinuationPoint = new List<HistoryReadValueId>();

                var eventFieldNames = EventFieldsForHistoryRead();
                for (int ii = 0; ii < nodesToRead.Count; ii++)
                {
                    Console.WriteLine(nodesToRead[ii].NodeId);
                    int eventCounter = 0;
                    foreach (var historicalEvent in results[ii].Events)
                    {
                        Console.WriteLine($"  Event {eventCounter++}");
                        for (int jj = 0; jj < eventFieldNames.Count; jj++)
                        {
                            Console.WriteLine($"    {eventFieldNames[jj]}: {historicalEvent.EventFields[jj]}");
                        }
                    }
                    if (results[ii].ContinuationPoint != null)
                    {
                        m_historyReadEventValueIdsWithContinuationPoint.Add(
                            new HistoryReadValueId()
                            {
                                NodeId = nodesToRead[ii].NodeId,
                                ContinuationPoint = results[ii].ContinuationPoint
                            });
                    }
                }
            }
        }

        List<HistoryReadValueId> HistoryReadValueIdsFromConfiguration()
        {
            List<HistoryReadValueId> ret = new List<HistoryReadValueId>();

            foreach (NodeId nodeId in Settings.HistoryObjectIds)
            {
                ret.Add(new HistoryReadValueId()
                {
                    NodeId = nodeId
                });
            }

            return ret;
        }

        List<QualifiedName> EventFieldsForHistoryRead()
        {
            return new List<QualifiedName>()
            {
                new QualifiedName(BrowseNames.Severity),
                new QualifiedName(BrowseNames.Time),
                new QualifiedName(BrowseNames.Message)
            };
        }
        #endregion
    }
}
