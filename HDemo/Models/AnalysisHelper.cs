using JiebaNet.Segmenter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace HDemo.Models
{
    public class AnalysisHelper
    {

        /// <summary>
        /// 分词
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private string Participles(string n)
        {
            var segmenter = new JiebaSegmenter();//分词
            var segments = segmenter.Cut(n, cutAll: true);
            return string.Join("\r\n", segments);

        }
        /// <summary>
        /// 读取词库
        /// </summary>
        /// <param name="TXTurl"></param>
        /// <returns></returns>
        private List<string> ReaderTXT(string TXTurl)
        {
            FileStream stream = new FileStream(TXTurl, FileMode.Open);
            StreamReader reader = new StreamReader(stream);
            string coed = reader.ReadToEnd();
            reader.Close();
            stream.Close();
            List<string> stringtxt = coed.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
            stringtxt = stringtxt.Where(p => !string.IsNullOrEmpty(p)).ToList();
            return stringtxt;
        }
        /// <summary>
        /// 库频（能同时添加两个词库）分析用到整体分析以及块型分析
        /// </summary>
        /// <param name="txtHTML">文字</param>
        /// <param name="txtURL1">词库1</param>
        /// <param name="txtURL2">词库2</param>
        /// <returns></returns>
        private int TypeSun(string txtHTML, string txtURL1, string txtURL2 = null)
        {
            string txtFenCi = Participles(txtHTML);//分词
            List<string> stringfctxt = txtFenCi.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();//将分词信息以\r\n分开

            int Sun = 0;
            List<string> stringtxt1 = new List<string>();//词库1
            List<string> stringtxt2 = new List<string>();//词库2
            if (txtURL2 == null)//判断是否只有一个词库
            {
                stringtxt1 = ReaderTXT(txtURL1);
                foreach (var itemA in stringfctxt)//遍历原文分词
                {
                    foreach (string item in stringtxt1)//遍历词库
                    {
                        if (itemA.Equals(item))//匹配是否相等
                        {
                            Sun++;
                        }
                    }
                }
                foreach (string item in stringtxt1)//整体匹配判断
                {
                    if (txtHTML.Contains(item))//匹配是否包含
                    {
                        Sun++;
                    }
                }
            }
            else//当有二个词库的时候
            {
                stringtxt1 = ReaderTXT(txtURL1);
                stringtxt2 = ReaderTXT(txtURL2);
                foreach (string itemA in stringfctxt)
                {
                    foreach (string item in stringtxt1)
                    {
                        if (itemA.Contains(item))
                        {
                            Sun++;
                        }
                        if (txtHTML.Contains(item))
                        {
                            Sun++;
                        }
                        if (Sun > 0)
                        {
                            foreach (string item2 in stringtxt2)
                            {
                                if (txtHTML.Contains(item2))
                                {
                                    Sun++;
                                }
                            }
                        }
                    }
                }

            }

            return Sun;
        }
        /// <summary>
        /// 输出最大值(数值4，5根据需求选择性写入)
        /// </summary>
        /// <param name="num1">数值1</param>
        /// <param name="num2">数值2</param>
        /// <param name="num3">数值3</param>
        /// <param name="num4">数值4</param>
        /// <param name="num5">数值5</param>
        /// <returns></returns>
        private int PutMax(int num1, int num2, int num3, int num4 = 0, int num5 = 0)
        {
            List<int> sum = new List<int>();
            if (num4 == 0)
            {
                int[] a = { num1, num2, num3 };
                sum.AddRange(a);
            }
            else if (num5 > 0)
            {
                int[] a = { num1, num2, num3, num4, num5 };
                sum.AddRange(a);
            }
            else
            {
                int[] a = { num1, num2, num3, num4 };
                sum.AddRange(a);
            }

            int num = sum[0];
            for (int i = 1; i < sum.Count(); i++)
            {
                int con = sum[i];
                if (num < con)
                {
                    num = con;
                }
            }
            return num;
        }
        private int a = 0, b = 0, c = 0, d = 0, e = 0, max = 0;
        /// <summary>
        /// 属性分析
        /// </summary>
        /// <param name="txthtml">txthtml为需要分析的文章</param>
        /// <returns></returns>
        public BasicAttr Analysis(string txthtml,string serverpath)
        {

            BasicAttr basic = new BasicAttr();
            
            string txturl9 = serverpath+@"Content\ThesaurusURL\gjzz.txt";//国际组织
            string txturl10 = serverpath + @"Content\ThesaurusURL\gjzf.txt";//国家政府
            string txturl11 = serverpath + @"Content\ThesaurusURL\szf.txt";//省政府
            string txturl12 = serverpath + @"Content\ThesaurusURL\dfzf.txt";//地方政府
            a = TypeSun(txthtml, txturl9);
            b = TypeSun(txthtml, txturl10);
            c = TypeSun(txthtml, txturl11);
            d = TypeSun(txthtml, txturl12);

            max = max = PutMax(a, b, c, d);
            if (a > 0 || b > 0 || c > 0 || d > 0)
            {
                if (max == a)
                {
                    //lblshuxing.Text = //lblshuxing.Text + "，国际组织";
                    basic.Is_International = true;
                }
                else if (max == b)
                {
                    //lblshuxing.Text = //lblshuxing.Text + "，国家政府";
                    basic.Is_c_Government = true;
                }
                else if (max == c)
                {
                    //lblshuxing.Text = //lblshuxing.Text + "，省政府";
                    basic.Is_p_Government = true;
                }
                else if (max == d)
                {
                    //lblshuxing.Text = //lblshuxing.Text + "，地方政府";
                    basic.Is_l_Government = true;
                }
            }

            a = b = c = d = e = 0;

            string hyxh = serverpath + @"Content\ThesaurusURL\hyxh.txt";//行业协会
            string mjxh = serverpath + @"Content\ThesaurusURL\mjxh.txt";//民间协会
            string gjhy = serverpath + @"Content\ThesaurusURL\gjhy.txt";//国际行业
            string gjmj = serverpath + @"Content\ThesaurusURL\gjmj.txt";//国际民间

            a = TypeSun(txthtml, hyxh);
            b = TypeSun(txthtml, mjxh);
            c = TypeSun(txthtml, gjhy);
            d = TypeSun(txthtml, gjmj);
            max = PutMax(a, b, c, d);
            if (a > 0 || b > 0 || c > 0 || d > 0)
            {
                if (max == a)
                {
                    // ",国内行业协会";
                    basic.Is_Hometrade_Association = true;
                }
                else if (max == b)
                {
                    // ",国内民间协会";
                    basic.Is_Homecivil_Association = true;
                }
                else if (max == c)
                {
                    // ",国际行业协会";
                    basic.Is_Intertrade_Association = true;
                }
                else if (max == d)
                {
                    // ",国际民间协会";
                    basic.Is_Intercivil_Association = true;
                }
            }

            a = b = c = d = e = 0;
            string txturl6 = serverpath + @"Content\ThesaurusURL\et.txt";//儿童
            string txturl7 = serverpath + @"Content\ThesaurusURL\qn.txt";//青年
            string txturl8 = serverpath + @"Content\ThesaurusURL\lr.txt";//老人
            a = TypeSun(txthtml, txturl6);
            b = TypeSun(txthtml, txturl7);
            c = TypeSun(txthtml, txturl8);

            max = PutMax(a, b, c);
            if (a > 0 || b > 0 || c > 0)
            {
                if (max == a)
                {
                    // "，儿童";
                    basic.Is_Influence_Children = true;
                }
                else if (max == b)
                {
                    // "，青年";
                    basic.Is_Influence_Young = true;
                }
                else if (max == c)
                {
                    // "，老人";
                    basic.Is_Influence_Old = true;
                }
            }
            else
            {
                // "，成年";
                basic.Is_Influence_Adult = true;
            }
            a = b = c = d = e = 0;
            string tyss = serverpath + @"Content\ThesaurusURL\tyss.txt";//体育赛事
            int sw = TypeSun(txthtml, tyss);
            if (sw > 3)
            {
                // ",固定人群";
                basic.Participant_Population = true;
            }
            a = b = c = d = e = 0;
            string txturl5 = serverpath + @"Content\ThesaurusURL\jj.txt";//经济人群
             sw = TypeSun(txthtml, txturl5);
            if (sw > 3)
            {
                // "，影响商务人群";
                basic.Is_Influence_Business = true;
            }
            else
            {
                // "，影响社会大众";
                basic.Is_Influence_Generalpublic = true;
            }
            a = b = c = d = e = 0;
            string txturl1 = serverpath + @"Content\ThesaurusURL\qq.txt";//全球
            string txturl2 = serverpath + @"Content\ThesaurusURL\zj.txt";//洲际
            string txturl3 = serverpath + @"Content\ThesaurusURL\qg.txt";//全国
            string txturl4 = serverpath + @"Content\ThesaurusURL\qs.txt";//全省

            a = TypeSun(txthtml, txturl1);
            b = TypeSun(txthtml, txturl2);
            c = TypeSun(txthtml, txturl3);
            d = TypeSun(txthtml, txturl4);
            max = PutMax(a, b, c, d);

            if (a > 0 || b > 0 || c > 0 || d > 0)
            {
                if (max == a)
                {
                    // ",全球";
                    basic.Is_Influence_Worldwide = true;
                }
                else if (max == b)
                {
                    // ",洲际";
                    basic.Is_Influence_Intercontinental = true;
                }
                else if (max == c)
                {
                    // ",全国";
                    basic.Is_Influence_Wholecountry = true;
                }
                else if (max == d)
                {
                    // ",全省";
                    basic.Is_Influence_Province = true;
                }
            }
            else
            {
                // ",全市";
                basic.Is_Influence_City = true;
            }

            a = b = c = d = e = 0;

            string ych = serverpath + @"Content\ThesaurusURL\ych.txt";//演唱会
            string hz = serverpath + @"Content\ThesaurusURL\hz.txt";//会展
            string hy = serverpath + @"Content\ThesaurusURL\hy.txt";//会议
            string dfxjjr = serverpath + @"Content\ThesaurusURL\dfxjjr.txt";//地方性节假日

            a = TypeSun(txthtml, tyss);
            b = TypeSun(txthtml, ych);
            c = TypeSun(txthtml, hz, tyss);
            d = TypeSun(txthtml, hy);
            e = TypeSun(txthtml, dfxjjr);
            max = PutMax(a, b, c, d, e);
            if (a > 0 || b > 0 || c > 0 || d > 0 || e > 0)
            {
                if (max == a)
                {
                    // ",体育赛事";
                    basic.Is_Sportevent = true;
                }
                else if (max == b)
                {
                    // ",演唱会";
                    basic.Is_Concert = true;
                }
                else if (max == c)
                {
                    // ",会展";
                    basic.Is_Exhibition = true;
                }
                else if (max == d)
                {
                    // ",会议";
                    basic.Is_Meeting = true;
                }
                else if (max == e)
                {
                    // ",地方性节假日";
                    basic.Is_Localholiday = true;
                }
            }

            ///三大玄学
            Random rd = new Random();
            basic.Event_Fever = rd.Next(1, 3);
            basic.Event_History = rd.Next(1, 3);
            basic.Freq = rd.Next(1, 3);
            return basic;
        }
    }
}