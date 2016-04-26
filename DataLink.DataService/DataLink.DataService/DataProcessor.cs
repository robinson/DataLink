using Microsoft.Azure.Devices.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;

namespace DataLink.DataService
{
    public class DataProcessor
    {
        static bool _debug = Convert.ToBoolean(ConfigurationManager.AppSettings["Debug"]);
        static string _connectionString = ConfigurationManager.ConnectionStrings["dsConnection"].ConnectionString.ToString();
        static string _tableName = ConfigurationManager.AppSettings["LoggingTable"].ToString();
        static int _dbLogTimerDueTime = Convert.ToInt32(ConfigurationManager.AppSettings["DbLogTimerDueTime"]);
        static int _dbLogTimerPeriod = Convert.ToInt32(ConfigurationManager.AppSettings["DbLogTimerPeriod"]);

        static int _azureTimerDueTime = Convert.ToInt32(ConfigurationManager.AppSettings["AzureTimerDueTime"]);
        static int _azureTimerPeriod = Convert.ToInt32(ConfigurationManager.AppSettings["AzureTImerPeriod"]);

        static string _iotHubUri = ConfigurationManager.AppSettings["IotHubUri"].ToString();
        static string _deviceId = ConfigurationManager.AppSettings["DeviceId"].ToString();
        static string _deviceKey = ConfigurationManager.AppSettings["DeviceKey"].ToString();

        static Queue _dBQueue = new Queue();
        static DataTable _dataTable = new DataTable();
        static System.Threading.Timer _timerDbLog ;

        static Queue _azureQueue = new Queue();
        static System.Threading.Timer _timerAzure;
        private static DeviceClient _deviceClient;

        public DataProcessor()
        {
            System.Threading.TimerCallback cb = new System.Threading.TimerCallback(ProcessData);
            _timerDbLog = new System.Threading.Timer(cb, null, _dbLogTimerDueTime, _dbLogTimerPeriod);
            InitDataTable();

            System.Threading.TimerCallback cba = new System.Threading.TimerCallback(SendToAzure);
            _timerAzure = new System.Threading.Timer(cba, null, _azureTimerDueTime, _azureTimerPeriod);

            _deviceClient = DeviceClient.Create(_iotHubUri,
               AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey(_deviceId, _deviceKey), TransportType.Http1);
        }
        private void SendToAzure(object state)
        {
            for (int i = 0; i < _azureQueue.Count; i++)
            {
                var data = (string)_azureQueue.Dequeue();
                if (!string.IsNullOrEmpty(data))
                {
                    SendDeviceToCloudMessagesAsync(data);
                    if (_debug)
                        Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, data);
                }
            }
        }
        private async void SendDeviceToCloudMessagesAsync(string messageInput)
        {
            var message = new Message(Encoding.ASCII.GetBytes(messageInput));
            await _deviceClient.SendEventAsync(message);
            //log when debug
            Debug.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageInput);
        }
        private void ProcessData(object state)
        {
            //Console.WriteLine("Start Bulk Insert");
            BulkInsert();
        }
        public void AddToDbQueue(string data)
        {
            _dBQueue.Enqueue(data);
        }

        public void AddToAzureQueue(string data)
        {
            _azureQueue.Enqueue(data);
        }
        private void InitDataTable()
        {
            _dataTable = new DataTable();

            //id
            DataColumn id = new DataColumn();
            id.DataType = System.Type.GetType("System.Guid");
            id.ColumnName = "Id";
            _dataTable.Columns.Add(id);


            //data
            DataColumn data = new DataColumn();
            data.DataType = System.Type.GetType("System.String");
            data.ColumnName = "Data";
            _dataTable.Columns.Add(data);

            //data
            DataColumn timeStamp = new DataColumn();
            timeStamp.DataType = System.Type.GetType("System.DateTime");
            timeStamp.ColumnName = "Timestamp";
            _dataTable.Columns.Add(timeStamp);
            
            DataColumn[] keys = new DataColumn[1];
            keys[0] = id;
            _dataTable.PrimaryKey = keys;
            //accept changes
            _dataTable.AcceptChanges();


        }
        private void FillDataRow(string data,ref DataTable dt)
        {
            var dr = dt.NewRow();
            dr["Id"] = Guid.NewGuid();
            dr["Data"] = data;
            dr["Timestamp"] = DateTime.UtcNow;
            dt.Rows.Add(dr);
        }
        private void BulkInsert()
        {
            for (int i = 0; i < _dBQueue.Count; i++)
            {
                //dequeue
                var data = (string)_dBQueue.Dequeue();
                if (!string.IsNullOrEmpty(data))
                {
                    FillDataRow(data, ref _dataTable);
                    _dataTable.AcceptChanges();
                }
            }
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = _tableName;
                    try
                    {
                        // Write from the source to the destination.
                        bulkCopy.WriteToServer(_dataTable);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    //clear datatable
                    _dataTable.Rows.Clear();
                }
            }

        }
    }
}