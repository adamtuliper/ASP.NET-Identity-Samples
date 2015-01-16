using System.Web;
using System.Web.Mvc;

namespace Local_Auth_v2_Raven_DB
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
