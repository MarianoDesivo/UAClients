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
using System.Text;
using System.Xml.Serialization;

using UnifiedAutomation.UaBase;

namespace ConsoleClient.Configuration
{
    public class ConnectionData
    {
        public string DiscoveryUrl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ClientUrlForReverseConnect { get; set; }
    }

    public class ClientSettings
    {
        public int SessionTimeout { get; set; }
        public double SubscriptionLifetime { get; set; }
    }

    public class TraceSettings
    {
        public bool Enabled { get; set; }
        public string TraceFile { get; set; }
        public UnifiedAutomation.UaSchema.TraceLevel TraceLevel { get; set; }
    }

    public class GdsSettings
    {
        public bool UseGdsSecurity { get; set; } = false;
        public string GdsDiscoveryUrl { get; set; } = "opc.tcp://localhost:48060";
        public long UpdateFrequency { get; set; }
        public string GdsCertificatesBaseFolder { get; set; } = "pkiClientGds";
    }

    public class MethodDescription
    {
        public NodeId ObjectId { get; set; }
        public NodeId MethodId { get; set; }
        public List<Variant> InputArguments { get; set; }
    }

    public interface IClientConfiguration
    {
        NamespaceTable CurrentNamespaceTable { set; }

        ConnectionData Connection { get; }

        ClientSettings ClientSettings { get;}

        TraceSettings TraceSettings { get; }

        GdsSettings GdsSettings { get;}

        IList<NodeId> ReadVariableIds { get; }
        IList<NodeId> ReadWithIndexRangeVariableIds { get; }

        IList<WriteValue> WriteVariables {get; }
        NodeId StructureToWriteId { get; }

        MethodDescription Method { get; }

        IList<NodeId> HistoryVariableIds { get; }
        IList<NodeId> HistoryObjectIds { get; }

        NodeId BrowseVariableId { get; }
        NodeId EventId { get; }
        NodeId EventOfTypeFilter { get; }
        NodeId TranslateStartingNodeId { get; }
        QualifiedName TranslateElement { get; }
    }

    public class ClientConfigurationInMemory : IClientConfiguration
    {
        public NamespaceTable ConfiguredNamespaces { get; set; }

        #region Mapping Namespaces
        public NamespaceTable CurrentNamespaceTable
        {
            set
            {
                //! [Create Mapping]
                m_currentMapping = value.CreateMapping(ConfiguredNamespaces, false);
                //! [Create Mapping]
                MapAll();
                //! [Update Mapping]
                ConfiguredNamespaces.Clear();
                NamespaceTable tmp = new NamespaceTable();
                List<ushort> usedNamespaces = new List<ushort>();
                foreach (ushort index in m_currentMapping)
                {
                    usedNamespaces.Add(index);
                }
                for (int ii = 1; ii < value.Count; ii++)
                {
                    tmp.Add(usedNamespaces.Contains((ushort) ii) ? value[ii] : null);
                }
                ConfiguredNamespaces = tmp;
                //! [Update Mapping]
            }
        }

        void MapAll()
        {
            if (ReadVariableIds != null)
            {
                List<NodeId> mappedNodes = new List<NodeId>();
                foreach (NodeId nodeId in ReadVariableIds)
                {
                    mappedNodes.Add(MapNodeId(nodeId));
                }
                ReadVariableIds = mappedNodes;
            }

            if (ReadWithIndexRangeVariableIds != null)
            {
                List<NodeId> mappedNodes = new List<NodeId>();
                foreach (NodeId nodeId in ReadWithIndexRangeVariableIds)
                {
                    mappedNodes.Add(MapNodeId(nodeId));
                }
                ReadWithIndexRangeVariableIds = mappedNodes;
            }

            if (WriteVariables != null)
            {
                foreach (WriteValue val in WriteVariables)
                {
                    val.NodeId = MapNodeId(val.NodeId);
                }
            }

            if (StructureToWriteId != null)
            {
                StructureToWriteId = MapNodeId(StructureToWriteId);
            }

            if (Method != null)
            {
                Method.ObjectId = MapNodeId(Method.ObjectId);
                Method.MethodId = MapNodeId(Method.MethodId);
            }

            if (HistoryVariableIds != null)
            {
                List<NodeId> mappedNodes = new List<NodeId>();
                foreach (NodeId nodeId in HistoryVariableIds)
                {
                    mappedNodes.Add(MapNodeId(nodeId));
                }
                HistoryVariableIds = mappedNodes;
            }

            if (HistoryObjectIds != null)
            {
                List<NodeId> mappedNodes = new List<NodeId>();
                foreach (NodeId nodeId in HistoryObjectIds)
                {
                    mappedNodes.Add(MapNodeId(nodeId));
                }
                HistoryObjectIds = mappedNodes;
            }

            if (BrowseVariableId != null)
            {
                BrowseVariableId = MapNodeId(BrowseVariableId);
            }

            if (TranslateStartingNodeId != null)
            {
                TranslateStartingNodeId = MapNodeId(TranslateStartingNodeId);
            }

            if (TranslateElement != null)
            {
                TranslateElement = MapQualifiedName(TranslateElement);
            }

            if (EventId != null)
            {
                EventId = MapNodeId(EventId);
            }

            if (EventOfTypeFilter != null)
            {
                EventOfTypeFilter = MapNodeId(EventOfTypeFilter);
            }

        }

