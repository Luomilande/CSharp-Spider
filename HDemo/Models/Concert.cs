using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HDemo.Models
{
    
    public class Concert
    {
        /// <summary>
        /// 分类名称
        /// </summary>
        public string Categoryname { get; set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        public string AttrName { get; set; }

        /// <summary>
        /// 举办城市
        /// </summary>
        public string Holding_City { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public string Start_Time { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public string End_Time { get; set; }


    }
}