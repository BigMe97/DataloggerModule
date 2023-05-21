using System.Xml;
using Opc.UaFx;
using Opc.UaFx.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SqlClient;
using System.Data;

namespace OPCtoSQLlogger
{
    internal class Program
    {
        public static Dictionary<string, string> queries = new Dictionary<string, string>();
        public static string DeviceConfigFile = $"..\\..\\DeviceConfig.xml";
        public static string SQLConfigFile = $"..\\..\\SQLconfig.xml";
        public static SqlConnection cnn;
        public static string connetionString;
        public static List<Device> Devices;
        public static bool localHost = true;


        static void Main(string[] args)
        {
            bool run = true;
            
            try
            {
                ConnectToSQL(SQLConfigFile);
                Console.WriteLine("Connected to SQL Server");
                Devices = LoadDevices(DeviceConfigFile);
                Console.WriteLine("Configuration loaded");

            }
            catch (Exception)
            {
                run = false;
                writeError("Could not connect to SQL or load device config");
            }

            while (run)
            {
                

                foreach (Device item in Devices)
                {
                    try
                    {
                        readOPC(item);
                    }
                    catch (Exception)
                    {
                        writeError("OPC reading failed");
                    }
                    
                    if (item.newUpdate)
                    {
                        Console.Write($"Read from: {item.OPCAddress}  {item.OPCTag}    ");
                        writeSQL(item);
                        Console.WriteLine($"Wrote to: {cnn.Database}  {item.Tag}  Value: {item.value}");
                    }
                }


                if (Console.KeyAvailable)
                {
                    ConsoleKey keypress = Console.ReadKey().Key;
                    if (keypress == ConsoleKey.X)
                    {
                        Console.Clear();
                        Console.WriteLine("Disconnecting");
                        run = false;
                    }
                }
            }
            try
            {
                cnn.Close();
            }
            catch (Exception)
            {

            }
            Console.ReadKey();
        }

        public static void ConnectToSQL(string FilePath)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(FilePath);

            // Select all Device elements
            XmlNode SQLNodes = xml.SelectSingleNode("SQL");

            string dbHost = SQLNodes.SelectSingleNode("DBHost").InnerText;
            string dbName = SQLNodes.SelectSingleNode("DBName").InnerText;
            string user = SQLNodes.SelectSingleNode("User").InnerText;
            string password = SQLNodes.SelectSingleNode("Password").InnerText;
            if (SQLNodes.SelectSingleNode("LocalHost").InnerText == "true")
            {
                dbHost = System.Environment.MachineName + dbHost;
            }
            connetionString = String.Format("Data Source={0}; Initial Catalog={1}; User id={2}; Password={3};", dbHost, dbName, user, password);
            Console.WriteLine($"Connection String:   {connetionString}");
            cnn = new SqlConnection(connetionString);
            cnn.Open();
            
        }

        public static List<Device> LoadDevices(string FilePath)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(FilePath);

            // Select all Device elements
            XmlNodeList deviceNodes = xml.SelectNodes("//Device");

            // Create a list to store the Device objects
            List<Device> deviceList = new List<Device>();
            foreach (XmlNode deviceNode in deviceNodes)
            {
                Device device = new Device();
                device.OPCAddress = deviceNode.SelectSingleNode("OPCAddress").InnerText;
                device.OPCTag = deviceNode.SelectSingleNode("OPCTag").InnerText;
                device.MainLocation = deviceNode.SelectSingleNode("MainLocation").InnerText;
                device.Location = deviceNode.SelectSingleNode("Location").InnerText;
                device.Equipment = deviceNode.SelectSingleNode("Equipment").InnerText;
                device.Tag = deviceNode.SelectSingleNode("Tag").InnerText;

                deviceList.Add(device);
            }
            return deviceList;
        }

        public static void readOPC(Device item)
        {
            OpcClient client = new OpcClient();

            try
            {
                client.ServerAddress = new Uri(item.OPCAddress);
                client.Connect();

                OpcValue value = client.ReadNode(item.OPCTag);

                DateTime? time = value.SourceTimestamp;
                if (time != item.lastUpdate)
                {
                    item.value = Convert.ToDouble(value.Value);
                    item.newUpdate = true;
                    item.lastUpdate = time;
                }

                client.Disconnect();

            }
            catch (Exception e)
            {

                writeError($"Fault on {item.OPCAddress} {item.OPCTag} : {e.Message}");
            }


            
        }

        public static void writeError(string errorMessage)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorMessage);
            Console.ForegroundColor = temp;
        }

        public static void writeSQL(Device device)
        {
            SqlCommand command = new SqlCommand("AddMeasurement", cnn);
            command.CommandType = CommandType.StoredProcedure;

            // add input parameters
            command.Parameters.AddWithValue("@main_location_name", device.MainLocation);
            command.Parameters.AddWithValue("@location_name", device.Location);
            command.Parameters.AddWithValue("@equipment_name", device.Equipment);
            command.Parameters.AddWithValue("@device_tag", device.Tag);
            command.Parameters.AddWithValue("@value", device.value);

            // cnn.Open();
            command.ExecuteNonQuery();
            device.newUpdate = false;
        }
    }
}