        //! [Map NodeId]
        NodeId MapNodeId(NodeId nodeId)
        {
            if (m_currentMapping == null)
            {
                throw new Exception("Configuration Error");
            }
            if (nodeId.NamespaceIndex > m_currentMapping.Length)
                return nodeId;
            return new NodeId(nodeId.IdType, nodeId.Identifier, m_currentMapping[nodeId.NamespaceIndex]);
        }
        //! [Map NodeId]

        QualifiedName MapQualifiedName(QualifiedName name)
        {
            if (m_currentMapping == null)
            {
                throw new Exception("Configuration Error");
            }
            if (name.NamespaceIndex > m_currentMapping.Length)
                return name;
            return new QualifiedName(name.Name, m_currentMapping[name.NamespaceIndex]);
        }
        #endregion

        public ConnectionData Connection { get; set; }
        public ClientSettings ClientSettings { get; set; }

        public TraceSettings TraceSettings { get; set; }
        public GdsSettings GdsSettings { get; set; }
        public IList<NodeId> ReadVariableIds { get; set; }
        public IList<NodeId> RegisteredNodes { get; set; }
        public IList<NodeId> ReadWithIndexRangeVariableIds { get; set; }
        public IList<WriteValue> WriteVariables { get; set; }
        public NodeId StructureToWriteId { get; set; }

        public MethodDescription Method { get; set; }

        public IList<NodeId> HistoryVariableIds { get; set; }
        public IList<NodeId> HistoryObjectIds { get; set; }
        public NodeId BrowseVariableId { get; set; }
        public NodeId EventId { get; set; }
        public NodeId EventOfTypeFilter { get; set; }
        public NodeId TranslateStartingNodeId { get; set; }
        public QualifiedName TranslateElement { get; set; }
        #region Private Fields
        ushort[] m_currentMapping;
        #endregion
    }

    public class ClientConfigurationInFromFile : IClientConfiguration
    {
        #region Private Fields
        ushort[] m_currentMapping;
        #endregion

        public NamespaceTable ConfiguredNamespaces { get; set; }

        #region Mapping
        [XmlIgnoreAttribute]
        public NamespaceTable CurrentNamespaceTable
        {
            set
            {
                m_currentMapping = value.CreateMapping(ConfiguredNamespaces, false);
                MapAll();
                ConfiguredNamespaces.Clear();
                NamespaceTable tmp = new NamespaceTable();
                List<ushort> usedNamespaces = new List<ushort>();
                foreach (ushort index in m_currentMapping)
                {
                    usedNamespaces.Add(index);
                }
                for (int ii = 1; ii < value.Count; ii++)
                {
                    tmp.Add(usedNamespaces.Contains((ushort)ii) ? value[ii] : null);
                }
                ConfiguredNamespaces = tmp;
            }
        }

        NodeId MapNodeId(NodeId nodeId)
        {
            if (m_currentMapping == null)
            {
                throw new Exception("Configuration Error");
            }
            if (nodeId.NamespaceIndex >= m_currentMapping.Length)
            {
                Console.WriteLine($"Error: Node can not be mapped: {nodeId}");
                return nodeId;
            }
            return new NodeId(nodeId.IdType, nodeId.Identifier, m_currentMapping[nodeId.NamespaceIndex]);
        }

