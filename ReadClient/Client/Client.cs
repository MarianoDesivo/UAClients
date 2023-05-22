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
using System.ServiceModel.Channels;
using System.Text;

using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;

namespace ConsoleClient
{
    public partial class Client
    {
        enum ClientState
        {
            Disconnected,
            StartDiscovery,
            GetEndpointsDone,
            FindServersDone,
            StartReverseDiscovery,
            ReverseFindServersDone,
            ReverseGetEndpointsDone,
            Connected,
            BrowseDone,
            HistoryReadDone,
            HistoryReadEventsDone,
            AcknowledgeAlarms,
            Shutdown
        }

        #region Properties
        Configuration.IClientConfiguration Settings { get; set; }
        ClientState State { get; set; }

        ApplicationInstanceBase Application
        {
            get { return ApplicationInstanceBase.Default; }
        }

        Session Session
        {
            get
            {
                if (m_session == null)
                {
                    if (String.IsNullOrEmpty(Settings.Connection.ClientUrlForReverseConnect))
                    {
                        m_session = new Session(Application);
                    }
                    else
                    {
                        //! [Create Reverse Session]
                        try
                        {
                            m_session = new Session(Application, Settings.Connection.ClientUrlForReverseConnect);
                        }
                        catch (System.Net.Sockets.SocketException se)
                        {
                            LogException(se, "\nCreating Session failed");
                            Output($"Check if other application binds the port {Settings.Connection.ClientUrlForReverseConnect}");
                            throw;
                        }
                        //! [Create Reverse Session]
                    }
                    m_session.UseDnsNameAndPortFromDiscoveryUrl = true;
                    m_session.AllowInsecureCredentials = true;
                    m_session.SessionName = Application.ApplicationSettings.ApplicationUri;
                    if (Settings.ClientSettings.SessionTimeout != 0)
                    {
                        m_session.SessionTimeout = Settings.ClientSettings.SessionTimeout;
                    }
                    //! [Set ConnectionStatusUpdate]
                    m_session.ConnectionStatusUpdate += OnConnectionStatusUpdate;
                    //! [Set ConnectionStatusUpdate]
                }
                return m_session;
            }
        }

        internal System.Threading.ManualResetEvent InitialGdsIteractionDone { get; set; }

        static readonly object ConsoleLock = new object();
        #endregion

        #region Private Fields
        Session m_session;
        #endregion

        public Client(Configuration.IClientConfiguration settings)
        {
            Settings = settings;
            State = ClientState.Disconnected;
        }

        public void Run()
        {
            InitializeReverseConnectManagerIfRequired();
            WaitForGdsIfRequired();

            while (true)
            {
                switch (State)
                {
                    case ClientState.Disconnected:
                        PrintPreConnectMenu();
                        break;
                    case ClientState.StartDiscovery:
                        PrintDiscoveryMenu();
                        break;
                    case ClientState.FindServersDone:
                        PrintServersMenu();
                        break;
                    case ClientState.GetEndpointsDone:
                        PrintAfterDiscoveryMenu();
                        break;
                    case ClientState.StartReverseDiscovery:
                        PrintReverseDiscoveryMenu();
                        break;
                    case ClientState.ReverseFindServersDone:
                        PrintServersMenu();
                        break;
                    case ClientState.ReverseGetEndpointsDone:
                        PrintAfterDiscoveryMenu();
                        break;
                    case ClientState.Connected:
                        PrintConnectedMenu();
                        break;
                    case ClientState.BrowseDone:
                        PrintBrowseDoneMenu();
                        break;
                    case ClientState.HistoryReadDone:
                        PrintHistoryReadDoneMenu();
                        break;
                    case ClientState.HistoryReadEventsDone:
                        PrintHistoryReadEventsDoneMenu();
                        break;
                    case ClientState.AcknowledgeAlarms:
                        if (!m_AcknowledgeMenuUpToDate)
                        {
                            PrintAcknowledgeAlarmsMenu();
                        }
                        break;
                    case ClientState.Shutdown:
                        return;
                }
                ConsoleKeyInfo key = Console.ReadKey();
                DoOpcUaAction action = NextAction(key);
                State = action();
                System.Threading.Thread.Sleep(100);
            }
        }

