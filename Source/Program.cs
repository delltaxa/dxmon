using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace dxmon
{
    public class iPrint {

        public static void printf(params object[] args) {
            for (int i = 0; i != args.Length; i += 2) {
                printColor(args[i].ToString(), (ConsoleColor)args[i + 1]);
            }
        }
        static void printColor(string str, ConsoleColor clr) {
            ConsoleColor oclr = Console.ForegroundColor;
            Console.ForegroundColor = clr;
            Console.Write(str);
            Console.ForegroundColor = oclr;
        }

        public static void error(string msg) {
            printf(" [-] ", ConsoleColor.Red, msg, ConsoleColor.White);
        }

        public static void info(string msg) {
            printf(" [+] ", ConsoleColor.Green, msg, ConsoleColor.White);
        }

        public static void even(string msg) {
            printf(" [*] ", ConsoleColor.Blue, msg, ConsoleColor.White);
        }
    }

    public class Program {
        public static bool is_up(string addr) {
            Ping ping = new Ping();
            PingReply pingReply = ping.Send(addr);

            if (pingReply.Status == IPStatus.Success) {
                return true;
            }
            else {
                return false;
            }
        }

        public static bool port_up(string addr, int port)
        {
            using (TcpClient tcpClient = new TcpClient()) {
                try {
                    tcpClient.Connect(addr, port);
                    return true;
                }
                catch (Exception) {
                    return false;
                }
            }
        }

        public static string port_to_service(int port) {
            Dictionary<int, string> ports = new Dictionary<int, string>(){
               {   23, "telnet"      },
               {   25, "smtp"        },
               {   53, "dns"         },
               {  137, "netBIOS/tcp" },
               {  139, "netBIOS/tcp" },
               {  445, "smb"         },
               {   80, "http"        },
               { 8080, "http"        },
               { 8443, "http"        },
               {  443, "https"       },
               { 1433, "db"          },
               { 1434, "db"          },
               { 3306, "db"          },
               { 3389, "remoted"     }
            };

            if (ports.ContainsKey(port)) {
                foreach (var prt in ports) {
                    if (prt.Key == port) {
                        return prt.Value;
                    }
                }
            }

            return "Unknown";
        }

        public static void print_ascii_art() {
            iPrint.printf(@"     ___   ____  __  ____  _   _ "+"\n", ConsoleColor.Green);
            iPrint.printf(@"    | \ \ / /  \/  |/ __ \| \ | |"+"\n", ConsoleColor.Green);
            iPrint.printf(@"  __| |\ V /| \  / | |  | |  \| |"+"\n", ConsoleColor.Green);
            iPrint.printf(@" / _` | > < | |\/| | |  | | . ` |"+"\n", ConsoleColor.Green);
            iPrint.printf(@"| (_| |/ . \| |  | | |__| | |\  |"+"\n", ConsoleColor.Green);
            iPrint.printf(@" \__,_/_/ \_\_|  |_|\____/|_| \_|"+"\n", ConsoleColor.Green);
            iPrint.printf(@"                 v1.0.0.0        "+"\n", ConsoleColor.Gray);
            iPrint.printf(@"                                 "+"\n", ConsoleColor.Gray);
        }

        public static void Main(string[] args) {
            Console.ForegroundColor = ConsoleColor.White;

            print_ascii_art();

            string interfacef = string.Empty;

            if (args.Length >= 1) {
                interfacef = args[0];
                iPrint.info("Selecting " + interfacef + "\n");
            }
            else {
                iPrint.error("No Interface set!\n");
                Environment.Exit(-1);
            }

            string interface_ip = interface_to_ip(interfacef);
            if (interface_ip != "") {
                iPrint.info($"Found {interface_ip}\n");
            }
            else {
                Console.Write("\n");
                iPrint.error("Could not find the selected Interface\n");
                Environment.Exit(-1);
            }

            string start_of_ip = string.Empty;
            string[] ip_splited = interface_ip.Split('.');
            start_of_ip = ip_splited[0] + "." + ip_splited[1] + "." + ip_splited[2];

            List<string> ip_list = new List<string>();

            for (int index = 1; index != 256; index++) {
                ip_list.Add(start_of_ip + "." + index);
            }

            string ip_range = start_of_ip + ".1-255";

            Dictionary<string, Dictionary<string, int[]>> results = new Dictionary<string, Dictionary<string, int[]>>();

            Dictionary<int, string> ports = new Dictionary<int, string>(){
               {   23, "telnet"      },
               {   25, "smtp"        },
               {   53, "dns"         },
               {  137, "netBIOS/tcp" },
               {  139, "netBIOS/tcp" },
               {  445, "smb"         },
               {   80, "http"        },
               { 8080, "http"        },
               { 8443, "http"        },
               {  443, "https"       },
               { 1433, "db"          },
               { 1434, "db"          },
               { 3306, "db"          },
               { 3389, "remoted"     }
            };


            Console.Write("\n");
            iPrint.even($"Scanning {ip_range}\n\n");

            int inde = 1;
            Parallel.ForEach(ip_list, addr => {
                bool isip_up = is_up(addr);
                if (isip_up) {
                    string hostname = "Unknown";
                    try {
                        hostname = Dns.GetHostEntry(addr).HostName;
                    } catch { }
                    List<int> openports = new List<int>();
                    Parallel.ForEach(ports, val => {
                        int port = val.Key;
                        string service = val.Value;
                        bool ispUP = port_up(addr, port);
                        if (ispUP) {
                            openports.Add(port);
                        }
                    });
                    Dictionary<string, int[]> second = new Dictionary<string, int[]>();
                    second.Add(hostname, openports.ToArray());
                    results.Add(addr, second);

                    Console.SetCursorPosition(0, Console.CursorTop);
                    iPrint.info($"Found {inde} of 255");

                    inde++;
                }
            });

            Console.Write("\n\n");

            foreach (var va in results) {
                string[] tmp_host = new string[255];
                va.Value.Keys.CopyTo(tmp_host, 0);
                string hostname = tmp_host[0];
                int[][] openports = new int[ports.Count][];
                va.Value.Values.CopyTo(openports, 0); 
                iPrint.info($"{va.Key} ({hostname})\n");

                foreach (int[] portss in openports) {
                    try {
                        foreach (int port in portss) {
                            Console.Write("    " /*4*/);
                            iPrint.printf(" [+] ", ConsoleColor.Green, $"Found (", ConsoleColor.White, $"{port_to_service(port)}", ConsoleColor.Blue, $") Service running on port ", ConsoleColor.White, $"{port}\n", ConsoleColor.Green);
                        }
                    } catch { }
                }
            }
        }

        public static string interface_to_ip(string interfacef) {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces()) {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) {
                    string name = ni.Name;
                    if (name == interfacef) {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses) {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }

            return "";
        }
    }
}
