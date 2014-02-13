using System;
using System.Globalization;
using System.Linq;
using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Quartz;
using Yad2Scraper;
using MongoDB.Driver.Builders;
using RestSharp;

namespace AppHarbor
{
    class Yad2ScraperJob : IJob
    {
        private const string CollectionName = "Yad2ScraperDev";

        private MongoCollection<BsonDocument> _yad2ScraperCollection;
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


        public void Execute(IJobExecutionContext context)
        {
            InitializeLogger();

            _logger.Debug("Yad2Scraper process started");

            _yad2ScraperCollection = InitaiteDataBaseConnection();
            SearchNewAds();

            _logger.Debug("Yad2Scraper process ended");
        }


        public static void InitializeLogger()
        {
            log4net.Config.BasicConfigurator.Configure();
            log4net.NDC.Push(string.Empty);
            _logger = log4net.LogManager.GetLogger(typeof(Yad2ScraperJob));
        }


        private MongoCollection<BsonDocument> InitaiteDataBaseConnection()
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


        private void SearchNewAds()
        {
            _logger.Debug("-->Yad2Scraper::SearchNewAds");

            foreach (var resource in ResourceList)
            {
                SearchNewAds(resource);
            }

            _logger.Debug("<--Yad2Scraper::SearchNewAds");
        }


        private void SearchNewAds(string resource)
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


        private int AddNewAdsToDB(JToken adsList, bool isPrivateAds)
        {
            if (adsList == null || adsList["Results"] == null || string.IsNullOrEmpty(adsList["Results"].ToString()))
                return 0;

            // Selects all ads from today and yestarday
            var yestardayDate = DateTime.Today.AddDays(-1);
            var ads = from ad in adsList["Results"]
                      where ad["Line4"] != null && DateTime.ParseExact(ad["Line4"].ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture) >= yestardayDate
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

        private Ad FindAd(string recordID)
        {
            var query = Query<Ad>.EQ(e => e.RecordID, recordID);
            var adDocument = _yad2ScraperCollection.FindOneAs<Ad>(query);

            return adDocument;
        }
    }
}