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
using UnifiedAutomation.UaBase;

namespace ConsoleClient
{
    partial class Client
    {
        void PrintDiscoveryMenu()
        {
            lock (ConsoleLock)
            {
                Console.WriteLine("\n");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine(" - Press ESC to return to previous menu               -");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine(" - Press 0 to get servers                             -");
                Console.WriteLine(" - Press 1 to get endpoints                           -");
            }
        }

        void PrintServersMenu()
        {
            lock (ConsoleLock)
            {
                Console.WriteLine("\n");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine(" - Press ESC to return to connect menu                -");
                for (int i = 0; i < DiscoveryUrls.Count; ++i)
                {
                    Console.WriteLine($" - Press {i} to get endpoints from: {DiscoveryUrls[i]}");
                }
            }
        }

        void PrintAfterDiscoveryMenu()
        {
            lock (ConsoleLock)
            {
                Console.WriteLine("\n");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine(" - Press ESC to return to connect menu                -");
                Console.WriteLine("-------------------------------------------------------");
                for (int i = 0; i < Endpoints.Count; ++i)
                {
                    //! [Check security policy]
                    string unsupported;
                    try
                    {
                        Application.SecurityProvider.CreateCryptoProvider(
                            new CryptoProviderSettings()
                            {
                                SecurityProfileUri = Endpoints[i].SecurityPolicyUri
                            });
                        unsupported = "";

                    }
                    catch (Exception)
                    {
                        unsupported = " [Not supported by CryptoProvider]";
                    }
                    //! [Check security policy]

                    string key;
                    if (i < 10)
                    {
                        key = i.ToString();
                    }
                    else
                    {
                        key = ((char)('a' + (i - 10))).ToString();
                    }

                    Console.WriteLine($" - Press {key} to connect to: {Endpoints[i]} {Endpoints[i].SecurityMode}{unsupported}");
                }
            }
        }

        void PrintReverseDiscoveryMenu()
        {
            lock (ConsoleLock)
            {
                Console.WriteLine("\n");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine(" - Press ESC to return to previous menu               -");
                Console.WriteLine("-------------------------------------------------------");
                DiscoveryUrls = ReverseDiscoveryUrls;
                for (int i=0; i<DiscoveryUrls.Count; i++)
                {
                    Console.WriteLine($" - Press {i} to reverse find servers at {DiscoveryUrls[i]}");
                }
                if (DiscoveryUrls.Count == 0)
                {
                    Console.WriteLine(" - Press w to wait for reverse hellos ");
                }
            }
        }

        void PrintConnectedMenu()
        {
            lock (ConsoleLock)
            {
                Console.WriteLine("\n");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine(" - Press 0 to disconnect                              -");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine(" - Press 1 to read synchronously                      -");
                Console.WriteLine(" - Press 2 to read asynchronously                     -");
                Console.WriteLine(" - Press 3 to read with index range                   -");
                Console.WriteLine(" - Press 4 to write values synchronously              -");
                Console.WriteLine(" - Press 5 to write values asynchronously             -");
                Console.WriteLine(" - Press 6 to write with index range                  -");
                Console.WriteLine(" - Press 7 to write a generic structure               -");
                Console.WriteLine(" - Press 8 to browse server                           -");
                Console.WriteLine(" - Press 9 to browse server asynchronously            -");
                Console.WriteLine(" - Press a to translate browse path to nodeId         -");
                Console.WriteLine(" - Press b to call a method                           -");
                Console.WriteLine(" - Press c to register nodes                          -");
                Console.WriteLine(" - Press d to register nodes asynchronously           -");
                Console.WriteLine(" - Press e to unregister nodes                        -");
                Console.WriteLine(" - Press f to history read data                       -");
                Console.WriteLine(" - Press g to history update data                     -");
                Console.WriteLine(" - Press h to history read events                     -");
                Console.WriteLine(" - Press i to create a subscription                   -");
                Console.WriteLine(" - Press j to modify a subscription                   -");
                Console.WriteLine(" - Press k to delete subscription                     -");
                Console.WriteLine(" - Press l to add monitored items                     -");
                Console.WriteLine(" - Press m to add monitored items with settings       -");
                Console.WriteLine(" - Press n to create event monitored items            -");
                Console.WriteLine(" - Press o to delete (event)monitored items           -");
                Console.WriteLine(" - Press p to subscribe for alarms                    -");
                Console.WriteLine(" - Press q to acknowledge alarms                      -");
                Console.WriteLine(" - Press r to transfer subscription                   -");
                Console.WriteLine(" - Press s to change the user                         -"); // missing documentation
            }
        }

        void PrintBrowseDoneMenu()
        {
            lock (ConsoleLock)
            {
                Console.WriteLine("\n");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine("-  Browse returned a continuation point               -");
                Console.WriteLine(" - Press 0 to browse next chunk                       -");
                Console.WriteLine(" - Press 1 to release the continuation point          -");
            }
        }

        void PrintHistoryReadDoneMenu()
        {
            lock (ConsoleLock)
            {
                Console.WriteLine("\n");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine("-  HistoryRead returned a continuation point          -");
                Console.WriteLine(" - Press 0 to read next chunk                         -");
                Console.WriteLine(" - Press 1 to release the continuation point          -");
            }
        }

        void PrintHistoryReadEventsDoneMenu()
        {
            lock (ConsoleLock)
            {
                Console.WriteLine("\n");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine("-  HistoryReadEvents returned a continuation point    -");
                Console.WriteLine(" - Press 0 to read next chunk                         -");
                Console.WriteLine(" - Press 1 to release the continuation point          -");
            }
        }

        void PrintAcknowledgeAlarmsMenu()
        {
            lock (ConsoleLock)
            {
                Console.WriteLine("\n");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine(" - Press ESC to return to connect menu                -");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine(" - Press a to acknowledge all alarms                  -");
                Console.WriteLine(" - Press number to acknowledge single alarm           -");
                PrintAlarmList();
            }
        }

        void PrintPreConnectMenu()
        {
            lock (ConsoleLock)
            {
                Console.WriteLine("\n");
                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine(" - Press x to close client                               -");
                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine(" - Press a to set user to Anonymous                      -");
                Console.WriteLine(" - Press b to set user name and password                 -");
                Console.WriteLine(" - Press 0 to simple connect without security            -");
                Console.WriteLine(" - Press 1 to simple connect with security               -");
                Console.WriteLine(" - Press 2 to start discovery                            -");
#if !REVERSE_CONNECT_UNSUPPORTED
                Console.WriteLine(" - Press 3 to simple reverse connect without security    -");
                Console.WriteLine(" - Press 4 to start reverse discovery                    -");
#endif
            }
        }
    }
}

