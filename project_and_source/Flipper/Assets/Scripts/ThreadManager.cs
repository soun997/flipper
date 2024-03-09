using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 멀티쓰레드 구현
/// </summary>
public class ThreadManager : MonoBehaviour
{
    private static readonly List<Action> executeOnMainThread = new List<Action>();
    private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
    private static bool actionToExecuteOnMainThread = false;

    private void Update()
    {
        UpdateMain();
    }

    /// <summary>main thread에서 실행되어야 할 함수 추가</summary>
    /// <param name="_action">The action to be executed on the main thread.</param>
    public static void ExecuteOnMainThread(Action _action)
    {
        if (_action == null)
        {
            Debug.Log("No action to execute on main thread!");
            return;
        }

        // lock -> 동기화 목적
        lock (executeOnMainThread)
        {
            executeOnMainThread.Add(_action);   // 실행할 함수 추가
            actionToExecuteOnMainThread = true;
        }
    }

    /// <summary>main thread에서 실행되어야 할 코드를 실행</summary>
    public static void UpdateMain()
    {
        // 실행할 함수가 있다면
        if (actionToExecuteOnMainThread)
        {
            executeCopiedOnMainThread.Clear();  // 초기화
            // lock -> 동기화 목적
            lock (executeOnMainThread)
            {
                executeCopiedOnMainThread.AddRange(executeOnMainThread);    // 복사
                executeOnMainThread.Clear();    // 기존의 리스트는 초기화
                actionToExecuteOnMainThread = false;
            }
            // executOnMainThread lock 해제, 다른 실행함수를 받아올 수 있음

            // 복사된 리스트에 있는 함수들 실행
            for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
            {
                executeCopiedOnMainThread[i]();
            }
        }
    }
}