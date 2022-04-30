using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace DiscordBotOnLinux
{
    //Singleton 구현 추가하기
    public class RedisDBManager
    {
        private static RedisDBManager _instance;
        public static RedisDBManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RedisDBManager("localhost", 6379);
                }

                return _instance;
            }
        }

        private ConnectionMultiplexer redisConnection;
        private IDatabase database;

        private RedisDBManager() { }

        private RedisDBManager(string host, int port) 
        {
            for(int count = 0; count < 10; ++count)
            {
                if(Init(host, port))
                {
                    Console.WriteLine("RedisDB Connect Complete!");
                    break;
                }
                else
                {
                    Console.WriteLine("RedisDB Connection Failed! Retry Count : " + count);
                }
            }
        }

        public bool Init(string host, int port)
        {
            try
            {
                this.redisConnection = ConnectionMultiplexer.Connect(host + ":" + port);
                if(redisConnection.IsConnected)
                {
                    database = redisConnection.GetDatabase();
                    return true;
                }

                return false;
            }

            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public string GetData(string key)
        {
            return database.StringGet(key);
        }

        public bool SetData(string key, string value)
        {
            return database.StringSet(key, value);
        }

        public void DeleteData(string key)
        {
            database.KeyDelete(key);
        }
    }
}
