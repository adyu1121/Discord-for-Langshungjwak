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