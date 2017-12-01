using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace HDemo
{
    public static class Static
    {
        /// <summary>
        /// 存储发生错误的链接
        /// </summary>
        public static List<string> Catchlist = new List<string>();
        /// <summary>
        /// 存放现在主流的网页域名后缀
        /// </summary>
        public static List<string> listDomainName = new List<string>()
        {
            ".com",
            ".cn",
            ".cx",
            ".com.cn",
            ".wang",
            ".cc",
            ".xin",
            ".net",
            ".top",
            ".tech",
            ".red",
            ".ink",
            ".xyz",
            ".win",
            ".org",
            ".gov",
            ".edu",
            ".gov.cn",
            ".org.cn",
            ".edu.cn"
        };
        /// <summary>
        /// 根据 url 获取网页编码
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetEncoding(string url)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamReader reader = null;
            string encoding = "";
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 10000;
                request.AllowAutoRedirect = false;
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.81 Safari/537.36";
                using (response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK && response.ContentLength < 1024 * 1024)
                    {
                        if (response.ContentEncoding.ToLower().Contains("gzip"))
                            reader = new StreamReader(new GZipStream(response.GetResponseStream(), CompressionMode.Decompress));
                        else
                            reader = new StreamReader(response.GetResponseStream(), Encoding.ASCII);
                        //string html = reader.ReadToEnd();
                        string content = "";
                        while ((content = reader.ReadLine()) != null)
                        {
                            if (content.Contains("charset"))
                            {
                                if (content.Contains("utf-8")) { encoding = "UTF-8"; break; }
                                if (content.Contains("gbk")) { encoding = "GBK"; break; }
                                if (content.Contains("gb2312")) { encoding = "GB2312"; break; }
                            }
                            if (content.Contains("/head"))
                            {
                                if (response.CharacterSet != string.Empty) encoding = response.CharacterSet;
                                break;
                            }
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {

                //Console.WriteLine(ex.Message);
                Catchlist.Add("链接:" + url + ex.Message);
                return null;
                //throw;
            }
            finally
            {
                if (request != null)
                    request = null;
            }

            return encoding;
        }
        /// <summary>
        /// 根据给定Xpath路径提取给定的网页的页数
        /// </summary>
        /// <param name="html"></param>
        /// <param name="RequestPageXpath"></param>
        /// <returns></returns>
        public static int GetPageCount(string html, string RequestPageXpath)
        {
            int count = 0;
            Type t = count.GetType();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection hc = doc.DocumentNode.SelectNodes(RequestPageXpath);
            foreach (var item in hc)
            {
                string pagehtml = Regex.Replace(item.InnerHtml, @"<.*?>", "-");
                pagehtml = clearTrans(pagehtml);
                pagehtml = Regex.Replace(pagehtml, @"\s", "-");
                string[] str = pagehtml.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < str.Length; i++)
                {
                    string pagestr = str[i];
                    if (pagestr == "下一页")
                    {
                        try { count = int.Parse(str[i - 1]); }
                        catch { count = int.Parse(str[i - 2]); }
                        break;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// 根据 url 和 encoding 获取当前url页面的 html 源代码        
        /// </summary>
        /// <param name="url"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string GetHtml(string url, Encoding encoding, string method, string dataStr)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 10000;
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.81 Safari/537.36";
                request.AllowAutoRedirect = false;
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                if (dataStr != null)
                {
                    request.Method = method;
                    byte[] data = Encoding.UTF8.GetBytes(dataStr);
                    request.ContentLength = data.Length;
                    Stream requeststream = request.GetRequestStream();
                    requeststream.Write(data, 0, data.Length);
                    requeststream.Close();
                }

                using (response = (HttpWebResponse)request.GetResponse())
                {
                    StreamReader reader = null;
                    if (response.StatusCode == HttpStatusCode.OK && response.ContentLength < 1024 * 1024)
                    {
                        if (response.ContentEncoding.ToLower().Contains("gzip"))
                            reader = new StreamReader(new GZipStream(response.GetResponseStream(), CompressionMode.Decompress), encoding);
                        else
                            reader = new StreamReader(response.GetResponseStream(), encoding);
                        string html = reader.ReadToEnd();
                        reader.Close();
                        return clearHtml(html);
                    }
                }
            }
            catch (Exception ex)
            {

                //Catchlist.Add("链接:" + url + ex.Message);
                Console.WriteLine(ex.Message);
                return null;
                //throw;
                //return string.Empty;
            }
            finally
            {
                if (request != null)
                    request = null;
            }
            return null;
        }
        /// <summary>
        /// 去除网页注释、样式表和JavaScript
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string clearHtml(string html)
        {
            html = Regex.Replace(html, @"(?is)<!--.*?-->", "");
            html = Regex.Replace(html, @"(?im)<style+?[\s\S]*?>[\s\S]*?</style>", "");
            html = Regex.Replace(html, @"(?im)<script+?[\s\S]*?>[\s\S]*?</script>", "");
            return html;
        }
        /// <summary>
        /// 去除html的转义字符串
        /// </summary>
        /// <param name="Htmlstring">需要去除转义字符的字符串</param>
        /// <returns></returns>
        public static string clearTrans(string Htmlstring)
        {
            Htmlstring = Regex.Replace(Htmlstring, @"&(quot|#34);", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(amp|#38);", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(lt|#60);", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(gt|#62);", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(nbsp|#160);", "", RegexOptions.IgnoreCase);
            return Htmlstring;
        }

        /// <summary>
        /// 获取目标网址响应的json数据
        /// </summary>
        /// <param name="requesturl">请求发送的网址</param>
        /// <param name="method">数据发送的类型</param>
        /// <param name="dataStr">拼接好的发送数据字符串</param>
        /// <returns></returns>
        public static JObject GetJson(string requesturl, string method, string dataStr)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            JObject jobject = new JObject();
            try
            {
                if (method == "POST")
                {
                    request = (HttpWebRequest)WebRequest.Create(requesturl);
                    request.Method = method;
                    request.Timeout = 10000;
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.81 Safari/537.36";
                    request.AllowAutoRedirect = false;
                    request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                    byte[] data = Encoding.UTF8.GetBytes(dataStr);
                    request.ContentLength = data.Length;
                    Stream requeststream = request.GetRequestStream();
                    requeststream.Write(data, 0, data.Length);
                    requeststream.Close();
                }
                else if (method == "GET")
                {
                    requesturl = requesturl + (dataStr == null ? null : ("?" + dataStr));
                    request = (HttpWebRequest)WebRequest.Create(requesturl);
                    request.Method = method;
                    request.Timeout = 10000;
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.81 Safari/537.36";
                    request.AllowAutoRedirect = false;
                    request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                }

                using (response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK && response.ContentLength < 1024 * 1024)
                    {
                        StreamReader reader = null;
                        if (response.ContentEncoding != null && response.ContentEncoding.Equals("gzip", StringComparison.InvariantCultureIgnoreCase))
                            reader = new StreamReader(new GZipStream(response.GetResponseStream(), CompressionMode.Decompress), Encoding.UTF8);
                        else
                            reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                        string json = reader.ReadToEnd();
                        jobject = (JObject)JsonConvert.DeserializeObject(json);
                        return jobject;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
                //throw;
            }
            finally
            {
                if (request != null)
                    request = null;
            }
            return null;
        }


    }
}
