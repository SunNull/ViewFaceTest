using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Data;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ViewFaceTest.Attributes;
using FreeSql.DataAnnotations;

namespace ViewFaceTest.Models
{
    public class UserInfo
    {
        [IsReadonly(true)]
        [Description("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Description("姓名")]
        public string Name { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [Description("年龄")]
        public int Age { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        [Description("性别")]
        public GenderEnum Gender { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Description("备注")]
        public string Remark { get; set; }

        /// <summary>
        /// 电话
        /// </summary>
        [Description("电话")]
        public string Phone { get; set; }

        /// <summary>
        /// 图片（Base64）编码
        /// </summary>
        [IsHidden(true)]
        public string Image { get; set; }

        /// <summary>
        /// 人脸识别数据
        /// </summary>
        [IsHidden(true)]
        [Description("人脸识别数据")]
        public string Extract { get; set; }

        [IsIgnore]
        public bool IsDelete { get; set; }

        [IsReadonly(true)]
        [Description("创建时间")]
        public DateTime CreateTime { get; set; }

        [IsReadonly(true)]
        [Description("更新时间")]
        public DateTime? UpdateTime { get; set; }

        private float[] _extractData = null;

        [IsIgnore]
        public float[] ExtractData
        {
            get
            {
                if (_extractData != null)
                {
                    return _extractData;
                }
                if (string.IsNullOrWhiteSpace(Extract))
                {
                    return new float[0];
                }
                string[] dataStr = this.Extract.Split(';');
                float[] data = new float[dataStr.Length];
                for (int i = 0; i < dataStr.Length; i++)
                {
                    data[i] = float.Parse(dataStr[i]);
                }
                _extractData = data;
                return _extractData;
            }
        }
    }
}
