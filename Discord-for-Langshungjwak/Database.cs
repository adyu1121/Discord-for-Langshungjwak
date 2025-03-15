using MySql.Data.MySqlClient;
using System;

namespace Lang_shung_jwak;

public static class Database
{
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


    public static bool IsSetServer(ulong serverId)
    {
        try
        {
            Log(Guid.Empty, $"[데이터베이스] {serverId} IsSetServer 시작");

            string q =

                "SELECT EXISTS" +
                "(" +
                "    SELECT * FROM channel WHERE serverId=?serverId" +
                ") AS t";

            using MySqlCommand cmd = new MySqlCommand(q, client);
            cmd.Parameters.Add("?serverId", MySqlDbType.UInt64).Value = serverId;

            using MySqlDataReader reader = cmd.ExecuteReader();

            reader.Read();
            int t = (int)reader["t"];
            Log(Guid.Empty, $"[데이터베이스] {serverId} IsSetServer 성공");
            return t == 1;
        }
        catch (Exception ex)
        {
            Log(Guid.Empty, $"[데이터베이스] {serverId} IsSetServer 오류 {ex.Message}");
            return false;
        }
    }


    public static ulong GetChannel(ulong serverId)
    {
        string q = "select channelId from channel where serverId=?serverId";
        try
        {
            Log(Guid.Empty, $"[데이터베이스] {serverId} GetChannel 시작");

            using MySqlCommand cmd = new MySqlCommand(q, client);

            cmd.Parameters.Add("?serverId", MySqlDbType.UInt64).Value = serverId;

            using MySqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            ulong id = (ulong)reader["channelId"];
            Log(Guid.Empty, $"[데이터베이스] {serverId} GetChannel 성공");
            return id;

        }
        catch (Exception ex)
        {
            Log(Guid.Empty, $"[데이터베이스] {serverId} GetChannel 오류 {ex.Message}");
            return 0;
        }
    }


    public static bool AddChannel(ulong serverId, ulong channelId)
    {
        string q = "insert into channel(serverId, channelId) VALUES(?serverId, ?channelId)";
        try    
        {
            Log(Guid.Empty, $"[데이터베이스] {serverId} AddChannel 시작");

            using MySqlCommand cmd = new MySqlCommand(q, client);

            cmd.Parameters.Add("?serverId", MySqlDbType.UInt64).Value = serverId;
            cmd.Parameters.Add("?channelId", MySqlDbType.UInt64).Value = channelId;
            if (!(cmd.ExecuteNonQuery() > 0))
            {
                Log(Guid.Empty, $"[데이터베이스] {serverId} AddChannel 실행 못함");
                return false;
            }
            else
            {
                Log(Guid.Empty, $"[데이터베이스] {serverId} AddChannel 성공");
                return true;
            }
        }
        catch (Exception ex)
        {
            Log(Guid.Empty, $"[데이터베이스] {serverId} AddChannel 오류 {ex.Message}");
            return false;
        }

    }
    public static bool RemoveChannel(ulong serverId)
    {
        string q = "delete from channel where serverId = ?serverId";
        try
        {
            Log(Guid.Empty, $"[데이터베이스] {serverId} RemoveChannel 시작");

            using MySqlCommand cmd = new MySqlCommand(q, client);

            cmd.Parameters.Add("?serverId", MySqlDbType.UInt64).Value = serverId;
            if (!(cmd.ExecuteNonQuery() > 0))
            {
                Log(Guid.Empty, $"[데이터베이스] {serverId} RemoveChannel 실행 못함");
                return false;
            }
            else
            {
                Log(Guid.Empty, $"[데이터베이스] {serverId} RemoveChannel 성공");
                return true;
            }
        }
        catch (Exception ex)
        {
            Log(Guid.Empty, $"[데이터베이스] {serverId} RemoveChannel 오류 {ex.Message}");
            return false;
        }
    }


    public static void Log(Guid guid, string msg)
    {
        string q = "insert into log(message, time, guid) VALUES(?message, Now(), ?guid)";
        try
        {
            using (MySqlCommand cmd = new MySqlCommand(q, client))
            {
                cmd.Parameters.Add("?message", MySqlDbType.VarString).Value = msg;
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


    public static bool Report(string msg, string code)
    {
        string q = "insert into report(message, code) VALUES(?message, ?code)";
        try
        {
            Log(Guid.Empty, $"[데이터베이스] Report 시작");
            using (MySqlCommand cmd = new MySqlCommand(q, client))
            {
                cmd.Parameters.Add("?message", MySqlDbType.VarString).Value = msg;
                cmd.Parameters.Add("?code", MySqlDbType.VarString).Value = code;
                if (!(cmd.ExecuteNonQuery() > 0))
                {
                    Log(Guid.Empty, $"[데이터베이스] Report 실행 못함");
                    return false;
                }
                else
                {
                    Log(Guid.Empty, $"[데이터베이스] Report 성공");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log(Guid.Empty, $"[데이터베이스] Report 오류 {ex.Message}");
            return false;
        }
    }
}