        delegate ClientState DoOpcUaAction();

        DoOpcUaAction NextAction(ConsoleKeyInfo key)
        {
            switch (State)
            {
                case ClientState.Disconnected:
                    switch (key.Key)
                    {
                        case ConsoleKey.A:
                            return SetUserToAnonymous;
                        case ConsoleKey.B:
                            return SetUsername;
                        case ConsoleKey.NumPad0:
                        case ConsoleKey.D0:
                            return SimpleConnect;
                        case ConsoleKey.NumPad1:
                        case ConsoleKey.D1:
                            return SimpleConnectWithSecurity;
                        case ConsoleKey.X:
                            return Shutdown;
                        case ConsoleKey.NumPad2:
                        case ConsoleKey.D2:
                            return StartDiscovery;
#if !REVERSE_CONNECT_UNSUPPORTED
                        case ConsoleKey.NumPad3:
                        case ConsoleKey.D3:
                            return SimpleReverseConnect;
                        case ConsoleKey.NumPad4:
                        case ConsoleKey.D4:
                            return StartReverseDiscovery;
#endif
                        default:
                            return DoNothing;
                    }

                case ClientState.StartDiscovery:
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            return ToDisconnected;
                        case ConsoleKey.NumPad0:
                        case ConsoleKey.D0:
                            return FindServers;
                        case ConsoleKey.NumPad1:
                        case ConsoleKey.D1:
                            return GetEndpoints;
                        default:
                            return DoNothing;
                    }
                case ClientState.FindServersDone:
                    {
                        int idx = IndexFromKey(key);
                        if (DiscoveryUrls.Count > 0 && idx >= 0 && idx < DiscoveryUrls.Count)
                        {
                            SelectedDiscoveryUrl = DiscoveryUrls[idx];
                            return GetEndpoints;
                        }
                        if (key.Key == ConsoleKey.Escape)
                        {
                            return ToDisconnected;
                        }
                        return DoNothing;
                    }
                case ClientState.GetEndpointsDone:
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            return ToDisconnected;
                        default:
                            {
                                int idx = IndexFromKey(key);
                                if (Endpoints.Count > 0 && idx >= 0 && idx < Endpoints.Count)
                                {
                                    SelectedEndpoint = Endpoints[idx];
                                    return ConnectToEndpoint;
                                }
                            }
                            return DoNothing;
                    }
#if !REVERSE_CONNECT_UNSUPPORTED
                case ClientState.StartReverseDiscovery:
                    {
                        int idx = IndexFromKey(key);
                        if (DiscoveryUrls.Count > 0 && idx >= 0 && idx < DiscoveryUrls.Count)
                        {
                            SelectedDiscoveryUrl = DiscoveryUrls[idx];
                            return ReverseFindServers;
                        }
                        if (DiscoveryUrls.Count == 0 && key.KeyChar == 'w')
                        {
                            return WaitForConnectionGroups;
                        }
                        if (key.Key == ConsoleKey.Escape)
                        {
                            return ToDisconnected;
                        }
                        return DoNothing;
                    }
                case ClientState.ReverseFindServersDone:
                    {
                        int idx = IndexFromKey(key);
                        if (DiscoveryUrls.Count > 0 && idx >= 0 && idx < DiscoveryUrls.Count)
                        {
                            SelectedDiscoveryUrl = DiscoveryUrls[idx];
                            return ReverseGetEndpoints;
                        }
                        if (key.Key == ConsoleKey.Escape)
                        {
                            return ToDisconnected;
                        }
                        return DoNothing;
                    }
                case ClientState.ReverseGetEndpointsDone:
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            return ToDisconnected;
                        default:
                            {
                                int idx = IndexFromKey(key);
                                if (Endpoints.Count > 0 && idx >= 0 && idx < Endpoints.Count)
                                {
                                    SelectedEndpoint = Endpoints[idx];
                                    return ReverseConnectToEndpoint;
                                }
                            }
                            return DoNothing;
                    }
#endif
                case ClientState.Connected:
                    switch (key.Key)
                    {
                        case ConsoleKey.NumPad0:
                        case ConsoleKey.D0:
                            return Disconnect;
                        case ConsoleKey.NumPad1:
                        case ConsoleKey.D1:
                            return Read;
                        case ConsoleKey.NumPad2:
                        case ConsoleKey.D2:
                            return ReadAsync;
                        case ConsoleKey.NumPad3:
                        case ConsoleKey.D3:
                            return ReadIndexRange;
                        case ConsoleKey.NumPad4:
                        case ConsoleKey.D4:
                            return Write;
                        case ConsoleKey.NumPad5:
                        case ConsoleKey.D5:
                            return WriteAsync;
                        case ConsoleKey.NumPad6:
                        case ConsoleKey.D6:
                            return WriteIndexRange;
                        case ConsoleKey.NumPad7:
                        case ConsoleKey.D7:
                            return WriteStructure;
                        case ConsoleKey.NumPad8:
                        case ConsoleKey.D8:
                            return Browse;
                        case ConsoleKey.NumPad9:
                        case ConsoleKey.D9:
                            return BrowseAsync;
                        case ConsoleKey.A:
                            return TranslateBrowsePathToNodeId;
                        case ConsoleKey.B:
                            return CallMethod;
                        case ConsoleKey.C:
                            return RegisterNodes;
                        case ConsoleKey.D:
                            return RegisterNodesAsync;
                        case ConsoleKey.E:
                            return UnregisterNodes;
                        case ConsoleKey.F:
                            return HistoryReadData;
                        case ConsoleKey.G:
                            return HistoryUpdateData;
                        case ConsoleKey.H:
                            return HistoryReadEvents;
                        case ConsoleKey.I:
                            return CreateSubscription;
                        case ConsoleKey.J:
                            return ModifySubscription;
                        case ConsoleKey.K:
                            return DeleteSubscription;
                        case ConsoleKey.L:
                            return AddMonitoredItems;
                        case ConsoleKey.M:
                            return AddMonitoredItemsWithDeadbandAndQueueSize;
                        case ConsoleKey.N:
                            return CreateEventMonitoredItems;
                        case ConsoleKey.O:
                            return DeleteMonitoredItems;
                        case ConsoleKey.P:
                            return SubscribeForAlarm;
                        case ConsoleKey.Q:
                            return AcknowledgeAlarms;
                        case ConsoleKey.R:
                            return TransferSubscrition;
                        case ConsoleKey.S:
                            return ChangeUser;
                        default:
                            return DoNothing;
                    }
                case ClientState.BrowseDone:
                    switch (key.Key)
                    {
                        case ConsoleKey.NumPad0:
                        case ConsoleKey.D0:
                            return BrowseNext;
                        case ConsoleKey.NumPad1:
                        case ConsoleKey.D1:
                            return ReleaseContinuationPoint;
                    }
                    return DoNothing;
                case ClientState.HistoryReadDone:
                    {
                        switch (key.Key)
                        {
                            case ConsoleKey.NumPad0:
                            case ConsoleKey.D0:
                                return HistoryReadRawNext;
                            case ConsoleKey.NumPad1:
                            case ConsoleKey.D1:
                                return ReleaseHistoryContinuationPoint;
                        }
                    }
                    return DoNothing;
                case ClientState.HistoryReadEventsDone:
                    {
                        switch (key.Key)
                        {
                            case ConsoleKey.NumPad0:
                            case ConsoleKey.D0:
                                return HistoryReadEventsNext;
                            case ConsoleKey.NumPad1:
                            case ConsoleKey.D1:
                                return ReleaseHistoryEventsContinuationPoint;
                        }
                    }
                    return DoNothing;
                case ClientState.AcknowledgeAlarms:
                    {
                        switch (key.Key)
                        {
                            case ConsoleKey.Escape:
                                return ToConnectedState;
                            case ConsoleKey.A:
                                return AcknowledgeAll;
                            default:
                                {
                                    int idx = IndexFromKey(key);
                                    if (m_alarmList.Count >= 0 && idx >= 0 && idx < m_alarmList.Count)
                                    {
                                        m_selectedAlarmIndex = idx; 
                                        return AcknowledgeSingle;
                                    }
                                }
                                return DoNothing;
                        }
                    }
            }
            return DoNothing;
        }

        static int IndexFromKey(ConsoleKeyInfo key)
        {
            int idx;
            if (key.KeyChar >= '0' && key.KeyChar <= '9')
                idx = key.KeyChar - '0';
            else if (key.KeyChar >= 'a' && key.KeyChar <= 'z')
                idx = key.KeyChar - 'a' + 10;
            else idx = -1;
            return idx;
        }

        ClientState Shutdown()
        {
            if (m_session != null)
            {
                if (m_session.ConnectionStatus != ServerConnectionStatus.Disconnected)
                {
                    m_session.Disconnect();
                }
                m_session.Dispose();
                m_session = null;
            }
            return ClientState.Shutdown;
        }

        ClientState DoNothing()
        {
            return State;
        }

        ClientState ToConnectedState()
        {
            return ClientState.Connected;
        }

        ClientState ToDisconnected()
        {
            return ClientState.Disconnected;
        }

        static void Output(string line)
        {
            lock (ConsoleLock)
            {
                Console.WriteLine(line);
            }
        }

        void LogException(Exception e, string message)
        {
            StatusException s = e as StatusException;
            if (s != null)
            {
                Output($"{message} with status {s.StatusCode} and message {s.Message}");
            }
            else
            {
                Output($"{message} with message {e.Message}");
            }
            if (e.InnerException != null)
            {
                LogInnerException(e.InnerException);
            }

        }

        void LogInnerException(Exception e)
        {
            Exception innerException = e;
            while (innerException.InnerException != null)
            {
                innerException = innerException.InnerException;
            }
            if (innerException is StatusException statusException)
            {
                Output($"InnerException: [{statusException}]: {statusException.Message}");
            }
            else
            {
                Output($"InnerException: {innerException.Message}");
            }
        }

        private void InitializeReverseConnectManagerIfRequired()
        {
            if (!String.IsNullOrEmpty(Settings.Connection.ClientUrlForReverseConnect))
            {
                try
                {
                    Application.ReverseConnectManager.InitializeTransportListener(Settings.Connection.ClientUrlForReverseConnect);
                }
                catch (Exception e)
                {
                    LogException(e, "Could not initialize TransportListener");
                }
            }
        }

        private void WaitForGdsIfRequired()
        {
            if (InitialGdsIteractionDone != null)
            {
                InitialGdsIteractionDone.WaitOne(2 * 60_000);
            }
        }


        ClientState WriteNode<T>(string path, T value)
        {
            try
            {
                //! [Write call]
                IList<WriteValue> nodesToWrite = new List<WriteValue>();

                nodesToWrite.Add(new WriteValue()
                {
                    NodeId = NodeId.Parse(path),
                    AttributeId = Attributes.Value,
                    Value = new DataValue() { Value = value}
                });

                List<StatusCode> res = Session.Write(nodesToWrite);
                //! [Write call]

                lock (ConsoleLock)
                {
                    Console.WriteLine("\nWrite succeeded");
                    foreach (StatusCode status in res)
                    {
                        Console.WriteLine($"\nWrite result {status}");
                    }
                }
            }
            catch (Exception e)
            {
                Output($"\nWrite failed with message {e.Message}");
            }
            return ClientState.Connected;
        }
        public void Init()
        {
            InitializeReverseConnectManagerIfRequired();
            WaitForGdsIfRequired();
          
            State = ClientState.Disconnected;
            SimpleConnect();

            AddMonitoredItems();

                        
        }
    }
}
