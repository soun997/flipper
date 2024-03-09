using System;
using System.Data;
using Mono.Data.SqliteClient;
using System.IO;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DBManager
{
    // DB 생성
    public static void DBCreate()
    {
        string filePath = string.Empty; // 파일경로
        filePath = Application.dataPath + "/database.db";
        if (!File.Exists(filePath))
        {
            File.Copy(Application.streamingAssetsPath + "/database.db", filePath);
        }

        Debug.Log("DB 생성 완료");
    }

    // DB파일 경로 획득
    public static string GetDBFilePath()
    {
        string str = string.Empty;
        str = "URI=file:" + Application.dataPath + "/database.db";

        return str;
    }

    // DB 연결 체크
    public static void DBConnectionCheck()
    {
        try
        {
            IDbConnection dbConnection = new SqliteConnection(GetDBFilePath());
            dbConnection.Open();

            if (dbConnection.State == ConnectionState.Open)
            {
                Debug.Log("DB 연결 성공");
            }
            else
            {
                Debug.LogError("DB 연결 실패(에러)");
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Error: {e}");
        }
    }

    // 쿼리문을 통해 데이터 읽어오기
    public static List<string> DataBaseRead(string query)
    {
        IDbConnection dbConnection = new SqliteConnection(GetDBFilePath());
        dbConnection.Open();
        IDbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = query;  // 쿼리 입력
        IDataReader dataReader = dbCommand.ExecuteReader(); // 쿼리 실행
        List<String> data = new List<String>();
        // 레코드 읽기
        while (dataReader.Read())
        {
            data.Add(dataReader.GetString(0));
            data.Add(dataReader.GetString(1));
            data.Add(dataReader.GetString(2));
            Debug.Log(dataReader.GetString(0) + ", " + dataReader.GetString(1) + ", " + dataReader.GetString(2));
        }
        dataReader.Dispose();
        dataReader = null;
        dbCommand.Dispose();
        dbCommand = null;
        dbConnection.Close();
        dbConnection = null;

        return data;
    }

    public static void DataBaseChange(string query)
    {
        IDbConnection dbConnection = new SqliteConnection(GetDBFilePath());
        dbConnection.Open();
        IDbCommand dbCommand = dbConnection.CreateCommand();

        dbCommand.CommandText = query;
        dbCommand.ExecuteNonQuery();

        dbCommand.Dispose();
        dbCommand = null;
        dbConnection.Close();
        dbConnection = null;
    }
}
