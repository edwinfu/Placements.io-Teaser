using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Api.Ads.Dfp.Lib;
using Google.Api.Ads.Dfp.Util.v201411;
using Google.Api.Ads.Dfp.v201411;

namespace Placements.io
{
    public class GoogleDFPCompanyService : IAccountService
    {
        public List<PlacementsAccount> GetAccounts()
        {
            var accts = new List<PlacementsAccount>();

            DfpUser user = new DfpUser();

            CompanyService companyService =
          (CompanyService)user.GetService(DfpService.v201411.CompanyService);

            // Set defaults for page and statement.
            CompanyPage page = new CompanyPage();
            StatementBuilder statementBuilder = new StatementBuilder()
                .OrderBy("id ASC")
                .Limit(StatementBuilder.SUGGESTED_PAGE_LIMIT);

            try
            {
                do
                {
                    // Get companies by statement.
                    page = companyService.getCompaniesByStatement(statementBuilder.ToStatement());

                    if (page.results != null && page.results.Length > 0)
                    {
                        int i = page.startIndex;
                        foreach (Company company in page.results)
                        {
                            accts.Add(ConvertGoogleCompanyToPlacementsAccount(company));
                            i++;
                        }
                    }
                    statementBuilder.IncreaseOffsetBy(StatementBuilder.SUGGESTED_PAGE_LIMIT);
                } while (statementBuilder.GetOffset() < page.totalResultSetSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to get companies. Exception says \"{0}\"", ex.Message);
            }

            return accts;
        }

        private static PlacementsAccount ConvertGoogleCompanyToPlacementsAccount(Company googCompany)
        {
            var acct = new PlacementsAccount();

            acct.PlacementsId = Guid.NewGuid();
            acct.Name = googCompany.name;
            acct.Phone = googCompany.primaryPhone;
            acct.Email = googCompany.email;
            acct.FaxPhone = googCompany.faxPhone;
            acct.Description = googCompany.comment;
            acct.Address1 = googCompany.address;
            acct.CreditStatus = googCompany.creditStatus.ToString();
            acct.DFPId = googCompany.id;
           
            return acct;
        }
    }
}
