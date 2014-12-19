﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Salesforce.Common;
using Salesforce.Force;
using System.Threading.Tasks;
using System.Dynamic;


namespace Placements.io
{
    public class SalesforceAccountService : IAccountService
    {
        private static readonly string SecurityToken = ConfigurationManager.AppSettings["SecurityToken"];
        private static readonly string ConsumerKey = ConfigurationManager.AppSettings["ConsumerKey"];
        private static readonly string ConsumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"];
        private static readonly string Username = ConfigurationManager.AppSettings["Username"];
        private static readonly string Password = ConfigurationManager.AppSettings["Password"] + SecurityToken;
        private static readonly string IsSandboxUser = ConfigurationManager.AppSettings["IsSandboxUser"];

        private const string ACCOUNT_QUERY = "SELECT ID, Name, Phone, Industry, BillingStreet, BillingCity, BillingState, BillingPostalCode, BillingCountry, Fax, Description, Website, OwnerId, ParentId  FROM Account";

        public List<PlacementsAccount> GetAccounts()
        {
            var auth = GetSalesforceAuthenticationClient().Result;
            var accts = GetSalesforceAccounts(auth).Result;

            var placementAccts = new List<PlacementsAccount>();

            foreach (var acct in accts)
            {
                placementAccts.Add(ConvertSalesForceAccountToPlacementsAccount(acct));
            }

            // Maintain Salesforces parents but convert parent id to the corresponding PlacementsId
            foreach (var childAcct in accts.Where(a => a.ParentId != null))
            {
                placementAccts.First(a => a.SFId == childAcct.Id).ParentId = placementAccts.First(a => a.SFId == childAcct.ParentId).PlacementsId;
            }

            return placementAccts;
        }

        public static async Task<List<Account>> GetSalesforceAccounts(AuthenticationClient auth)
        {
            var client = new ForceClient(auth.InstanceUrl, auth.AccessToken, auth.ApiVersion);

            // retrieve all accounts
            var accts = new List<Account>();
            var results = await client.QueryAsync<Account>(ACCOUNT_QUERY);
            var totalSize = results.totalSize;

            accts.AddRange(results.records);
            var nextRecordsUrl = results.nextRecordsUrl;

            if (!string.IsNullOrEmpty(nextRecordsUrl))
            {
                while (true)
                {
                    var continuationResults = await client.QueryContinuationAsync<Account>(nextRecordsUrl);
                    totalSize = continuationResults.totalSize;

                    accts.AddRange(continuationResults.records);
                    if (string.IsNullOrEmpty(continuationResults.nextRecordsUrl)) break;

                    //pass nextRecordsUrl back to client.QueryAsync to request next set of records
                    nextRecordsUrl = continuationResults.nextRecordsUrl;
                }
            }

            return accts;
        }
        private static async Task<AuthenticationClient> GetSalesforceAuthenticationClient()
        {
            var auth = new AuthenticationClient();

            // Authenticate with Salesforce
            var url = IsSandboxUser.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                ? "https://test.salesforce.com/services/oauth2/token"
                : "https://login.salesforce.com/services/oauth2/token";

            await auth.UsernamePasswordAsync(ConsumerKey, ConsumerSecret, Username, Password, url);
            return auth; 
        }

        private static PlacementsAccount ConvertSalesForceAccountToPlacementsAccount(Account salesforceAcct)
        {
            var acct = new PlacementsAccount();

            acct.PlacementsId = Guid.NewGuid();
            acct.Name = salesforceAcct.Name;
            acct.Phone = salesforceAcct.Phone;
            acct.FaxPhone = salesforceAcct.Fax;
            acct.Description = salesforceAcct.Description;
            acct.Address1 = salesforceAcct.BillingStreet;
            acct.City = salesforceAcct.BillingCity;
            acct.Zip = salesforceAcct.BillingPostalCode;
            acct.State = salesforceAcct.BillingState;
            acct.Country = salesforceAcct.BillingCountry;
            acct.Category = salesforceAcct.Industry;
            acct.Website = salesforceAcct.Website;
            acct.UserId = salesforceAcct.OwnerID;
            acct.SFId = salesforceAcct.Id;

            return acct;
        }
    }

    public class Account
    {
        public const String SObjectTypeName = "Account";

        public string Id { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public string Industry { get; set; }

        public string BillingStreet { get; set; }

        public string BillingStreet2 { get; set; }

        public string BillingCity { get; set; }

        public string BillingState { get; set; }

        public string BillingPostalCode { get; set; }

        public string BillingCountry { get; set; }

        public string Fax { get; set; }

        public string Description { get; set; }

        public string PlacementsId { get; set; }

        public string Website { get; set; }

        public string OwnerID { get; set; }

        public string ParentId { get; set; }
    }
}
