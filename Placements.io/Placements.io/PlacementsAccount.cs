using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Placements.io
{
    public class AccountContext : DbContext
    {
        public DbSet<PlacementsAccount> Accounts { get; set; }     
    }
    public class PlacementsAccount
    {
        [Key]
        public Guid PlacementsId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Category { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Country { get; set; }
        public string FaxPhone { get; set; }
        public string Description { get; set; }
        public string CreditStatus { get; set; }
        public long DFPId { get; set; }
        public string SFId { get; set; }
        public string Website { get; set; }
        public string UserId { get; set; }
        public Guid ParentId { get; set; }

        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var prop in this.GetType().GetProperties())
            {
                sb.Append(prop.Name);
                sb.Append(" ");
                if (prop.GetValue(this) != null)
                {
                    sb.Append(prop.GetValue(this).ToString());
                }
                sb.Append("\n");
            }

            return sb.ToString();
        }
    }
}
