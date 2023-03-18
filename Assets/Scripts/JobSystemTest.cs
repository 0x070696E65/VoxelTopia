using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class JobSystemTest : MonoBehaviour
{
    private struct MyParallelJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> a;
        [ReadOnly] public NativeArray<float> b;

        public NativeArray<float> result;
        
        public void Execute(int index)
        {
            result[index] = a[index] + b[index];
        }
    }
    
    private void Update()
    {
        var count = 2;
        var a = new NativeArray<float>(count, Allocator.TempJob);
        var b = new NativeArray<float>(count, Allocator.TempJob);
        var result = new NativeArray<float>(count, Allocator.TempJob);
        
        a[0] = 1.1f;
        b[0] = 2.2f;
        a[1] = 3.3f;
        b[1] = 4.4f;

        // Job生成&データ設定
        var job = new MyParallelJob
        {
            a = a,
            b = b,
            result = result
        };

        // Jobを並列実行するようにスケジュール
        // 第一引数 arrayLength : The number of iterations the for loop will execute.
        // 第二引数 innerloopBatchCount : Granularity in which workstealing is performed. A value of 32, means the job queue will steal 32 iterations and then perform them in an efficient inner loop.
        var handle = job.Schedule(result.Length, 1);
        
        // ジョブを開始する(メインスレッドで処理待ちだけする場合はこれを書かずにCompleteでよい)
        JobHandle.ScheduleBatchedJobs();
        
        handle.Complete();

        Debug.Log($"{result[0]} , {result[1]}");
        
        a.Dispose();
        b.Dispose();
        result.Dispose();
    }
}