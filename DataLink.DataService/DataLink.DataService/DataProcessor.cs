using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace DataLink.DataService
{
    public class DataProcessor
    {
        static string _connectionString = ConfigurationManager.ConnectionStrings["dsConnection"].ConnectionString;
        static string _tableName = ConfigurationManager.AppSettings["LoggingTable"].ToString();
        static Queue _dataQueue = new Queue();
        static DataTable _dataTable = new DataTable();
        static System.Threading.Timer _timer ;

        public DataProcessor()
        {
            System.Threading.TimerCallback cb = new System.Threading.TimerCallback(ProcessData);
            _timer = new System.Threading.Timer(cb, null, 4000, 1000);
            InitDataTable();
        }
        private void ProcessData(object state)
        {
            //Console.WriteLine("Start Bulk Insert");
            BulkInsert();
        }
        public void AddToQueue(string data)
        {
            _dataQueue.Enqueue(data);
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
            for (int i = 0; i < _dataQueue.Count; i++)
            {
                //dequeue
                var data = (string)_dataQueue.Dequeue();
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