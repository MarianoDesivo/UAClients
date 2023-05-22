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
        DataTypeManager m_dataTypeManager;
        DataTypeManager DataTypeManager
        {
            get
            {
                if (m_dataTypeManager == null)
                {
                    m_dataTypeManager = new DataTypeManager(Session);
                }
                return m_dataTypeManager;
            }
        }
        #endregion

        #region Read
        ClientState Read()
        {
            try
            {
                IList<ReadValueId> nodesToRead = NodesToReadFromConfiguration();
                //! [Call Read]
                var results = Session.Read(nodesToRead);
                //! [Call Read]
                Output("\nRead succeeded");
                PrintReadResults(results, nodesToRead);
            }
            catch (Exception e)
            {
                Output($"\nRead failed with message {e.Message}");
            }
            return ClientState.Connected;
        }

        ClientState ReadAsync()
        {
            IList<ReadValueId> nodesToRead = NodesToReadFromConfiguration();
            try
            {
                //! [Call BeginRead]
                Session.BeginRead(nodesToRead, 0, OnReadComplete, nodesToRead);
                //! [Call BeginRead]
                Output("\nBeginRead succeeded");
            }
            catch (Exception e)
            {
                Output($"\nBeginRead failed with message {e.Message}");
            }
            return ClientState.Connected;
        }

        //! [Read callback]
        void OnReadComplete(IAsyncResult result)
        {
            try
            {
                List<DataValue> results = Session.EndRead(result);
                //! [Read callback]
                Output("\nEndRead succeeded");
                PrintReadResults(results, result.AsyncState as IList<ReadValueId>);
            }
            catch (Exception e)
            {
                Output($"\nEndRead failed with message {e.Message}");
            }
        }
        #endregion

        #region Write
        ClientState Write()
        {
            try
            {
                //! [Write call]
                IList<WriteValue> nodesToWrite = Settings.WriteVariables;

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


        ClientState WriteAsync()
        {
            try
            {
                IList<WriteValue> nodesToWrite = Settings.WriteVariables;
                Session.BeginWrite(nodesToWrite, OnWriteComplete, null);
                Output("\nBeginWrite succeeded");
            }
            catch (Exception e)
            {
                Output($"\nBeginWrite failed with message {e.Message}");
            }
            return ClientState.Connected;
        }

        void OnWriteComplete(IAsyncResult result)
        {
            try
            {
                List<StatusCode> results = Session.EndWrite(result);
                Output("\nEndWrite succeeded");
            }
            catch (Exception e)
            {
                Output($"\nEnWrite failed with message {e.Message}");
            }
        }
        #endregion

        #region Write Structure
        ClientState WriteStructure()
        {
            NodeId structureId = Settings.StructureToWriteId;

            //read structure type
            List<DataValue> results = null;
            try
            {
                //! [Read data type information]
                IList<ReadValueId> nodesToRead = new List<ReadValueId>
                {
                    new ReadValueId()
                    {
                        NodeId = structureId,
                        AttributeId = Attributes.DataType
                    }
                };
                results = Session.Read(nodesToRead);
                //! [Read data type information]
                Output("\nWriteStructure - Read of data type succeeded");
            }
            catch(Exception e)
            {
                Output($"\nWriteStructure - Read of data type failed {e.Message}");
            }

            try
            {
                //! [create generic structure type]
                NodeId datatypeId = (NodeId)results[0].Value;
                ExpandedNodeId eNodeId = new ExpandedNodeId(datatypeId.IdType, datatypeId.Identifier, Session.NamespaceUris[datatypeId.NamespaceIndex], datatypeId.NamespaceIndex);
                GenericStructureDataType newType = DataTypeManager.NewTypeFromDataType(eNodeId, BrowseNames.DefaultBinary, true);
                //! [create generic structure type]

                //! [write structure]
                //Create encodeable object
                var geo = new GenericEncodeableObject(newType);

                //set fields of encodeable object
                for( int i = 0; i < newType.Fields.Count; ++i)
                {
                    if (newType[i].TypeDescription.TypeClass == GenericDataTypeClass.Structured)
                    {
                        var subGeo = new GenericEncodeableObject(newType[i].TypeDescription);
                    }
                    if (geo[i].TypeInfo == TypeInfo.Scalars.String)
                    {
                        geo[i] = new Variant("Generic Person name");
                    }
                    else if (geo[i].TypeInfo == TypeInfo.Scalars.UInt16)
                    {
                        geo[i] = new Variant((UInt16)123);
                    }
                    else if (geo[i].TypeInfo == TypeInfo.Scalars.Float)
                    {
                        geo[i] = new Variant(13.37f);
                    }
                }

                IList<WriteValue> nodesToWrite = new List<WriteValue>
                {
                    new WriteValue()
                    {
                        NodeId = structureId,
                        Value = new DataValue() { WrappedValue = geo },
                        AttributeId = Attributes.Value
                    }
                };
                List<StatusCode> res = Session.Write(nodesToWrite);
                //! [write structure]

                lock (ConsoleLock)
                {
                    Console.WriteLine("\nWriteStructure succeeded");
                    foreach (StatusCode status in res)
                    {
                        Console.WriteLine($"\nWrite result {status}");
                    }
                }
            }
            catch (Exception e)
            {
                Output($"\nWriteStructure failed with message {e.Message}");
            }

            return ClientState.Connected;
        }
        #endregion

        #region IndexRange
        ClientState ReadIndexRange()
        {
            IList<ReadValueId> nodesToRead = NodesToReadWithIndexRangeFromConfiguration();
            try
            {
                var results = Session.Read(nodesToRead);
                Output("\nRead succeeded");
                PrintReadResults(results, nodesToRead);
            }
            catch (Exception e)
            {
                Output($"\nRead failed with message {e.Message}");
            }
            return ClientState.Connected;
        }

        ClientState WriteIndexRange()
        {
            IList<WriteValue> nodeIdsToWrite = new List<WriteValue>();
            List<bool> value = new List<bool>
            {
                false
            };

            try
            {
                //! [write index]
                nodeIdsToWrite.Add(new WriteValue()
                {
                    NodeId = new NodeId("Demo.Static.Arrays.Boolean", 2),
                    Value = new DataValue() { WrappedValue = new Variant(value) },
                    AttributeId = Attributes.Value,
                    IndexRange = "0:1"
                });
                //! [write index]

                var results = Session.Write(nodeIdsToWrite);
                Output("\nWrite succeeded");
            }
            catch (Exception e)
            {
                Output($"\nWrite failed with message {e.Message}");
            }
            return ClientState.Connected;
        }
        #endregion

        #region Updating data from configuration
        IList<ReadValueId> NodesToReadFromConfiguration()
        {
            IList<NodeId> configuredVariables;
            if (RegisteredNodes != null)
            {
                // use registered nodes for read
                configuredVariables = RegisteredNodes;
            }
            else
            {
                configuredVariables = Settings.ReadVariableIds;
            }
            //! [NodesToRead from configuration]
            List<ReadValueId> readValueIds = new List<ReadValueId>();
            foreach (NodeId nodeId in configuredVariables)
            {
                readValueIds.Add(new ReadValueId()
                {
                    AttributeId = Attributes.Value,
                    NodeId = nodeId
                });
            }
            return readValueIds;
            //! [NodesToRead from configuration]
        }
        IList<ReadValueId> NodesToReadWithIndexRangeFromConfiguration()
        {
            List<ReadValueId> nodesToRead = new List<ReadValueId>();
            //! [read index]
            foreach (NodeId nodeId in Settings.ReadWithIndexRangeVariableIds)
            {
                nodesToRead.Add(new ReadValueId()
                {
                    AttributeId = Attributes.Value,
                    NodeId = nodeId,
                    IndexRange = "1:3"
                });
            }
            //! [read index]

            return nodesToRead;
        }
        #endregion

        #region Display values
        void PrintReadResults(IList<DataValue> values, IList<ReadValueId> nodes)
        {
            lock (ConsoleLock)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    Variant value = values[i].WrappedValue;
                    if (value.DataType != BuiltInType.ExtensionObject)
                    {
                        Console.WriteLine($"[{i}] {nodes[i].NodeId}: {values[i]}");
                    }
                    else
                    {
                        ExtensionObject e = value.ToExtensionObject();
                        if (e.Encoding != ExtensionObjectEncoding.EncodeableObject)
                        {
                            GenericEncodeableObject encodeable = DataTypeManager.ParseValue(e);
                            string resultText = ValueToString(encodeable);
                            Console.WriteLine($"\n{resultText}");
                        }
                        else
                        {
                            Console.WriteLine($"\n{e}");
                        }
                    }
                }
            }
        }
        /// [Recursive Method 1]
        static string ValueToString(GenericEncodeableObject genericValue)
        {
            string result = "";
            // Extract the data type description
            GenericStructureDataType structuredDataType = genericValue.TypeDefinition;
            // We have a structure, process structure elements
            if (structuredDataType.TypeClass == GenericDataTypeClass.Structured)
            {
                result = structuredDataType.Name.Name;

                // Use a recursive method, because structures can be nested.
                ValueToStringRec(genericValue, 1, ref result);
            }
            return result;
        }

        static void ValueToStringRec(GenericEncodeableObject genericValue, int depth, ref string result)
        {
            /// [Recursive Method 1]
            /// [Recursive Method 2]
            GenericStructureDataType structuredDataType = genericValue.TypeDefinition;
            /// [Recursive Method 2]

            // get indentation for layout
            string intend = "";
            /// [Recursive Method 3]
            for (int i = 0; i < depth; i++)
            {
                intend += "  ";
            }
            for (int i = 0; i < structuredDataType.Count; i++)
            {
                // Get the description of the field (name, data type etc.)
                GenericStructureDataTypeField fieldDescription = structuredDataType[i];
                // Get the value of the field
                Variant fieldValue = genericValue[i];

                string fieldName = fieldDescription.Name;
                /// [Recursive Method 3]
                // In this example we take care about simple types and structures, each scalar and array.
                /// [Recursive Method 4]
                if (fieldDescription.ValueRank == -1)
                {
                    if (fieldDescription.TypeDescription.TypeClass == GenericDataTypeClass.Simple)
                    {
                        // Print the name and the value
                        result += $"\r\n{intend}{fieldName} = {fieldValue}";
                    }
                    else if (fieldDescription.TypeDescription.TypeClass == GenericDataTypeClass.Structured)
                    {
                        // Print the name and call this method again the child
                        result += $"\r\n{intend}{fieldName}";
                        ValueToStringRec((GenericEncodeableObject)fieldValue.GetValue<GenericEncodeableObject>(null), depth + 1, ref result);
                    }
                }
                else if (fieldDescription.ValueRank == 1)
                {
                    // Print the fields name
                    result += $"\r\n{intend}{fieldName}";
                    if (fieldDescription.TypeDescription.TypeClass == GenericDataTypeClass.Simple)
                    {
                        // Print each element
                        Array array = fieldValue.Value as Array;
                        intend += "  ";
                        int counter = 0;
                        foreach (object element in array)
                        {
                            result += $"\r\n{intend}{fieldDescription.TypeDescription.Name.Name}[{counter++}]: {element}";
                        }
                    }
                    else if (fieldDescription.TypeDescription.TypeClass == GenericDataTypeClass.Structured)
                    {
                        // Call this method again for each element
                        ExtensionObjectCollection extensionObjects = fieldValue.ToExtensionObjectArray();
                        intend += "  ";
                        int counter = 0;
                        foreach (ExtensionObject e in extensionObjects)
                        {
                            result += $"\r\n{intend}{fieldDescription.TypeDescription.Name.Name}[{counter++}]";
                            ValueToStringRec((GenericEncodeableObject)e.Body, depth + 2, ref result);
                        }
                    }
                }
                /// [Recursive Method 4]
            }
        }
        #endregion
    }
}