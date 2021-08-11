using Open.Nat;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class uPnPHelper {
    private static List<Socket> sockets = new List<Socket>();
    private static NatDevice device;
    private static List<Mapping> maps = new List<Mapping>();
    private static int cancellationTime = 7000; //milliseconds
    private static List<string> errorMessages = new List<string>();
    private static List<string> debugMessages = new List<string>();
    public enum NatType { Closed, Open, Checking, Failed }
    public enum Protocol { UDP, TCP }
    private static NatType natType = NatType.Closed;
    /// <summary>
    /// Sets debug mode. Debug mode writes more detailed information in the debug log during port forwarding. Must be enabled BEFORE starting port forwarding in order to function.
    /// </summary>
    public static bool DebugMode { get; set; } = true;
    /// <summary>
    /// Sets error logging. Error logging writes all serious errors into its own log accessible through GetLastErrorMessage(), GetErrorMessages() and GetErrorMessageArray().
    /// </summary>
    public static bool LogErrors { get; set; } = true;

    /// <summary>
    /// Starts automatically forwarding the requested port asynchronously.
    /// </summary>
    /// <param name="protocol">Either UDP or TCP.</param>
    /// <param name="portNumber">Desired port to forward.</param>
    /// <param name="lifetime">The length of time to keep the port active for in milliseconds (0 = infinite).</param>
    /// <param name="description">The description of the port forward. This is shown by routers to help describe what the port forward is for. </param>
    public static void Start(Protocol protocol, int portNumber, int lifetime, string description)
    {
        errorMessages = new List<string>();
        debugMessages = new List<string>();
        natType = NatType.Checking;
        CreateDebugMessage("Starting uPnP Port Forwarding || " + description + " || " + portNumber + " " + protocol.ToString());
        if (protocol == Protocol.TCP)
        {
            Mapping newMap = new Mapping(Open.Nat.Protocol.Tcp, portNumber, portNumber, lifetime, description);
            MapPort(newMap);
        }
        else if (protocol == Protocol.UDP)
        {
            Mapping newMap = new Mapping(Open.Nat.Protocol.Udp, portNumber, portNumber, description);
            MapPort(newMap);
        } 
    }

    /// <summary>
    /// Removes any previously forwarded ports this session and closes connections.
    /// </summary>
    public static void CloseAll()
    {
        errorMessages = new List<string>();
        debugMessages = new List<string>();
        natType = NatType.Checking;
        CreateDebugMessage("Closing all ports and sockets.");
        if (device != null)
        {
            Task closeMaps = Task.Run(() =>
            {
                int count = 0;
                foreach (Mapping map in maps)
                {
                    if (device != null)
                    {
                        CreateDebugMessage("CloseAll() - Deleting Map #" + count.ToString() + " || " + map.Description + " || " + map.Protocol + ", " + map.PublicPort + "-" + map.PrivatePort + ", " + map.PublicIP + "-" + map.PrivateIP + ", Life: " + map.Lifetime + ", Exp: " + map.Expiration);
                        Task deleteAsyncTask = device.DeletePortMapAsync(map);
                        deleteAsyncTask.Wait(1000);
                        if (deleteAsyncTask.Status == TaskStatus.RanToCompletion)
                        {
                            CreateDebugMessage("CloseAll() - Deleted Map #" + count.ToString() + " Successfully.");
                        }
                        else if (deleteAsyncTask.Exception != null)
                        {
                            CreateErrorMessage("CloseAll() - DELETE PORT MAP ERROR: " + deleteAsyncTask.Exception);
                            natType = NatType.Failed;
                        }
                        deleteAsyncTask.Dispose();
                    }
                    else
                    {
                        CreateDebugMessage("CloseAll() - Lost Device When Deleting Map #" + count.ToString());
                    }
                    count++;
                }
                if (count == 0)
                {
                    CreateDebugMessage("CloseAll() - No Maps To Delete.");
                }
                maps.Clear();
            }).ContinueWith(t =>
            {
                device = null;
                int counter = 0;
                foreach (Socket socket in sockets)
                {
                    if (socket != null)
                    {
                        try
                        {
                            CreateDebugMessage("CloseAll() - Closing Socket #" + counter.ToString() + ": " + socket.ProtocolType.ToString());
                            socket.Close();
                            socket.Dispose();
                            CreateDebugMessage("CloseAll() - Closed Socket #" + counter.ToString() + " Successfully.");
                        }
                        catch (SocketException e)
                        {
                            CreateErrorMessage("CloseAll() - SOCKET CLOSE ERROR: " + e.Message);
                            natType = NatType.Failed;
                        }
                    }
                    else
                    {
                        CreateErrorMessage("CloseAll() - Lost Socket #" + counter.ToString() + " On Close.");
                        natType = NatType.Failed;
                    }
                    counter++;
                }
                if (counter == 0)
                {
                    CreateDebugMessage("CloseAll() - No Sockets To Close.");
                }
                sockets.Clear();
                natType = NatType.Closed;
                CreateDebugMessage("Finished closing all ports and sockets.");
                t.Dispose();
            });
        }
        else
        {
            natType = NatType.Closed;
            CreateDebugMessage("CloseAll() - Device Isn't Connected.");
        }
    }

    /// <summary>
    /// Returns the current status of the port forward.
    /// </summary>
    public static NatType GetNATType()
    {
        return natType;
    }

    /// <summary>
    /// Returns the last error message generated.
    /// </summary>
    public static string GetLastErrorMessage()
    {
        if (errorMessages != null && errorMessages.Count > 0)
        {
            return errorMessages[errorMessages.Count - 1];
        }
        return null;
    }

    /// <summary>
    /// Returns all error messages generated in session as an array.
    /// </summary>
    public static List<string> GetErrorMessageArray()
    {
        return errorMessages;
    }

    /// <summary>
    /// Returns all error messages generated in session as a single formatted string.
    /// </summary>
    /// <returns></returns>
    public static string GetErrorMessages()
    {
        if (errorMessages != null)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string message in errorMessages)
            {
                if (sb.Length > 0)
                {
                    sb.Append("\n" + message);
                }
                else
                {
                    sb.Append(message);
                }
            }
            return sb.ToString();
        }
        return null;
    }

    private static void CreateErrorMessage(string text)
    {
        natType = NatType.Failed;
        if (LogErrors == true)
        {
            errorMessages.Add(text);
        }
        CreateDebugMessage(text);
    }

    /// <summary>
    /// Returns the last debug message generated.
    /// </summary>
    public static string GetLastDebugMessage()
    {
        if (debugMessages != null && debugMessages.Count > 0)
        {
            return debugMessages[debugMessages.Count - 1];
        }
        return null;
    }

    /// <summary>
    /// Returns all debug messages generated in session as an array.
    /// </summary>
    public static List<string> GetDebugMessageArray()
    {
        return debugMessages;
    }

    /// <summary>
    /// Returns all debug messages generated in session as a single formatted string.
    /// </summary>
    /// <returns></returns>
    public static string GetDebugMessages()
    {
        if (debugMessages != null)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < debugMessages.Count; i++)
            {
                if (sb.Length > 0)
                {
                    sb.Append("\n" + debugMessages[i]);
                }
                else
                {
                    sb.Append(debugMessages[i]);
                }
            }
            return sb.ToString();
        }
        return null;
    }

    private static void CreateDebugMessage(string text)
    {
        if (DebugMode == true)
        {
            debugMessages.Add(text);
        }
    }

    private static void OpenSocket(ProtocolType protocol, int portNumber)
    {
        if (protocol == ProtocolType.Tcp)
        {
            //TCP Socket
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, portNumber);
            Socket newSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
            if (!SocketAlreadyOpen(newSocket, endPoint))
            {
                CreateDebugMessage("OpenSocket() - Connecting TCP Socket: " + portNumber);
                try
                {
                    newSocket.Bind(endPoint);
                    CreateDebugMessage("OpenSocket() - Connected Socket (" + protocol.ToString() + " - " + portNumber + ") Successfully.");
                    sockets.Add(newSocket);
                }
                catch (Exception e)
                {
                    CreateErrorMessage("OpenSocket() - BIND ERROR: " + e.Message);
                    natType = NatType.Failed;
                }
                CreateDebugMessage("OpenSocket() - Start Listening To TCP Socket.");
                try
                {
                    newSocket.Listen(4);
                    CreateDebugMessage("OpenSocket() - Listened To Socket (" + protocol.ToString() + " - " + portNumber + ") Successfully.");
                }
                catch (Exception e)
                {
                    CreateErrorMessage("OpenSocket() - LISTEN ERROR: " + e.Message);
                    natType = NatType.Failed;
                }
            }
            else
            {
                CreateDebugMessage("OpenSocket() - Socket Already Open.");
                newSocket.Dispose();
            }
        }
        else if (protocol == ProtocolType.Udp)
        {
            //UDP Socket
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, portNumber);
            Socket newSocket = new Socket(endPoint.AddressFamily, SocketType.Dgram, protocol);
            newSocket.MulticastLoopback = true;
            IPEndPoint newEndPoint = new IPEndPoint(IPAddress.Loopback, portNumber);
            if (!SocketAlreadyOpen(newSocket, newEndPoint))
            {
                CreateDebugMessage("OpenSocket() - Connecting UDP Socket: " + portNumber);
                try
                {
                    newSocket.Connect(newEndPoint);
                    CreateDebugMessage("OpenSocket() - Connected Socket (" + protocol.ToString() + " - " + portNumber + ") Successfully.");
                    sockets.Add(newSocket);
                }
                catch (Exception e)
                {
                    CreateErrorMessage("OpenSocket() - CONNECT ERROR: " + e.Message);
                    natType = NatType.Failed;
                }
            }
            else
            {
                CreateDebugMessage("OpenSocket() - Socket Already Open.");
                newSocket.Dispose();
            }
        }
        CreateDebugMessage("OpenSocket() - Finished Opening Socket.");
    }

    private static bool SocketAlreadyOpen(Socket newSocket, EndPoint endPoint)
    {
        for (int i = 0; i < sockets.Count; i++)
        {
            if(sockets[i].AddressFamily == newSocket.AddressFamily && sockets[i].SocketType == newSocket.SocketType && sockets[i].ProtocolType == newSocket.ProtocolType && sockets[i].RemoteEndPoint.AddressFamily == endPoint.AddressFamily)
            {
                return true;
            }
        }
        return false;
    }

    private static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("Local IP Address Not Found!");
    }

    private static void AddMap(Mapping map)
    {
        if (maps != null)
        {
            bool foundMap = false;
            string localIP = GetLocalIPAddress();
            for (int i = 0; i < maps.Count; i++)
            {
                if ((maps[i].PrivatePort == map.PrivatePort || maps[i].PublicPort == map.PublicPort) && maps[i].PrivateIP.ToString() == localIP)
                {
                    foundMap = true;
                    break;
                }
            }
            if (!foundMap)
            {
                maps.Add(map);
            }
        }
    }

    private static Task MapPort(Mapping map)
    {
        natType = NatType.Checking;
        NatDiscoverer nat = new NatDiscoverer();
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(cancellationTime);
        device = null;
        bool preExistingPortFound = false;
        int counter = 0;

        if (maps == null)
        {
            maps = new List<Mapping>();
        }
        CreateDebugMessage("MapPort() - Starting Device Discovery.");
        return nat.DiscoverDeviceAsync(PortMapper.Upnp, cts).ContinueWith(task =>
        {
            CreateDebugMessage("MapPort() - Device Discovery Status: " + task.Status.ToString());
            if (task != null && task.Result != null)
            {
                device = task.Result;
                CreateDebugMessage("MapPort() - Discovered Device Successfully.");
                CreateDebugMessage("MapPort() - Getting Device External IP.");
                try
                {
                    if (device != null)
                    {
                        return device.GetExternalIPAsync();
                    }
                    else
                    {
                        CreateErrorMessage("MapPort() - DEVICE IS NULL ERROR");
                        natType = NatType.Failed;
                        return null;
                    }
                }
                catch (NatDeviceNotFoundException e)
                {
                    CreateErrorMessage("MapPort() - GET EXTERNAL IP TASK ERROR: " + e.Message);
                    natType = NatType.Failed;
                    return null;
                }
            }
            else
            {
                CreateErrorMessage("MapPort() - FAILED TO DISCOVER A DEVICE");
                natType = NatType.Failed;
                return null;
            }
        }).Unwrap().ContinueWith(task =>
        {
            if (task != null)
            {
                CreateDebugMessage("MapPort() - Received External IP Successfully.");
                CreateDebugMessage("MapPort() - Getting Device Mappings.");
                try
                {
                    if (device != null)
                    {
                        return device.GetAllMappingsAsync();
                    }
                    else
                    {
                        CreateErrorMessage("MapPort() - DEVICE IS NULL ERROR");
                        natType = NatType.Failed;
                        return null;
                    }
                }
                catch (MappingException e)
                {                    
                    CreateErrorMessage("MapPort() - GET PORT MAPS TASK ERROR: " + e.Message);
                    natType = NatType.Failed;
                    return null;
                }
            }
            else
            {
                CreateErrorMessage("MapPort() - FAILED TO GET EXTERNAL IP");
                natType = NatType.Failed;
                return null;
            }
        }).Unwrap().ContinueWith(task =>
        {
            if (task != null && task.Result != null)
            {
                try
                {
                    if (device != null)
                    {              
                        string localIP = GetLocalIPAddress();
                        foreach (Mapping deviceMaps in task.Result)
                        {
                            CreateDebugMessage("MapPort() - Mapping #" + counter.ToString() + " || " + deviceMaps.Description + " || " + deviceMaps.Protocol + ", " + deviceMaps.PublicPort + "-" + deviceMaps.PrivatePort + ", " + deviceMaps.PublicIP + "-" + deviceMaps.PrivateIP + ", Life: " + deviceMaps.Lifetime + ", Exp: " + deviceMaps.Expiration);
                            if (deviceMaps.PrivatePort == map.PrivatePort || deviceMaps.PublicPort == map.PublicPort)
                            {
                                if (localIP == deviceMaps.PrivateIP.ToString())
                                {
                                    CreateDebugMessage("MapPort() - Port Already Open.");
                                    natType = NatType.Open;
                                    preExistingPortFound = true;
                                    break;
                                }
                                else
                                {
                                    CreateErrorMessage("MapPort() - THE SELECTED PORT IS ALREADY IN USE WITHIN YOUR NETWORK BY ANOTHER DEVICE.");
                                    preExistingPortFound = true;
                                    natType = NatType.Failed;
                                    return null;
                                }
                            }
                            counter++;
                        }
                        if (device != null)
                        {
                            if (preExistingPortFound == false)
                            {
                                CreateDebugMessage("MapPort() - Creating Port Map.");
                                try
                                {
                                    return device.CreatePortMapAsync(map);                                    
                                }
                                catch(MappingException e)
                                {
                                    CreateErrorMessage("MapPort() - FAILED TO CREATE MAPPING. CREATE MAPPING EXCEPTION: " + e.Message);
                                    natType = NatType.Failed;
                                }
                            }                            
                            return null;
                        }
                        else
                        {
                            CreateErrorMessage("MapPort() - Lost Connection To Device.");
                            natType = NatType.Failed;
                            return null;
                        }
                    }
                    else
                    {
                        CreateErrorMessage("MapPort() - DEVICE IS NULL ERROR");
                        natType = NatType.Failed;
                        return null;
                    }
                }
                catch (MappingException e)
                {
                    CreateErrorMessage("MapPort() - CREATE PORT MAP TASK ERROR: " + e.Message);
                    natType = NatType.Failed;
                    return null;
                }
            }
            else
            {
                CreateErrorMessage("MapPort() - FAILED TO GET PORT MAPS");
                natType = NatType.Failed;
                return null;
            }      
        }).Unwrap().ContinueWith(task =>
        {
            if (task != null)
            {
                AddMap(map);
                if (preExistingPortFound == true && natType == NatType.Open)
                {
                    if (map.Protocol == Open.Nat.Protocol.Udp)
                    {
                        OpenSocket(ProtocolType.Udp, map.PublicPort);
                    }
                    else
                    {
                        OpenSocket(ProtocolType.Tcp, map.PublicPort);
                    }
                }
                else
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        natType = NatType.Open;
                        CreateDebugMessage("MapPort() - Mapping #" + counter.ToString() + " || " + map.Description + " || " + map.Protocol + ", " + map.PublicPort + "-" + map.PrivatePort + ", " + map.PublicIP + "-" + map.PrivateIP + ", Life: " + map.Lifetime + ", Exp: " + map.Expiration);
                        if (map.Protocol == Open.Nat.Protocol.Udp)
                        {
                            OpenSocket(ProtocolType.Udp, map.PublicPort);
                        }
                        else
                        {
                            OpenSocket(ProtocolType.Tcp, map.PublicPort);
                        }
                    }
                    else
                    {
                        CreateErrorMessage("MapPort() - FAILED TO CREATE PORT MAPS");
                        natType = NatType.Failed;
                    }
                }
            }
            CreateDebugMessage("Finished uPnP Port Mapping.");
            cts.Dispose();
            task.Dispose();
        });
    }
}