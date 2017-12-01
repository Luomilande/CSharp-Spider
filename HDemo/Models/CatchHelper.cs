using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace HDemo.Models
{
    public  class CatchHelper
    {
        public  HDemoEntities db=new HDemoEntities();
        public  List<Concert> FConcertJsonToList(JObject jobject,string webName)
        {
            List<Concert> listdata = new List<Concert>();
            
            if (webName == "damai")
            {
                foreach (var damai in jobject["pageData"]["resultData"])
                {
                    Concert dataitem = new Concert();
                    dataitem.Holding_City = damai["cityname"].ToString();
                    dataitem.AttrName = damai["nameNoHtml"].ToString();
                    string time = damai["showtime"].ToString();
                    string[] timeArry = time.Split('-');
                    if (timeArry.Count() > 1)
                    {
                        dataitem.Start_Time = timeArry[0];
                        dataitem.End_Time = timeArry[1];
                    }
                    else dataitem.Start_Time = dataitem.End_Time = time;
                    dataitem.Categoryname = damai["categoryname"].ToString();
                    listdata.Add(dataitem);
                }
            }
            else if (webName == "yongle")
            {
                foreach (var damai in jobject["products"])
                {
                    Concert dataitem = new Concert();
                    dataitem.Holding_City = damai["cityname"].ToString();
                    dataitem.AttrName = damai["name"].ToString();
                    dataitem.Start_Time = damai["begindate"].ToString();
                    dataitem.End_Time = damai["enddate"].ToString();
                    dataitem.Categoryname = damai["typeaname"].ToString();
                    listdata.Add(dataitem);
                }
            }
            if (listdata.Count == 0) return null;
            else ConcertToData(listdata);
            return listdata;
        }

        private  void ConcertToData(List<Concert> list)
        {
            List<BasicAttr> listbasic = new List<BasicAttr>();
            foreach (var item in list)
            {
                BasicAttr basic = new BasicAttr();
                basic.AttrName = item.AttrName;
                basic.End_Time = item.End_Time;
                basic.Holding_City = item.Holding_City;
                basic.Start_Time = item.Start_Time;
                
                basic.Is_Concert = false;
                basic.Is_Exhibition = false;
                basic.Is_Meeting = false;
                basic.Is_Localholiday = false;
                basic.Is_Sportevent = false;

                switch (item.Categoryname)
                {
                    case "演唱会":
                        basic.Is_Concert = true;
                        break;
                    case "体育比赛":
                        basic.Is_Sportevent = true;
                        break;
                    case "展会":
                        basic.Is_Exhibition = true;
                        break;
                    case "会议":
                        basic.Is_Meeting = true;
                        break;
                    default:
                        basic.Is_Localholiday = true;
                        break;
                }

                basic.Is_Influence_Adult = true;
                basic.Is_Influence_City = true;
                basic.Is_Influence_Generalpublic = true;
                basic.Event_Fever = 3;
                basic.Event_History = 1;
                basic.Freq = 2;

                basic.Is_c_Government = false;
                
                basic.Is_Homecivil_Association = false;
                basic.Is_Hometrade_Association = false;
                basic.Is_Influence_Business = false;
                basic.Is_Influence_Children = false;
                basic.Is_Influence_Intercontinental = false;
                basic.Is_Influence_Old = false;
                basic.Is_Influence_Province = false;
                basic.Is_Influence_Wholecountry = false;
                basic.Is_Influence_Worldwide = false;
                basic.Is_Influence_Young = false;
                basic.Is_Intercivil_Association = false;
                basic.Is_International = false;
                basic.Is_Intertrade_Association= false;
                
                basic.Is_l_Government = false;
                
                basic.Is_p_Government = false;
                
                basic.Participant_Population = false;

                listbasic.Add(basic);
            }
            db.BasicAttr.AddRange(listbasic);
            db.SaveChanges();
        }
    }
}