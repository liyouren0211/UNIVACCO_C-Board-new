using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;
namespace UNIVACCO_UI
{
    public class Access
    {
        OleDbConnection oleDb;

        public Access(string file) //建構函式
        {
            oleDb = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + file);
            oleDb.Open();
        }

        public DataTable Get()
        {
            string sql = "select * from Database1";
            //獲取表1的內容
            OleDbDataAdapter dbDataAdapter = new OleDbDataAdapter(sql, oleDb); //建立適配物件
            DataTable dt = new DataTable(); //新建表物件
            dbDataAdapter.Fill(dt); //用適配物件填充表物件
            //foreach (DataRow item in dt.Rows)
            //{
            //    Console.WriteLine(item[0] + " | " + item[1]);
            //}
            return dt;
        }

        public void Find(string project, string target)
        {
            string sql = "select * from Database1 WHERE " + project + "=" + target;
            //獲取表1中暱稱為LanQ的內容
            OleDbDataAdapter dbDataAdapter = new OleDbDataAdapter(sql, oleDb); //建立適配物件
            DataTable dt = new DataTable(); //新建表物件
            dbDataAdapter.Fill(dt); //用適配物件填充表物件
            foreach (DataRow item in dt.Rows)
            {
                Console.WriteLine(item[0] + " | " + item[1]);
            }
        }
        public bool Add(string sql)
        {
            //string sql = "insert into Database1 (暱稱,賬號) values ('LanQ','2545493686')";
            //往表1新增一條記錄，暱稱是LanQ，賬號是2545493686
            OleDbCommand oleDbCommand = new OleDbCommand(sql, oleDb);
            int i = oleDbCommand.ExecuteNonQuery(); //返回被修改的數目
            return i > 0;
        }
        public bool Del()
        {
            string sql = "delete from Database1 where 暱稱=''";
            //刪除暱稱為LanQ的記錄
            OleDbCommand oleDbCommand = new OleDbCommand(sql, oleDb);
            int i = oleDbCommand.ExecuteNonQuery();
            return i > 0;
        }
        public bool Change(string sql)
        {
            //string sql = "update Database1 set 賬號='233333' where 暱稱='東熊'";
            //將表1中暱稱為東熊的賬號修改成233333
            OleDbCommand oleDbCommand = new OleDbCommand(sql, oleDb);
            int i = oleDbCommand.ExecuteNonQuery();
            return i > 0;
        }
    }

}
