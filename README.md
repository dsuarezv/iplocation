# iplocation

This is a small and fast library that will import a geolocation database from IP2LOCATION into sqlite and provide a quick package to lookup IP address locations. 

## Installation



## Usage

You need to register at ip2location.com and download one of the databases they provide. This code is made to parse the CSV version of the ip2location-lite-db5 database. This database will provide accuracy up to the city level, as well as latitude and longitude.

Once you have the ip2location-lite-db5.csv file on your computer, run this code to import it into a sqlite DB that will later use for querying:

	IpLocation.ImportIpLocationCsv(ipLocationCsv, ipLocationDb);

You only need to this once. It will generate a sqlite file with the parsed contents of the CSV file. 

To query IP address: 

    using (var ipl = new IpLocation(ipLocationDb))
    {
        var result = ipl.GetIpLocation("8.8.8.8");

        Console.WriteLine(result);
    }

You can check the "example" project for a full example. 