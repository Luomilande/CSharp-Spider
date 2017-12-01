using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using HDemo.Models;
using System.Text;
using HtmlAgilityPack;
using JiebaNet.Segmenter;
using Newtonsoft.Json;
using Kendo.Mvc;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Text.RegularExpressions;

namespace HDemo.Controllers
{
    public class HomeController : Controller
    {
        private HDemoEntities db = new HDemoEntities();

        /// <summary>
        /// 网站抓取规则集合
        /// </summary>
        private static IEnumerable<WebRule> webRule_list;

        /// <summary>
        /// 等待抓取的链接集合
        /// </summary>
        private Queue<string> WaitCatchLink;

        /// <summary>
        /// 已经抓取的链接集合
        /// </summary>
        private List<string> CatchedLink;

        /// <summary>
        /// 网页开始抓取的入口
        /// </summary>
        /// <returns></returns>
        public ActionResult StartCatch()
        {
            //实例化StreamReader
            var readerInput = new StreamReader(Request.InputStream);
            var javaser = new JavaScriptSerializer();
            string s = readerInput.ReadToEnd();
            var data = javaser.Deserialize<webUrl>(s);

            //把url网址转换成UTF-8编码格式
            string url = HttpUtility.UrlDecode(data.url, Encoding.UTF8);
            foreach (var item in webRule_list)
            {
                //判断是否包含在数据库规则中
                if (url.StartsWith(item.UrlWeb))
                {
                    if (item.Response == "json")
                    {
                        string datastr = "";
                        string method = "POST";
                        CatchHelper cathelp = new CatchHelper();
                        //判断是否是国家预警信息网
                        if (item.WebName == "alarm")
                        {
                            JObject jobect = Static.GetJson(item.RequestUrl, "GET", null);
                            if (jobect == null) return null;

                            string path = Server.MapPath(System.Web.HttpContext.Current.Request.ApplicationPath.ToString());
                            FileStream file = new FileStream(path + @"Content\ThesaurusURL\city.txt", FileMode.Open, FileAccess.Read);
                            StreamReader cityreader = new StreamReader(file, Encoding.GetEncoding("GBK"));
                            string citystr = cityreader.ReadToEnd().Replace("\r", " ").Replace("\n", " ");
                            List<string> citylist = citystr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                            List<BasicAttr> listbasic = new List<BasicAttr>();
                            foreach (var alarmitem in jobect["alertData"])
                            {
                                BasicAttr basic = new BasicAttr();
                                basic.AttrName = alarmitem["headline"].ToString();
                                basic.Start_Time = basic.End_Time = alarmitem["sendTime"].ToString();

                                foreach (var city in citylist)
                                {
                                    if (basic.AttrName.Contains(city))
                                    {
                                        basic.Holding_City = city;
                                        if (city.Contains("市") || city.Contains("州") || city.Contains("区") || city.Contains("县")) basic.Is_Influence_City = true;
                                        else basic.Is_Influence_Province = true;
                                        break;
                                    }
                                }
                                listbasic.Add(basic);
                            }
                            db.BasicAttr.AddRange(listbasic);
                            db.SaveChanges();
                            return Json(new { ExecuteResult = listbasic.Count, Success = "fail" });
                        }

                        if (item.WebName == "damai")
                        {
                            if (url.Contains("演唱会")) datastr = "ctl=演唱会&currPage=";
                            if (url.Contains("体育比赛")) datastr = "ctl=体育比赛&currPage=";
                            for (int i = 1; i < 100; i++)
                            {
                                //获取请求链接返回的json对象
                                JObject jobject = Static.GetJson(item.RequestUrl, method, datastr + i);
                                List<Concert> listconvert = new List<Concert>();
                                //把jobject对象数据整理到数据库
                                if (jobject != null) listconvert = cathelp.FConcertJsonToList(jobject, item.WebName);
                                else return Json(new { ExecuteResult = "基础连接已经关闭", Success = "fail" });

                                //listconvert为空代表数据已经抓完
                                if (listconvert == null) return Json(new { ExecuteResult = "success", Success = "success" });
                            }
                        }

                        if (item.WebName == "yongle")
                        {
                            datastr = "j=1&p="; method = "GET";
                            item.RequestUrl = url;
                            for (int i = 1; i < 100; i++)
                            {
                                //获取请求链接返回的json对象
                                JObject jobject = Static.GetJson(item.RequestUrl, method, datastr + i);
                                List<Concert> listconvert = new List<Concert>();
                                //把jobject对象数据整理到数据库
                                if (jobject != null) listconvert = cathelp.FConcertJsonToList(jobject, item.WebName);
                                else return Json(new { ExecuteResult = "基础连接已经关闭", Success = "fail" });

                                //listconvert为空代表数据已经抓完
                                if (listconvert == null) return Json(new { ExecuteResult = "success", Success = "success" });
                            }
                        }

                    }
                    if (item.Response == "html")
                    {
                        //判断是否是E展会
                        if (item.WebName == "E")
                        {
                            //实例化对象
                            WaitCatchLink = new Queue<string>();
                            CatchedLink = new List<string>();

                            //获取编码
                            string encoding = Static.GetEncoding(url);
                            //如果获取源码失败则默认使用UTF-8编码
                            if (encoding == null) encoding = "UTF-8";

                            //获取网页源码
                            string html = Static.GetHtml(item.RequestUrl, Encoding.GetEncoding(encoding), "POST", "serarchwhere={1:1}&SearType=1&page=1");
                            if (html == null) return Json(new { ExecuteResult = "fail", success = "fail" });

                            //获取该抓取的网页页数
                            int page = Static.GetPageCount(html, item.PageXpath);
                            for (int i = 1; i < page; i++)
                            {
                                //获取网页源码
                                html = Static.GetHtml(item.RequestUrl, Encoding.GetEncoding(encoding), "POST", "serarchwhere={1:1}&SearType=1&page=" + i);
                                HtmlDocument doc = new HtmlDocument();
                                doc.LoadHtml(html);

                                //获取指定Xpath路径的所有链接
                                HtmlNodeCollection nodecollection = doc.DocumentNode.SelectSingleNode(item.WebContentXpath).SelectNodes(".//a[@href]");
                                if (nodecollection == null) continue;

                                foreach (HtmlNode nodelink in nodecollection)
                                {
                                    //获取集合中的一条链接
                                    string newlink = nodelink.Attributes["href"].Value;
                                    //判断链接的有效性
                                    if (newlink == "" || newlink == "#" || newlink == "javascript") continue;
                                    //补全子链接
                                    if (newlink.StartsWith("/") && !newlink.StartsWith("//")) newlink = item.UrlWeb.TrimEnd('/') + newlink;
                                    //判断该链接是否已经抓取过或者已经在等待抓取队列中
                                    if (newlink.StartsWith(item.UrlWeb) && !WaitCatchLink.Contains(newlink) && !CatchedLink.Contains(newlink)) WaitCatchLink.Enqueue(newlink);
                                }
                                //实例化基础属性类
                                List<BasicAttr> listbasic = new List<BasicAttr>();

                                while (WaitCatchLink.Count > 0)
                                {
                                    //取出等待抓取链接队列中的第一条链接并移除
                                    string catchlink = WaitCatchLink.Dequeue();

                                    //把次链接添加到已经抓取的集合中
                                    CatchedLink.Add(catchlink);

                                    //获取此链接的源码
                                    string catchhtml = Static.GetHtml(catchlink, Encoding.GetEncoding(encoding), null, null);

                                    //如果源码没有抓取成功则跳过此链接
                                    if (catchhtml == null) continue;
                                    doc = new HtmlDocument();
                                    doc.LoadHtml(catchhtml);

                                    //根据给定的Xpath路径获取内容节点
                                    HtmlNode contentnode = doc.DocumentNode.SelectSingleNode(item.AttrContentXpath);
                                    if (contentnode == null) continue;

                                    AnalysisHelper analy = new AnalysisHelper();
                                    string serverpath = Server.MapPath(System.Web.HttpContext.Current.Request.ApplicationPath.ToString());
                                    BasicAttr ba = analy.Analysis(contentnode.InnerText, serverpath);
                                    listbasic.Add(ba);
                                }
                                //把处理好的事件属性添加到数据库中
                                db.BasicAttr.AddRange(listbasic);
                                //保存数据
                                db.SaveChanges();
                            }
                            return Json(new { ExecuteResult = CatchedLink.Count, Success = "success" });
                        }
                        //判读是否是中国会展门户
                        if (item.WebName == "cnena")
                        {
                            DateTime dt = DateTime.Now;
                            //实例化对象
                            WaitCatchLink = new Queue<string>();
                            CatchedLink = new List<string>();
                            string encoding = Static.GetEncoding(url);

                            //如果获取源码失败则默认使用UTF-8编码
                            if (encoding == null) encoding = "UTF-8";

                            //默认抓取未来七天内开始的展会
                            DateTime dtseven = dt.AddDays(7);

                            //用于判断该链接的内容是否在指定的抓取的时间段内
                            bool Isoverdue = false;
                            int[] Monthtime;
                            //判断是否应该抓取下一个月的数据
                            if (dtseven.Month > dt.Month) Monthtime = new int[] { dt.Month, dtseven.Month };
                            else Monthtime = new int[] { dt.Month };
                            foreach (var month in Monthtime)
                            {
                                for (int i = 1; i < 100; i++)
                                {
                                    string requesturl = item.RequestUrl + "?daytime=" + month + "&page=" + i;
                                    string html = Static.GetHtml(requesturl, Encoding.GetEncoding(encoding), null, null);
                                    //判断是否成功获取网页源码
                                    if (html == null) continue;
                                    HtmlDocument doc = new HtmlDocument();
                                    doc.LoadHtml(html);
                                    //根据给定的Xpath路径获取内容节点
                                    HtmlNode hn = doc.DocumentNode.SelectSingleNode(item.WebContentXpath);
                                    //判断是否成功获取该节点
                                    if (hn == null) continue;
                                    foreach (var cnenatable in hn.SelectNodes("table"))
                                    {
                                        Match math = Regex.Match(cnenatable.InnerText, @"(\d{4}|\d{2})(\-|\/|\\)\d{1,2}(\-|\/|\\)\d{1,2}(\s?\d{2}:\d{2})?", RegexOptions.IgnoreCase);
                                        if (math.Success)
                                        {
                                            DateTime dd = Convert.ToDateTime(math.Groups[0].Value).Date;
                                            if (dd > dt && dd < dtseven)
                                            {
                                                foreach (var cnenalink in cnenatable.SelectNodes(".//td[2]"))
                                                {
                                                    string linktext = item.UrlWeb.TrimEnd('/') + "/" + cnenalink.SelectSingleNode(".//a[@href]").Attributes["href"].Value;
                                                    if (linktext == "") continue;
                                                    if (!WaitCatchLink.Contains(linktext) && !CatchedLink.Contains(linktext))
                                                    {
                                                        WaitCatchLink.Enqueue(linktext);
                                                        continue;
                                                    }
                                                }
                                            }
                                            if (dd < dt)
                                            {
                                                Isoverdue = true;
                                                break;
                                            }
                                        }
                                    }
                                    List<BasicAttr> listbasic = new List<BasicAttr>();
                                    while (WaitCatchLink.Count > 0)
                                    {
                                        //取出等待抓取链接队列中的第一条链接并移除
                                        string catchlink = WaitCatchLink.Dequeue();

                                        //把次链接添加到已经抓取的集合中
                                        CatchedLink.Add(catchlink);

                                        //获取此链接的源码
                                        string catchhtml = Static.GetHtml(catchlink, Encoding.GetEncoding(encoding), null, null);

                                        //如果源码没有抓取成功则跳过此链接
                                        if (catchhtml == null) continue;
                                        doc = new HtmlDocument();
                                        doc.LoadHtml(catchhtml);

                                        //根据给定的Xpath路径获取内容节点
                                        HtmlNode contentnode = doc.DocumentNode.SelectSingleNode(item.AttrContentXpath);
                                        if (contentnode == null) continue;

                                        AnalysisHelper analy = new AnalysisHelper();
                                        string serverpath = Server.MapPath(System.Web.HttpContext.Current.Request.ApplicationPath.ToString());
                                        BasicAttr ba = analy.Analysis(contentnode.InnerText, serverpath);
                                        listbasic.Add(ba);
                                    }
                                    if (Isoverdue) break;
                                }
                            }
                            return Json(new { ExecuteResult = CatchedLink.Count, Success = "success" });
                        }
                        //判读是否中国天气预警
                        if (item.WebName == "tianqialarm")
                        {
                            //实例化对象
                            WaitCatchLink = new Queue<string>();
                            CatchedLink = new List<string>();

                            //获取网页编码
                            string encoding = Static.GetEncoding(url);
                            //如果获取源码失败则默认使用UTF-8编码
                            if (encoding == null) encoding = "UTF-8";

                            //用于判断该链接的内容是否在指定的抓取的时间段内
                            bool Isoverdue = false;
                            for (int i = 1; i < 100; i++)
                            {
                                //补全请求链接
                                string requesturl = item.RequestUrl.TrimEnd('/') + @"/" + i + @"/";

                                //获取网页编码
                                string html = Static.GetHtml(requesturl, Encoding.GetEncoding(encoding), null, null);
                                //判断是否成功获取网页源码
                                if (html == null) continue;
                                HtmlDocument doc = new HtmlDocument();
                                doc.LoadHtml(html);

                                //根据给定的Xpath路径获取内容节点
                                HtmlNode hn = doc.DocumentNode.SelectSingleNode(item.WebContentXpath);
                                //判断是否成功获取该节点
                                if (hn == null) continue;

                                //存放网页所有的时间
                                List<DateTime> listtime = new List<DateTime>();

                                //匹配该网页中所有符合条件的时间存到集合match中
                                MatchCollection match = Regex.Matches(
                                    hn.InnerText,
                                    @"(\d{4}|\d{2})(\-|\/|\\)\d{1,2}(\-|\/|\\)\d{1,2}(\s?\d{2}:\d{2})?",
                                    RegexOptions.IgnoreCase);
                                if (match.Count > 0)
                                {
                                    string dateStr = "";
                                    for (int j = 0; j < match.Count; j++)
                                    {
                                        dateStr = match[j].Value;
                                        try { listtime.Add(Convert.ToDateTime(dateStr)); }
                                        catch { continue; }
                                    }
                                }
                                //获取该节点的链接存到集合hc中
                                HtmlNodeCollection hc = hn.SelectNodes(".//a[@href]");
                                for (int n = 0; n < hc.Count; n++)
                                {
                                    //获取第n个链接
                                    string tianqilink = hc[n].Attributes["href"].Value;
                                    //获取当前时间
                                    DateTime dt = DateTime.Now.Date;

                                    //判断该链接的日期是否是在当前日期段内
                                    if (listtime[n].Date >= dt)
                                    {
                                        if (!WaitCatchLink.Contains(tianqilink) && !CatchedLink.Contains(tianqilink)) WaitCatchLink.Enqueue(tianqilink);
                                        else continue;
                                    }
                                    else Isoverdue = true; //已经超过指定的时间段
                                }
                                List<BasicAttr> listbasic = new List<BasicAttr>();
                                while (WaitCatchLink.Count > 0)
                                {
                                    //取出等待抓取链接队列中的第一条链接并移除
                                    string catchlink = WaitCatchLink.Dequeue();

                                    //把次链接添加到已经抓取的集合中
                                    CatchedLink.Add(catchlink);

                                    //获取此链接的源码
                                    string catchhtml = Static.GetHtml(catchlink, Encoding.GetEncoding(encoding), null, null);

                                    //如果源码没有抓取成功则跳过此链接
                                    if (catchhtml == null) continue;
                                    doc = new HtmlDocument();
                                    doc.LoadHtml(catchhtml);

                                    //根据给定的Xpath路径获取内容节点
                                    HtmlNode contentnode = doc.DocumentNode.SelectSingleNode(item.AttrContentXpath);
                                    if (contentnode == null) continue;

                                    AnalysisHelper analy = new AnalysisHelper();
                                    string serverpath = Server.MapPath(System.Web.HttpContext.Current.Request.ApplicationPath.ToString());
                                    BasicAttr ba = analy.Analysis(contentnode.InnerText, serverpath);
                                    listbasic.Add(ba);
                                }
                                //判断是否已经超过指定的时间段
                                if (Isoverdue) break;
                            }
                            return Json(new { ExecuteResult = CatchedLink.Count, Success = "success" });
                        }

                    }
                }
            }
            return Json(new { ExecuteResult = "该链接未包含在规则表中", Success = "success" });
        }

        public ActionResult GetJson([DataSourceRequest] DataSourceRequest request)
        {
            var list = db.BasicAttr;
            return Json(list.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }
        public ActionResult Index()
        {
            init();
            return View();
        }

        private void init()
        {
            var data = db.WebRule;
            webRule_list = db.WebRule;
        }

    }
}