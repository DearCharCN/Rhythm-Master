using System;
using System.Collections.Generic;
using UnityEngine;

namespace F8Framework.Core
{
    [UpdateRefresh]
    public class TimerManager : ModuleSingleton<TimerManager>, IModule
    {
        private List<Timer> times = new List<Timer>(); // 存储计时器的列表
        private long initTime; // 初始化时间
        private long serverTime; // 服务器时间
        private long tempTime; // 临时时间
        private bool isFocus = true; // 是否处于焦点状态
        private int frameTime = 1; // 帧时间，默认为1

        public void OnInit(object createParam)
        {
            initTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            serverTime = 0;
            tempTime = 0;
        }

        public void OnLateUpdate()
        {

        }

        public void OnFixedUpdate()
        {

        }

        public void OnTermination()
        {
            MessageManager.Instance.RemoveEventListener(MessageEvent.ApplicationFocus, OnApplicationFocus, this);
            MessageManager.Instance.RemoveEventListener(MessageEvent.NotApplicationFocus, NotApplicationFocus, this);
            times.Clear();
            base.Destroy();
        }

        public void OnUpdate()
        {
            if (isFocus == false || times.Count <= 0) // 如果失去焦点或者计时器数量为0，则返回
            {
                return;
            }
            float dt = Time.deltaTime;

            for (int i = 0; i < times.Count; i++)
            {
                Timer timer = times[i];

                if (timer.IsFinish)
                {
                    times.RemoveAt(i);
                    i--;
                    continue;
                }

                // 调用计时器
                int triggerCount = timer.IsFrameTimer ? timer.Update(frameTime) : timer.Update(dt);

                if (triggerCount > 0) // 如果本帧触发次数大于0，执行相关逻辑
                {
                    if (timer.IsFinish || timer.Handle == null || timer.Handle.Equals(null))
                    {
                        timer.IsFinish = true;
                        continue;
                    }

                    int field = timer.Field; // 获取计时器剩余字段值

                    for (int j = 0; j < triggerCount; j++)
                    {
                        field = field > 0 ? field - 1 : field; // 每次减少计时器字段值

                        if (field == 0) // 若字段值为0，触发onSecond事件，并执行OnTimerComplete
                        {
                            timer.Field = field; // 更新计时器剩余字段值
                            timer.OnSecond?.Invoke();
                            OnTimerComplete(timer);
                            break;
                        }
                        else
                        {
                            timer.Field = field; // 更新计时器剩余字段值
                            timer.OnSecond?.Invoke();
                        }
                    }
                }
            }
        }

        private void OnTimerComplete(Timer timer)
        {
            timer.IsFinish = true;
            if (timer.OnComplete is { } onComplete) // 若OnComplete事件存在，触发事件
            {
                onComplete.Invoke();
            }
        }

        // 注册一个计时器并返回其ID
        public int AddTimer(object handle, float step = 1f, float delay = 0f, int field = 0, Action onSecond = null, Action onComplete = null)
        {
            int id = Guid.NewGuid().GetHashCode(); // 生成一个唯一的ID
            Timer timer = new Timer(handle, id, step, delay, field, onSecond, onComplete, false); // 创建一个计时器对象
            times.Add(timer);
            return id;
        }

        // 注册一个以帧为单位的计时器并返回其ID
        public int AddTimerFrame(object handle, float stepFrame = 1f, float delayFrame = 0f, int field = 0, Action onFrame = null, Action onComplete = null)
        {
            int id = Guid.NewGuid().GetHashCode(); // 生成一个唯一的ID
            Timer timer = new Timer(handle, id, stepFrame, delayFrame, field, onFrame, onComplete, true); // 创建一个以帧为单位的计时器对象
            times.Add(timer);
            return id;
        }

        // 根据ID注销计时器
        public void RemoveTimer(int id)
        {
            for (int i = 0; i < times.Count; i++)
            {
                if (times[i].ID == id)
                {
                    times[i].IsFinish = true;
                    break;
                }
            }
        }

        // 设置服务器时间
        public void SetServerTime(long val)
        {
            if (val != 0) // 如果传入的值不为0，则更新服务器时间和临时时间
            {
                serverTime = val;
                tempTime = GetTime();
            }
        }

        // 获取服务器时间
        public long GetServerTime()
        {
            return (serverTime + (GetTime() - tempTime)); // 返回服务器时间加上当前时间与临时时间之间的差值
        }

        // 获取游戏中的总时长
        public long GetTime()
        {
            //可改为Unity启动的总时长
            // float floatValue = Time.time;
            // long longValue = (long)(floatValue * 1000000);
            // return longValue;
            return GetLocalTime() - initTime; // 返回当前时间与初始化时间的差值
        }

        // 获取本地时间
        public long GetLocalTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 返回当前时间的毫秒数
        }

        // 暂停所有计时器
        public void Pause()
        {
            for (int i = 0; i < times.Count; i++)
            {
                times[i].StartTime = GetTime();
            }
        }

        public void AddListenerApplicationFocus()
        {
            MessageManager.Instance.AddEventListener(MessageEvent.ApplicationFocus, OnApplicationFocus, this);
            MessageManager.Instance.AddEventListener(MessageEvent.NotApplicationFocus, NotApplicationFocus, this);
        }

        // 当应用程序获得焦点时调用
        void OnApplicationFocus()
        {
            Restart();
            isFocus = true;
        }

        // 当应用程序失去焦点时调用
        void NotApplicationFocus()
        {
            isFocus = false;
            Pause();
        }

        // 重新启动所有计时器
        public void Restart()
        {
            long currentTime = GetTime();
            for (int i = 0; i < times.Count; i++)
            {
                Timer timer = times[i];
                if (timer.StartTime != 0) // 如果计时器的开始时间不为0
                {
                    long startTime = timer.StartTime; // 获取计时器的开始时间
                    int interval = (int)((currentTime - startTime) / 1000); // 计算时间间隔（秒数）
                    int field = timer.Field; // 获取计时器字段值
                    timer.StartTime = 0; // 重置计时器的开始时间为0

                    if (field < 0)
                    {
                        // 处理循环计时器（若有需要）
                    }
                    else
                    {
                        field -= interval; // 减去时间间隔
                        if (field < 0) // 如果字段值小于0，将其置为0，并执行OnTimerComplete
                        {
                            field = 0;
                            timer.Field = field; // 更新计时器字段值
                            OnTimerComplete(timer);
                        }
                        else
                        {
                            timer.Field = field; // 更新计时器字段值
                        }
                    }
                }
            }
        }
    }
}