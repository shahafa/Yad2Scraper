using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace Yad2Scraper
{
    class Ad
    {
        public ObjectId Id { get; set; }
        public string RecordID { get; set; }
        public string Type { get; set; }
        public string Address { get; set; }
        public string Price { get; set; }
        public string URL { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsRelevant { get; set; }
        public string Comment { get; set; }
        public int DaysInBoard { get; set; }

        [BsonDateTimeOptions(DateOnly = true)]
        public DateTime Date { get; set; }

        [BsonDateTimeOptions(DateOnly = true)]
        public DateTime LastSeen { get; set; }

        public Ad() { }

        public Ad(JToken adJObject)
        {
            RecordID = adJObject["RecordID"].ToString();
            Date = DateTime.Parse(adJObject["Line4"].ToString());
            LastSeen = DateTime.Today;
            DaysInBoard = LastSeen.Subtract(Date).Days;
            Address = adJObject["Line1"].ToString();
            Price = adJObject["Line3"].ToString();
            URL = adJObject["URL"].ToString();
            IsRelevant = true;
            Comment = string.Empty;
            Type = string.Empty;
            try
            {
                Latitude = (double)adJObject["latitude"];
                Longitude = (double)adJObject["longitude"];
            }
            catch (Exception)
            {
                Latitude = 0.0;
                Longitude = 0.0;
            }
        }
    }
}
