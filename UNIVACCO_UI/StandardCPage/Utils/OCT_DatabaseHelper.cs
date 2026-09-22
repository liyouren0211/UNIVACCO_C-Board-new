using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StandardOPage
{
    public class OCT_DatabaseHelper
    {
        private string connectionString;
        public OCT_DatabaseHelper(string dbname)
        {
            connectionString = $"Data Source={dbname}.db;Version=3;";
        }

        //撈全部資料
        public List<OCT_ParameterMeshP> GetAllData(string tablename)
        {
            List<OCT_ParameterMeshP> parameterSets = new List<OCT_ParameterMeshP>();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = $"SELECT Name, `Area10`, `Area20`, `Area30`, `Area40`, `Area60`, `Area70`, `Area80`, `Area90` FROM {tablename}";
                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        parameterSets.Add(new OCT_ParameterMeshP
                        {
                            Name = reader["Name"].ToString(),
                            Area10 = Convert.ToDouble(reader["Area10"]),
                            Area20 = Convert.ToDouble(reader["Area20"]),
                            Area30 = Convert.ToDouble(reader["Area30"]),
                            Area40 = Convert.ToDouble(reader["Area40"]),
                            Area60 = Convert.ToDouble(reader["Area60"]),
                            Area70 = Convert.ToDouble(reader["Area70"]),
                            Area80 = Convert.ToDouble(reader["Area80"]),
                            Area90 = Convert.ToDouble(reader["Area90"])
                        });
                    }
                }
            }
            return parameterSets;
        }
        //新增一筆資料
        public (bool,string) InsertOneData(string tablename, OCT_ParameterMeshP parameter)
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    string checkQuery = $"SELECT COUNT(*) FROM {tablename} WHERE Name = @Name";
                    using (var checkCmd = new SQLiteCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Name", parameter.Name);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        string message = $"臨時標準 {parameter.Name} 已存在，請更換名稱！";

                        if (count > 0)
                        {
                            return (false,message);
                        }
                    }


                    string query = @"INSERT INTO TempMeshP 
                            (Name, `Area10`, `Area20`, `Area30`, `Area40`, `Area60`, `Area70`, `Area80`, `Area90`) 
                            VALUES (@Name, @Area10, @Area20, @Area30, @Area40, @Area60, @Area70, @Area80, @Area90)";

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", parameter.Name);
                        cmd.Parameters.AddWithValue("@Area10", parameter.Area10);
                        cmd.Parameters.AddWithValue("@Area20", parameter.Area20);
                        cmd.Parameters.AddWithValue("@Area30", parameter.Area30);
                        cmd.Parameters.AddWithValue("@Area40", parameter.Area40);
                        cmd.Parameters.AddWithValue("@Area60", parameter.Area60);
                        cmd.Parameters.AddWithValue("@Area70", parameter.Area70);
                        cmd.Parameters.AddWithValue("@Area80", parameter.Area80);
                        cmd.Parameters.AddWithValue("@Area90", parameter.Area90);

                        cmd.ExecuteNonQuery();
                    }
                }
                return (true, $"臨時標準{parameter.Name}新增完成");
            }
            catch(Exception ex)
            {
                return (false, "資料庫發生錯誤，請聯絡系統管理員");
            }
          
        }
        public (bool, string) DeleteOneData(string tablename, string name)
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    string deleteQuery = $"DELETE FROM {tablename} WHERE Name = @Name";
                    using (var cmd = new SQLiteCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        int affectedRows = cmd.ExecuteNonQuery();

                        if (affectedRows == 0)
                        {
                            return (false, $"⚠️ 找不到名稱為 {name} 的資料，無法刪除");
                        }

                        return (true, $"✅ 臨時標準 {name} 已被刪除");
                    }
                }
            }
            catch (Exception ex)
            {
                // 回傳簡潔訊息給 UI 顯示
                return (false, "❌ 資料庫發生錯誤，請聯絡系統管理員");
            }
        }

    }
}
