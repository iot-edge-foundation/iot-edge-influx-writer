namespace InfluxWriterModule
{
    using System;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Newtonsoft.Json;

    using InfluxDB.Collector;
    using System.Collections.Generic;
    using InfluxDB.Collector.Diagnostics;
    using Microsoft.Azure.Devices.Shared;
    class Program
    {
        static string DefaultInfluxDbUrl = "http://localhost:8086";
        static string DefaultInfluxDatabase = "influxDatabase";
        static string DefaultTableName = "tablename";
        static readonly char[] Token = "".ToCharArray();

        static Dictionary<string,string> Tags;

        private static string _moduleId; 

        private static string _deviceId;


        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        static async Task Init()
        {
            _deviceId = System.Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            _moduleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");

            Console.WriteLine(" ");
            Console.WriteLine("  _       _                  _                   _        __ _                                 _ _            ");
            Console.WriteLine(" (_)     | |                | |                 (_)      / _| |                               (_) |           ");
            Console.WriteLine("  _  ___ | |_ ______ ___  __| | __ _  ___ ______ _ _ __ | |_| |_   ___  __________      ___ __ _| |_ ___ _ __ ");
            Console.WriteLine(" | |/ _ \\| __|______/ _ \\/ _` |/ _` |/ _ \\______| | '_ \\|  _| | | | \\ \\/ /______\\ \\ /\\ / / '__| | __/ _ \\ '__|");
            Console.WriteLine(" | | (_) | |_      |  __/ (_| | (_| |  __/      | | | | | | | | |_| |>  <        \\ V  V /| |  | | ||  __/ |   ");
            Console.WriteLine(" |_|\\___/ \\__|      \\___|\\__,_|\\__, |\\___|      |_|_| |_|_| |_|\\__,_/_/\\_\\        \\_/\\_/ |_|  |_|\\__\\___|_|   ");
            Console.WriteLine("                                __/ |                                                                         ");
            Console.WriteLine("                               |___/                                                                          ");
            Console.WriteLine(" ");
            Console.WriteLine("   Copyright © 2021 - IoT Edge Foundation");
            Console.WriteLine(" ");

            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);

            // Attach callback for Twin desired properties updates
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(onDesiredPropertiesUpdate, ioTHubModuleClient);

            // Execute callback method for Twin desired properties updates
            var twin = await ioTHubModuleClient.GetTwinAsync();
            await onDesiredPropertiesUpdate(twin.Properties.Desired, ioTHubModuleClient);


            await ioTHubModuleClient.OpenAsync();

            Console.WriteLine($"Module '{_deviceId}'-'{_moduleId}' initialized.");

            Metrics.Collector = new CollectorConfiguration()
                            .Batch.AtInterval(TimeSpan.FromSeconds(2))
                            .WriteTo.InfluxDB(InfluxDbUrl, InfluxDatabase)
                            .CreateCollector();

            CollectorLog.RegisterErrorHandler((message, exception) =>
            {
                Console.WriteLine($"Infux Error. {message}: {exception}");
            });

            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", PipeMessage, ioTHubModuleClient);
        }

        static async Task<MessageResponse> PipeMessage(Message message, object userContext)
        {
            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            try
            {
                byte[] messageBytes = message.GetBytes();
                string messageString = Encoding.UTF8.GetString(messageBytes);
                Console.WriteLine($"Received message with body: '{messageString}'");

                if (!string.IsNullOrEmpty(messageString))
                {
                    var jsonMessage = JsonConvert.DeserializeObject<JsonMessage>(messageString);

                    // Write to InfluxDB
                    Metrics.Measure(TableName, jsonMessage.ambient.temperature, Tags );
                    System.Console.WriteLine("entry saved.");                
                    await Task.Delay(1);                    
                }                    
            }
            catch (System.Exception ex)
            { 
                System.Console.WriteLine($"Exception: {ex.Message}");
            }

            return MessageResponse.Completed;
        }

        private static string InfluxDbUrl { get; set; } = DefaultInfluxDbUrl;
        private static string InfluxDatabase { get; set; } = DefaultInfluxDatabase;
        private static string TableName { get; set; } = DefaultTableName;


        static Task Hello()
        {
            Task.Delay(100);

            return Task.CompletedTask;
        }

        private static Task onDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            if (desiredProperties.Count == 0)
            {
                return Task.CompletedTask;
            }

            try
            {
                Console.WriteLine("Desired property change:");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

                var client = userContext as ModuleClient;

                if (client == null)
                {
                    throw new InvalidOperationException($"UserContext doesn't contain expected ModuleClient");
                }

                var reportedProperties = new TwinCollection();

                Tags = new Dictionary<string, string>();

                if (desiredProperties.Contains("tags"))
                {
                    if (desiredProperties["tags"] != null)
                    {
                        var configArray = desiredProperties["tags"];

                        foreach (var config in configArray)
                        {
                            string name = config.Name;
                            string key = config.First.key;
                            string value = config.First.value;

                            Console.WriteLine($"{name}: key {key} - value {value}");

                            Tags.Add(key,value);
                        }
                    }

                    reportedProperties["tags"] = Tags.Count;
                }

                if (desiredProperties.Contains("influxDbUrl")) 
                {
                    if (desiredProperties["influxDbUrl"] != null)
                    {
                        InfluxDbUrl = desiredProperties["influxDbUrl"];
                    }
                    else
                    {
                        InfluxDbUrl = DefaultInfluxDbUrl;
                    }

                    Console.WriteLine($"InfluxDbUrl changed to {InfluxDbUrl}");

                    reportedProperties["influxDbUrl"] = InfluxDbUrl;
                }

                if (desiredProperties.Contains("influxDatabase")) 
                {
                    if (desiredProperties["influxDatabase"] != null)
                    {
                        InfluxDatabase = desiredProperties["influxDatabase"];
                    }
                    else
                    {
                        InfluxDatabase = DefaultInfluxDatabase;
                    }

                    Console.WriteLine($"InfluxDatabase changed to {InfluxDatabase}");

                    reportedProperties["influxDatabase"] = InfluxDatabase;
                }

                if (desiredProperties.Contains("tableName")) 
                {
                    if (desiredProperties["tableName"] != null)
                    {
                        TableName = desiredProperties["tableName"];
                    }
                    else
                    {
                        TableName = DefaultTableName;
                    }

                    Console.WriteLine($"tableName changed to {TableName}");

                    reportedProperties["tableName"] = TableName;
                }

                if (reportedProperties.Count > 0)
                {
                    client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
                }
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving desired property: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
            }

            return Task.CompletedTask;
        }
    }

    public class JsonMessage
    {
        public JsonMessage()
        {
            machine = new Machine();
            ambient = new Ambient();
        }

        public Machine machine {get; private set;}
        public Ambient ambient {get; private set;}

        public DateTime timeCreated { get; set; }
    }

    public class Machine
    {
        public double temperature {get; set;}
        public double pressure {get; set;}
        
    }

    public class Ambient
    {
        public double temperature {get; set;}
        public double humidity {get; set;}
        
    }
}