using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Placements.io
{
    public class AccountMerger
    {
        public enum MergeResult  {
                                      NoMatch,
                                      Merge,
                                      LeftIsParentOfRight,
                                      RightIsParentOfLeft
                                  }
        /// <summary>
        /// Common words to ignore when trying to match on Account name
        /// </summary>
        private HashSet<string> ignored = new HashSet<string>{
                                      "inc", 
                                      "the",
                                      "dev" /* this one might be cheating bit with the given dataset :) */,
                                      "of",
                                      "and",
                                      ""
        };

        public List<PlacementsAccount> MergedAccountList { get; private set; }

        /// <summary>
        /// Merge 2 given lists of Accounts into one list using some matching heuristics on some relevant fields like name, email, phone
        /// 
        /// Basic algorithm:
        /// For every account in one list 
        ///     Compare it to every account in the other list until there is a probable match.
        ///         If it's a strong match then auto merge.
        ///         If it's a weak match then request user input.
        ///         Remove the account from the second list
        ///     If no match then add the account from first list as is
        /// Add all remaining accounts from second list
        ///    
        ///
        /// </summary>
        /// <param name="salesForceAccounts"></param>
        /// <param name="googleAccounts"></param>
        /// <param name="mergeCheck"></param>
        /// <returns></returns>
        public AccountMergeStats MergeAccountLists(List<PlacementsAccount> salesForceAccounts, List<PlacementsAccount> googleAccounts, Func<PlacementsAccount, PlacementsAccount, MergeResult> mergeCheck = null)
        {
            var stats = new AccountMergeStats();
            MergedAccountList = new List<PlacementsAccount>();

            foreach (PlacementsAccount sfAcc in salesForceAccounts)
            {
                PlacementsAccount matchedAccountToRemove = null;

                foreach (PlacementsAccount googAcc in googleAccounts)
                {
                    var confidence = MatchConfidence(sfAcc, googAcc);

                    if (confidence >= 50)
                    {
                        // automerge
                        MergedAccountList.Add(SanitizeAccount(AutoMerge(sfAcc, googAcc)));
                        stats.Total++;
                        stats.AutoMerged++;
                        matchedAccountToRemove = googAcc;
                        break;
                    }
                    else if (confidence >= 20)
                    {
                        // request user input
                        if (mergeCheck != null)
                        {
                            var choice = mergeCheck(sfAcc, googAcc);

                            // If the choice is null then there is no match and we proceed without merge, otherwise use the chosen account.
                            if (choice == MergeResult.Merge)
                            {
                                MergedAccountList.Add(SanitizeAccount(AutoMerge(sfAcc, googAcc)));
                                stats.Total++;
                                stats.ManuallyMerged++;
                                matchedAccountToRemove = googAcc;
                                break;
                            }
                            else if (choice == MergeResult.LeftIsParentOfRight)
                            {
                                if (googAcc.ParentId == Guid.Empty)
                                {
                                    googAcc.ParentId = sfAcc.PlacementsId;
                                }
                            }
                            else if (choice == MergeResult.RightIsParentOfLeft)
                            {
                                if (sfAcc.ParentId == Guid.Empty)
                                {
                                    sfAcc.ParentId = googAcc.PlacementsId;
                                }
                            }
                        }
                    }
                }

                if (matchedAccountToRemove == null)
                {
                    // No matching google account - add the salesforce account
                    MergedAccountList.Add(SanitizeAccount(sfAcc));
                    stats.Total++;
                }
                else
                {
                    googleAccounts.Remove(matchedAccountToRemove);
                }
            }

            foreach (PlacementsAccount googAcc in googleAccounts)
            {
                // Add all remaining, unmatched google accounts
                MergedAccountList.Add(SanitizeAccount(googAcc));
                stats.Total++;
            }

            return stats;
        }

        /// <summary>
        /// Basic matching algorithm that builds a confidence ranking based on matching properties, for now focusing on Name, Phone, and Email
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns></returns>
        private int MatchConfidence(PlacementsAccount item1, PlacementsAccount item2)
        {
            var confidence = 0;

            if (item1.Email != null && item2.Email != null && item1.Email.ToLower() == item2.Email.ToLower())
            {
                confidence += 20;
            }

            if (item1.Phone != null && item2.Phone != null && item1.Phone == item2.Phone)
            {
                confidence += 20;
            }

            if (item1.Name != null && item2.Name != null && item1.Name.ToLower() == item2.Name.ToLower())
            {
                confidence += 50;
            }
            else if (item1.Name != null && item2.Name != null)
            {
                // Grab only the alphanumeric chars to filter out punctuation or any weird chars that could vary.
                // The method used here should perform better than a reg expression.
                var item1NameSanitized = new string(item1.Name.ToLower().Where(c => (char.IsLetterOrDigit(c) ||
                                                                           char.IsWhiteSpace(c))).ToArray());

                var item2NameSanitized = new string(item2.Name.ToLower().Where(c => (char.IsLetterOrDigit(c) ||
                                                                           char.IsWhiteSpace(c))).ToArray());

                // Breakdown the name into individual words and look for ones in common
                var item1NameTokens = new HashSet<string>(item1NameSanitized.ToLower().Split(' '));
                item1NameTokens.RemoveWhere(w => (ignored.Contains(w)));

                var item2NameTokens = new HashSet<string>(item2NameSanitized.ToLower().Split(' '));
                item2NameTokens.RemoveWhere(w => (ignored.Contains(w)));

                var matchedWords = item1NameTokens.Intersect(item2NameTokens).Count();
                var totalWords = item1NameTokens.Count < item2NameTokens.Count ? item1NameTokens.Count : item2NameTokens.Count;

                if (matchedWords == totalWords)
                {
                    confidence += 50;
                }
                else if ((double)matchedWords/totalWords > 0.5)
                {
                    confidence += 30;
                }
                else if (matchedWords >= 2)
                {
                    confidence += 20;
                }
            }

            return confidence;
        }

        private PlacementsAccount AutoMerge(PlacementsAccount acct1, PlacementsAccount acct2)
        {
            var mergedAccount = new PlacementsAccount();

            mergedAccount.PlacementsId = Guid.NewGuid();
            mergedAccount.Name = String.IsNullOrWhiteSpace(acct1.Name) ? acct2.Name : acct1.Name;

            mergedAccount.Phone = String.IsNullOrWhiteSpace(acct1.Phone) ? acct2.Phone : acct1.Phone;

            mergedAccount.Email = String.IsNullOrWhiteSpace(acct1.Email) ? acct2.Email : acct1.Email;
            mergedAccount.Category = String.IsNullOrWhiteSpace(acct1.Category) ? acct2.Category : acct1.Category;
            mergedAccount.Address1 = String.IsNullOrWhiteSpace(acct1.Address1) ? acct2.Address1 : acct1.Address1;
            mergedAccount.Address2 = String.IsNullOrWhiteSpace(acct1.Address2) ? acct2.Address2 : acct1.Address2;

            mergedAccount.City = String.IsNullOrWhiteSpace(acct1.City) ? acct2.City : acct1.City;
            mergedAccount.State = String.IsNullOrWhiteSpace(acct1.State) ? acct2.State : acct1.State;
            mergedAccount.Zip = String.IsNullOrWhiteSpace(acct1.Zip) ? acct2.Zip : acct1.Zip;
            mergedAccount.Country = String.IsNullOrWhiteSpace(acct1.Country) ? acct2.Country : acct1.Country;

            mergedAccount.FaxPhone = String.IsNullOrWhiteSpace(acct1.FaxPhone) ? acct2.FaxPhone : acct1.FaxPhone;
            mergedAccount.Description = String.IsNullOrWhiteSpace(acct1.Description) ? acct2.Description : acct1.Description;
            mergedAccount.CreditStatus = String.IsNullOrWhiteSpace(acct1.CreditStatus) ? acct2.CreditStatus : acct1.CreditStatus;
            mergedAccount.DFPId = acct1.DFPId == 0 ? acct2.DFPId : acct1.DFPId;
            mergedAccount.SFId = String.IsNullOrWhiteSpace(acct1.SFId) ? acct2.SFId : acct1.SFId;

            mergedAccount.Website = String.IsNullOrWhiteSpace(acct1.Website) ? acct2.Website : acct1.Website;
            mergedAccount.UserId = String.IsNullOrWhiteSpace(acct1.UserId) ? acct2.UserId : acct1.UserId;
            mergedAccount.ParentId = acct1.ParentId == Guid.Empty ? acct2.ParentId : acct1.ParentId;

            return mergedAccount;
        }

        /// <summary>
        /// The beginnings of some basic sanitization of the data.  Ideally this would be made more robust.
        /// </summary>
        /// <param name="acct"></param>
        /// <returns></returns>
        private PlacementsAccount SanitizeAccount(PlacementsAccount acct)
        {
            if (!String.IsNullOrWhiteSpace(acct.Website) && !acct.Website.ToLower().StartsWith("http://"))
            {
                acct.Website = "http://" + acct.Website;
            }

            return acct;
        }
    }

    public class AccountMergeStats
    {
        public int AutoMerged;
        public int ManuallyMerged;
        public int Total;
    }
}
