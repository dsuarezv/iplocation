using System;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Text;
using System.Linq;

namespace dyquo.iplocation
{
    public class IpLocation: IDisposable
    {
        /// <summary>
        /// Import a ip2location-lite-db5.csv file into Sqlite DB.
        /// </summary>
        /// <param name="ipLocationCsv"></param>
        /// <param name="ipLocationDb"></param>
        public static void ImportIpLocationCsv(string ipLocationCsv, string ipLocationDb)
        {
            if (File.Exists(ipLocationDb)) throw new Exception($"DB file {ipLocationDb} already exists. Not importing.");

            using (var c = GetConnection(ipLocationDb))
            {
                CreateTables(c, null);

                using (var r = File.OpenText(ipLocationCsv))
                {
                    string line;
                    int num = 0;
                    
                    var cmd = c.CreateCommand();
                    var cmdText = GetInsertBuilder();

                    while ((line = r.ReadLine()) != null)
                    {
                        cmdText.Append('(' + line + ')');

                        if (++num % 100000 == 0)
                        {
                            Console.WriteLine($"{num} records");

                            cmdText.AppendLine(";");
                            ExecuteInTransaction(c, cmdText.ToString());
                            cmdText = GetInsertBuilder();
                        }
                        else
                        {
                            cmdText.AppendLine(",");
                        }
                    }

                    cmdText[cmdText.Length - 3] = ';';  // Replace comma with semicolon
                    ExecuteInTransaction(c, cmdText.ToString());
                }

                CreateIndices(c, null);
            }
        }

        /// <summary>
        /// Initialize a new instance. 
        /// </summary>
        /// <param name="ipLocationDb">The Sqlite database file that contains the ip2location city-level database</param>
        public IpLocation(string ipLocationDb)
        {
            if (!File.Exists(ipLocationDb)) throw new Exception($"DB file {ipLocationDb} not found");

            mConnection = GetConnection(ipLocationDb);
        }

        /// <summary>
        /// Free Sqlite connection associated to the iplocation object.
        /// </summary>
        public void Dispose()
        {
            if (mConnection == null) return;

            mConnection.Dispose();
            mConnection = null;
        }

        /// <summary>
        /// Retrieve the location details for the given IP address.
        /// </summary>
        /// <param name="ipAddress">IPv4 address in x.x.x.x format</param>
        /// <returns></returns>
        public IpLocationResult GetIpLocation(string ipAddress)
        {
            var intIp = GetIntFromIp(ipAddress);

            var cmd = mConnection.CreateCommand();
            cmd.CommandText = $"SELECT countryCode, country, state, city, latitude, longitude FROM ipranges WHERE ipFrom <= {intIp} AND ipTo >= {intIp};";
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.Read()) return null;

                return new IpLocationResult
                {
                    IpAddress = ipAddress,
                    IntIpAddress = intIp,
                    CountryCode = reader.GetString(0),
                    Country = reader.GetString(1),
                    State = reader.GetString(2),
                    City = reader.GetString(3),
                    Latitude = reader.GetString(4),
                    Longitude = reader.GetString(5)
                };
            }
        }


        // __ Impl ____________________________________________________________


        private static void CreateTables(IDbConnection c, IDbTransaction t)
        {
            var cmd = c.CreateCommand();
            cmd.Transaction = t;
            cmd.CommandText = @"
                DROP INDEX IF EXISTS ipranges_fromip;
                DROP INDEX IF EXISTS ipranges_toip;
                DROP TABLE IF EXISTS ipranges;
                CREATE TABLE ipranges (
                    ipFrom  INTEGER,
                    ipTo    INTEGER,
                    countryCode TEXT,
                    country     TEXT,
                    state       TEXT,
                    city        TEXT,
                    latitude    NUMERIC,
                    longitude   NUMERIC
                );
            ";

            cmd.ExecuteNonQuery();
        }

        private static void CreateIndices(IDbConnection c, IDbTransaction t)
        {
            var cmd = c.CreateCommand();
            cmd.Transaction = t;
            cmd.CommandText = @"
                DROP INDEX IF EXISTS ipranges_fromip;
                DROP INDEX IF EXISTS ipranges_toip;
                CREATE INDEX ipranges_fromip ON ipranges(ipFrom);
                CREATE INDEX ipranges_toip ON ipranges(ipto);
            ";

            cmd.ExecuteNonQuery();
        }

        private static StringBuilder GetInsertBuilder()
        {
            var cmdText = new StringBuilder();
            cmdText.AppendLine("INSERT INTO ipranges (ipFrom, ipTo, countryCode, country, state, city, latitude, longitude) VALUES ");

            return cmdText;
        }

        private static void ExecuteInTransaction(IDbConnection c, string sql)
        {
            var t = c.BeginTransaction();
            try
            {
                var cmd = c.CreateCommand();
                cmd.Transaction = t;
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
                t.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                t.Rollback();
            }
        }

        private static IDbConnection GetConnection(string ipLocationDb)
        {
            var cs = $"Data Source={ipLocationDb}";
            var result = new SqliteConnection(cs);
            result.Open();

            return result;
        }

        private static uint GetIntFromIp(string ipAddress)
        {
            var cs = GetIpComponents(ipAddress);
            if (cs == null || cs.Length != 4) return 0;

            var c1 = cs[0] << 24;
            var c2 = cs[1] << 16;
            var c3 = cs[2] << 8;
            var c4 = cs[3];

            return c1 + c2 + c3 + c4;
        }

        private static uint[] GetIpComponents(string ipAddress)
        {
            if (String.IsNullOrEmpty(ipAddress)) return null;
            
            var comps = ipAddress.Split('.');
            if (comps.Length != 4) return null;

            return comps.Select<string, uint>(byteString =>
            {
                if (!uint.TryParse(byteString, out uint result)) return 0;
                if (result > 255) return 0;

                return result;
            }).ToArray();
        }


        private IDbConnection mConnection;
    }


    public class IpLocationResult
    {
        public string IpAddress { get; set; }
        public uint IntIpAddress { get; set; }
        public string CountryCode { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }

        public override string ToString()
        {
            return $"IP: {IpAddress} -> {IntIpAddress}: {Country}, {State}, {City}, ({Latitude}, {Longitude})";
        }
    }
}
