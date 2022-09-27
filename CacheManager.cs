using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewFaceCore.Core;
using ViewFaceTest.Models;

namespace ViewFaceTest
{
    internal class CacheManager
    {
        private IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        private const string key = "DefaultViewUserKey";

        public static CacheManager Instance = new CacheManager();

        private CacheManager()
        {

        }

        public List<UserInfo> Get()
        {
            object data = cache.Get(key);
            if (data != null && data is List<UserInfo>)
            {
                return data as List<UserInfo>;
            }
            return null;
        }

        public UserInfo Get(FaceRecognizer faceRecognizer, float[] data)
        {
            if (faceRecognizer == null || data == null)
            {
                return null;
            }
            List<UserInfo> users = Get();
            if (users == null || !users.Any())
            {
                return null;
            }
            foreach (var item in users)
            {
                float[] floats = item.ExtractData;
                if (item.ExtractData.Length > data.Length)
                {
                    floats = new float[data.Length];
                    Array.Copy(data, floats, data.Length);
                }
                if (faceRecognizer.IsSelf(data, floats))
                {
                    return item;
                }
            }
            return null;
        }

        public void Refesh()
        {
            using (DefaultDbContext db = new DefaultDbContext())
            {
                var allUsers = db.UserInfo.Where(p => !p.IsDelete).ToList();
                if (allUsers != null)
                {
                    cache.Set(key, allUsers, new DateTimeOffset(DateTime.Now.AddDays(3650)));
                }
            }
        }
    }
}
