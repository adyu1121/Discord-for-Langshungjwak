using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Threading.Channels;

public static class Database
{
    private static Dictionary<ulong, ulong> C = new Dictionary<ulong, ulong>();
    private static MySqlConnection client;

    public static void Init()
    {
        string host = Environment.GetEnvironmentVariable("HOST");
        string port = Environment.GetEnvironmentVariable("PORT");
        string database = Environment.GetEnvironmentVariable("DATABASE_NAME");
        string userDB = Environment.GetEnvironmentVariable("USER_NAME");
        string password = Environment.GetEnvironmentVariable("PASSWORD");

        string con = $"server={host};Port={port};Database={database};User ID={userDB};Password={password}";

        try
        {
            client = new MySqlConnection(con);
            client.Open();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverId"></param>
    /// <returns></returns>
    public static bool IsSetServer(ulong serverId)
    {
        try
        {
            string q = """
                
                SELECT EXISTS
                (
                	SELECT * FROM channel WHERE serverId=?serverId
                ) AS t
                
                """;

            using MySqlCommand cmd = new MySqlCommand(q, client);
            cmd.Parameters.Add("?serverId", MySqlDbType.UInt64).Value = serverId;

            using MySqlDataReader reader = cmd.ExecuteReader();

            reader.Read();
            int t = (int)reader["t"];
            return t == 1;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// 현재 랭슝좍을 감지하는 채널을 가져옵니다.
    /// 만약 존재하지 않으면 0을 반환합니다.
    /// 실행하기 전에 IsSetServer로 검사하세요.
    /// </summary>
    /// <param name="serverId"></param>
    /// <returns></returns>
    public static ulong GetChannel(ulong serverId)
    {
        string q = "select channelId from channel where serverId=?serverId";
        try
        {
            using MySqlCommand cmd = new MySqlCommand(q, client);

            cmd.Parameters.Add("?serverId", MySqlDbType.UInt64).Value = serverId;

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                reader.Read();
                ulong id = (ulong)reader["channelId"];
                return id;
            }
        }
        catch (Exception ex)
        {
            return 0;
        }
    }

    /// <summary>
    /// 랭슝좍을 감지하는 채널을 추가합니다.
    /// 만약 추가에 실패한다면 false를 반환합니다.
    /// 실행하기 전에 IsSetServer로 검사하세요.
    /// </summary>
    /// <param name="serverId">서버의 id</param>
    /// <param name="channelId">채널의 id</param>
    /// <returns></returns>
    public static bool AddChannel(ulong serverId, ulong channelId)
    {
        string q = "insert into channel(serverId, channelId) VALUES(?serverId, ?channelId)";
        try
        {
            using MySqlCommand cmd = new MySqlCommand(q, client);

            cmd.Parameters.Add("?serverId", MySqlDbType.UInt64).Value = serverId;
            cmd.Parameters.Add("?channelId", MySqlDbType.UInt64).Value = channelId;
            if (!(cmd.ExecuteNonQuery() > 0))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            return false;
        }

    }

    /// <summary>
    /// 랭슝좍을 감지하는 채널을 삭제합니다.
    /// 만약 삭제에 실패한다면 false를 반환합니다.
    /// 실행하기 전에 IsSetServer로 검사하세요.
    /// </summary>
    /// <param name="serverId">서버의 id</param>
    /// <param name="channelId">채널의 id</param>
    /// <returns></returns>
    public static bool RemoveChannel(ulong serverId)
    {
        string q = "delete from channel where serverId = ?serverId";
        try
        {
            using MySqlCommand cmd = new MySqlCommand(q, client);

            cmd.Parameters.Add("?serverId", MySqlDbType.UInt64).Value = serverId;
            if (!(cmd.ExecuteNonQuery() > 0))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="msg"></param>
    public static void Log(Guid guid, string msg)
    {
        string q = "insert into log(message, data, time, guid) VALUES(?message, ?data, Now(), ?guid)";
        try
        {
            using (MySqlCommand cmd = new MySqlCommand(q, client))
            {
                cmd.Parameters.Add("?message", MySqlDbType.VarString).Value = msg;
                cmd.Parameters.Add("?data", MySqlDbType.Date).Value = DateTime.Now.Date;
                //cmd.Parameters.Add("?time", MySqlDbType.Timestamp).Value = DateTime.Now;
                cmd.Parameters.Add("?guid", MySqlDbType.Guid).Value = guid;
                if (!(cmd.ExecuteNonQuery() > 0))
                {
                    Console.WriteLine("Log가 찍히지 않음");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        Console.WriteLine(msg);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="code"></param>
    public static bool Report(string msg, string code)
    {
        string q = "insert into report(message, code) VALUES(?message, ?code)";
        try
        {
            using (MySqlCommand cmd = new MySqlCommand(q, client))
            {
                cmd.Parameters.Add("?message", MySqlDbType.VarString).Value = msg;
                cmd.Parameters.Add("?code", MySqlDbType.VarString).Value = code;
                if (!(cmd.ExecuteNonQuery() > 0))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }
}