using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Placements.io
{
    class Program
    {
        /// <summary>
        /// The main method.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Console.WriteLine("Getting Google accounts...");
            IAccountService googService = new GoogleDFPCompanyService();
            var googAccts = googService.GetAccounts();
            Console.WriteLine(googAccts.Count + " Google accounts found.");

            Console.WriteLine("Getting Salesforce accounts...");
            IAccountService salesforceService = new SalesforceAccountService();
            var salesforceAccts = salesforceService.GetAccounts();
            Console.WriteLine(salesforceAccts.Count + " Salesforce accounts found.");

            using (var acctContext = new AccountContext())
            {
                var accountMerger = new AccountMerger();
                var stats = accountMerger.MergeAccountLists(salesforceAccts, googAccts, ResolveMerge);

                acctContext.Accounts.AddRange(accountMerger.MergedAccountList);

                Console.WriteLine("Total number of distinct accounts found: " + stats.Total);
                Console.WriteLine("Number accounts auto-merged: " + stats.AutoMerged);
                Console.WriteLine("Number of accounts manually merged: " + stats.ManuallyMerged);

                try
                {
                    acctContext.SaveChanges();
                }
                catch (DbEntityValidationException e)
                {
                    foreach (var eve in e.EntityValidationErrors)
                    {
                        Console.WriteLine("Entry for company \"{0}\" has formatting errors.",
                            ((PlacementsAccount)eve.Entry.Entity).Name);
                        foreach (var ve in eve.ValidationErrors)
                        {
                            Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                                ve.PropertyName, ve.ErrorMessage);
                        }
                    }
                    throw;
                }           
            }   
        }

        public static AccountMerger.MergeResult ResolveMerge(PlacementsAccount acct1, PlacementsAccount acct2)
        {
            Console.Clear();
            Console.WriteLine("Press y to merge, p1 if account 1 is parent and p2 if account 2 is parent or any other key to keep accounts separate.");
            Console.WriteLine("Account 1:");
            Console.WriteLine(acct1.ToString());
            Console.WriteLine();
            Console.WriteLine("Account 2:");
            Console.WriteLine(acct2.ToString());
            var input = Console.ReadLine();
            Console.Clear();
            if (input == "y")
            {
                return AccountMerger.MergeResult.Merge;
            }
            else if (input == "p1")
            {
                return AccountMerger.MergeResult.LeftIsParentOfRight;
            }
            else if (input == "p2")
            {
                return AccountMerger.MergeResult.RightIsParentOfLeft;
            }
            else
            {
                return AccountMerger.MergeResult.NoMatch;
            }
        }
    }
}
