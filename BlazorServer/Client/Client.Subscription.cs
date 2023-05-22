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
        #region Private Fields
        Subscription m_subscription = null;
        Dictionary<NodeId, EventFieldList> m_alarms;
        List<EventFieldList> m_alarmList; // values of m_alarms
        int m_selectedAlarmIndex;
        bool m_AcknowledgeMenuUpToDate;
        #endregion


        #region Create / Modify / Delete Subscription
        ClientState CreateSubscription()
        {
            if (m_subscription == null)
            {
                try
                {
                    //! [Create Subscription]
                    m_subscription = new Subscription(Session)
                    {
                        PublishingEnabled = true,
                        PublishingInterval = 100
                    };
                    if (Settings.ClientSettings.SubscriptionLifetime != 0)
                    {
                        m_subscription.Lifetime = Settings.ClientSettings.SubscriptionLifetime;
                    }
                    m_subscription.DataChanged += OnDataChanged;
                    m_subscription.NewEvents += OnEvent;
                    m_subscription.NotificationMessageReceived += OnNotificationMessageReceived;
                    m_subscription.StatusChanged += OnStatusChanged;
                    m_subscription.Recreated += OnSubscriptionRecreated;
                    m_subscription.Create();
                    //! [Create Subscription]
                    m_alarms = new Dictionary<NodeId, EventFieldList>();
                    Output("\nCreate subscription succeeded");
                    Output($"    CurrentLifetime: {m_subscription.CurrentLifetime} ms");
                }
                catch (Exception e)
                {
                    Output($"\nCreate subscription failed with message {e.Message}");
                }
            }
            else
            {
                Output("\nAborted by client. Subscription already created.");
            }
            return ClientState.Connected;
        }

        void OnSubscriptionRecreated(Subscription subscription, SubscriptionRecreatedEventArgs e)
        {
            Output($"\n{DateTime.Now}: Subscription with {subscription.MonitoredItems.Length} MonitoredItems recreated.");
            if (e.Exception != null)
            {
                Console.WriteLine($"    Exception on recreate: {e.Exception}");
            }
            if (e.NotMappedMonitoredItems.Count + e.RemovedMonitoredItems.Count > 0)
            {
                lock (ConsoleLock)
                {
                    foreach (var monitoredItem in e.NotMappedMonitoredItems)
                    {
                        Console.WriteLine($"    {monitoredItem.NodeId} could not be recreated: Namespace could not be mapped.");
                    }
                    foreach (var monitoredItem in e.RemovedMonitoredItems)
                    {
                        Console.WriteLine($"    {monitoredItem.NodeId} could not be recreated: {monitoredItem.LastError}");
                    }
                }
            }
        }

        void OnStatusChanged(Subscription subscription, SubscriptionStatusChangedEventArgs e)
        {
            Output($"\n{DateTime.Now}: SubscriptionStatus changed from {e.OldStatus} to {e.CurrentStatus}");
        }

        #region EventHandlers
        //! [DataChanged EventHandler]
        void OnDataChanged(Subscription subscription, DataChangedEventArgs e)
        {
            //Process DataChanges in separate threads/tasks to avoid blocking the client application.
            this.Application.ThreadPool.Queue(e, ProcessDataChange);
        }
        //! [DataChanged EventHandler]


        void ProcessDataChange(object data, StatusCode error)
        {
            DataChangedEventArgs e = data as DataChangedEventArgs;

            lock (ConsoleLock)
            {
                
                //Console.WriteLine("---- DataChanges received:");
                foreach (var dataChange in e.DataChanges)
                {
                    if (dataChange.MonitoredItem.NodeId.Identifier == "Demo.Processor.Utilization")
                    {
                        BlazorServer.Data.PCInfo.CpuUtilization = dataChange.Value.Value.ToString();
                    }
                    if (dataChange.MonitoredItem.NodeId.Identifier == "Demo.Processor.Count")
                    {
                        BlazorServer.Data.PCInfo.ProcessorCount = dataChange.Value.Value.ToString();
                    }
                    if (dataChange.MonitoredItem.NodeId.Identifier == "Demo.Memory.Available")
                    {
                        BlazorServer.Data.PCInfo.RAM = dataChange.Value.Value.ToString();
                    }

                    // Print result for variable - check first the result code
                    if (StatusCode.IsGood(dataChange.Value.StatusCode))
                    {
                        // The node succeeded - print the value as string
                        Console.WriteLine($": {dataChange.Value.WrappedValue}");
                    }
                    else
                    {
                        Console.WriteLine($" failed with StatusCode: {dataChange.Value.StatusCode}");
                    }
                }
            }
        }

        static string EventFieldName(QualifiedNameCollection qualifiedNames)
        {
            return string.Join("/", qualifiedNames.Select(q => q.ToString()).ToArray());
        }

        //! [NewEvents EventHandler]
        void OnEvent(Subscription subscription, NewEventsEventArgs e)
        {
            //Process Events in separate threads/tasks to avoid blocking the client application.
            this.Application.ThreadPool.Queue(e, ProcessEvent);
        }

        void ProcessEvent(object data, StatusCode error)
        {
            NewEventsEventArgs e = data as NewEventsEventArgs;

            lock (ConsoleLock)
            {
                foreach (NewEvent anEvent in e.Events)
                {
                    Console.WriteLine($"\nEvent occurred: {anEvent.Event.EventFields[2]}");
                    for (int ii = 0; ii < anEvent.Event.EventFields.Count; ii++)
                    {
                        Console.WriteLine($"  {EventFieldName(anEvent.MonitoredItem.Filter.SelectClauses[ii].BrowsePath)}: {anEvent.Event.EventFields[ii]}");
                    }
                    //! [NewEvents EventHandler]

                    AddAlarm(anEvent);
                }
            }

            if (State == ClientState.AcknowledgeAlarms && !m_AcknowledgeMenuUpToDate)
            {
                PrintAcknowledgeAlarmsMenu();
            }

        }
        void AddAlarm(NewEvent anEvent)
        {
            if (anEvent.Event.EventFields[0].DataType == BuiltInType.NodeId
                && anEvent.Event.EventFields[0].ValueRank == ValueRanks.Scalar
                && anEvent.Event.EventFields[9].DataType == BuiltInType.Boolean
                && anEvent.Event.EventFields[9].ValueRank == ValueRanks.Scalar)
            {
                NodeId conditionId = anEvent.Event.EventFields[0].ToNodeId();
                bool retain = anEvent.Event.EventFields[9].ToBoolean();

                if (retain)
                {
                    m_alarms[conditionId] = anEvent.Event;
                }
                else
                {
                    if (m_alarms.ContainsKey(conditionId))
                    {
                        m_alarms.Remove(conditionId);
                    }
                }

                m_AcknowledgeMenuUpToDate = false;
            }
        }

        static bool IsKeepAlive(ExtensionObjectCollection extensionObjects)
        {
            return extensionObjects.Count == 0;
        }

        void OnNotificationMessageReceived(Subscription subscription, NotificationMessageReceivedEventArgs e)
        {
            if (IsKeepAlive(e.NotificationMessage.NotificationData))
            {
                Output("KeepAlive received");
            }
            // DataChanges and events are handles by other EventHandlers
        }

        #endregion

        ClientState ModifySubscription()
        {
            if (m_subscription != null)
            {
                if (m_subscription.PublishingEnabled)
                {
                    m_subscription.PublishingEnabled = false;
                    Output("Disable Publishing");
                }
                else
                {
                    m_subscription.PublishingEnabled = true;
                    Output("Enable Publishing");
                }
                try
                {
                    m_subscription.Modify();
                    Output("\nModify Subscription succeeded");
                }
                catch (Exception e)
                {
                    Output($"\nModify Subscription failed with message {e.Message}");
                }
            }
            else
            {
                Output("\nAborted by client. Subscription has not been created.");
            }
            return ClientState.Connected;
        }

        ClientState DeleteSubscription()
        {
            if (m_subscription != null)
            {
                try
                {
                    m_subscription.Delete();
                    Output("\nDelete Subscription succeeded");
                }
                catch (Exception e)
                {
                    Output($"\nDelete Subscription failed with message: {e.Message}");
                }
                finally
                {
                    m_subscription = null;
                }
            }
            else
            {
                Output("\nAborted by client. Subscription has not been created.");
            }
            return ClientState.Connected;
        }
        #endregion

        #region Create DataMonitoredItems
        ClientState AddMonitoredItems()
        {
            if (m_subscription == null)
            {
                CreateSubscription();
            }

            //! [Create DataMonitoredItems]
            IList<MonitoredItem> monitoredItems = ItemsToMonitorFromConfiguration();

            try
            {
                List<StatusCode> results = m_subscription.CreateMonitoredItems(monitoredItems);
                //! [Create DataMonitoredItems]
                PrintCreateMonitoredItemsResult(monitoredItems, results);
            }
            catch (Exception e)
            {
                Output($"Create MonitoredItems failed: {e.Message}");
            }

            return ClientState.Connected;
        }

        static void PrintCreateMonitoredItemsResult(IList<MonitoredItem> monitoredItems, List<StatusCode> results)
        {
            lock (ConsoleLock)
            {
                Console.WriteLine("\nCreate MonitoredItems succeeded");

                for (int ii = 0; ii < results.Count; ii++)
                {
                    if (StatusCode.IsBad(results[ii]))
                    {
                        Console.WriteLine($"[{monitoredItems[ii].NodeId}] Create MonitoredItem failed: {results[ii]}");
                    }
                    else
                    {
                        Console.WriteLine($"[{monitoredItems[ii].NodeId}] Create MonitoredItem succeeded - RevisedQueueSize {monitoredItems[ii].CurrentQueueSize}");
                    }
                }
            }
        }

        ClientState AddMonitoredItemsWithDeadbandAndQueueSize()
        {
            if (m_subscription == null)
            {
                CreateSubscription();
            }

            IList<MonitoredItem> monitoredItems = ItemsToMonitorWithDeadbandAndQueueFromConfiguration();

            try
            {
                List<StatusCode> results = m_subscription.CreateMonitoredItems(monitoredItems);
                PrintCreateMonitoredItemsResult(monitoredItems, results);
            }
            catch (Exception e)
            {
                Output($"Create MonitoredItems failed: {e.Message}");
            }

            return ClientState.Connected;
        }
        #endregion

        #region Delete DataMonitoredItems
        ClientState DeleteMonitoredItems()
        {
            try
            {
                if (m_subscription != null)
                {
                    // Find MonitoredItems to delete.
                    IList<MonitoredItem> monitoredItemsToDelete = new List<MonitoredItem>();
                    foreach (var monitoredItem in m_subscription.MonitoredItems)
                    {
                        foreach (NodeId nodeId in Settings.ReadVariableIds)
                        {
                            if (monitoredItem.NodeId == nodeId)
                            {
                                monitoredItemsToDelete.Add(monitoredItem);
                            }
                        }
                        if (monitoredItem.NodeId == Settings.EventId)
                        {
                            monitoredItemsToDelete.Add(monitoredItem);
                        }
                    }
                    m_subscription.DeleteMonitoredItems(monitoredItemsToDelete);
                    Output("\nDeleteMonitoredItems succeeded.");
                }
            }
            catch (Exception e)
            {
                Output($"\nDeleteMonitoredItems failed: {e.Message}");
            }
            return ClientState.Connected;
        }
        #endregion


        #region Transfer Subscription
        ClientState TransferSubscrition()
        {
            if (m_subscription != null)
            {
                EndpointDescription endpoint = Session.EndpointDescription;
                Session.Disconnect(SubscriptionCleanupPolicy.Keep);
                Console.Write("\n Transfer Subscription");

                /* wait some seconds for pending events */
                for (int a = 0; a < 5; ++a)
                {
                    Console.Write($" {5 - a}");
                    System.Threading.Thread.Sleep(1000);
                }
                Session.Connect(endpoint, null);
                try
                {
                    //! [Call TransferSubscription]
                    StatusCode ret = Session.TransferSingleSubscription(m_subscription);
                    //! [Call TransferSubscription]
                    if (ret.IsBad())
                    {
                        Output($"TransferSubscription failed {ret}");
                    }
                }
                catch (Exception e)
                {
                    Output($"TransferSubscription failed with message {e.Message}");
                }
            }
            else
            {
                Output("\nAborted by client. Subscription has not been created.");
            }
            return ClientState.Connected;
        }
        #endregion

        #region Create EventMonitoredItems
        ClientState CreateEventMonitoredItems()
        {
            try
            {
                if (m_subscription == null)
                {
                    CreateSubscription();
                }
                List<MonitoredItem> monitoredItems = new List<MonitoredItem>();

                //! [Create EventMonitoredItem]
                EventMonitoredItem monitoredItem = new EventMonitoredItem(Settings.EventId)
                {
                    //! [Create EventMonitoredItem]

                    //! [Create EventMonitoredItemFilter]
                    Filter = new ItemEventFilter(m_subscription.Session.NamespaceUris)
                    {
                        SelectClauses =
                        {
                            BrowseNames.EventId,
                            BrowseNames.EventType,
                            BrowseNames.Message,
                            BrowseNames.Severity,
                            BrowseNames.SourceName,
                            BrowseNames.Time
                        }
                    }
                };
                //! [Create EventMonitoredItemFilter]

                if (Settings.EventOfTypeFilter != null)
                {
                    //! [Create EventMonitoredItemFilterWhere]
                    monitoredItem.Filter.WhereClauses.Add(FilterOperator.OfType, new LiteralOperand() { Value = new Variant(Settings.EventOfTypeFilter) });
                    //! [Create EventMonitoredItemFilterWhere]

                }

                monitoredItems.Add(monitoredItem);


                // create monitored items.
                List<StatusCode> results = m_subscription.CreateMonitoredItems(
                    monitoredItems,
                    new RequestSettings() { OperationTimeout = 10000 });

                PrintCreateMonitoredItemsResult(monitoredItems, results);
            }
            catch (Exception e)
            {
                Output($"\nCreateMonitorItems failed with message {e.Message}");
            }
            return ClientState.Connected;
        }

        ClientState SubscribeForAlarm()
        {
            if (m_subscription == null)
            {
                CreateSubscription();
            }

            EventMonitoredItem monitoredItem = new EventMonitoredItem(ObjectIds.Server)
            {
                Filter = new UnifiedAutomation.UaClient.ItemEventFilter(m_subscription.Session.NamespaceUris)
            };

            // build the filter.
            //! [Select ConditionId]
            /*00*/ monitoredItem.Filter.SelectClauses.AddConditionId();
            //! [Select ConditionId]
            /*01*/
            monitoredItem.Filter.SelectClauses.Add(BrowseNames.EventId);
            /*02*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.EventType);
            /*03*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.Message);
            /*04*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.Severity);
            /*05*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.SourceName);
            /*06*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.Time);
            /*07*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.ConditionName);
            /*08*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.ConditionClassId);
            /*09*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.Retain);
            /*10*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.Comment);
            /*11*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.EnabledState);
            /*12*/ monitoredItem.Filter.SelectClauses.Add(AbsoluteName.ToString(BrowseNames.EnabledState, BrowseNames.Id));
            /*13*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.AckedState);
            /*14*/ monitoredItem.Filter.SelectClauses.Add(AbsoluteName.ToString(BrowseNames.AckedState, BrowseNames.Id));
            /*15*/ monitoredItem.Filter.SelectClauses.Add(BrowseNames.ActiveState);
            /*16*/ monitoredItem.Filter.SelectClauses.Add(AbsoluteName.ToString(BrowseNames.ActiveState, BrowseNames.Id));
            /*17*/ monitoredItem.Filter.SelectClauses.Add(new ExpandedNodeId(ObjectTypes.ConditionType, 0), new QualifiedName[] { BrowseNames.EnabledState, BrowseNames.TransitionTime });
            /*18*/ monitoredItem.Filter.SelectClauses.Add(new ExpandedNodeId(ObjectTypes.AlarmConditionType, 0), new QualifiedName[] { BrowseNames.ShelvingState, BrowseNames.UnshelveTime });

            monitoredItem.Filter.WhereClauses.Add(
                FilterOperator.Equals,
                BrowseNames.SourceName,
                new Variant("Boiler1"));

            m_subscription.CreateMonitoredItems(new List<MonitoredItem>() { monitoredItem });

            //! [Call CondtionRefresh]
            List<StatusCode> inputResults;
            List<Variant> outArguments;
            Session.Call(
                ObjectTypeIds.ConditionType,
                MethodIds.ConditionType_ConditionRefresh,
                new List<Variant>() { m_subscription.SubscriptionId },
                out inputResults,
                out outArguments);
            return ClientState.Connected;
            //! [Call CondtionRefresh]
        }

        #endregion

        #region Acknowledge Alarms
        ClientState AcknowledgeAlarms()
        {
            if (m_alarms == null || m_alarms.Count == 0)
            {
                Output("\nNo Alarms to acknowledge");
                return ClientState.Connected;
            }
            m_AcknowledgeMenuUpToDate = false;
            return ClientState.AcknowledgeAlarms;
        }

        void PrintAlarmList()
        {
            m_alarmList = new List<EventFieldList>();
            foreach (var anEvent in m_alarms)
            {
                m_alarmList.Add(anEvent.Value);
            }
            lock (ConsoleLock)
            {
                Console.WriteLine(" - Number: Acknowledged - Time - Severity - Source - Message");
                for (int ii = 0; ii < m_alarmList.Count; ii++)
                {
                    bool acknowledged = m_alarmList[ii].EventFields[14].ToBoolean();
                    byte[] eventId = m_alarmList[ii].EventFields[1].ToByteString();
                    DateTime time = m_alarmList[ii].EventFields[6].ToDateTime();
                    ushort severity = m_alarmList[ii].EventFields[4].ToUInt16();
                    string source = m_alarmList[ii].EventFields[5].ToString();
                    LocalizedText message = m_alarmList[ii].EventFields[3].ToLocalizedText();
                    Console.WriteLine($" - Alarm {ii}:  {BitConverter.ToString(eventId)} - {acknowledged} - {time} - {severity} - {source} - {message}");
                }
            }
            m_AcknowledgeMenuUpToDate = true;
        }

        ClientState AcknowledgeAll()
        {
            List<CallMethodRequest> methods = new List<CallMethodRequest>();
            try
            {
                //! [Acknowledge All Alarms]
                foreach (var alarm in m_alarmList)
                {
                    methods.Add(new CallMethodRequest()
                    {
                        ObjectId = alarm.EventFields[0].ToNodeId(),
                        MethodId = MethodIds.AcknowledgeableConditionType_Acknowledge,
                        InputArguments = new VariantCollection()
                        {
                            new Variant(alarm.EventFields[1].ToByteString()),
                            new Variant(new LocalizedText("Acknowledge all from ConsoleClient"))
                        }
                    });
                }
                var results = Session.CallList(methods, null);
                //! [Acknowledge All Alarms]

                lock (ConsoleLock)
                {
                    Console.WriteLine("\nCall Acknowledge succeeded");
                    for (int ii = 0; ii < results.Count; ii++)
                    {
                        var result = results[ii];
                        var state = result.StatusCode.IsGood() ? "succeeded" : "failed";
                        Console.WriteLine($"Acknowledge for alarm {ii} {state} with StatusCode {result.StatusCode}");
                    }
                }
            }
            catch (Exception e)
            {
                Output($"\nCall Acknowledge failed with message {e.Message}");
            }
            
            return ClientState.AcknowledgeAlarms;
        }

        ClientState AcknowledgeSingle()
        {
            try
            {
                var alarm = m_alarmList[m_selectedAlarmIndex];
                List<StatusCode> inputArgumentResults;
                List<Variant> outputArguments;
                //! [Acknowledge Single Alarm]
                var result = Session.Call(
                    alarm.EventFields[0].ToNodeId(),
                    MethodIds.AcknowledgeableConditionType_Acknowledge,
                    new List<Variant>()
                    {
                        new Variant(alarm.EventFields[1].ToByteString()),
                        new Variant(new LocalizedText("Acknowledge from ConsoleClient"))
                    },
                    out inputArgumentResults,
                    out outputArguments);
                //! [Acknowledge Single Alarm]
                lock (ConsoleLock)
                {
                    var state = result.IsGood() ? "succeeded" : "failed";
                    Console.WriteLine("\nCall Acknowledge succeeded");
                    Console.WriteLine("Acknowledge for alarm {state} with StatusCode {result}");
                }
            }
            catch (Exception e)
            {
                Output($"\nCall Acknowledge failed with message {e.Message}");
            }
            return ClientState.AcknowledgeAlarms;
        }
        #endregion

        #region Information from Configuration
        //! [ItemsToMonitorFromConfiguration]
        IList<MonitoredItem> ItemsToMonitorFromConfiguration()
        {
            IList <MonitoredItem> result = new List<MonitoredItem>();
            foreach (NodeId nodeId in Settings.ReadVariableIds)
            {
                DataMonitoredItem monitoredItem = new DataMonitoredItem(nodeId)
                {
                    SamplingInterval = 0
                };
                result.Add(monitoredItem);
            }
            return result;
        }
        //! [ItemsToMonitorFromConfiguration]

        IList<MonitoredItem> ItemsToMonitorWithDeadbandAndQueueFromConfiguration()
        {
            IList<MonitoredItem> result = new List<MonitoredItem>();
            foreach (NodeId nodeId in Settings.ReadVariableIds)
            {
                DataMonitoredItem monitoredItem = new DataMonitoredItem(nodeId)
                {
                    Deadband = 1,
                    DeadbandType = DeadbandType.Absolute,
                    UserData = nodeId.ToString() + " - Deadband"
                };
                result.Add(monitoredItem);
            }
            foreach (NodeId nodeId in Settings.ReadVariableIds)
            {
                DataMonitoredItem monitoredItem = new DataMonitoredItem(nodeId)
                {
                    QueueSize = 100,
                    UserData = nodeId.ToString() + " - QueueSize"
                };
                result.Add(monitoredItem);
            }
            return result;
        }
        #endregion
    }
}
