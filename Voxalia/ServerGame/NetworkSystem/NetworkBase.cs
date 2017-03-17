//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using Voxalia.ServerGame.ServerMainSystem;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Voxalia.Shared;
using Open.Nat;
using FreneticGameCore;

namespace Voxalia.ServerGame.NetworkSystem
{
    public class NetworkBase
    {
        public Server TheServer;

        public Thread ListenThread;

        public Socket ListenSocket;

        public NetStringManager Strings;

        public List<Connection> Connections;

        public NetworkBase(Server tserver)
        {
            TheServer = tserver;
            Strings = new NetStringManager(TheServer);
            Connections = new List<Connection>();
        }

        public void Init()
        {
            try
            {
                NatDiscoverer natdisc = new NatDiscoverer();
                NatDevice natdev = natdisc.DiscoverDeviceAsync().Result;
                Mapping map = natdev.GetSpecificMappingAsync(Protocol.Tcp, TheServer.Port).Result;
                if (map != null)
                {
                    natdev.DeletePortMapAsync(map).Wait();
                }
                natdev.CreatePortMapAsync(new Mapping(Protocol.Tcp, TheServer.Port, TheServer.Port, "Voxalia")).Wait();
                map = natdev.GetSpecificMappingAsync(Protocol.Tcp, TheServer.Port).Result;
                IPAddress publicIP = natdev.GetExternalIPAsync().Result;
                SysConsole.Output(OutputType.INIT, "Successfully opened server to public address " + map.PrivateIP + " or " + publicIP.ToString() + ", with port " + map.PrivatePort + ", as " + map.Description);
            }
            catch (Exception ex)
            {
                SysConsole.Output("Trying to open port " + TheServer.Port, ex);
            }
            if (Socket.OSSupportsIPv6)
            {
                try
                {
                    ListenSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    ListenSocket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27 /* IPv6Only */, false);
                    ListenSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, TheServer.Port));
                }
                catch (Exception ex)
                {
                    SysConsole.Output("Opening IPv6/IPv4 combo-socket", ex);
                    ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    ListenSocket.Bind(new IPEndPoint(IPAddress.Any, TheServer.Port));
                }
            }
            else
            {
                ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ListenSocket.Bind(new IPEndPoint(IPAddress.Any, TheServer.Port));
            }
            ListenSocket.Listen(100);
            ListenThread = new Thread(new ThreadStart(ListenLoop)) { Name = VoxProgram.GameName + "_v" + VoxProgram.GameVersion + "_NetworkListenThread" };
            ListenThread.Start();
        }

        void ListenLoop()
        {
            while (true)
            {
                try
                {
                    Socket socket = ListenSocket.Accept();
                    lock (networkLock)
                    {
                        Connections.Add(new Connection(TheServer, socket));
                    }
                }
                catch (Exception ex)
                {
                    Utilities.CheckException(ex);
                    SysConsole.Output(OutputType.ERROR, "Network listen: " + ex.ToString());
                }
            }
        }

        public Object networkLock = new Object();

        public void Tick(double delta)
        {
            lock (networkLock)
            {
                for (int i = 0; i < Connections.Count; i++)
                {
                    if (Connections[i] == null)
                    {
                        Connections.RemoveAt(i);
                        i--;
                    }
                    if (Connections[i].Alive)
                    {
                        Connections[i].Tick(delta);
                    }
                    if (!Connections[i].Alive)
                    {
                        Connections.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }
}