        void MapAll()
        {
            if (ReadVariableIds != null)
            {
                List<NodeId> mappedNodes = new List<NodeId>();
                foreach (NodeId nodeId in ReadVariableIds)
                {
                    mappedNodes.Add(MapNodeId(nodeId));
                }
                ReadVariableIds = mappedNodes;
            }
            if (ReadWithIndexRangeVariableIds != null)
            {
                List<NodeId> mappedNodes = new List<NodeId>();
                foreach (NodeId nodeId in ReadWithIndexRangeVariableIds)
                {
                    mappedNodes.Add(MapNodeId(nodeId));
                }
                ReadWithIndexRangeVariableIds = mappedNodes;
            }

            if (WriteVariables != null)
            {
                List<WriteValue> mapped = new List<WriteValue>();
                foreach (WriteValue val in WriteVariables)
                {
                    val.NodeId = MapNodeId(val.NodeId);
                    mapped.Add(val);
                }
                WriteVariables = mapped;
            }

            if (StructureToWriteId != null)
            {
                StructureToWriteId = MapNodeId(StructureToWriteId);
            }

            if (Method != null)
            {
                MethodDescription mapedMethod = new MethodDescription()
                {
                    ObjectId = MapNodeId(Method.ObjectId),
                    MethodId = MapNodeId(Method.MethodId),
                    InputArguments = Method.InputArguments,
                };
                Method = mapedMethod;
            }

            if (HistoryVariableIds != null)
            {
                List<NodeId> historyVariableIds = new List<NodeId>();
                foreach (NodeId nodeId in HistoryVariableIds)
                {
                    historyVariableIds.Add(MapNodeId(nodeId));
                }
                HistoryVariableIds = historyVariableIds;
            }

            if (HistoryObjectIds != null)
            {
                List<NodeId> historyObjectIds = new List<NodeId>();
                foreach (NodeId nodeId in HistoryObjectIds)
                {
                    historyObjectIds.Add(MapNodeId(nodeId));
                }
                HistoryObjectIds = historyObjectIds;
            }

            if (BrowseVariableId != null)
            {
                BrowseVariableId = MapNodeId(BrowseVariableId);
            }
            if (TranslateStartingNodeId != null)
            {
                TranslateStartingNodeId = MapNodeId(TranslateStartingNodeId);
            }
            if (EventId != null)
            {
                EventId = MapNodeId(EventId);
            }
            if (EventOfTypeFilter != null)
            {
                EventOfTypeFilter = MapNodeId(EventOfTypeFilter);
            }
        }
        #endregion

        public ConnectionData Connection { get; set; }

        public ClientSettings ClientSettings { get; set; }

        public GdsSettings GdsSettings { get; set; }

        public TraceSettings TraceSettings { get; set; }

        [XmlIgnoreAttribute]
        public IList<NodeId> ReadVariableIds
        {
            get
            {
                List<NodeId> list = new List<NodeId>();
                foreach (String nodeId in readVariableIds)
                {
                    list.Add(NodeId.Parse(nodeId));
                }
                return list;
            }
            set
            {
                readVariableIds = new List<string>();
                foreach (NodeId nodeId in value)
                {
                    readVariableIds.Add(nodeId.ToString());
                }
            }
        }
        [XmlIgnoreAttribute]
        public IList<NodeId> RegisteredNodes { get; set; }
        public List<string> readVariableIds;
        [XmlIgnoreAttribute]
        public IList<NodeId> ReadWithIndexRangeVariableIds
        {
            get
            {
                List<NodeId> list = new List<NodeId>();
                foreach (String nodeId in readRangeVariableIds)
                {
                    list.Add(NodeId.Parse(nodeId));
                }
                return list;
            }
            set
            {
                readRangeVariableIds = new List<string>();
                foreach (NodeId nodeId in value)
                {
                    readRangeVariableIds.Add(nodeId.ToString());
                }
            }
        }
        public List<string> readRangeVariableIds;

        [XmlIgnoreAttribute]
        public IList<WriteValue> WriteVariables
        {
            get
            {
                List<WriteValue> list = new List<WriteValue>();
                foreach (WriteValueInFile writeValue in writeVariables)
                {
                    Variant newValue = new Variant(writeValue.value);
                    newValue = newValue.ConvertTo(writeValue.type);

                    list.Add(
                        new WriteValue()
                        {
                            NodeId = NodeId.Parse(writeValue.nodeId),
                            AttributeId = Attributes.Value,
                            Value = new DataValue() { WrappedValue = newValue }
                        });

                }
                return list;
            }
            set
            {
                writeVariables = new List<WriteValueInFile>();
                foreach (WriteValue writeValue in value)
                {
                    writeVariables.Add(new WriteValueInFile()
                    {
                        value = writeValue.Value.WrappedValue.ToString(),
                        type = writeValue.Value.WrappedValue.DataType,
                        nodeId = writeValue.NodeId.ToString()
                    });
                }
            }
        }
        public List<WriteValueInFile> writeVariables;
        public class WriteValueInFile
        {
            public BuiltInType type;
            public string value;
            public string nodeId;
        }

