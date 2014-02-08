using System;
using System.Globalization;
using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Linq;

namespace Yad2Scraper
{
    public class Program
    {
        private const string CollectionName = "tester";

        private static MongoCollection<BsonDocument> _yad2ScraperCollection;
        private const string DBConnectionString = "mongodb://shahaf:shahaf@troup.mongohq.com:10053/Yad2Scraper";
        private static log4net.ILog _logger;

        private const string BaseUrl = "http://m.yad2.co.il";
        private static readonly string[] ResourceList =
        {
            "/API/MadorResults.php?CatID=2&SubCatID=2&CityID=1800&NeighborhoodID=1316&HomeTypeID=1&FromRooms=3&ToPrice=6200&FromFloor=1&ToFloor=1&AppType=Iphone&Page={0}",     // old north first floor
            "/API/MadorResults.php?CatID=2&SubCatID=2&CityID=1800&NeighborhoodID=1316&HomeTypeID=1&FromRooms=3&ToPrice=6200&FromFloor=1&Elevator=1&AppType=Iphone&Page={0}",    // old north with elevator
            "/API/MadorResults.php?CatID=2&SubCatID=2&CityID=1800&NeighborhoodID=1410&HomeTypeID=1&FromRooms=3&ToPrice=6200&FromFloor=1&ToFloor=1&AppType=Iphone&Page={0}",     // Kikar HaMedina first floor
            "/API/MadorResults.php?CatID=2&SubCatID=2&CityID=1800&NeighborhoodID=1410&HomeTypeID=1&FromRooms=3&ToPrice=6200&FromFloor=1&Elevator=1&AppType=Iphone&Page={0}",    // Kikar HaMedina with elevator
            "/API/MadorResults.php?CatID=2&SubCatID=2&CityID=1800&NeighborhoodID=158&HomeTypeID=1&FromRooms=3&ToPrice=6200&FromFloor=1&AppType=Iphone&Page={0}"                 // Bavli
        };


        public static void Main()
        {
            InitializeLogger();

            _logger.Debug("Yad2Scraper process started");

            _yad2ScraperCollection = InitaiteDataBaseConnection();
            SearchNewAds();

            _logger.Debug("Yad2Scraper process ended");
        }


        private static void InitializeLogger()
        {
            log4net.Config.BasicConfigurator.Configure();
            log4net.NDC.Push(string.Empty);
            _logger = log4net.LogManager.GetLogger(typeof (Program));
        }


        private static MongoCollection<BsonDocument> InitaiteDataBaseConnection()
        {
            _logger.Debug("-->Yad2Scraper::InitaiteDataBaseConnection");

            var mongoClient = new MongoClient(DBConnectionString);
            var server = mongoClient.GetServer();
            var database = server.GetDatabase("Yad2Scraper");

            if (!database.CollectionExists(CollectionName))
                database.CreateCollection(CollectionName);

            var collection = database.GetCollection(CollectionName);

            _logger.Debug("<--Yad2Scraper::InitaiteDataBaseConnection");
            return collection;
        }


        private static void SearchNewAds()
        {
            _logger.Debug("-->Yad2Scraper::SearchNewAds");

            foreach (var resource in ResourceList)
            {
                SearchNewAds(resource);
            }

            _logger.Debug("<--Yad2Scraper::SearchNewAds");
        }


        private static void SearchNewAds(string resource)
        {
            var client = new RestClient(BaseUrl);
            var page = 0;
            int privateTotalRecords;
            int tradeTotalRecords;
            do
            {
                page++;

                var request = new RestRequest(string.Format(resource, page), Method.GET);
                var response = client.Execute(request);
                if (response.StatusCode != HttpStatusCode.OK || string.IsNullOrEmpty(response.Content)) break;

                var jObject = JObject.Parse(response.Content);

                privateTotalRecords = AddNewAdsToDB(jObject["Private"], true);
                tradeTotalRecords = AddNewAdsToDB(jObject["Trade"], false);

            } while (privateTotalRecords > 0 || tradeTotalRecords > 0);
        }


        private static int AddNewAdsToDB(JToken adsList, bool isPrivateAds)
        {
            if (adsList == null || adsList["Results"] == null || string.IsNullOrEmpty(adsList["Results"].ToString()))
                return 0;

            // Selects all ads from today and yestarday
            var yestardayDate = DateTime.Today.AddDays(-1);
            var ads = from ad in adsList["Results"]
                      where ad["Line4"] != null && DateTime.Parse(ad["Line4"].ToString(), new CultureInfo("he-IL")) >= yestardayDate
                      select ad;

            foreach (var ad in ads)
            {
                var recordID = ad["RecordID"];
                if (recordID == null) continue;

                var adDocument = FindAd(recordID.ToString());
                if (adDocument == null)
                {
                    adDocument = new Ad(ad)
                    {
                        Type = isPrivateAds ? "פרטי" : "תיווך"
                    };

                    _yad2ScraperCollection.Insert(adDocument);
                }
                else
                {
                    adDocument.LastSeen = DateTime.Today;
                    adDocument.DaysInBoard = adDocument.LastSeen.Subtract(adDocument.Date).Days;
                    
                    // todo look for price change

                    _yad2ScraperCollection.Save(adDocument);
                }
            }

            return ads.Count();
        }

        private static Ad FindAd(string recordID)
        {
            var query = Query<Ad>.EQ(e => e.RecordID, recordID);
            var adDocument = _yad2ScraperCollection.FindOneAs<Ad>(query);

            return adDocument;
        }

        
        //private static void ConvertOldDBToNewDB()
        //{
        //    var mongoClient = new MongoClient(DBConnectionString);
        //    var server = mongoClient.GetServer();
        //    var database = server.GetDatabase("Yad2Scraper");

        //    var oldCollection = database.GetCollection("Yad2Scraper");
        //    var newCollection = database.GetCollection(CollectionName);

        //    newCollection.RemoveAll();

        //    var oldAdsCollection = oldCollection.FindAll();
        //    foreach (var oldAd in oldAdsCollection)
        //    {
        //        var newAd = new Ad
        //        {
        //            RecordID = oldAd["RecordID"].ToString(),
        //            Date = DateTime.Parse(oldAd["Line4"].ToString()),
        //            LastSeen = DateTime.Parse(oldAd["Line4"].ToString()),
        //            DaysInBoard = 0,
        //            Address = oldAd["Line1"].ToString(),
        //            Price = oldAd["Line3"].ToString(),
        //            URL = oldAd["URL"].ToString(),
        //            IsRelevant = false,
        //            Comment = string.Empty,
        //            Type = oldAd["RecordID"].ToString().Length < 8 ? "תיווך" : "פרטי"
        //        };

        //        try
        //        {
        //            newAd.Latitude = (double)oldAd["latitude"];
        //            newAd.Longitude = (double)oldAd["longitude"];
        //        }
        //        catch
        //        {
        //            // do nothing
        //        }

        //        newCollection.Insert(newAd);
        //    }
        //}
    }
}
