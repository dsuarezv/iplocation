using dyquo.iplocation;
using System;

namespace example
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: example {ip2location-lite-db5.csv} {dbFileName}");
                return;
            }

            var ipLocationCsv = args[0];
            var ipLocationDb = args[1];

            //IpLocation.ImportIpLocationCsv(ipLocationCsv, ipLocationDb);

            using (var ipl = new IpLocation(ipLocationDb))
            {
                PrintIp(ipl, "8.8.65.8");
                PrintIp(ipl, "8.8.4.4");
            }
        }

        static void PrintIp(IpLocation ipl, string ipAddress)
        {
            var result = ipl.GetIpLocation(ipAddress);
            
            if (result == null) 
                Console.WriteLine(ipAddress + " not found");
            else
                Console.WriteLine(result);
        }
    }
}
