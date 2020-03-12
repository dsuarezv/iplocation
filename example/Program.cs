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

            IpLocation.ImportIpLocationCsv(ipLocationCsv, ipLocationDb);

            using (var ipl = new IpLocation(ipLocationDb))
            {
                var result = ipl.GetIpLocation("79.65.23.52");

                Console.WriteLine(result);
            }
        }
    }
}