        [XmlIgnoreAttribute]
        public NodeId StructureToWriteId
        {
            get
            {
                return NodeId.Parse(structureToWriteId);
            }
            set
            {
                structureToWriteId = value.ToString();
            }
        }
        public string structureToWriteId;

        public class MethodDescriptionInFile
        {
            [XmlIgnoreAttribute]
            public NodeId ObjectId
            {
                get
                {
                    return NodeId.Parse(objectId);
                }
                set
                {
                    objectId = value.ToString();

                }
            }
            public string objectId;

            [XmlIgnoreAttribute]
            public NodeId MethodId
            {
                get
                {
                    return NodeId.Parse(methodId);
                }
                set
                {
                    methodId = value.ToString();
                }
            }
            public string methodId;

            [XmlIgnoreAttribute]
            public List<Variant> InputArguments
            {
                set
                {
                    inputArguments = new List<VariantInFile>();
                    foreach (Variant variant in value)
                    {
                        inputArguments.Add(new VariantInFile()
                        {
                            value = variant.ToString(),
                            type = variant.DataType
                        });
                    }
                }
                get
                {
                    List<Variant> res = new List<Variant>();
                    foreach(VariantInFile variant in inputArguments)
                    {
                        Variant newValue = new Variant(variant.value);
                        newValue = newValue.ConvertTo(variant.type);
                        res.Add(newValue);
                    }
                    return res;
                }
            }
            public List<VariantInFile> inputArguments;

            [XmlIgnoreAttribute]
            public Argument[] OutputArguments { get; set; }
        }
        public class VariantInFile
        {
            public string value;
            public BuiltInType type;
        }

        [XmlIgnoreAttribute]
        public MethodDescription Method
        {
            get
            {
                MethodDescription description = new MethodDescription()
                {
                    ObjectId = MethodDescription.ObjectId,
                    MethodId = MethodDescription.MethodId,
                    InputArguments = MethodDescription.InputArguments
                };
                return description;
            }
            set
            {
                MethodDescription = new MethodDescriptionInFile()
                {
                    ObjectId = value.ObjectId,
                    MethodId = value.MethodId,
                    InputArguments = value.InputArguments
                };
            }
        }
        public MethodDescriptionInFile MethodDescription;

        [XmlIgnoreAttribute]
        public IList<NodeId> HistoryVariableIds
        {
            get
            {
                List<NodeId> list = new List<NodeId>();
                foreach (String nodeId in HistoryReadValueId)
                {
                    list.Add(NodeId.Parse(nodeId));
                }
                return list;
            }
            set
            {
                HistoryReadValueId = new List<string>();
                foreach (NodeId nodeId in value)
                {
                    HistoryReadValueId.Add(nodeId.ToString());
                }
            }
        }
        public List<string> HistoryReadValueId;

        [XmlIgnoreAttribute]
        public IList<NodeId> HistoryObjectIds
        {
            get
            {
                List<NodeId> list = new List<NodeId>();
                foreach (String nodeId in historyObjectIds)
                {
                    list.Add(NodeId.Parse(nodeId));
                }
                return list;
            }
            set
            {
                historyObjectIds = new List<string>();
                foreach (NodeId nodeId in value)
                {
                    historyObjectIds.Add(nodeId.ToString());
                }
            }
        }
        public List<string> historyObjectIds;

        [XmlIgnoreAttribute]
        public NodeId BrowseVariableId
        {
            get
            {
                return NodeId.Parse(browseVariableId);
            }
            set
            {
                browseVariableId = value.ToString();
            }
        }
        public string browseVariableId;

        [XmlIgnoreAttribute]
        public NodeId TranslateStartingNodeId
        {
            get
            {
                return NodeId.Parse(translateId);
            }
            set
            {
                translateId = value.ToString();
            }
        }
        public string translateId;

        [XmlIgnoreAttribute]
        public QualifiedName TranslateElement
        {
            get
            {
                return QualifiedName.Parse(translateElement);
            }
            set
            {
                translateElement = value.ToString();
            }
        }
        public string translateElement;

        [XmlIgnoreAttribute]
        public NodeId EventId
        {
            get
            {
                return NodeId.Parse(eventId);
            }
            set
            {
                eventId = value.ToString();
            }
        }
        public string eventId;
        [XmlIgnoreAttribute]
        public NodeId EventOfTypeFilter
        {
            get
            {
                return NodeId.Parse(eventOfTypeFilter);
            }
            set
            {
                eventOfTypeFilter = value.ToString();
            }
        }
        public string eventOfTypeFilter;
    }
}
