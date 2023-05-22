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
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization;

using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;
using ConsoleClient.Configuration;

namespace ConsoleClient
{
    public class Settings
    {
        public static IClientConfiguration LoadDefaults()
        {

            ClientConfigurationInMemory ret = new ClientConfigurationInMemory()
            {
                ConfiguredNamespaces = new NamespaceTable()
                {
                    "http://www.unifiedautomation.com/DemoServer/"
                },
                Connection = new ConnectionData()
                {
                    DiscoveryUrl = "opc.tcp://DESKTOP-MU3HI5L:49320",
                    // We store the user name and the password in code here to keep the example simple.
                    // In OPC UA end user products user names and especially password shall not be hard coded.
                    UserName = "john",
                    Password = "master",
                    // The client opens this port to listen for ReverseHello
                    ClientUrlForReverseConnect = "opc.tcp://localhost:48071"
                },
                ClientSettings = new ClientSettings(),
                ReadVariableIds = new List<NodeId>()
                {
                    VariableIds.Server_ServerStatus_CurrentTime,
                    new NodeId("Demo.Static.Scalar.Boolean", 1),
                    new NodeId("Demo.Static.Scalar.Double", 1),
                    new NodeId("Demo.Dynamic.Scalar.Double", 1),
                    new NodeId("Demo.Static.Scalar.WorkOrder", 1),
                },
                ReadWithIndexRangeVariableIds = new List<NodeId>()
                {
                    new NodeId("Demo.Static.Arrays.Boolean", 1),
                    new NodeId("Demo.Static.Arrays.Double", 1),
                    new NodeId("Demo.Dynamic.Arrays.Double", 1)
                },
                RegisteredNodes = null,
                WriteVariables = new List<WriteValue>()
                {
                    new WriteValue()
                    {
                        NodeId = new NodeId("Demo.Static.Scalar.Double", 1),
                        Value = new DataValue()
                        {
                            WrappedValue = new Variant(20.0)
                        },
                        AttributeId = Attributes.Value
                    }
                },
                StructureToWriteId = new NodeId("Demo.Static.Scalar.WorkOrder", 1),
                BrowseVariableId = new NodeId("Demo.Massfolder_Static", 1),
                Method = new Configuration.MethodDescription()
                {
                    ObjectId = new NodeId("Demo.Method", 1),
                    MethodId = new NodeId("Demo.Method.Multiply", 1),
                    InputArguments = new List<Variant>()
                    {
                       new Variant(10.0),
                       new Variant(20.0)
                    }

                },
                HistoryVariableIds = new List<NodeId>()
                {
                    new NodeId("Demo.History.ByteWithHistory", 1),
                    new NodeId("Demo.History.DoubleWithHistory", 1),
                    new NodeId("Demo.History.Historian_1", 1),
                    new NodeId("Demo.History.Historian_2", 1)
                },
                HistoryObjectIds = new List<NodeId>()
                {
                    new NodeId("Demo.History.NotifierWithHistory", 1)
                },
                EventId = UnifiedAutomation.UaBase.ObjectIds.Server,
                EventOfTypeFilter = new NodeId(1005, 1),
                TranslateStartingNodeId = new NodeId("Demo.BoilerDemo.Boiler1", 1),
                TranslateElement = new QualifiedName("FillLevelSetPoint", 1)
            };

            return ret;
        }


        public static IClientConfiguration LoadFromFile(string path)
        {
            ClientConfigurationInFromFile ret = new ClientConfigurationInFromFile();

            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(ClientConfigurationInFromFile));
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        ret = (ClientConfigurationInFromFile)xs.Deserialize(sr);
                    }
                    Console.WriteLine($"\n Settings loaded successfully: {path}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n load file failed: {e.Message}");
                ret = null;
            }

            return ret;
        }


        public static void SaveToFile(String path, ClientConfigurationInFromFile config)
        {
            XmlSerializer xs;
            TextWriter tw = null;
            try
            {
                xs = new XmlSerializer(typeof(ClientConfigurationInFromFile));
                using (var fs = new FileStream(path, FileMode.OpenOrCreate))
                {
                    using (tw = new StreamWriter(fs))
                    {
                        xs.Serialize(tw, config);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine($"\n load file failed {e.Message}");
            }
            finally
            {
                tw.Close();
            }
        }
    }
}