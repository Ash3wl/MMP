//#if WINDOWS || MONOMAC
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace FEGame
{
    class NetConnection
    {
        const int CONNECTION_RESET_FAILURE_COUNT = 2; // After this many failed attempts to contact a known host IP, retest for the IP

        private static Dictionary<string, IPAddress> HostIps = new Dictionary<string, IPAddress>();
        private static Dictionary<string, int> HostFails = new Dictionary<string, int>();

        static object NetworkLock = new object();

        internal static string webPost(string _URI, string _postString = "", TimeSpan timeout = default(TimeSpan))
        {
            const string REQUEST_METHOD_POST = "POST";
            const string CONTENT_TYPE = "application/x-www-form-urlencoded";
            WebResponse response = null;
            string result = null;
            string responseFromServer = null;
            try
            {
                // First try connecting to the server with a quick timeout message, because if that fails we don't want to be wasting time
                if (test_connection(_URI))
                {
                    // Create a request using a URL that can receive a post.
                    WebRequest request = WebRequest.Create(_URI);
                    if (timeout.TotalMilliseconds > 0)
                        request.Timeout = (int)timeout.TotalMilliseconds;
                    // Set the Method property of the request to POST.
                    request.Method = REQUEST_METHOD_POST;
                    // Create POST data and convert it to a byte array.
                    string postData = "message=" + _postString;
                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                    // Set the ContentType property of the WebRequest.
                    request.ContentType = CONTENT_TYPE;
                    // Set the ContentLength property of the WebRequest.
                    request.ContentLength = byteArray.Length;
                    // Get the request stream.
                    using (Stream dataStream = request.GetRequestStream())
                    {
                        // Write the data to the request stream.
                        dataStream.Write(byteArray, 0, byteArray.Length);
                    }
                    // Get the response.
                    response = request.GetResponse();
                    // Display the status.
                    Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                    // Get the stream containing content returned by the server.
                    using (Stream dataStream = response.GetResponseStream())
                    {
                        // Open the stream using a StreamReader for easy access.
                        using (StreamReader reader = new StreamReader(dataStream))
                        {
                            // Read the content.
                            result = reader.ReadToEnd();
                            // Display the content.
                            Console.WriteLine(responseFromServer);
                        }
                    }
                }
            }
            catch (System.Net.WebException ex)
            { }
            finally
            {
                if (response != null)
                    response.Close();
            }
            return result;
        }

        internal static bool host_ip_found(string url)
        {
            String host = new Uri(url).Host;
            return HostIps.ContainsKey(host);
        }

        internal static bool test_connection_dns(string url)
        {
            lock (NetworkLock)
            {
                String host = new Uri(url).Host;
                bool already_have_ip = HostIps.ContainsKey(host);
                // If DNS hasn't been checked for the IP of this domain, or we've failed multiple times
                if (!already_have_ip || (HostFails[host] > 0 && HostFails[host] % CONNECTION_RESET_FAILURE_COUNT == 0))
                {
                    if (!get_host_ip(host))
                        return false;
                }

                bool response_received = ping_ip(HostIps[host]);
                if (response_received)
                {
                    // Reconfirm that we have the correct IP here just in case, since we have a connection this should be fast
                    if (!get_host_ip(host))
                        return false;
                }
                return response_received;
            }
        }
        private static bool test_connection(string url)
        {
            lock (NetworkLock)
            {
                String host = new Uri(url).Host;
                if (!HostIps.ContainsKey(host))
                    return false;

                bool response_received = ping_ip(HostIps[host]);
                if (response_received)
                    HostFails[host] = 0;
                else
                    HostFails[host]++;
                return response_received;
            }
        }

        private static bool get_host_ip(String host)
        {
            try
            {
                var dns = Dns.GetHostEntry(host);
                set_host_ip(host, dns.AddressList[0]);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private static void set_host_ip(string host, IPAddress ip)
        {
            HostIps[host] = ip;
            HostFails[host] = 0;
        }

        private static bool ping_ip(IPAddress ip)
        {
            try
            {
                Ping ping = new Ping();
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = ping.Send(ip, timeout, buffer, pingOptions);
                return reply.Status == IPStatus.Success;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Finds the MAC address of the NIC with maximum speed.
        /// </summary>
        /// <returns>The MAC address.</returns>
        internal static string GetMacAddress()
        {
            const int MIN_MAC_ADDR_LENGTH = 12;
            string macAddress = string.Empty;
            long maxSpeed = -1;

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                Console.WriteLine(
                    "Found MAC Address: " + nic.GetPhysicalAddress() +
                    " Type: " + nic.NetworkInterfaceType);

                string tempMac = nic.GetPhysicalAddress().ToString();
                if (nic.Speed > maxSpeed &&
                    !string.IsNullOrEmpty(tempMac) &&
                    tempMac.Length >= MIN_MAC_ADDR_LENGTH)
                {
                    Console.WriteLine("New Max Speed = " + nic.Speed + ", MAC: " + tempMac);
                    maxSpeed = nic.Speed;
                    macAddress = tempMac;
                }
            }

            return macAddress;
        }
    }
}
//#endif